#!/usr/bin/env pwsh
# Go-Nomads Infrastructure Deployment Script
# Supports: Windows (PowerShell/Podman), Linux (Bash/Docker/Podman)
# Usage: pwsh deploy-infrastructure.ps1 [start|stop|restart|status|clean]

param(
    [Parameter(Position=0)]
    [ValidateSet('start', 'stop', 'restart', 'status', 'clean', 'help')]
    [string]$Action = 'start'
)

# Configuration
$NETWORK_NAME = "go-nomads-network"
$REDIS_CONFIG_COUNT = 21

# Detect container runtime
$CONTAINER_RUNTIME = ""
if (Get-Command podman -ErrorAction SilentlyContinue) {
    $CONTAINER_RUNTIME = "podman"
} elseif (Get-Command docker -ErrorAction SilentlyContinue) {
    $CONTAINER_RUNTIME = "docker"
} else {
    Write-Host "[ERROR] Neither Docker nor Podman found. Please install one of them." -ForegroundColor Red
    exit 1
}

Write-Host "Using container runtime: $CONTAINER_RUNTIME" -ForegroundColor Cyan

# Helper Functions
function Show-Header {
    param([string]$Title)
    Write-Host ""
    Write-Host "============================================================" -ForegroundColor Cyan
    Write-Host "  $Title" -ForegroundColor White
    Write-Host "============================================================" -ForegroundColor Cyan
    Write-Host ""
}

function Test-ContainerRunning {
    param([string]$Name)
    $result = & $CONTAINER_RUNTIME ps --filter "name=$Name" --format "{{.Names}}" 2>$null
    return $result -eq $Name
}

function Remove-ContainerIfExists {
    param([string]$Name)
    $exists = & $CONTAINER_RUNTIME ps -a --filter "name=$Name" --format "{{.Names}}" 2>$null
    if ($exists -eq $Name) {
        Write-Host "  Removing existing container: $Name" -ForegroundColor Yellow
        & $CONTAINER_RUNTIME rm -f $Name | Out-Null
    }
}

function Wait-ForService {
    param(
        [string]$Name,
        [string]$Url,
        [int]$MaxAttempts = 30
    )
    Write-Host "  Waiting for $Name to be ready..." -ForegroundColor Yellow
    for ($i = 1; $i -le $MaxAttempts; $i++) {
        try {
            $response = Invoke-WebRequest -Uri $Url -TimeoutSec 2 -ErrorAction Stop
            Write-Host "  $Name is ready!" -ForegroundColor Green
            return $true
        } catch {
            Write-Host "    Attempt $i/$MaxAttempts..." -ForegroundColor Gray
            Start-Sleep -Seconds 2
        }
    }
    Write-Host "  [WARNING] $Name did not become ready in time" -ForegroundColor Yellow
    return $false
}

# Create Network
function Initialize-Network {
    Show-Header "Creating Network"
    
    $networkExists = & $CONTAINER_RUNTIME network ls --filter "name=$NETWORK_NAME" --format "{{.Name}}" 2>$null
    if ($networkExists -eq $NETWORK_NAME) {
        Write-Host "  Network '$NETWORK_NAME' already exists" -ForegroundColor Green
    } else {
        Write-Host "  Creating network '$NETWORK_NAME'..." -ForegroundColor Yellow
        & $CONTAINER_RUNTIME network create $NETWORK_NAME | Out-Null
        Write-Host "  Network created successfully!" -ForegroundColor Green
    }
}

# Deploy Redis
function Deploy-Redis {
    Show-Header "Deploying Redis"
    
    Remove-ContainerIfExists "go-nomads-redis"
    
    Write-Host "  Starting Redis container..." -ForegroundColor Yellow
    & $CONTAINER_RUNTIME run -d `
        --name go-nomads-redis `
        --network $NETWORK_NAME `
        -p 6379:6379 `
        redis:latest | Out-Null
    
    if (Test-ContainerRunning "go-nomads-redis") {
        Write-Host "  Redis deployed successfully!" -ForegroundColor Green
        Start-Sleep -Seconds 3
        Import-RedisConfig
    } else {
        Write-Host "  [ERROR] Failed to start Redis" -ForegroundColor Red
        exit 1
    }
}

# Import Redis Configuration
function Import-RedisConfig {
    Write-Host "  Importing configuration to Redis..." -ForegroundColor Yellow
    
    $configs = @{
        "go-nomads:config:app-version" = "1.0.0"
        "go-nomads:config:environment" = "development"
        "go-nomads:config:gateway:endpoint" = "http://go-nomads-gateway:8080"
        "go-nomads:config:product-service:endpoint" = "http://go-nomads-product-service:8080"
        "go-nomads:config:user-service:endpoint" = "http://go-nomads-user-service:8080"
        "go-nomads:config:redis:endpoint" = "go-nomads-redis:6379"
        "go-nomads:config:consul:endpoint" = "http://go-nomads-consul:8500"
        "go-nomads:config:zipkin:endpoint" = "http://go-nomads-zipkin:9411"
        "go-nomads:config:prometheus:endpoint" = "http://go-nomads-prometheus:9090"
        "go-nomads:config:grafana:endpoint" = "http://go-nomads-grafana:3000"
        "go-nomads:config:logging:level" = "info"
        "go-nomads:config:logging:format" = "json"
        "go-nomads:config:tracing:enabled" = "true"
        "go-nomads:config:tracing:sample-rate" = "1.0"
        "go-nomads:config:metrics:enabled" = "true"
        "go-nomads:config:features:user-registration" = "true"
        "go-nomads:config:features:product-reviews" = "true"
        "go-nomads:config:features:recommendations" = "false"
        "go-nomads:config:business:max-cart-items" = "50"
        "go-nomads:config:business:session-timeout" = "3600"
        "go-nomads:config:business:price-precision" = "2"
    }
    
    $importCount = 0
    foreach ($key in $configs.Keys) {
        & $CONTAINER_RUNTIME exec go-nomads-redis redis-cli SET $key $configs[$key] | Out-Null
        $importCount++
    }
    
    Write-Host "  Imported $importCount configuration items" -ForegroundColor Green
}

# Deploy Consul
function Deploy-Consul {
    Show-Header "Deploying Consul"
    
    Remove-ContainerIfExists "go-nomads-consul"
    
    # Create Consul config directory
    $consulConfigDir = Join-Path $PSScriptRoot "consul"
    if (-not (Test-Path $consulConfigDir)) {
        New-Item -ItemType Directory -Path $consulConfigDir | Out-Null
    }
    
    # Create Consul configuration
    $consulConfig = @"
{
  "datacenter": "dc1",
  "data_dir": "/consul/data",
  "log_level": "INFO",
  "server": true,
  "ui_config": {
    "enabled": true
  },
  "ports": {
    "http": 8500,
    "dns": 8600,
    "grpc": 8502
  },
  "addresses": {
    "http": "0.0.0.0",
    "dns": "0.0.0.0",
    "grpc": "0.0.0.0"
  },
  "client_addr": "0.0.0.0",
  "bootstrap_expect": 1
}
"@
    $consulConfig | Out-File -FilePath (Join-Path $consulConfigDir "consul-config.json") -Encoding utf8 -Force
    
    Write-Host "  Starting Consul container..." -ForegroundColor Yellow
    & $CONTAINER_RUNTIME run -d `
        --name go-nomads-consul `
        --network $NETWORK_NAME `
        -p 8500:8500 `
        -p 8502:8502 `
        -p 8600:8600/udp `
        -e CONSUL_BIND_INTERFACE=eth0 `
        consul:latest agent -server -ui -bootstrap-expect=1 '-client=0.0.0.0' | Out-Null
    
    if (Test-ContainerRunning "go-nomads-consul") {
        Write-Host "  Consul deployed successfully!" -ForegroundColor Green
        Wait-ForService "Consul" "http://localhost:8500/v1/status/leader" | Out-Null
        Register-ConsulServices
    } else {
        Write-Host "  [ERROR] Failed to start Consul" -ForegroundColor Red
        exit 1
    }
}

# Register Services to Consul
function Register-ConsulServices {
    Write-Host "  Registering services to Consul..." -ForegroundColor Yellow
    
    $services = @(
        @{name="gateway"; address="go-nomads-gateway"; port=8080},
        @{name="product-service"; address="go-nomads-product-service"; port=8080},
        @{name="user-service"; address="go-nomads-user-service"; port=8080}
    )
    
    Start-Sleep -Seconds 3
    
    foreach ($svc in $services) {
        & $CONTAINER_RUNTIME exec go-nomads-consul consul services register `
            -name=$($svc.name) `
            -address=$($svc.address) `
            -port=$($svc.port) `
            -tag=dapr 2>&1 | Out-Null
    }
    
    Write-Host "  Registered $($services.Count) services to Consul" -ForegroundColor Green
}

# Deploy Zipkin
function Deploy-Zipkin {
    Show-Header "Deploying Zipkin"
    
    Remove-ContainerIfExists "go-nomads-zipkin"
    
    Write-Host "  Starting Zipkin container..." -ForegroundColor Yellow
    & $CONTAINER_RUNTIME run -d `
        --name go-nomads-zipkin `
        --network $NETWORK_NAME `
        -p 9411:9411 `
        openzipkin/zipkin:latest | Out-Null
    
    if (Test-ContainerRunning "go-nomads-zipkin") {
        Write-Host "  Zipkin deployed successfully!" -ForegroundColor Green
    } else {
        Write-Host "  [ERROR] Failed to start Zipkin" -ForegroundColor Red
        exit 1
    }
}

# Deploy Prometheus
function Deploy-Prometheus {
    Show-Header "Deploying Prometheus"
    
    Remove-ContainerIfExists "go-nomads-prometheus"
    
    # Create Prometheus config directory
    $prometheusConfigDir = Join-Path $PSScriptRoot "prometheus"
    if (-not (Test-Path $prometheusConfigDir)) {
        New-Item -ItemType Directory -Path $prometheusConfigDir | Out-Null
    }
    
    # Create Prometheus configuration
    $prometheusConfig = @"
global:
  scrape_interval: 15s
  evaluation_interval: 15s
  external_labels:
    cluster: 'go-nomads'
    environment: 'development'

scrape_configs:
  - job_name: 'prometheus'
    static_configs:
      - targets: ['localhost:9090']
        labels:
          service: 'prometheus'

  - job_name: 'dapr-services'
    metrics_path: '/metrics'
    consul_sd_configs:
      - server: 'go-nomads-consul:8500'
        services: ['product-service', 'user-service', 'gateway']
        tags: ['dapr']
    relabel_configs:
      - source_labels: [__address__]
        regex: '([^:]+):.*'
        replacement: '$${1}:9090'
        target_label: __address__
      - source_labels: [__meta_consul_service]
        target_label: app_id
      - replacement: 'dapr'
        target_label: service_type

  - job_name: 'app-services'
    metrics_path: '/metrics'
    consul_sd_configs:
      - server: 'go-nomads-consul:8500'
        services: ['product-service', 'user-service', 'gateway']
        tags: ['dapr']
    relabel_configs:
      - source_labels: [__address__]
        regex: '([^:]+):.*'
        replacement: '$${1}:8080'
        target_label: __address__
      - source_labels: [__meta_consul_service]
        target_label: app
      - replacement: 'application'
        target_label: service_type

  - job_name: 'redis'
    static_configs:
      - targets: ['go-nomads-redis:6379']
        labels:
          service: 'redis'

  - job_name: 'zipkin'
    static_configs:
      - targets: ['go-nomads-zipkin:9411']
        labels:
          service: 'zipkin'
"@
    $prometheusConfig | Out-File -FilePath (Join-Path $prometheusConfigDir "prometheus.yml") -Encoding utf8 -Force
    
    Write-Host "  Starting Prometheus container..." -ForegroundColor Yellow
    & $CONTAINER_RUNTIME run -d `
        --name go-nomads-prometheus `
        --network $NETWORK_NAME `
        -p 9090:9090 `
        -v "${prometheusConfigDir}:/etc/prometheus:Z" `
        prom/prometheus:latest `
        --config.file=/etc/prometheus/prometheus.yml | Out-Null
    
    if (Test-ContainerRunning "go-nomads-prometheus") {
        Write-Host "  Prometheus deployed successfully!" -ForegroundColor Green
    } else {
        Write-Host "  [ERROR] Failed to start Prometheus" -ForegroundColor Red
        exit 1
    }
}

# Deploy Grafana
function Deploy-Grafana {
    Show-Header "Deploying Grafana"
    
    Remove-ContainerIfExists "go-nomads-grafana"
    
    Write-Host "  Starting Grafana container..." -ForegroundColor Yellow
    & $CONTAINER_RUNTIME run -d `
        --name go-nomads-grafana `
        --network $NETWORK_NAME `
        -p 3000:3000 `
        -e GF_SECURITY_ADMIN_PASSWORD=admin `
        grafana/grafana:latest | Out-Null
    
    if (Test-ContainerRunning "go-nomads-grafana") {
        Write-Host "  Grafana deployed successfully!" -ForegroundColor Green
    } else {
        Write-Host "  [ERROR] Failed to start Grafana" -ForegroundColor Red
        exit 1
    }
}

# Deploy Nginx
function Deploy-Nginx {
    Show-Header "Deploying Nginx"
    
    Remove-ContainerIfExists "go-nomads-nginx"
    
    Write-Host "  Starting Nginx container..." -ForegroundColor Yellow
    & $CONTAINER_RUNTIME run -d `
        --name go-nomads-nginx `
        --network $NETWORK_NAME `
        -p 80:80 `
        -p 443:443 `
        nginx:latest | Out-Null
    
    if (Test-ContainerRunning "go-nomads-nginx") {
        Write-Host "  Nginx deployed successfully!" -ForegroundColor Green
        Write-Host "  Note: You can mount custom nginx.conf using volumes" -ForegroundColor Cyan
    } else {
        Write-Host "  [ERROR] Failed to start Nginx" -ForegroundColor Red
        exit 1
    }
}

# Show Status
function Show-Status {
    Show-Header "Infrastructure Status"
    
    Write-Host "Container Status:" -ForegroundColor Yellow
    & $CONTAINER_RUNTIME ps --filter "name=go-nomads" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
    Write-Host ""
    
    if (Test-ContainerRunning "go-nomads-consul") {
        Write-Host "Consul Services:" -ForegroundColor Yellow
        & $CONTAINER_RUNTIME exec go-nomads-consul consul catalog services 2>&1 | ForEach-Object {
            Write-Host "  - $_" -ForegroundColor Cyan
        }
        Write-Host ""
    }
    
    if (Test-ContainerRunning "go-nomads-redis") {
        Write-Host "Redis Configuration:" -ForegroundColor Yellow
        $configCount = (& $CONTAINER_RUNTIME exec go-nomads-redis redis-cli KEYS "go-nomads:config:*" 2>&1 | Measure-Object).Count
        Write-Host "  - Config Items: $configCount" -ForegroundColor Cyan
        Write-Host ""
    }
    
    Write-Host "Access URLs:" -ForegroundColor Yellow
    Write-Host "  - Nginx:        http://localhost" -ForegroundColor Cyan
    Write-Host "  - Consul UI:    http://localhost:8500" -ForegroundColor Cyan
    Write-Host "  - Prometheus:   http://localhost:9090" -ForegroundColor Cyan
    Write-Host "  - Grafana:      http://localhost:3000 (admin/admin)" -ForegroundColor Cyan
    Write-Host "  - Zipkin:       http://localhost:9411" -ForegroundColor Cyan
    Write-Host ""
}

# Stop All Containers
function Stop-Infrastructure {
    Show-Header "Stopping Infrastructure"
    
    $containers = @(
        "go-nomads-grafana",
        "go-nomads-prometheus",
        "go-nomads-zipkin",
        "go-nomads-consul",
        "go-nomads-redis"
    )
    
    foreach ($container in $containers) {
        if (Test-ContainerRunning $container) {
            Write-Host "  Stopping $container..." -ForegroundColor Yellow
            & $CONTAINER_RUNTIME stop $container | Out-Null
        }
    }
    
    Write-Host ""
    Write-Host "All infrastructure containers stopped!" -ForegroundColor Green
}

# Clean All Resources
function Clean-Infrastructure {
    Show-Header "Cleaning Infrastructure"
    
    $containers = @(
        "go-nomads-grafana",
        "go-nomads-prometheus",
        "go-nomads-zipkin",
        "go-nomads-consul",
        "go-nomads-redis"
    )
    
    foreach ($container in $containers) {
        Remove-ContainerIfExists $container
    }
    
    Write-Host "  Removing network..." -ForegroundColor Yellow
    & $CONTAINER_RUNTIME network rm $NETWORK_NAME 2>&1 | Out-Null
    
    Write-Host "  Removing configuration directories..." -ForegroundColor Yellow
    $consulDir = Join-Path $PSScriptRoot "consul"
    $prometheusDir = Join-Path $PSScriptRoot "prometheus"
    
    if (Test-Path $consulDir) {
        Remove-Item -Path $consulDir -Recurse -Force
    }
    if (Test-Path $prometheusDir) {
        Remove-Item -Path $prometheusDir -Recurse -Force
    }
    
    Write-Host ""
    Write-Host "All infrastructure resources cleaned!" -ForegroundColor Green
}

# Show Help
function Show-Help {
    Write-Host ""
    Write-Host "Go-Nomads Infrastructure Deployment" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Usage:" -ForegroundColor Yellow
    Write-Host "  pwsh deploy-infrastructure.ps1 [command]" -ForegroundColor White
    Write-Host ""
    Write-Host "Commands:" -ForegroundColor Yellow
    Write-Host "  start    - Deploy all infrastructure components (default)" -ForegroundColor White
    Write-Host "  stop     - Stop all infrastructure containers" -ForegroundColor White
    Write-Host "  restart  - Restart all infrastructure containers" -ForegroundColor White
    Write-Host "  status   - Show current infrastructure status" -ForegroundColor White
    Write-Host "  clean    - Remove all infrastructure resources" -ForegroundColor White
    Write-Host "  help     - Show this help message" -ForegroundColor White
    Write-Host ""
    Write-Host "Infrastructure Components:" -ForegroundColor Yellow
    Write-Host "  - Redis (Configuration & State Store)" -ForegroundColor White
    Write-Host "  - Consul (Service Registry)" -ForegroundColor White
    Write-Host "  - Zipkin (Distributed Tracing)" -ForegroundColor White
    Write-Host "  - Prometheus (Metrics Collection)" -ForegroundColor White
    Write-Host "  - Grafana (Metrics Visualization)" -ForegroundColor White
    Write-Host ""
}

# Main Execution
switch ($Action) {
    'start' {
        Show-Header "Go-Nomads Infrastructure Deployment"
        Initialize-Network
        Deploy-Redis
        Deploy-Consul
        Deploy-Zipkin
        Deploy-Prometheus
        Deploy-Grafana
        Deploy-Nginx
        Show-Status
        
        Write-Host ""
        Write-Host "============================================================" -ForegroundColor Green
        Write-Host "  Infrastructure Deployment Completed Successfully!" -ForegroundColor Green
        Write-Host "============================================================" -ForegroundColor Green
        Write-Host ""
    }
    
    'stop' {
        Stop-Infrastructure
    }
    
    'restart' {
        Stop-Infrastructure
        Start-Sleep -Seconds 2
        & $PSCommandPath start
    }
    
    'status' {
        Show-Status
    }
    
    'clean' {
        Write-Host ""
        Write-Host "[WARNING] This will remove all infrastructure containers and data!" -ForegroundColor Yellow
        $confirm = Read-Host "Are you sure? (yes/no)"
        if ($confirm -eq 'yes') {
            Clean-Infrastructure
        } else {
            Write-Host "Operation cancelled." -ForegroundColor Cyan
        }
    }
    
    'help' {
        Show-Help
    }
}
