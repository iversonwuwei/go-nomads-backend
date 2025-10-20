# Go-Nomads Services Deployment (Local Build + Container)
param([switch]$SkipBuild, [switch]$Help)
$ErrorActionPreference = 'Stop'
$SCRIPT_DIR = Split-Path -Parent $MyInvocation.MyCommand.Path
$ROOT_DIR = Split-Path -Parent $SCRIPT_DIR
$NETWORK_NAME = "go-nomads-network"

function Get-Runtime {
    if (Get-Command podman -EA SilentlyContinue) {
        $test = podman ps -a --filter "name=go-nomads-redis" --format '{{.Names}}' 2>$null
        if ($test -eq "go-nomads-redis") { return "podman" }
    }
    if (Get-Command docker -EA SilentlyContinue) {
        $test = docker ps -a --filter "name=go-nomads-redis" --format '{{.Names}}' 2>$null
        if ($test -eq "go-nomads-redis") { return "docker" }
    }
    if (Get-Command podman -EA SilentlyContinue) { return "podman" }
    if (Get-Command docker -EA SilentlyContinue) { return "docker" }
    Write-Error "Docker or Podman not found"; exit 1
}

$RUNTIME = Get-Runtime
Write-Host "Using: $RUNTIME" -ForegroundColor Green

if ($Help) {
    Write-Host "`nUsage: .\deploy-services-local.ps1 [-SkipBuild] [-Help]`n"
    exit 0
}

Write-Host "`nChecking prerequisites..." -ForegroundColor Cyan
$required = @("go-nomads-redis", "go-nomads-consul")
foreach ($svc in $required) {
    $running = & $RUNTIME ps --filter "name=$svc" --filter "status=running" --format '{{.Names}}' 2>$null
    if ($running -ne $svc) {
        Write-Host "[ERROR] $svc not running. Please run: .\deploy-infrastructure-local.ps1" -ForegroundColor Red
        exit 1
    }
    Write-Host "[OK] $svc running" -ForegroundColor Green
}

$services = @(
    @{Name="gateway"; Port=8080; DaprPort=3500; AppId="gateway"; Path="src/Gateway/Gateway"},
    @{Name="product"; Port=5001; DaprPort=3501; AppId="product-service"; Path="src/Services/ProductService/ProductService"},
    @{Name="user"; Port=5002; DaprPort=3502; AppId="user-service"; Path="src/Services/UserService/UserService"},
    @{Name="document"; Port=5003; DaprPort=3503; AppId="document-service"; Path="src/Services/DocumentService/DocumentService"}
)

if (-not $SkipBuild) {
    Write-Host "`nBuilding services..." -ForegroundColor Cyan
    foreach ($svc in $services) {
        $proj = Join-Path $ROOT_DIR $svc.Path
        Write-Host "Building $($svc.Name)..."
        Push-Location $proj
        dotnet publish -c Release --no-self-contained 2>&1 | Out-Null
        if ($LASTEXITCODE -ne 0) { Write-Error "Build failed"; exit 1 }
        Pop-Location
        Write-Host "[OK] $($svc.Name)" -ForegroundColor Green
    }
}

Write-Host "`nDeploying services..." -ForegroundColor Cyan
foreach ($svc in $services) {
    $container = "go-nomads-$($svc.Name)"
    $dapr = "$container-dapr"
    Write-Host "`nDeploying $($svc.Name)..."
    
    # Remove existing containers if they exist
    $existing = & $RUNTIME ps -a --format '{{.Names}}' 2>$null
    if ($existing -match $dapr) { & $RUNTIME rm -f $dapr 2>&1 | Out-Null }
    if ($existing -match $container) { & $RUNTIME rm -f $container 2>&1 | Out-Null }
    
    $publish = Join-Path $ROOT_DIR "$($svc.Path)/bin/Release/net9.0/publish"
    if (-not (Test-Path $publish)) {
        Write-Error "Publish folder not found: $publish"; exit 1
    }
    
    & $RUNTIME run -d --name $container --network $NETWORK_NAME -p "$($svc.Port):8080" -e ASPNETCORE_URLS="http://+:8080" -e ASPNETCORE_ENVIRONMENT=Development -e CONSUL_HTTP_ADDR="http://localhost:8500" -v "${publish}:/app:ro" -w /app mcr.microsoft.com/dotnet/aspnet:9.0 dotnet "$container.dll" | Out-Null
    
    if ($LASTEXITCODE -ne 0) { Write-Error "Failed to start $container"; exit 1 }
    Write-Host "[OK] $($svc.Name) container started" -ForegroundColor Green
    
    Start-Sleep -Seconds 2
    
    & $RUNTIME run -d --name $dapr --network $NETWORK_NAME -p "$($svc.DaprPort):$($svc.DaprPort)" daprio/daprd:latest ./daprd --app-id $svc.AppId --app-port 8080 --dapr-http-port $svc.DaprPort --dapr-grpc-port 50001 --log-level info | Out-Null
    
    Write-Host "[OK] $($svc.Name) deployed at http://localhost:$($svc.Port)" -ForegroundColor Green
}

Write-Host "`n============================================================" -ForegroundColor Green
Write-Host "All services deployed!" -ForegroundColor Green
Write-Host "============================================================`n" -ForegroundColor Green
& $RUNTIME ps --filter "name=go-nomads" --format 'table {{.Names}}`t{{.Status}}`t{{.Ports}}'
