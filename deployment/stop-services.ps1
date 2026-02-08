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

# Service definitions
$SERVICES = @(
    @{ Name = "go-nomads-gateway" },
    @{ Name = "go-nomads-product" },
    @{ Name = "go-nomads-user" },
    @{ Name = "go-nomads-document" }
)

function Write-Header {
    param([string]$Message)
    Write-Host ""
    Write-Host "============================================================" -ForegroundColor Cyan
    Write-Host "  $Message" -ForegroundColor Cyan
    Write-Host "============================================================" -ForegroundColor Cyan
    Write-Host ""
}

function Stop-Service {
    param(
        [string]$ServiceName
    )
    
    Write-Host "`nStopping service: $ServiceName" -ForegroundColor Yellow
    
    try {
        $running = & $CONTAINER_RUNTIME ps --filter "name=$ServiceName" --filter "status=running" --format '{{.Names}}' 2>$null
        if ($running -eq $ServiceName) {
            Write-Host "  Stopping service: $ServiceName..." -ForegroundColor Gray
            & $CONTAINER_RUNTIME stop $ServiceName 2>&1 | Out-Null
            if ($LASTEXITCODE -eq 0) {
                Write-Host "    Service stopped" -ForegroundColor Green
            }
        }
    } catch {
        Write-Host "    Warning: Could not stop service" -ForegroundColor Yellow
    }
}

function Remove-Service {
    param(
        [string]$ServiceName
    )
    
    Write-Host "`nRemoving service: $ServiceName" -ForegroundColor Yellow
    
    try {
        $exists = & $CONTAINER_RUNTIME ps -a --filter "name=$ServiceName" --format '{{.Names}}' 2>$null
        if ($exists -eq $ServiceName) {
            Write-Host "  Removing service: $ServiceName..." -ForegroundColor Gray
            & $CONTAINER_RUNTIME rm -f $ServiceName 2>&1 | Out-Null
            if ($LASTEXITCODE -eq 0) {
                Write-Host "    Service removed" -ForegroundColor Green
            }
        }
    } catch {
        Write-Host "    Warning: Could not remove service" -ForegroundColor Yellow
    }
}

# Main logic
if ($Clean) {
    Write-Header "Stopping and Removing All Go-Nomads Services"
    foreach ($service in $SERVICES) {
        Stop-Service -ServiceName $service.Name
        Remove-Service -ServiceName $service.Name
    }
    Write-Host "`nAll services stopped and removed!" -ForegroundColor Green
} else {
    Write-Header "Stopping All Go-Nomads Services"
    foreach ($service in $SERVICES) {
        Stop-Service -ServiceName $service.Name
    }
    Write-Host "`nAll services stopped!" -ForegroundColor Green
    Write-Host "Use 'stop-services.ps1 -Clean' to also remove containers" -ForegroundColor Cyan
}

# Show status
Write-Host "`nCurrent service status:" -ForegroundColor Cyan
& $CONTAINER_RUNTIME ps --filter "name=go-nomads" --format 'table {{.Names}}`t{{.Status}}`t{{.Ports}}'
