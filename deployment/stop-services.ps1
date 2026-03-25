# Go-Nomads Stop Services Script (Windows PowerShell)
# Usage: .\stop-services.ps1 [-Clean]

param(
    [switch]$Clean = $false
)

$ErrorActionPreference = 'Stop'

# Auto-detect container runtime
$CONTAINER_RUNTIME = $null
if (Get-Command podman -ErrorAction SilentlyContinue) {
    Write-Host "Detected Podman" -ForegroundColor Cyan
    try {
        $podmanContainers = podman ps -a --filter "name=go-nomads-" --format '{{.Names}}' 2>$null
        if ($LASTEXITCODE -eq 0) {
            $CONTAINER_RUNTIME = "podman"
        }
    } catch {}
}

if (-not $CONTAINER_RUNTIME -and (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Host "Detected Docker" -ForegroundColor Cyan
    try {
        $dockerContainers = docker ps -a --filter "name=go-nomads-" --format '{{.Names}}' 2>$null
        if ($LASTEXITCODE -eq 0) {
            $CONTAINER_RUNTIME = "docker"
        }
    } catch {}
}

if (-not $CONTAINER_RUNTIME) {
    Write-Error "Neither Docker nor Podman found or accessible!"
    exit 1
}

Write-Host "Using container runtime: $CONTAINER_RUNTIME" -ForegroundColor Green

# Service definitions（与 deploy-services-local.ps1 保持同步）
$SERVICES = @(
    "go-nomads-gateway",
    "go-nomads-user-service",
    "go-nomads-product-service",
    "go-nomads-document-service",
    "go-nomads-city-service",
    "go-nomads-event-service",
    "go-nomads-coworking-service",
    "go-nomads-ai-service",
    "go-nomads-cache-service",
    "go-nomads-message-service",
    "go-nomads-accommodation-service",
    "go-nomads-innovation-service",
    "go-nomads-search-service"
)

function Stop-Service {
    param([string]$ServiceName)
    
    $exists = & $CONTAINER_RUNTIME ps -a --filter "name=$ServiceName" --format '{{.Names}}' 2>$null
    if ($exists -eq $ServiceName) {
        Write-Host "  停止容器: $ServiceName..." -ForegroundColor Yellow
        & $CONTAINER_RUNTIME stop $ServiceName 2>&1 | Out-Null
        & $CONTAINER_RUNTIME rm $ServiceName 2>&1 | Out-Null
        Write-Host "  ✓ $ServiceName 已停止并删除" -ForegroundColor Green
    } else {
        Write-Host "  - $ServiceName 不存在，跳过" -ForegroundColor Blue
    }
}

# Main logic
Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  停止 Go-Nomads 服务 (使用 $CONTAINER_RUNTIME)" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

foreach ($svc in $SERVICES) {
    Stop-Service -ServiceName $svc
}

Write-Host ""
Write-Host "所有服务已停止! ✓" -ForegroundColor Green
Write-Host ""
Write-Host "注意: 基础设施服务 (Redis, RabbitMQ, Elasticsearch, etc.) 仍在运行" -ForegroundColor Cyan
Write-Host "如需停止基础设施，请运行: .\deploy-infrastructure-local.ps1 stop" -ForegroundColor Cyan
Write-Host ""

# Show status
Write-Host "当前容器状态:" -ForegroundColor Cyan
& $CONTAINER_RUNTIME ps --filter "name=go-nomads" --format 'table {{.Names}}`t{{.Status}}`t{{.Ports}}'
