#!/usr/bin/env pwsh
# Deploy Dapr sidecars with Podman
# This script starts Dapr sidecars for each microservice

param(
    [Parameter()]
    [ValidateSet("start", "stop", "status")]
    [string]$Action = "start"
)

$ErrorActionPreference = "Stop"

# Configuration
$NETWORK = "go-nomads-network"
$DAPR_VERSION = "1.14.4"
$COMPONENTS_PATH = "E:\Workspaces\WaldenProjects\go-nomads\deployment\dapr\components"
$CONFIG_PATH = "E:\Workspaces\WaldenProjects\go-nomads\deployment\dapr\config"

# Service configurations
$services = @(
    @{
        Name = "product-service"
        AppId = "product-service"
        AppPort = 8080
        DaprHttpPort = 3500
        DaprGrpcPort = 51001
        Container = "go-nomads-product-service"
    },
    @{
        Name = "user-service"
        AppId = "user-service"
        AppPort = 8080
        DaprHttpPort = 3501
        DaprGrpcPort = 51002
        Container = "go-nomads-user-service"
    },
    @{
        Name = "gateway"
        AppId = "gateway"
        AppPort = 8080
        DaprHttpPort = 3502
        DaprGrpcPort = 51003
        Container = "go-nomads-gateway"
    }
)

function Start-DaprSidecars {
    Write-Host "`n=== Starting Dapr Sidecars ===" -ForegroundColor Cyan
    
    # Check if placement service exists
    $placementExists = podman ps -a --filter "name=dapr-placement" --format "{{.Names}}" | Select-String "dapr-placement"
    
    if (-not $placementExists) {
        Write-Host "`nNote: Dapr Placement service image not available." -ForegroundColor Yellow
        Write-Host "Actor support will be limited. For full Actor support, manually pull:" -ForegroundColor Yellow
        Write-Host "  podman pull daprio/placement:1.14.4" -ForegroundColor Gray
    }
    
    foreach ($svc in $services) {
        Write-Host "`nStarting Dapr sidecar for $($svc.Name)..." -ForegroundColor Green
        
        $sidecarName = "dapr-$($svc.Name)"
        
        # Check if sidecar already exists
        $exists = podman ps -a --filter "name=$sidecarName" --format "{{.Names}}" | Select-String $sidecarName
        
        if ($exists) {
            Write-Host "  Removing existing sidecar..." -ForegroundColor Yellow
            podman rm -f $sidecarName | Out-Null
        }
        
        # Start Dapr sidecar - use network mode to share network with app container
        $daprCmd = @(
            "run", "-d",
            "--name", $sidecarName,
            "--network", "container:$($svc.Container)",
            "-v", "${COMPONENTS_PATH}:/components",
            "-v", "${CONFIG_PATH}:/config",
            "daprio/dapr:$DAPR_VERSION",
            "./daprd",
            "--app-id", $svc.AppId,
            "--app-port", $svc.AppPort.ToString(),
            "--dapr-http-port", $svc.DaprHttpPort.ToString(),
            "--dapr-grpc-port", $svc.DaprGrpcPort.ToString(),
            "--components-path", "/components",
            "--config", "/config/config.yaml",
            "--log-level", "info"
        )
        
        try {
            $result = & podman $daprCmd 2>&1
            if ($LASTEXITCODE -eq 0) {
                Write-Host "  ✓ Dapr sidecar started for $($svc.Name)" -ForegroundColor Green
                Write-Host "    HTTP port: $($svc.DaprHttpPort)" -ForegroundColor Gray
                Write-Host "    gRPC port: $($svc.DaprGrpcPort)" -ForegroundColor Gray
            } else {
                Write-Host "  ✗ Failed to start Dapr sidecar: $result" -ForegroundColor Red
            }
        } catch {
            Write-Host "  ✗ Error starting Dapr sidecar: $_" -ForegroundColor Red
        }
    }
    
    Write-Host "`n=== Dapr Deployment Complete ===" -ForegroundColor Cyan
    Show-Status
}

function Stop-DaprSidecars {
    Write-Host "`n=== Stopping Dapr Sidecars ===" -ForegroundColor Cyan
    
    foreach ($svc in $services) {
        $sidecarName = "dapr-$($svc.Name)"
        Write-Host "Stopping $sidecarName..." -ForegroundColor Yellow
        podman stop $sidecarName 2>$null | Out-Null
        podman rm $sidecarName 2>$null | Out-Null
    }
    
    Write-Host "✓ All Dapr sidecars stopped" -ForegroundColor Green
}

function Show-Status {
    Write-Host "`n=== Deployment Status ===" -ForegroundColor Cyan
    
    Write-Host "`nInfrastructure Services:" -ForegroundColor Yellow
    $infraServices = @("go-nomads-redis", "go-nomads-zipkin")
    foreach ($svc in $infraServices) {
        $status = podman ps --filter "name=$svc" --format "{{.Status}}" 2>$null
        if ($status) {
            Write-Host "  ✓ $svc - $status" -ForegroundColor Green
        } else {
            Write-Host "  ✗ $svc - Not running" -ForegroundColor Red
        }
    }
    
    Write-Host "`nApplication Services:" -ForegroundColor Yellow
    foreach ($svc in $services) {
        $containerStatus = podman ps --filter "name=$($svc.Container)" --format "{{.Status}}" 2>$null
        $sidecarStatus = podman ps --filter "name=dapr-$($svc.Name)" --format "{{.Status}}" 2>$null
        
        if ($containerStatus) {
            Write-Host "  ✓ $($svc.Container) - $containerStatus" -ForegroundColor Green
        } else {
            Write-Host "  ✗ $($svc.Container) - Not running" -ForegroundColor Red
        }
        
        if ($sidecarStatus) {
            Write-Host "    ✓ Dapr sidecar - $sidecarStatus" -ForegroundColor Green
        } else {
            Write-Host "    ✗ Dapr sidecar - Not running" -ForegroundColor Red
        }
    }
    
    Write-Host "`nAccess Points:" -ForegroundColor Cyan
    Write-Host "  Gateway:         http://localhost:5000" -ForegroundColor White
    Write-Host "  Gateway Dapr:    http://localhost:3502" -ForegroundColor White
    Write-Host "  Product Service: http://localhost:5001" -ForegroundColor White
    Write-Host "  Product Dapr:    http://localhost:3500" -ForegroundColor White
    Write-Host "  User Service:    http://localhost:5002" -ForegroundColor White
    Write-Host "  User Dapr:       http://localhost:3501" -ForegroundColor White
    Write-Host "  Zipkin UI:       http://localhost:9411" -ForegroundColor White
    Write-Host "  Redis:           localhost:6379`n" -ForegroundColor White
}

# Main execution
switch ($Action) {
    "start" {
        Start-DaprSidecars
    }
    "stop" {
        Stop-DaprSidecars
    }
    "status" {
        Show-Status
    }
}
