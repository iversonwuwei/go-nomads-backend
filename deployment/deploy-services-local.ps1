# Go-Nomads Services Deployment (Local Build + Container)
param([switch]$SkipBuild, [switch]$Help)
$ErrorActionPreference = 'Stop'
$SCRIPT_DIR = Split-Path -Parent $MyInvocation.MyCommand.Path
$ROOT_DIR = Split-Path -Parent $SCRIPT_DIR
$NETWORK_NAME = "go-nomads-network"

# ============================================================
# 基础设施连接配置（Docker 容器名称）
# ============================================================
$REDIS_HOST = "go-nomads-redis"
$REDIS_PORT = "6379"
$RABBITMQ_HOST = "go-nomads-rabbitmq"
$RABBITMQ_USER = "walden"
$RABBITMQ_PASS = "walden"
$ELASTICSEARCH_URL = "http://go-nomads-elasticsearch:9200"

# 服务发现辅助函数
function SvcUrl([string]$name) { return "http://go-nomads-${name}:8080" }

# 获取容器运行时
function Get-Runtime {
    if (Get-Command podman -EA SilentlyContinue) {
        $test = podman ps -a --filter "name=go-nomads-redis" --format '{{.Names}}' 2>$null
        if ($test -eq "go-nomads-redis") { 
            Write-Host "检测到 Podman，使用 Podman 作为容器运行时" -ForegroundColor Green
            return "podman" 
        }
    }
    if (Get-Command docker -EA SilentlyContinue) {
        $test = docker ps -a --filter "name=go-nomads-redis" --format '{{.Names}}' 2>$null
        if ($test -eq "go-nomads-redis") { 
            Write-Host "检测到 Docker，使用 Docker 作为容器运行时" -ForegroundColor Green
            return "docker" 
        }
    }
    if (Get-Command podman -EA SilentlyContinue) { 
        Write-Host "使用 Podman 作为容器运行时" -ForegroundColor Green
        return "podman" 
    }
    if (Get-Command docker -EA SilentlyContinue) { 
        Write-Host "使用 Docker 作为容器运行时" -ForegroundColor Green
        return "docker" 
    }
    Write-Error "[错误] 未检测到 Docker 或 Podman，请先安装容器运行时"
    exit 1
}

# 检查网络是否存在
function Test-NetworkExists {
    param([string]$NetworkName)
    $network = & $RUNTIME network ls --filter "name=$NetworkName" --format '{{.Name}}' 2>$null
    return $network -eq $NetworkName
}

# 确保网络存在
function Ensure-Network {
    if (-not (Test-NetworkExists -NetworkName $NETWORK_NAME)) {
        Write-Host "  创建 Docker 网络: $NETWORK_NAME" -ForegroundColor Yellow
        & $RUNTIME network create $NETWORK_NAME > $null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  网络创建成功" -ForegroundColor Green
        } else {
            Write-Host "  [错误] 网络创建失败" -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "  网络 $NETWORK_NAME 已存在" -ForegroundColor Green
    }
}

$RUNTIME = Get-Runtime
Write-Host "使用容器运行时: $RUNTIME" -ForegroundColor Cyan
Write-Host "根目录: $ROOT_DIR" -ForegroundColor Cyan
if ($SkipBuild) {
    Write-Host "构建模式: 跳过构建" -ForegroundColor Yellow
} else {
    Write-Host "构建模式: 完整构建" -ForegroundColor Cyan
}
Write-Host ""

if ($Help) {
    Write-Host "`nUsage: .\deploy-services-local.ps1 [-SkipBuild] [-Help]`n"
    exit 0
}

Write-Host "`n============================================================" -ForegroundColor Blue
Write-Host "  检查前置条件" -ForegroundColor Blue
Write-Host "============================================================`n" -ForegroundColor Blue

# 检查 .NET SDK
if (-not (Get-Command dotnet -EA SilentlyContinue)) {
    Write-Host "[错误] 未找到 .NET SDK" -ForegroundColor Red
    exit 1
}
$dotnetVersion = dotnet --version
Write-Host "  .NET SDK: $dotnetVersion" -ForegroundColor Green

# 确保网络存在
Ensure-Network

# 检查前置服务
$required = @("go-nomads-redis")
foreach ($svc in $required) {
    $running = & $RUNTIME ps --filter "name=$svc" --filter "status=running" --format '{{.Names}}' 2>$null
    if ($running -ne $svc) {
        Write-Host "  [错误] $svc 未运行" -ForegroundColor Red
        Write-Host "  请先运行基础设施部署脚本: .\deploy-infrastructure-local.ps1" -ForegroundColor Yellow
        exit 1
    }
    Write-Host "  $svc 运行正常" -ForegroundColor Green
}

# 检查 Nginx（非必须）
$nginxRunning = & $RUNTIME ps --filter "name=go-nomads-nginx" --filter "status=running" --format '{{.Names}}' 2>$null
if ($nginxRunning -ne "go-nomads-nginx") {
    Write-Host "  [提示] Nginx 未运行，可通过 deploy-infrastructure-local.ps1 部署" -ForegroundColor Yellow
} else {
    Write-Host "  Nginx 运行正常" -ForegroundColor Green
}

Write-Host "  前置条件检查完成" -ForegroundColor Green
Write-Host ""

$services = @(
    @{Name="gateway"; Port=5080; Path="src/Gateway/Gateway"; Dll="Gateway.dll"; Container="go-nomads-gateway"},
    @{Name="product-service"; Port=5002; Path="src/Services/ProductService/ProductService"; Dll="ProductService.dll"; Container="go-nomads-product-service"},
    @{Name="user-service"; Port=5001; Path="src/Services/UserService/UserService"; Dll="UserService.dll"; Container="go-nomads-user-service"},
    @{Name="document-service"; Port=5003; Path="src/Services/DocumentService/DocumentService"; Dll="DocumentService.dll"; Container="go-nomads-document-service"},
    @{Name="city-service"; Port=8002; Path="src/Services/CityService/CityService"; Dll="CityService.dll"; Container="go-nomads-city-service"},
    @{Name="event-service"; Port=8005; Path="src/Services/EventService/EventService"; Dll="EventService.dll"; Container="go-nomads-event-service"},
    @{Name="coworking-service"; Port=8006; Path="src/Services/CoworkingService/CoworkingService"; Dll="CoworkingService.dll"; Container="go-nomads-coworking-service"},
    @{Name="ai-service"; Port=8009; Path="src/Services/AIService/AIService"; Dll="AIService.dll"; Container="go-nomads-ai-service"},
    @{Name="cache-service"; Port=8010; Path="src/Services/CacheService/CacheService"; Dll="CacheService.dll"; Container="go-nomads-cache-service"},
    @{Name="message-service"; Port=5005; Path="src/Services/MessageService/MessageService/API"; Dll="MessageService.dll"; Container="go-nomads-message-service"},
    @{Name="accommodation-service"; Port=8012; Path="src/Services/AccommodationService/AccommodationService"; Dll="AccommodationService.dll"; Container="go-nomads-accommodation-service"},
    @{Name="innovation-service"; Port=8011; Path="src/Services/InnovationService/InnovationService"; Dll="InnovationService.dll"; Container="go-nomads-innovation-service"},
    @{Name="search-service"; Port=8015; Path="src/Services/SearchService/SearchService"; Dll="SearchService.dll"; Container="go-nomads-search-service"}
)

if (-not $SkipBuild) {
    Write-Host "`n============================================================" -ForegroundColor Blue
    Write-Host "  构建服务" -ForegroundColor Blue
    Write-Host "============================================================`n" -ForegroundColor Blue
    
    foreach ($svc in $services) {
        $proj = Join-Path $ROOT_DIR $svc.Path
        Write-Host "  构建 $($svc.Name)..." -ForegroundColor Yellow
        Push-Location $proj
        dotnet publish -c Release --no-self-contained 2>&1 | Out-Null
        if ($LASTEXITCODE -ne 0) { 
            Write-Host "  [错误] 构建失败" -ForegroundColor Red
            exit 1 
        }
        Pop-Location
        Write-Host "  $($svc.Name) 构建成功!" -ForegroundColor Green
    }
    Write-Host ""
}

Write-Host "`n============================================================" -ForegroundColor Blue
Write-Host "  部署服务" -ForegroundColor Blue
Write-Host "============================================================`n" -ForegroundColor Blue

# 停止并删除旧容器（如果存在）
Write-Host "`n清理旧容器和镜像..." -ForegroundColor Yellow
$oldContainers = @("go-nomads-gateway", "go-nomads-user-service", "go-nomads-product-service", "go-nomads-document-service", "go-nomads-city-service", "go-nomads-event-service", "go-nomads-coworking-service", "go-nomads-ai-service", "go-nomads-cache-service", "go-nomads-message-service", "go-nomads-accommodation-service", "go-nomads-innovation-service", "go-nomads-travel-planning-service", "go-nomads-ecommerce-service", "go-nomads-search-service")
foreach ($oldName in $oldContainers) {
    $exists = & $RUNTIME ps -a --filter "name=^${oldName}$" --format '{{.Names}}' 2>$null
    if ($exists) {
        Write-Host "  停止并移除容器: $oldName" -ForegroundColor Yellow
        & $RUNTIME stop $oldName 2>$null | Out-Null
        & $RUNTIME rm $oldName 2>$null | Out-Null
    }
}

# 清理未使用的镜像
$danglingImages = & $RUNTIME images --filter "dangling=true" -q 2>$null
if ($danglingImages) {
    Write-Host "  清理未使用的镜像..." -ForegroundColor Yellow
    & $RUNTIME rmi $danglingImages 2>$null | Out-Null
}

Write-Host ""

foreach ($svc in $services) {
    $container = $svc.Container
    
    Write-Host "`n------------------------------------------------------------" -ForegroundColor Cyan
    Write-Host "部署 $($svc.Name)" -ForegroundColor Cyan
    Write-Host "------------------------------------------------------------" -ForegroundColor Cyan
    
    # Remove existing container if it exists
    $existing = & $RUNTIME ps -a --format '{{.Names}}' 2>$null
    if ($existing -match $container) { 
        Write-Host "  移除旧容器..." -ForegroundColor Yellow
        & $RUNTIME stop $container 2>&1 | Out-Null
        & $RUNTIME rm -f $container 2>&1 | Out-Null 
    }
    
    $publish = Join-Path $ROOT_DIR "$($svc.Path)/bin/Release/net9.0/publish"
    if (-not (Test-Path $publish)) {
        Write-Error "  [错误] 发布目录未找到: $publish"
        exit 1
    }
    
    # Gateway 使用生产配置
    # 其他服务继续使用 Development 环境
    $aspnetEnv = if ($svc.Name -eq "gateway") { "Production" } else { "Development" }
    
    # 额外的环境变量（基础设施连接 + 服务发现）
    $extraEnvArgs = @()

    # --- Gateway: 需要所有服务的服务发现地址 ---
    if ($svc.Name -eq "gateway") {
        $extraEnvArgs = @(
            "-e", "services__user-service__http__0=$(SvcUrl 'user-service')",
            "-e", "services__city-service__http__0=$(SvcUrl 'city-service')",
            "-e", "services__product-service__http__0=$(SvcUrl 'product-service')",
            "-e", "services__document-service__http__0=$(SvcUrl 'document-service')",
            "-e", "services__coworking-service__http__0=$(SvcUrl 'coworking-service')",
            "-e", "services__accommodation-service__http__0=$(SvcUrl 'accommodation-service')",
            "-e", "services__event-service__http__0=$(SvcUrl 'event-service')",
            "-e", "services__innovation-service__http__0=$(SvcUrl 'innovation-service')",
            "-e", "services__ai-service__http__0=$(SvcUrl 'ai-service')",
            "-e", "services__search-service__http__0=$(SvcUrl 'search-service')",
            "-e", "services__cache-service__http__0=$(SvcUrl 'cache-service')",
            "-e", "services__message-service__http__0=$(SvcUrl 'message-service')"
        )
    }

    # --- User Service: Redis + RabbitMQ + 服务发现 ---
    if ($svc.Name -eq "user-service") {
        $extraEnvArgs = @(
            "-e", "ConnectionStrings__redis=${REDIS_HOST}:${REDIS_PORT}",
            "-e", "RabbitMQ__Host=$RABBITMQ_HOST",
            "-e", "RabbitMQ__Username=$RABBITMQ_USER",
            "-e", "RabbitMQ__Password=$RABBITMQ_PASS",
            "-e", "services__city-service__http__0=$(SvcUrl 'city-service')",
            "-e", "services__product-service__http__0=$(SvcUrl 'product-service')",
            "-e", "services__event-service__http__0=$(SvcUrl 'event-service')"
        )
    }

    # --- City Service: RabbitMQ + 服务发现 ---
    if ($svc.Name -eq "city-service") {
        $extraEnvArgs = @(
            "-e", "RabbitMQ__Host=$RABBITMQ_HOST",
            "-e", "RabbitMQ__Username=$RABBITMQ_USER",
            "-e", "RabbitMQ__Password=$RABBITMQ_PASS",
            "-e", "services__user-service__http__0=$(SvcUrl 'user-service')",
            "-e", "services__cache-service__http__0=$(SvcUrl 'cache-service')",
            "-e", "services__coworking-service__http__0=$(SvcUrl 'coworking-service')",
            "-e", "services__event-service__http__0=$(SvcUrl 'event-service')",
            "-e", "services__message-service__http__0=$(SvcUrl 'message-service')",
            "-e", "services__ai-service__http__0=$(SvcUrl 'ai-service')"
        )
    }

    # --- Product Service: 服务发现 ---
    if ($svc.Name -eq "product-service") {
        $extraEnvArgs = @(
            "-e", "services__user-service__http__0=$(SvcUrl 'user-service')"
        )
    }

    # --- Document Service: 服务配置 + 服务发现 ---
    if ($svc.Name -eq "document-service") {
        $extraEnvArgs = @(
            "-e", "Services__Gateway__Url=http://go-nomads-gateway:5000",
            "-e", "Services__Gateway__OpenApiUrl=http://go-nomads-gateway:5000/openapi/v1.json",
            "-e", "Services__ProductService__Url=http://go-nomads-product-service:8080",
            "-e", "Services__ProductService__OpenApiUrl=http://go-nomads-product-service:8080/openapi/v1.json",
            "-e", "Services__UserService__Url=http://go-nomads-user-service:8080",
            "-e", "Services__UserService__OpenApiUrl=http://go-nomads-user-service:8080/openapi/v1.json",
            "-e", "services__product-service__http__0=$(SvcUrl 'product-service')"
        )
    }

    # --- Coworking Service: RabbitMQ + 服务发现 ---
    if ($svc.Name -eq "coworking-service") {
        $extraEnvArgs = @(
            "-e", "RabbitMQ__Host=$RABBITMQ_HOST",
            "-e", "RabbitMQ__Username=$RABBITMQ_USER",
            "-e", "RabbitMQ__Password=$RABBITMQ_PASS",
            "-e", "services__cache-service__http__0=$(SvcUrl 'cache-service')",
            "-e", "services__user-service__http__0=$(SvcUrl 'user-service')",
            "-e", "services__city-service__http__0=$(SvcUrl 'city-service')"
        )
    }

    # --- Event Service: RabbitMQ + 服务发现 ---
    if ($svc.Name -eq "event-service") {
        $extraEnvArgs = @(
            "-e", "RabbitMQ__Host=$RABBITMQ_HOST",
            "-e", "RabbitMQ__Username=$RABBITMQ_USER",
            "-e", "RabbitMQ__Password=$RABBITMQ_PASS",
            "-e", "services__city-service__http__0=$(SvcUrl 'city-service')",
            "-e", "services__user-service__http__0=$(SvcUrl 'user-service')",
            "-e", "services__message-service__http__0=$(SvcUrl 'message-service')"
        )
    }

    # --- AI Service: Redis + RabbitMQ (注意: key 名不同!) + 服务发现 ---
    if ($svc.Name -eq "ai-service") {
        $extraEnvArgs = @(
            "-e", "Redis__ConnectionString=${REDIS_HOST}:${REDIS_PORT}",
            "-e", "RabbitMQ__HostName=$RABBITMQ_HOST",
            "-e", "RabbitMQ__UserName=$RABBITMQ_USER",
            "-e", "RabbitMQ__Password=$RABBITMQ_PASS",
            "-e", "services__user-service__http__0=$(SvcUrl 'user-service')",
            "-e", "services__city-service__http__0=$(SvcUrl 'city-service')"
        )
    }

    # --- Cache Service: Redis + 服务发现 ---
    if ($svc.Name -eq "cache-service") {
        $extraEnvArgs = @(
            "-e", "ConnectionStrings__Redis=${REDIS_HOST}:${REDIS_PORT}",
            "-e", "services__city-service__http__0=$(SvcUrl 'city-service')",
            "-e", "services__coworking-service__http__0=$(SvcUrl 'coworking-service')"
        )
    }

    # --- Message Service: Redis + RabbitMQ + 服务发现 ---
    if ($svc.Name -eq "message-service") {
        $extraEnvArgs = @(
            "-e", "ConnectionStrings__Redis=${REDIS_HOST}:${REDIS_PORT}",
            "-e", "RabbitMQ__Host=$RABBITMQ_HOST",
            "-e", "RabbitMQ__Username=$RABBITMQ_USER",
            "-e", "RabbitMQ__Password=$RABBITMQ_PASS",
            "-e", "services__user-service__http__0=$(SvcUrl 'user-service')"
        )
    }

    # --- Accommodation Service: 服务发现 ---
    if ($svc.Name -eq "accommodation-service") {
        $extraEnvArgs = @(
            "-e", "services__user-service__http__0=$(SvcUrl 'user-service')"
        )
    }

    # --- Innovation Service: RabbitMQ + 服务发现 ---
    if ($svc.Name -eq "innovation-service") {
        $extraEnvArgs = @(
            "-e", "RabbitMQ__Host=$RABBITMQ_HOST",
            "-e", "RabbitMQ__Username=$RABBITMQ_USER",
            "-e", "RabbitMQ__Password=$RABBITMQ_PASS",
            "-e", "services__user-service__http__0=$(SvcUrl 'user-service')"
        )
    }

    # --- Search Service: Elasticsearch + RabbitMQ + 服务发现 ---
    if ($svc.Name -eq "search-service") {
        $extraEnvArgs = @(
            "-e", "Elasticsearch__Url=$ELASTICSEARCH_URL",
            "-e", "RabbitMQ__Host=$RABBITMQ_HOST",
            "-e", "RabbitMQ__Username=$RABBITMQ_USER",
            "-e", "RabbitMQ__Password=$RABBITMQ_PASS",
            "-e", "services__city-service__http__0=$(SvcUrl 'city-service')",
            "-e", "services__coworking-service__http__0=$(SvcUrl 'coworking-service')"
        )
    }

    # 启动应用容器
    Write-Host "  启动应用容器..." -ForegroundColor Yellow
    
    # 确定监听端口
    $listenPort = if ($svc.Name -eq "gateway") { "5000" } else { "8080" }
    
    $runArgs = @(
        "run", "-d",
        "--name", $container,
        "--network", $NETWORK_NAME,
        "--label", "com.docker.compose.project=go-nomads",
        "--label", "com.docker.compose.service=$($svc.Name)",
        "-p", "$($svc.Port):$listenPort",
        "-e", "ASPNETCORE_URLS=http://+:$listenPort",
        "-e", "ASPNETCORE_ENVIRONMENT=$aspnetEnv",
        "-e", "HTTP_PROXY=",
        "-e", "HTTPS_PROXY=",
        "-e", "NO_PROXY="
    )
    $runArgs += $extraEnvArgs
    $runArgs += @(
        "-v", "${publish}:/app:ro",
        "-w", "/app",
        "mcr.microsoft.com/dotnet/aspnet:9.0",
        "dotnet", $svc.Dll
    )
    
    & $RUNTIME $runArgs | Out-Null
    
    if ($LASTEXITCODE -ne 0) { 
        Write-Host "  [错误] 应用容器启动失败" -ForegroundColor Red
        Write-Host "  查看日志: $RUNTIME logs $container" -ForegroundColor Yellow
        exit 1 
    }
    Write-Host "  应用容器启动成功!" -ForegroundColor Green
    Write-Host "  $($svc.Name) 部署成功!" -ForegroundColor Green
    Write-Host "  应用端口: http://localhost:$($svc.Port)" -ForegroundColor Green
    
    Start-Sleep -Seconds 2
}

Write-Host "`n============================================================" -ForegroundColor Green
Write-Host "  部署摘要" -ForegroundColor Green
Write-Host "============================================================`n" -ForegroundColor Green

Write-Host "所有服务部署完成!" -ForegroundColor Green
Write-Host ""

Write-Host "反向代理:" -ForegroundColor Cyan
Write-Host "  Nginx (推荐):         http://localhost" -ForegroundColor Green
Write-Host ""

Write-Host "服务访问地址:" -ForegroundColor Cyan
Write-Host "  Gateway:              http://localhost:5080" -ForegroundColor Green
Write-Host "  User Service:         http://localhost:5001" -ForegroundColor Green
Write-Host "  Product Service:      http://localhost:5002" -ForegroundColor Green
Write-Host "  Document Service:     http://localhost:5003" -ForegroundColor Green
Write-Host "  City Service:         http://localhost:8002" -ForegroundColor Green
Write-Host "  Event Service:        http://localhost:8005" -ForegroundColor Green
Write-Host "  Coworking Service:    http://localhost:8006" -ForegroundColor Green
Write-Host "  AI Service:           http://localhost:8009" -ForegroundColor Green
Write-Host "  Cache Service:        http://localhost:8010" -ForegroundColor Green
Write-Host "  Message Service:      http://localhost:5005" -ForegroundColor Green
Write-Host "  Accommodation Service: http://localhost:8012" -ForegroundColor Green
Write-Host "  Innovation Service:   http://localhost:8011" -ForegroundColor Green
Write-Host "  Search Service:       http://localhost:8015" -ForegroundColor Green
Write-Host "  Message Swagger:      http://localhost:5005/swagger" -ForegroundColor Green
Write-Host ""

Write-Host "常用命令:" -ForegroundColor Cyan
Write-Host "  查看运行中的容器:  $RUNTIME ps" -ForegroundColor Yellow
Write-Host "  查看服务日志:      $RUNTIME logs go-nomads-gateway" -ForegroundColor Yellow
Write-Host "  停止所有服务:      .\stop-services.ps1" -ForegroundColor Yellow
Write-Host ""

Write-Host "容器状态:" -ForegroundColor Cyan
& $RUNTIME ps --filter "name=go-nomads-" --format 'table {{.Names}}`t{{.Status}}`t{{.Ports}}'
Write-Host ""

Write-Host "部署完成! 🚀" -ForegroundColor Green
