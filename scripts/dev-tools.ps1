# Go-Nomads Backend 开发辅助脚本
# 提供各种开发和调试功能

param(
    [Parameter(Position=0)]
    [ValidateSet("status", "logs", "test", "build", "clean", "dashboard", "help")]
    [string]$Action = "help",
    
    [Parameter(Position=1)]
    [string]$Service = "",
    
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

# 服务配置
$Services = @{
    "gateway" = @{
        Name = "gateway"
        Path = "src\Gateway\Gateway"
        Port = 5000
        DaprAppId = "gateway"
    }
    "user-service" = @{
        Name = "user-service"
        Path = "src\Services\UserService\UserService"
        Port = 5001
        DaprAppId = "user-service"
    }
    "product-service" = @{
        Name = "product-service"
        Path = "src\Services\ProductService\ProductService"
        Port = 5002
        DaprAppId = "product-service"
    }
}

function Show-Help {
    Write-Info "Go-Nomads Backend 开发辅助工具"
    Write-Info "========================================"
    Write-Info "用法: .\dev-tools.ps1 <action> [service] [-Verbose]"
    Write-Info ""
    Write-Info "可用操作:"
    Write-Info "  status     - 显示所有服务状态"
    Write-Info "  logs       - 显示服务日志 (需要指定服务名)"
    Write-Info "  test       - 运行测试 (可选择指定服务)"
    Write-Info "  build      - 构建项目 (可选择指定服务)"
    Write-Info "  clean      - 清理构建输出"
    Write-Info "  dashboard  - 打开Dapr dashboard"
    Write-Info "  help       - 显示此帮助信息"
    Write-Info ""
    Write-Info "可用服务:"
    foreach ($key in $Services.Keys) {
        Write-Info "  $key"
    }
    Write-Info ""
    Write-Info "示例:"
    Write-Info "  .\dev-tools.ps1 status"
    Write-Info "  .\dev-tools.ps1 logs user-service"
    Write-Info "  .\dev-tools.ps1 build gateway"
    Write-Info "  .\dev-tools.ps1 test -Verbose"
}

function Show-Status {
    Write-Info "Go-Nomads Backend 服务状态"
    Write-Info "========================================"
    
    # 检查Dapr应用状态
    Write-Info "Dapr应用状态:"
    try {
        $daprList = dapr list --output json | ConvertFrom-Json
        
        foreach ($serviceKey in $Services.Keys) {
            $service = $Services[$serviceKey]
            $daprApp = $daprList | Where-Object { $_.app_id -eq $service.DaprAppId }
            
            if ($daprApp) {
                $status = $daprApp.enabled
                $color = if ($status -eq $true) { "Green" } else { "Red" }
                $statusText = if ($status -eq $true) { "运行中" } else { "已停止" }
                Write-ColorOutput "  $($service.Name): $statusText (端口: $($service.Port))" $color
                
                if ($Verbose -and $daprApp) {
                    Write-Host "    App ID: $($daprApp.app_id)" -ForegroundColor Gray
                    Write-Host "    HTTP端口: $($daprApp.http_port)" -ForegroundColor Gray
                    Write-Host "    gRPC端口: $($daprApp.grpc_port)" -ForegroundColor Gray
                    Write-Host "    进程ID: $($daprApp.pid)" -ForegroundColor Gray
                }
            } else {
                Write-ColorOutput "  $($service.Name): 未运行" "Red"
            }
        }
    } catch {
        Write-Error "无法获取Dapr状态: $_"
    }
    
    Write-Info ""
    Write-Info "基础设施状态:"
    
    # 检查Redis
    try {
        $redisContainer = podman ps --filter "name=redis" --filter "status=running" --format "{{.Status}}"
        if ($redisContainer) {
            Write-ColorOutput "  Redis: 运行中 (端口: 6379)" "Green"
        } else {
            Write-ColorOutput "  Redis: 未运行" "Red"
        }
    } catch {
        Write-ColorOutput "  Redis: 状态未知" "Yellow"
    }
    
    # 检查Zipkin
    try {
        $zipkinContainer = podman ps --filter "name=zipkin" --filter "status=running" --format "{{.Status}}"
        if ($zipkinContainer) {
            Write-ColorOutput "  Zipkin: 运行中 (端口: 9411)" "Green"
        } else {
            Write-ColorOutput "  Zipkin: 未运行" "Red"
        }
    } catch {
        Write-ColorOutput "  Zipkin: 状态未知" "Yellow"
    }
    
    # 端口检查
    Write-Info ""
    Write-Info "端口占用状态:"
    $ports = @(5000, 5001, 5002, 6379, 9411)
    
    foreach ($port in $ports) {
        try {
            $connection = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
            if ($connection) {
                $processId = $connection.OwningProcess
                $processName = (Get-Process -Id $processId -ErrorAction SilentlyContinue).Name
                Write-ColorOutput "  端口 $port : 被占用 ($processName)" "Yellow"
            } else {
                Write-ColorOutput "  端口 $port : 空闲" "Green"
            }
        } catch {
            Write-ColorOutput "  端口 $port : 空闲" "Green"
        }
    }
}

function Show-Logs {
    param([string]$ServiceName)
    
    if (-not $ServiceName) {
        Write-Error "请指定服务名称"
        Write-Info "可用服务: $($Services.Keys -join ', ')"
        return
    }
    
    if (-not $Services.ContainsKey($ServiceName)) {
        Write-Error "未知服务: $ServiceName"
        Write-Info "可用服务: $($Services.Keys -join ', ')"
        return
    }
    
    $service = $Services[$ServiceName]
    Write-Info "显示 $($service.Name) 的日志..."
    Write-Info "按 Ctrl+C 退出日志查看"
    Write-Info "========================================"
    
    try {
        dapr logs --app-id $service.DaprAppId
    } catch {
        Write-Error "无法获取 $($service.Name) 的日志: $_"
        Write-Info "请确保服务正在运行"
    }
}

function Run-Tests {
    param([string]$ServiceName)
    
    Write-Info "运行测试..."
    Write-Info "========================================"
    
    Set-Location $RootDir
    
    if ($ServiceName) {
        if (-not $Services.ContainsKey($ServiceName)) {
            Write-Error "未知服务: $ServiceName"
            return
        }
        
        $service = $Services[$ServiceName]
        $servicePath = Join-Path $RootDir $service.Path
        
        Write-Info "运行 $($service.Name) 的测试..."
        Set-Location $servicePath
        
        try {
            dotnet test --verbosity normal
            Write-Success "$($service.Name) 测试完成"
        } catch {
            Write-Error "$($service.Name) 测试失败: $_"
        }
    } else {
        Write-Info "运行所有测试..."
        try {
            dotnet test --verbosity normal
            Write-Success "所有测试完成"
        } catch {
            Write-Error "测试失败: $_"
        }
    }
}

function Build-Project {
    param([string]$ServiceName)
    
    Write-Info "构建项目..."
    Write-Info "========================================"
    
    Set-Location $RootDir
    
    if ($ServiceName) {
        if (-not $Services.ContainsKey($ServiceName)) {
            Write-Error "未知服务: $ServiceName"
            return
        }
        
        $service = $Services[$ServiceName]
        $servicePath = Join-Path $RootDir $service.Path
        
        Write-Info "构建 $($service.Name)..."
        Set-Location $servicePath
        
        try {
            if ($Verbose) {
                dotnet build --configuration Release --verbosity detailed
            } else {
                dotnet build --configuration Release --verbosity minimal
            }
            Write-Success "$($service.Name) 構建成功"
        } catch {
            Write-Error "$($service.Name) 構建失败: $_"
        }
    } else {
        Write-Info "构建整个解决方案..."
        try {
            if ($Verbose) {
                dotnet build --configuration Release --verbosity detailed
            } else {
                dotnet build --configuration Release --verbosity minimal
            }
            Write-Success "解决方案构建成功"
        } catch {
            Write-Error "解决方案构建失败: $_"
            Write-Info "尝试单独构建各个服务..."
            
            foreach ($serviceKey in $Services.Keys) {
                Build-Project -ServiceName $serviceKey
            }
        }
    }
}

function Clean-Project {
    Write-Info "清理构建输出..."
    Write-Info "========================================"
    
    Set-Location $RootDir
    
    try {
        dotnet clean
        Write-Success "构建输出清理完成"
        
        # 清理额外的文件
        $patterns = @("bin", "obj", "*.user", "*.cache")
        foreach ($pattern in $patterns) {
            $files = Get-ChildItem -Path $RootDir -Recurse -Name $pattern -Force -ErrorAction SilentlyContinue
            foreach ($file in $files) {
                $fullPath = Join-Path $RootDir $file
                if (Test-Path $fullPath) {
                    if (Test-Path $fullPath -PathType Container) {
                        Remove-Item $fullPath -Recurse -Force -ErrorAction SilentlyContinue
                    } else {
                        Remove-Item $fullPath -Force -ErrorAction SilentlyContinue
                    }
                }
            }
        }
        
        Write-Success "额外文件清理完成"
    } catch {
        Write-Error "清理失败: $_"
    }
}

function Open-Dashboard {
    Write-Info "启动Dapr Dashboard..."
    
    try {
        Start-Process -FilePath "dapr" -ArgumentList "dashboard" -WindowStyle Normal
        Write-Success "Dapr Dashboard 启动中..."
        Write-Info "将在浏览器中打开 http://localhost:8080"
        
        # 等待几秒后打开浏览器
        Start-Sleep -Seconds 3
        Start-Process "http://localhost:8080"
    } catch {
        Write-Error "无法启动Dapr Dashboard: $_"
        Write-Info "请确保Dapr CLI已正确安装"
    }
}

# 主逻辑
Write-Info "Go-Nomads Backend 开发工具"

switch ($Action.ToLower()) {
    "status" {
        Show-Status
    }
    "logs" {
        Show-Logs -ServiceName $Service
    }
    "test" {
        Run-Tests -ServiceName $Service
    }
    "build" {
        Build-Project -ServiceName $Service
    }
    "clean" {
        Clean-Project
    }
    "dashboard" {
        Open-Dashboard
    }
    "help" {
        Show-Help
    }
    default {
        Write-Error "未知操作: $Action"
        Show-Help
    }
}