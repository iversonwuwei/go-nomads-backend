# Go-Nomads Services Deployment (Local Build + Container)
param([switch]$SkipBuild, [switch]$Help)
$ErrorActionPreference = 'Stop'
$SCRIPT_DIR = Split-Path -Parent $MyInvocation.MyCommand.Path
$ROOT_DIR = Split-Path -Parent $SCRIPT_DIR
$NETWORK_NAME = "go-nomads-network"

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
$required = @("go-nomads-redis", "go-nomads-consul", "go-nomads-rabbitmq")
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
    @{Name="gateway"; Port=5000; DaprPort=3500; AppId="gateway"; Path="src/Gateway/Gateway"; Dll="Gateway.dll"; Container="go-nomads-gateway"},
    @{Name="product-service"; Port=5002; DaprPort=3501; AppId="product-service"; Path="src/Services/ProductService/ProductService"; Dll="ProductService.dll"; Container="go-nomads-product-service"},
    @{Name="user-service"; Port=5001; DaprPort=3502; AppId="user-service"; Path="src/Services/UserService/UserService"; Dll="UserService.dll"; Container="go-nomads-user-service"},
    @{Name="document-service"; Port=5003; DaprPort=3503; AppId="document-service"; Path="src/Services/DocumentService/DocumentService"; Dll="DocumentService.dll"; Container="go-nomads-document-service"},
    @{Name="city-service"; Port=8002; DaprPort=3504; AppId="city-service"; Path="src/Services/CityService/CityService"; Dll="CityService.dll"; Container="go-nomads-city-service"},
    @{Name="event-service"; Port=8005; DaprPort=3505; AppId="event-service"; Path="src/Services/EventService/EventService"; Dll="EventService.dll"; Container="go-nomads-event-service"},
    @{Name="coworking-service"; Port=8006; DaprPort=3506; AppId="coworking-service"; Path="src/Services/CoworkingService/CoworkingService"; Dll="CoworkingService.dll"; Container="go-nomads-coworking-service"},
    @{Name="ai-service"; Port=8009; DaprPort=3509; AppId="ai-service"; Path="src/Services/AIService/AIService"; Dll="AIService.dll"; Container="go-nomads-ai-service"},
    @{Name="cache-service"; Port=8010; DaprPort=3512; AppId="cache-service"; Path="src/Services/CacheService/CacheService"; Dll="CacheService.dll"; Container="go-nomads-cache-service"},
    @{Name="message-service"; Port=5005; DaprPort=3511; AppId="message-service"; Path="src/Services/MessageService/MessageService/API"; Dll="MessageService.dll"; Container="go-nomads-message-service"},
    @{Name="accommodation-service"; Port=8012; DaprPort=3513; AppId="accommodation-service"; Path="src/Services/AccommodationService/AccommodationService"; Dll="AccommodationService.dll"; Container="go-nomads-accommodation-service"},
    @{Name="innovation-service"; Port=8011; DaprPort=3514; AppId="innovation-service"; Path="src/Services/InnovationService/InnovationService"; Dll="InnovationService.dll"; Container="go-nomads-innovation-service"},
    @{Name="travel-planning-service"; Port=8007; DaprPort=3515; AppId="travel-planning-service"; Path="src/Services/TravelPlanningService/TravelPlanningService"; Dll="TravelPlanningService.dll"; Container="go-nomads-travel-planning-service"},
    @{Name="ecommerce-service"; Port=8008; DaprPort=3516; AppId="ecommerce-service"; Path="src/Services/EcommerceService/EcommerceService"; Dll="EcommerceService.dll"; Container="go-nomads-ecommerce-service"},
    @{Name="search-service"; Port=8015; DaprPort=3517; AppId="search-service"; Path="src/Services/SearchService/SearchService"; Dll="SearchService.dll"; Container="go-nomads-search-service"}
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
        
        # 同时删除对应的 Dapr sidecar
        $daprName = "$oldName-dapr"
        $daprExists = & $RUNTIME ps -a --filter "name=^${daprName}$" --format '{{.Names}}' 2>$null
        if ($daprExists) {
            & $RUNTIME stop $daprName 2>$null | Out-Null
            & $RUNTIME rm $daprName 2>$null | Out-Null
        }
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
    $dapr = "$container-dapr"
    
    Write-Host "`n------------------------------------------------------------" -ForegroundColor Cyan
    Write-Host "部署 $($svc.Name)" -ForegroundColor Cyan
    Write-Host "------------------------------------------------------------" -ForegroundColor Cyan
    
    # Remove existing containers if they exist
    $existing = & $RUNTIME ps -a --format '{{.Names}}' 2>$null
    if ($existing -match $dapr) { 
        Write-Host "  移除旧 Dapr sidecar..." -ForegroundColor Yellow
        & $RUNTIME stop $dapr 2>&1 | Out-Null
        & $RUNTIME rm -f $dapr 2>&1 | Out-Null 
    }
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
    
    # message-service 需要指定 ServiceAddress（它有自己的 Consul 注册逻辑）
    if ($svc.Name -eq "message-service") {
        $extraEnvArgs = @(
            "-e", "Consul__ServiceAddress=go-nomads-$($svc.Name)",
            "-e", "Consul__ServicePort=8080"
        )
    }

    # 启动应用容器
    Write-Host "  启动应用容器..." -ForegroundColor Yellow
    
    if ($svc.Name -eq "gateway") {
        # Gateway 容器配置（不需要 Dapr）
        $runArgs = @(
            "run", "-d",
            "--name", $container,
            "--network", $NETWORK_NAME,
            "--label", "com.docker.compose.project=go-nomads",
            "--label", "com.docker.compose.service=$($svc.Name)",
            "-p", "$($svc.Port):8080",
            "-e", "ASPNETCORE_URLS=http://+:8080",
            "-e", "ASPNETCORE_ENVIRONMENT=$aspnetEnv",
            "-e", "Consul__Address=http://go-nomads-consul:7500",
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
    } else {
        # 其他服务需要 Dapr 支持
        $runArgs = @(
            "run", "-d",
            "--name", $container,
            "--network", $NETWORK_NAME,
            "--label", "com.docker.compose.project=go-nomads",
            "--label", "com.docker.compose.service=$($svc.Name)",
            "-p", "$($svc.Port):8080",
            "-p", "$($svc.DaprPort):$($svc.DaprPort)",
            "-e", "ASPNETCORE_URLS=http://+:8080",
            "-e", "ASPNETCORE_ENVIRONMENT=$aspnetEnv",
            "-e", "DAPR_GRPC_PORT=50001",
            "-e", "DAPR_HTTP_PORT=$($svc.DaprPort)",
            "-e", "Consul__Address=http://go-nomads-consul:7500",
            "-e", "Consul__ServicePort=8080",
            "-e", "DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_HTTP2UNENCRYPTEDSUPPORT=true",
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
    }
    
    & $RUNTIME $runArgs | Out-Null
    
    if ($LASTEXITCODE -ne 0) { 
        Write-Host "  [错误] 应用容器启动失败" -ForegroundColor Red
        Write-Host "  查看日志: $RUNTIME logs $container" -ForegroundColor Yellow
        exit 1 
    }
    Write-Host "  应用容器启动成功!" -ForegroundColor Green
    
    Start-Sleep -Seconds 2
    
    # Gateway 不需要 Dapr sidecar（只使用 YARP 反向代理 + JWT 验证）
    if ($svc.Name -eq "gateway") {
        Write-Host "  跳过 Dapr sidecar (Gateway 不需要 Dapr)" -ForegroundColor Yellow
        Write-Host "  $($svc.Name) 部署成功!" -ForegroundColor Green
        Write-Host "  应用端口: http://localhost:$($svc.Port)" -ForegroundColor Green
        Start-Sleep -Seconds 2
        continue
    }
    
    # 启动 Dapr sidecar（共享应用容器的网络命名空间）
    # 使用 --network container:<app-container> 实现真正的 sidecar 模式
    # 应用和 Dapr 通过 localhost 通信，端口已在应用容器暴露
    Write-Host "  启动 Dapr sidecar (container sidecar 模式)..." -ForegroundColor Yellow
    
    & $RUNTIME run -d --name $dapr --network "container:$container" --label "com.docker.compose.project=go-nomads" --label "com.docker.compose.service=$($svc.Name)-dapr" daprio/daprd:latest ./daprd --app-id $svc.AppId --app-port 8080 --dapr-http-port $svc.DaprPort --dapr-grpc-port 50001 --log-level info | Out-Null
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  [错误] Dapr sidecar 启动失败" -ForegroundColor Red
        Write-Host "  查看日志: $RUNTIME logs $dapr" -ForegroundColor Yellow
        exit 1
    }
    
    Write-Host "  Dapr sidecar 启动成功!" -ForegroundColor Green
    Write-Host "  $($svc.Name) 部署成功!" -ForegroundColor Green
    Write-Host "  应用端口: http://localhost:$($svc.Port)" -ForegroundColor Green
    Write-Host "  Dapr HTTP: localhost:$($svc.DaprPort) (通过应用容器暴露)" -ForegroundColor Green
    Write-Host "  Dapr gRPC: localhost:50001 (container sidecar 模式)" -ForegroundColor Green
    
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
Write-Host "  Gateway:              http://localhost:5000" -ForegroundColor Green
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
Write-Host "  Travel Planning:      http://localhost:8007" -ForegroundColor Green
Write-Host "  Ecommerce Service:    http://localhost:8008" -ForegroundColor Green
Write-Host "  Search Service:       http://localhost:8015" -ForegroundColor Green
Write-Host "  Message Swagger:      http://localhost:5005/swagger" -ForegroundColor Green
Write-Host ""

Write-Host "Dapr 配置:" -ForegroundColor Cyan
Write-Host "  模式:              Container Sidecar (共享网络命名空间)" -ForegroundColor White
Write-Host "  gRPC 端口:         50001 (通过 DAPR_GRPC_PORT 环境变量)" -ForegroundColor White
Write-Host "  HTTP 端口:         3500-3516 (各服务独立端口)" -ForegroundColor White
Write-Host ""

Write-Host "基础设施:" -ForegroundColor Cyan
Write-Host "  Consul UI:         http://localhost:7500" -ForegroundColor White
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
