# Go-Nomads Backend 启动脚本
# 此脚本将启动所有微服务和必要的基础设施

param(
    [switch]$SkipBuild = $false,
    [switch]$Verbose = $false
)

# 设置颜色输出
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

function Write-Success {
    param([string]$Message)
    Write-ColorOutput "✓ $Message" "Green"
}

function Write-Info {
    param([string]$Message)
    Write-ColorOutput "ℹ $Message" "Cyan"
}

function Write-Warning {
    param([string]$Message)
    Write-ColorOutput "⚠ $Message" "Yellow"
}

function Write-Error {
    param([string]$Message)
    Write-ColorOutput "✗ $Message" "Red"
}

# 获取脚本所在目录
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$RootDir = Split-Path -Parent $ScriptDir

Write-Info "Go-Nomads Backend 启动脚本"
Write-Info "根目录: $RootDir"
Write-Info "========================================"

# 检查必要的工具是否安装
Write-Info "检查必要的工具..."

# 检查.NET 9
try {
    $dotnetVersion = dotnet --version
    if ($dotnetVersion -like "9.*") {
        Write-Success ".NET 9 SDK 已安装: $dotnetVersion"
    } else {
        Write-Warning ".NET 版本: $dotnetVersion (建议使用.NET 9)"
    }
} catch {
    Write-Error ".NET SDK 未安装"
    exit 1
}

# 检查Dapr CLI
try {
    $daprVersion = dapr --version
    Write-Success "Dapr CLI 已安装"
    if ($Verbose) {
        Write-Host $daprVersion
    }
} catch {
    Write-Error "Dapr CLI 未安装，请运行: powershell -Command `"iwr -useb https://raw.githubusercontent.com/dapr/cli/master/install/install.ps1 | iex`""
    exit 1
}

# 检查Podman
try {
    $podmanVersion = podman --version
    Write-Success "Podman 已安装: $podmanVersion"
} catch {
    Write-Error "Podman 未安装"
    exit 1
}

Write-Info "========================================"

# 构建项目
if (-not $SkipBuild) {
    Write-Info "构建项目..."
    
    Set-Location $RootDir
    
    try {
        dotnet build --configuration Release --verbosity minimal
        Write-Success "项目构建成功"
    } catch {
        Write-Error "项目构建失败"
        # 尝试修复ProductService的构建问题
        Write-Info "尝试修复ProductService构建问题..."
        
        $productServicePath = Join-Path $RootDir "src\Services\ProductService\ProductService"
        Set-Location $productServicePath
        
        # 清理和重新构建
        dotnet clean
        dotnet build --verbosity normal
        Write-Warning "ProductService可能需要手工修复protobuf类型冲突"
    }
} else {
    Write-Info "跳过构建步骤"
}

Write-Info "========================================"

# 启动基础设施服务
Write-Info "启动基础设施服务..."

# 启动Redis
Write-Info "启动Redis..."
try {
    $existingRedis = podman ps --filter "name=redis" --filter "status=running" -q
    if ($existingRedis) {
        Write-Success "Redis 容器已运行"
    } else {
        podman run -d --name redis --rm -p 6379:6379 redis:alpine
        Start-Sleep -Seconds 3
        Write-Success "Redis 容器已启动"
    }
} catch {
    Write-Error "Redis 启动失败"
    exit 1
}

# 启动Zipkin (可选的追踪服务)
Write-Info "启动Zipkin追踪服务..."
try {
    $existingZipkin = podman ps --filter "name=zipkin" --filter "status=running" -q
    if ($existingZipkin) {
        Write-Success "Zipkin 容器已运行"
    } else {
        podman run -d --name zipkin --rm -p 9411:9411 openzipkin/zipkin
        Start-Sleep -Seconds 5
        Write-Success "Zipkin 容器已启动"
    }
} catch {
    Write-Warning "Zipkin 启动失败，追踪功能可能不可用"
}

Write-Info "========================================"

# 启动微服务
Write-Info "启动微服务..."

# 定义服务配置
$services = @(
    @{
        Name = "user-service"
        Path = "src\Services\UserService\UserService"
        AppPort = 5001
        DaprHttpPort = 3001
        DaprGrpcPort = 50001
    },
    @{
        Name = "product-service"
        Path = "src\Services\ProductService\ProductService"
        AppPort = 5002
        DaprHttpPort = 3002
        DaprGrpcPort = 50002
    },
    @{
        Name = "gateway"
        Path = "src\Gateway\Gateway"
        AppPort = 5000
        DaprHttpPort = 3000
        DaprGrpcPort = 50000
    }
)

# 存储启动的进程ID
$Global:ServiceProcesses = @()

# 启动服务函数
function Start-Service {
    param(
        [hashtable]$ServiceConfig
    )
    
    $servicePath = Join-Path $RootDir $ServiceConfig.Path
    $componentsPath = Join-Path $RootDir "dapr"
    
    Write-Info "启动 $($ServiceConfig.Name)..."
    Write-Info "  路径: $servicePath"
    Write-Info "  应用端口: $($ServiceConfig.AppPort)"
    Write-Info "  Dapr HTTP端口: $($ServiceConfig.DaprHttpPort)"
    Write-Info "  Dapr gRPC端口: $($ServiceConfig.DaprGrpcPort)"
    
    # 构建Dapr命令
    $daprArgs = @(
        "run"
        "--app-id", $ServiceConfig.Name
        "--app-port", $ServiceConfig.AppPort
        "--dapr-http-port", $ServiceConfig.DaprHttpPort
        "--dapr-grpc-port", $ServiceConfig.DaprGrpcPort
        "--components-path", $componentsPath
        "--"
        "dotnet", "run"
    )
    
    if ($Verbose) {
        $daprArgs += "--verbosity", "detailed"
    }
    
    try {
        # 启动服务进程
        $process = Start-Process -FilePath "dapr" -ArgumentList $daprArgs -WorkingDirectory $servicePath -PassThru -WindowStyle Minimized
        $Global:ServiceProcesses += @{
            Name = $ServiceConfig.Name
            Process = $process
            Port = $ServiceConfig.AppPort
        }
        
        Write-Success "$($ServiceConfig.Name) 启动中... (PID: $($process.Id))"
        
        # 等待服务启动
        Start-Sleep -Seconds 3
        
    } catch {
        Write-Error "$($ServiceConfig.Name) 启动失败: $_"
        return $false
    }
    
    return $true
}

# 依次启动所有服务
foreach ($service in $services) {
    $success = Start-Service -ServiceConfig $service
    if (-not $success) {
        Write-Error "服务启动失败，正在清理..."
        & "$ScriptDir\stop-all.ps1"
        exit 1
    }
}

Write-Info "========================================"
Write-Success "所有服务启动完成！"

# 显示服务状态
Write-Info "服务状态:"
foreach ($serviceProcess in $Global:ServiceProcesses) {
    $status = if ($serviceProcess.Process.HasExited) { "已停止" } else { "运行中" }
    $color = if ($status -eq "运行中") { "Green" } else { "Red" }
    Write-ColorOutput "  $($serviceProcess.Name): $status (端口: $($serviceProcess.Port))" $color
}

Write-Info "基础设施 (Podman容器):"
Write-ColorOutput "  Redis: 端口 6379" "Green"
Write-ColorOutput "  Zipkin: 端口 9411 (http://localhost:9411)" "Green"

Write-Info "========================================"
Write-Info "API网关地址: http://localhost:5000"
Write-Info "可用的API端点:"
Write-Info "  GET  http://localhost:5000/api/users      - 获取用户列表"
Write-Info "  POST http://localhost:5000/api/users      - 创建用户"
Write-Info "  GET  http://localhost:5000/api/products   - 获取产品列表"
Write-Info "  POST http://localhost:5000/api/products   - 创建产品"

Write-Info "========================================"
Write-Info "管理工具:"
Write-Info "  Dapr Dashboard: dapr dashboard"
Write-Info "  Zipkin追踪:     http://localhost:9411"
Write-Info "  停止所有服务:   .\scripts\stop-all.ps1"

Write-Info "========================================"
Write-Success "启动完成！按任意键退出..."

# 等待用户输入
Read-Host

Write-Info "保持服务运行中..."
Write-Info "要停止服务，请运行: .\scripts\stop-all.ps1"