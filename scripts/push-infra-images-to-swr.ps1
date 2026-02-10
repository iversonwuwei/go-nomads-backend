# 上传基础设施镜像到华为云 SWR 仓库

[CmdletBinding()]
param(
    [switch]$Help,
    [switch]$Login,
    [switch]$List,
    [switch]$All,
    [string]$Images,
    [Parameter(ValueFromRemainingArguments)]
    [string[]]$RemainingArgs
)

$ErrorActionPreference = "Stop"

# 配置
$SWR_REGISTRY     = $(if ($env:SWR_REGISTRY)     { $env:SWR_REGISTRY }     else { "swr.ap-southeast-3.myhuaweicloud.com" })
$SWR_ORGANIZATION = $(if ($env:SWR_ORGANIZATION)  { $env:SWR_ORGANIZATION }  else { "go-nomads" })
$MIRROR_REGISTRY  = $(if ($env:MIRROR_REGISTRY)   { $env:MIRROR_REGISTRY }   else { "" })
$MIRROR_MODE      = $(if ($env:MIRROR_MODE)       { $env:MIRROR_MODE }       else { "registry" })   # registry | daemon
$MIRROR_STRICT    = $(if ($env:MIRROR_STRICT)     { $env:MIRROR_STRICT }     else { "0" })           # 1=失败即退出，0=失败回退源镜像
$PUSH_RETRIES     = $(if ($env:PUSH_RETRIES)      { [int]$env:PUSH_RETRIES } else { 3 })

# 基础设施镜像列表: 源镜像|源标签|目标名称|目标标签
$INFRA_IMAGES = @(
    @{ SrcImage = "redis";                                              SrcTag = "latest";                DestName = "redis";           DestTag = "latest" }
    @{ SrcImage = "rabbitmq";                                           SrcTag = "3-management-alpine";   DestName = "rabbitmq";        DestTag = "3-management-alpine" }
    @{ SrcImage = "docker.elastic.co/elasticsearch/elasticsearch";      SrcTag = "8.16.1";               DestName = "elasticsearch";   DestTag = "8.16.1" }
)

function Show-Help {
    Write-Host @"
用法: push-infra-images-to-swr.ps1 [选项]
  -Help   / --help / -h      显示帮助
  -Login  / --login / -l     登录到 SWR
  -List   / --list           列出将要上传的镜像
  -All    / --all / -a       上传列表中全部镜像
  -Images / --images n1,n2   仅上传指定目标镜像名（DestName）

环境变量:
  SWR_REGISTRY       SWR 仓库地址 (默认: swr.ap-southeast-3.myhuaweicloud.com)
  SWR_ORGANIZATION   SWR 组织名称 (默认: go-nomads)
  MIRROR_REGISTRY    镜像加速地址 (可选)
  MIRROR_MODE        registry(默认) 或 daemon
  MIRROR_STRICT      1 失败即退出；0 失败回退源镜像(默认)
  PUSH_RETRIES       push 失败重试次数 (默认: 3)
"@
}

function Show-ImageList {
    Write-Host "将要上传的基础设施镜像列表:"
    Write-Host "============================="
    foreach ($img in $INFRA_IMAGES) {
        $src  = "$($img.SrcImage):$($img.SrcTag)"
        $dest = "$SWR_REGISTRY/$SWR_ORGANIZATION/$($img.DestName):$($img.DestTag)"
        Write-Host "  $src -> $dest"
    }
}

function Resolve-PullImage {
    param([string]$Image)

    if ([string]::IsNullOrEmpty($MIRROR_REGISTRY)) { return $Image }
    if ($MIRROR_MODE -eq "daemon")                  { return $Image }

    $mirror = $MIRROR_REGISTRY.TrimEnd('/')
    $mirror = $mirror -replace '^https?://', ''

    # 如果 image 含显式 registry（包含 . 或 : 的首段），直接前置 mirror
    if ($Image -match '^[^/]*[.:][^/]*/') {
        return "$mirror/$Image"
    }

    # Docker Hub library 镜像
    return "$mirror/library/$Image"
}

function Build-PullCandidates {
    param(
        [string]$SrcImage,
        [string]$SrcTag
    )
    $candidates = [System.Collections.Generic.List[string]]::new()

    # 1) 自定义 MIRROR_REGISTRY（仅 registry 改写模式）
    if (-not [string]::IsNullOrEmpty($MIRROR_REGISTRY) -and $MIRROR_MODE -eq "registry") {
        $mirrored = Resolve-PullImage "${SrcImage}:${SrcTag}"
        if ($mirrored -and -not $candidates.Contains($mirrored)) {
            $candidates.Add($mirrored)
        }
    }

    # 2) 原始源镜像
    $original = "${SrcImage}:${SrcTag}"
    if (-not $candidates.Contains($original)) {
        $candidates.Add($original)
    }

    # 3) 阿里云公共库（仅对无 registry 的镜像适用）
    if ($SrcImage -notmatch '^[^/]*[.:][^/]*/') {
        $aliyun = "registry.cn-hangzhou.aliyuncs.com/library/${SrcImage}:${SrcTag}"
        if (-not $candidates.Contains($aliyun)) {
            $candidates.Add($aliyun)
        }
    }

    return $candidates
}

function Login-SWR {
    Write-Host "请手动登录到华为云 SWR: $SWR_REGISTRY"
    Write-Host "示例: docker login -u [区域项目名]@[AK] -p [临时密码] $SWR_REGISTRY"
}

function Push-AllImages {
    param([string]$ImageFilter = "")

    Write-Host "================================================"
    Write-Host "开始上传基础设施镜像到 SWR"
    Write-Host "================================================"

    $filterArr = @()
    if (-not [string]::IsNullOrEmpty($ImageFilter)) {
        $filterArr = $ImageFilter -split ','
    }

    foreach ($img in $INFRA_IMAGES) {
        $srcImage  = $img.SrcImage
        $srcTag    = $img.SrcTag
        $destName  = $img.DestName
        $destTag   = $img.DestTag

        # 过滤
        if ($filterArr.Count -gt 0) {
            if ($destName -notin $filterArr) { continue }
        }

        $src  = "${srcImage}:${srcTag}"
        $dest = "$SWR_REGISTRY/$SWR_ORGANIZATION/${destName}:${destTag}"

        Write-Host ""
        Write-Host "处理镜像: $src -> $dest"
        Write-Host "----------------------------------------"

        # 构建拉取候选列表
        $candidates = Build-PullCandidates -SrcImage $srcImage -SrcTag $srcTag

        $pulled  = $false
        $pullSrc = ""
        $firstCandidate = $true

        foreach ($candidate in $candidates) {
            Write-Host "拉取源镜像: $candidate (linux/amd64)"
            docker pull --platform linux/amd64 $candidate 2>&1 | Write-Host
            if ($LASTEXITCODE -eq 0) {
                $pullSrc = $candidate
                $pulled  = $true
                break
            }
            if ($MIRROR_STRICT -eq "1" -and $firstCandidate) {
                Write-Error "镜像拉取失败且 MIRROR_STRICT=1，退出"
                exit 1
            }
            $firstCandidate = $false
        }

        if (-not $pulled) {
            Write-Error "镜像拉取全部失败，退出"
            exit 1
        }

        Write-Host "打标签: $dest"
        docker tag $pullSrc $dest
        if ($LASTEXITCODE -ne 0) {
            Write-Error "打标签失败"
            exit 1
        }

        Write-Host "推送到 SWR: $dest"
        for ($attempt = 1; $attempt -le $PUSH_RETRIES; $attempt++) {
            docker push $dest 2>&1 | Write-Host
            if ($LASTEXITCODE -eq 0) { break }
            if ($attempt -eq $PUSH_RETRIES) {
                Write-Error "推送失败, 已达最大重试次数 ($PUSH_RETRIES)"
                exit 1
            }
            Write-Host "推送失败, 重试第 $($attempt + 1) 次..." -ForegroundColor Yellow
            Start-Sleep -Seconds (2 * $attempt)
        }

        Write-Host "✓ 完成: $dest" -ForegroundColor Green
    }

    Write-Host ""
    Write-Host "================================================"
    Write-Host "所有基础设施镜像上传完成!"
    Write-Host "================================================"
}

# 兼容 GNU 风格参数 (--help, --login, --list, --all, --images)
foreach ($arg in $RemainingArgs) {
    switch ($arg) {
        { $_ -in '--help','-h' }    { $Help  = [switch]::new($true) }
        { $_ -in '--login','-l' }   { $Login = [switch]::new($true) }
        '--list'                     { $List  = [switch]::new($true) }
        { $_ -in '--all','-a' }     { $All   = [switch]::new($true) }
        default {
            # --images 后面的值或 bare 值
            if (-not [string]::IsNullOrEmpty($arg) -and $arg -notlike '--*') {
                $Images = $arg
            }
        }
    }
}

# 主程序
if ($Help) {
    Show-Help
    return
}

if ($Login) {
    Login-SWR
    return
}

if ($List) {
    Show-ImageList
    return
}

if (-not [string]::IsNullOrEmpty($Images)) {
    Push-AllImages -ImageFilter $Images
    return
}

if ($All) {
    Push-AllImages
    return
}

# 默认显示帮助
Show-Help
