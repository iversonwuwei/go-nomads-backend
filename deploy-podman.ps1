# Go-Nomads Podman Deployment Script with Dapr
# This script deploys the microservices architecture using Podman

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("start", "stop", "restart", "build", "logs", "status", "clean")]
    [string]$Action = "start"
)

$ErrorActionPreference = "Stop"

# Configuration
$NetworkName = "go-nomads-network"
$ProjectRoot = $PSScriptRoot

# Service configurations
$Services = @{
    "redis" = @{
        Image = "redis:7-alpine"
        Container = "go-nomads-redis"
        Ports = @("6379:6379")
        HealthCheck = "redis-cli ping"
    }
    "zipkin" = @{
        Image = "openzipkin/zipkin:latest"
        Container = "go-nomads-zipkin"
        Ports = @("9411:9411")
    }
    "placement" = @{
        Image = "daprio/dapr:1.13.0"
        Container = "go-nomads-placement"
        Ports = @("50006:50006")
        Command = "./placement -port 50006"
    }
}

$AppServices = @{
    "product-service" = @{
        Container = "go-nomads-product-service"
        DaprContainer = "go-nomads-product-service-dapr"
        AppId = "product-service"
        AppPort = "8080"
        HttpPort = "3500"
        GrpcPort = "50001"
        Ports = @("5001:8080", "50001:50001")
        Dockerfile = "src/Services/ProductService/ProductService/Dockerfile"
    }
    "user-service" = @{
        Container = "go-nomads-user-service"
        DaprContainer = "go-nomads-user-service-dapr"
        AppId = "user-service"
        AppPort = "8080"
        HttpPort = "3501"
        GrpcPort = "50002"
        Ports = @("5002:8080", "50002:50002")
        Dockerfile = "src/Services/UserService/UserService/Dockerfile"
    }
    "gateway" = @{
        Container = "go-nomads-gateway"
        DaprContainer = "go-nomads-gateway-dapr"
        AppId = "gateway"
        AppPort = "8080"
        HttpPort = "3502"
        GrpcPort = "50003"
        Ports = @("5000:8080", "50003:50003")
        Dockerfile = "src/Gateway/Gateway/Dockerfile"
    }
}

function Write-Status {
    param([string]$Message, [string]$Type = "Info")
    
    $color = switch ($Type) {
        "Success" { "Green" }
        "Error" { "Red" }
        "Warning" { "Yellow" }
        default { "Cyan" }
    }
    
    Write-Host "[$Type] $Message" -ForegroundColor $color
}

function Test-PodmanInstalled {
    try {
        $null = podman --version
        return $true
    }
    catch {
        Write-Status "Podman is not installed or not in PATH" "Error"
        return $false
    }
}

function Initialize-Network {
    Write-Status "Checking network: $NetworkName"
    
    $networkExists = podman network ls --format "{{.Name}}" | Where-Object { $_ -eq $NetworkName }
    
    if (-not $networkExists) {
        Write-Status "Creating network: $NetworkName"
        podman network create $NetworkName
        Write-Status "Network created successfully" "Success"
    } else {
        Write-Status "Network already exists" "Info"
    }
}

function Start-InfrastructureServices {
    Write-Status "Starting infrastructure services..."
    
    foreach ($key in $Services.Keys) {
        $service = $Services[$key]
        Write-Status "Starting $key..."
        
        # Check if container exists
        $existing = podman ps -a --format "{{.Names}}" | Where-Object { $_ -eq $service.Container }
        
        if ($existing) {
            Write-Status "Removing existing container: $($service.Container)"
            podman rm -f $service.Container
        }
        
        # Build port mappings
        $portArgs = @()
        foreach ($port in $service.Ports) {
            $portArgs += "-p"
            $portArgs += $port
        }
        
        # Start container
        $cmd = @("run", "-d", "--name", $service.Container, "--network", $NetworkName) + $portArgs
        
        if ($service.Command) {
            $cmd += $service.Image
            $cmd += $service.Command.Split(" ")
        } else {
            $cmd += $service.Image
        }
        
        & podman $cmd
        Write-Status "$key started successfully" "Success"
    }
    
    # Wait for Redis to be ready
    Write-Status "Waiting for Redis to be ready..."
    Start-Sleep -Seconds 5
}

function Build-ApplicationServices {
    Write-Status "Building application images..."
    
    Set-Location $ProjectRoot
    
    foreach ($key in $AppServices.Keys) {
        $service = $AppServices[$key]
        $imageName = "go-nomads-$key"
        
        Write-Status "Building image: $imageName"
        
        podman build -t $imageName `
            -f $service.Dockerfile `
            .
        
        if ($LASTEXITCODE -eq 0) {
            Write-Status "Image $imageName built successfully" "Success"
        } else {
            Write-Status "Failed to build image: $imageName" "Error"
            throw "Build failed"
        }
    }
}

function Start-ApplicationServices {
    Write-Status "Starting application services..."
    
    foreach ($key in $AppServices.Keys) {
        $service = $AppServices[$key]
        $imageName = "go-nomads-$key"
        
        Write-Status "Starting $key..."
        
        # Remove existing containers
        podman rm -f $service.Container 2>$null
        podman rm -f $service.DaprContainer 2>$null
        
        # Build port mappings
        $portArgs = @()
        foreach ($port in $service.Ports) {
            $portArgs += "-p"
            $portArgs += $port
        }
        
        # Start application container
        $appCmd = @(
            "run", "-d",
            "--name", $service.Container,
            "--network", $NetworkName
        ) + $portArgs + @(
            "-e", "ASPNETCORE_ENVIRONMENT=Development",
            "-e", "ASPNETCORE_URLS=http://+:8080",
            $imageName
        )
        
        & podman $appCmd
        Write-Status "Application container started: $($service.Container)" "Success"
        
        # Start Dapr sidecar
        Start-Sleep -Seconds 2
        
        $daprCmd = @(
            "run", "-d",
            "--name", $service.DaprContainer,
            "--network", "container:$($service.Container)",
            "-v", "$ProjectRoot/deployment/dapr/components:/components:z",
            "-v", "$ProjectRoot/deployment/dapr/config:/config:z",
            "daprio/daprd:1.13.0",
            "./daprd",
            "-app-id", $service.AppId,
            "-app-port", $service.AppPort,
            "-dapr-http-port", $service.HttpPort,
            "-dapr-grpc-port", $service.GrpcPort,
            "-placement-host-address", "go-nomads-placement:50006",
            "-components-path", "/components",
            "-config", "/config/config.yaml"
        )
        
        & podman $daprCmd
        Write-Status "Dapr sidecar started: $($service.DaprContainer)" "Success"
    }
}

function Stop-AllServices {
    Write-Status "Stopping all services..."
    
    # Stop application services and their sidecars
    foreach ($key in $AppServices.Keys) {
        $service = $AppServices[$key]
        podman stop $service.DaprContainer 2>$null
        podman stop $service.Container 2>$null
        podman rm $service.DaprContainer 2>$null
        podman rm $service.Container 2>$null
    }
    
    # Stop infrastructure services
    foreach ($key in $Services.Keys) {
        $service = $Services[$key]
        podman stop $service.Container 2>$null
        podman rm $service.Container 2>$null
    }
    
    Write-Status "All services stopped" "Success"
}

function Show-Logs {
    param([string]$ServiceName = "all")
    
    if ($ServiceName -eq "all") {
        Write-Status "Available services:"
        foreach ($key in $Services.Keys) {
            Write-Host "  - $key" -ForegroundColor Yellow
        }
        foreach ($key in $AppServices.Keys) {
            Write-Host "  - $key" -ForegroundColor Yellow
            Write-Host "  - $key-dapr" -ForegroundColor Yellow
        }
    } else {
        $containerName = "go-nomads-$ServiceName"
        Write-Status "Showing logs for: $containerName"
        podman logs -f $containerName
    }
}

function Show-Status {
    Write-Status "Service Status:"
    Write-Host ""
    
    Write-Host "Infrastructure Services:" -ForegroundColor Cyan
    foreach ($key in $Services.Keys) {
        $service = $Services[$key]
        $status = podman ps --filter "name=$($service.Container)" --format "{{.Status}}"
        $color = if ($status) { "Green" } else { "Red" }
        $statusText = if ($status) { "Running ($status)" } else { "Stopped" }
        Write-Host "  [$key]" -ForegroundColor Yellow -NoNewline
        Write-Host " $statusText" -ForegroundColor $color
    }
    
    Write-Host ""
    Write-Host "Application Services:" -ForegroundColor Cyan
    foreach ($key in $AppServices.Keys) {
        $service = $AppServices[$key]
        
        $appStatus = podman ps --filter "name=$($service.Container)" --format "{{.Status}}"
        $daprStatus = podman ps --filter "name=$($service.DaprContainer)" --format "{{.Status}}"
        
        $appColor = if ($appStatus) { "Green" } else { "Red" }
        $daprColor = if ($daprStatus) { "Green" } else { "Red" }
        
        $appText = if ($appStatus) { "Running" } else { "Stopped" }
        $daprText = if ($daprStatus) { "Running" } else { "Stopped" }
        
        Write-Host "  [$key]" -ForegroundColor Yellow
        Write-Host "    App:  " -NoNewline; Write-Host $appText -ForegroundColor $appColor
        Write-Host "    Dapr: " -NoNewline; Write-Host $daprText -ForegroundColor $daprColor
    }
    
    Write-Host ""
    Write-Host "Endpoints:" -ForegroundColor Cyan
    Write-Host "  Gateway:         http://localhost:5000" -ForegroundColor White
    Write-Host "  Product Service: http://localhost:5001" -ForegroundColor White
    Write-Host "  User Service:    http://localhost:5002" -ForegroundColor White
    Write-Host "  Zipkin:          http://localhost:9411" -ForegroundColor White
    Write-Host "  Redis:           localhost:6379" -ForegroundColor White
}

function Clean-All {
    Write-Status "Cleaning up all resources..." "Warning"
    
    Stop-AllServices
    
    # Remove images
    Write-Status "Removing images..."
    foreach ($key in $AppServices.Keys) {
        podman rmi "go-nomads-$key" 2>$null
    }
    
    # Remove network
    Write-Status "Removing network..."
    podman network rm $NetworkName 2>$null
    
    Write-Status "Cleanup completed" "Success"
}

# Main execution
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   Go-Nomads Podman Deployment" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if (-not (Test-PodmanInstalled)) {
    exit 1
}

switch ($Action) {
    "start" {
        Initialize-Network
        Start-InfrastructureServices
        Build-ApplicationServices
        Start-ApplicationServices
        Write-Host ""
        Show-Status
        Write-Host ""
        Write-Status "Deployment completed successfully!" "Success"
    }
    "stop" {
        Stop-AllServices
    }
    "restart" {
        Stop-AllServices
        Start-Sleep -Seconds 2
        Initialize-Network
        Start-InfrastructureServices
        Start-ApplicationServices
        Show-Status
    }
    "build" {
        Build-ApplicationServices
    }
    "logs" {
        Show-Logs
    }
    "status" {
        Show-Status
    }
    "clean" {
        Clean-All
    }
}

Write-Host ""
