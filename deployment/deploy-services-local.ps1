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
    @{Name="gateway"; Port=5000; DaprPort=3500; AppId="gateway"; Path="src/Gateway/Gateway"; Dll="Gateway.dll"; Container="go-nomads-gateway"},
    @{Name="user-service"; Port=5001; DaprPort=3502; AppId="user-service"; Path="src/Services/UserService/UserService"; Dll="UserService.dll"; Container="go-nomads-user-service"},
    @{Name="product-service"; Port=5002; DaprPort=3501; AppId="product-service"; Path="src/Services/ProductService/ProductService"; Dll="ProductService.dll"; Container="go-nomads-product-service"},
    @{Name="document-service"; Port=5003; DaprPort=3503; AppId="document-service"; Path="src/Services/DocumentService/DocumentService"; Dll="DocumentService.dll"; Container="go-nomads-document-service"},
    @{Name="city-service"; Port=8002; DaprPort=3504; AppId="city-service"; Path="src/Services/CityService/CityService"; Dll="CityService.dll"; Container="go-nomads-city-service"},
    @{Name="event-service"; Port=8005; DaprPort=3505; AppId="event-service"; Path="src/Services/EventService/EventService"; Dll="EventService.dll"; Container="go-nomads-event-service"},
    @{Name="coworking-service"; Port=8006; DaprPort=3506; AppId="coworking-service"; Path="src/Services/CoworkingService/CoworkingService"; Dll="CoworkingService.dll"; Container="go-nomads-coworking-service"},
    @{Name="ai-service"; Port=8009; DaprPort=3509; AppId="ai-service"; Path="src/Services/AIService/AIService"; Dll="AIService.dll"; Container="go-nomads-ai-service"}
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

Write-Host "\nDeploying services..." -ForegroundColor Cyan

# 停止并删除旧容器（如果存在）
Write-Host "\nCleaning up old containers and images..." -ForegroundColor Yellow
$oldContainers = @("go-nomads-gateway", "go-nomads-user-service", "go-nomads-product-service", "go-nomads-document-service", "go-nomads-city-service", "go-nomads-event-service", "go-nomads-coworking-service", "go-nomads-ai-service")
foreach ($oldName in $oldContainers) {
    $exists = & $RUNTIME ps -a --filter "name=^${oldName}$" --format '{{.Names}}' 2>$null
    if ($exists) {
        Write-Host "  Stopping and removing container: $oldName" -ForegroundColor Yellow
        & $RUNTIME stop $oldName 2>$null | Out-Null
        & $RUNTIME rm $oldName 2>$null | Out-Null
        
        # 同时删除对应的 Dapr sidecar
        $daprName = "$oldName-dapr"
        $daprExists = & $RUNTIME ps -a --filter "name=^${daprName}$" --format '{{.Names}}' 2>$null
        if ($daprExists) {
            & $RUNTIME stop $daprName 2>$null | Out-Null
            & $RUNTIME rm $daprName 2>$null | Out-Null
        }
    }
    
    # 删除对应的镜像（如果存在）
    $imageName = $oldName
    $imageExists = & $RUNTIME images --filter "reference=${imageName}:latest" --format '{{.Repository}}' 2>$null
    if ($imageExists) {
        Write-Host "  Removing image: ${imageName}:latest" -ForegroundColor Yellow
        & $RUNTIME rmi -f "${imageName}:latest" 2>$null | Out-Null
    }
}

foreach ($svc in $services) {
    $container = $svc.Container
    $dapr = "$container-dapr"
    Write-Host "\nDeploying $($svc.Name)..."
    
    # Remove existing containers if they exist
    $existing = & $RUNTIME ps -a --format '{{.Names}}' 2>$null
    if ($existing -match $dapr) { 
        & $RUNTIME stop $dapr 2>&1 | Out-Null
        & $RUNTIME rm -f $dapr 2>&1 | Out-Null 
    }
    if ($existing -match $container) { 
        & $RUNTIME stop $container 2>&1 | Out-Null
        & $RUNTIME rm -f $container 2>&1 | Out-Null 
    }
    
    $publish = Join-Path $ROOT_DIR "$($svc.Path)/bin/Release/net9.0/publish"
    if (-not (Test-Path $publish)) {
        Write-Error "Publish folder not found: $publish"; exit 1
    }
    
    # Gateway 使用生产配置（不设置 Development 环境）以使用容器化 Consul 地址
    # 其他服务继续使用 Development 环境
    $aspnetEnv = if ($svc.Name -eq "gateway") { "Production" } else { "Development" }
    
    # 额外的环境变量（针对特定服务）
    $extraEnvArgs = @()
    if ($svc.Name -eq "document-service") {
        $extraEnvArgs = @(
            "-e", "Services__Gateway__Url=http://go-nomads-gateway:8080",
            "-e", "Services__Gateway__OpenApiUrl=http://go-nomads-gateway:8080/openapi/v1.json",
            "-e", "Services__ProductService__Url=http://go-nomads-product-service:8080",
            "-e", "Services__ProductService__OpenApiUrl=http://go-nomads-product-service:8080/openapi/v1.json",
            "-e", "Services__UserService__Url=http://go-nomads-user-service:8080",
            "-e", "Services__UserService__OpenApiUrl=http://go-nomads-user-service:8080/openapi/v1.json"
        )
    }
    
    # 启动应用容器（暴露应用端口和 Dapr HTTP 端口）
    # Dapr sidecar 将共享此容器的网络命名空间
    # 配置 Dapr gRPC: 通过环境变量 DAPR_GRPC_PORT 启用 gRPC 通信
    $runArgs = @(
        "run", "-d",
        "--name", $container,
        "--network", $NETWORK_NAME,
        "-p", "$($svc.Port):8080",
        "-p", "$($svc.DaprPort):$($svc.DaprPort)",
        "-e", "ASPNETCORE_URLS=http://+:8080",
        "-e", "ASPNETCORE_ENVIRONMENT=$aspnetEnv",
        "-e", "DAPR_GRPC_PORT=50001",
        "-e", "DAPR_HTTP_PORT=$($svc.DaprPort)",
        "-e", "Consul__Address=http://go-nomads-consul:8500",
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
    
    if ($LASTEXITCODE -ne 0) { Write-Error "Failed to start $container"; exit 1 }
    Write-Host "[OK] $($svc.Name) container started" -ForegroundColor Green
    
    Start-Sleep -Seconds 2
    
    # 启动 Dapr sidecar（共享应用容器的网络命名空间）
    # 使用 --network container:<app-container> 实现真正的 sidecar 模式
    # 应用和 Dapr 通过 localhost 通信，端口已在应用容器暴露
    & $RUNTIME run -d --name $dapr --network "container:$container" daprio/daprd:latest ./daprd --app-id $svc.AppId --app-port 8080 --dapr-http-port $svc.DaprPort --dapr-grpc-port 50001 --log-level info | Out-Null
    
    Write-Host "[OK] $($svc.Name) deployed at http://localhost:$($svc.Port)" -ForegroundColor Green
}

Write-Host "\n============================================================" -ForegroundColor Green
Write-Host "All services deployed!" -ForegroundColor Green
Write-Host "============================================================\n" -ForegroundColor Green

Write-Host "Service access URLs:" -ForegroundColor Cyan
Write-Host "  Gateway:           http://localhost:5000" -ForegroundColor Green
Write-Host "  User Service:      http://localhost:5001" -ForegroundColor Green
Write-Host "  Product Service:   http://localhost:5002" -ForegroundColor Green
Write-Host "  Document Service:  http://localhost:5003" -ForegroundColor Green
Write-Host "  City Service:      http://localhost:8002" -ForegroundColor Green
Write-Host "  Event Service:     http://localhost:8005" -ForegroundColor Green
Write-Host "  Coworking Service: http://localhost:8006" -ForegroundColor Green

Write-Host "\nDapr Configuration:" -ForegroundColor Cyan
Write-Host "  Mode:              Container Sidecar (shared network namespace)" -ForegroundColor White
Write-Host "  gRPC Port:         50001 (via DAPR_GRPC_PORT env)" -ForegroundColor White
Write-Host "  HTTP Ports:        3500-3506 (per service)" -ForegroundColor White

Write-Host "\nInfrastructure:" -ForegroundColor Cyan
Write-Host "  Consul UI:         http://localhost:8500" -ForegroundColor White

Write-Host "\nCommon Commands:" -ForegroundColor Cyan
Write-Host "  View containers:   $RUNTIME ps" -ForegroundColor Yellow
Write-Host "  View logs:         $RUNTIME logs go-nomads-gateway" -ForegroundColor Yellow

Write-Host "\nContainer Status:" -ForegroundColor Cyan
& $RUNTIME ps --filter "name=go-nomads" --format 'table {{.Names}}`t{{.Status}}`t{{.Ports}}'

Write-Host "\nDeployment complete! 🚀" -ForegroundColor Green
