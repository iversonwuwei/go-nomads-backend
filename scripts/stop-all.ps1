# Go-Nomads Backend 停止脚本
# 此脚本将停止所有微服务和基础设施

param(
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

Write-Info "Go-Nomads Backend 停止脚本"
Write-Info "========================================"

# 停止Dapr应用
Write-Info "停止Dapr应用..."

$daprApps = @("user-service", "product-service", "gateway")

foreach ($appId in $daprApps) {
    try {
        Write-Info "停止 $appId..."
        dapr stop --app-id $appId
        Write-Success "$appId 已停止"
    } catch {
        Write-Warning "$appId 停止失败或未运行: $_"
    }
}

# 停止所有Dapr进程
Write-Info "清理Dapr进程..."
try {
    $daprProcesses = Get-Process -Name "daprd" -ErrorAction SilentlyContinue
    if ($daprProcesses) {
        $daprProcesses | Stop-Process -Force
        Write-Success "Dapr进程已清理"
    } else {
        Write-Info "没有发现Dapr进程"
    }
} catch {
    Write-Warning "清理Dapr进程时出错: $_"
}

# 停止.NET应用进程
Write-Info "停止.NET应用进程..."
try {
    $dotnetProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | Where-Object {
        $_.MainWindowTitle -like "*Gateway*" -or 
        $_.MainWindowTitle -like "*UserService*" -or 
        $_.MainWindowTitle -like "*ProductService*" -or
        $_.CommandLine -like "*Gateway*" -or
        $_.CommandLine -like "*UserService*" -or
        $_.CommandLine -like "*ProductService*"
    }
    
    if ($dotnetProcesses) {
        $dotnetProcesses | Stop-Process -Force
        Write-Success ".NET应用进程已停止"
    } else {
        Write-Info "没有发现相关的.NET进程"
    }
} catch {
    Write-Warning "停止.NET进程时出错: $_"
}

# 停止Podman容器
Write-Info "停止Podman基础设施..."

# 停止Redis容器
try {
    $redisContainer = podman ps --filter "name=redis" --filter "status=running" -q
    if ($redisContainer) {
        podman stop redis
        Write-Success "Redis容器已停止"
    } else {
        Write-Info "Redis容器未运行"
    }
} catch {
    Write-Warning "停止Redis容器失败: $_"
}

# 停止Zipkin容器
try {
    $zipkinContainer = podman ps --filter "name=zipkin" --filter "status=running" -q
    if ($zipkinContainer) {
        podman stop zipkin
        Write-Success "Zipkin容器已停止"
    } else {
        Write-Info "Zipkin容器未运行"
    }
} catch {
    Write-Warning "停止Zipkin容器失败: $_"
}

# 清理端口占用（可选）
Write-Info "检查端口占用..."
$ports = @(5000, 5001, 5002, 3000, 3001, 3002, 50000, 50001, 50002)

foreach ($port in $ports) {
    try {
        $process = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
        if ($process) {
            $processId = $process.OwningProcess
            $processName = (Get-Process -Id $processId -ErrorAction SilentlyContinue).Name
            Write-Warning "端口 $port 仍被进程占用: $processName (PID: $processId)"
            
            # 如果是已知的服务进程，尝试停止
            if ($processName -in @("dotnet", "daprd")) {
                try {
                    Stop-Process -Id $processId -Force
                    Write-Success "已停止占用端口 $port 的进程"
                } catch {
                    Write-Error "无法停止进程 $processId : $_"
                }
            }
        }
    } catch {
        # 端口未被占用，这是正常的
    }
}

# 显示最终状态
Write-Info "========================================"
Write-Info "检查剩余进程..."

# Dapr进程检查
$remainingDapr = Get-Process -Name "daprd" -ErrorAction SilentlyContinue
if ($remainingDapr) {
    Write-Warning "仍有Dapr进程运行:"
    $remainingDapr | ForEach-Object { Write-Host "  PID: $($_.Id)" -ForegroundColor Yellow }
} else {
    Write-Success "所有Dapr进程已停止"
}

# Podman容器检查
$runningContainers = podman ps --filter "name=redis" --filter "name=zipkin" --format "table {{.Names}}\t{{.Status}}"
if ($runningContainers -and $runningContainers.Count -gt 1) {
    Write-Warning "仍有相关容器运行:"
    Write-Host $runningContainers -ForegroundColor Yellow
} else {
    Write-Success "所有相关容器已停止"
}

# 端口状态检查
Write-Info "最终端口状态检查..."
$occupiedPorts = @()
foreach ($port in $ports) {
    try {
        $connection = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue
        if ($connection) {
            $occupiedPorts += $port
        }
    } catch {
        # 端口空闲
    }
}

if ($occupiedPorts.Count -gt 0) {
    Write-Warning "以下端口仍被占用: $($occupiedPorts -join ', ')"
    Write-Info "如需强制释放，请重启相关进程或重启系统"
} else {
    Write-Success "所有相关端口已释放"
}

Write-Info "========================================"
Write-Success "停止脚本执行完成！"

if ($Verbose) {
    Write-Info "详细信息:"
    Write-Info "  如果仍有进程运行，可能需要手动停止"
    Write-Info "  重启系统可以确保完全清理所有资源"
    Write-Info "  下次启动前建议检查端口占用情况"
}

Write-Info "要重新启动服务，请运行: .\scripts\start-all.ps1"