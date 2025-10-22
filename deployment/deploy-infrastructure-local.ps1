# Go-Nomads Local Infrastructure Deployment (Docker/Podman for Windows)
# Usage: .\deploy-infrastructure-local.ps1 [start|stop|restart|status|clean|help]

param(
    [Parameter(Position=0)]
    [ValidateSet('start', 'stop', 'restart', 'status', 'clean', 'help')]
    [string]$Action = 'start'
)

$ErrorActionPreference = 'Stop'

$NETWORK_NAME = "go-nomads-network"
$SCRIPT_DIR = Split-Path -Parent $MyInvocation.MyCommand.Path

# Detect container runtime
function Get-ContainerRuntime {
    if (Get-Command podman -ErrorAction SilentlyContinue) {
        return "podman"
    }
    elseif (Get-Command docker -ErrorAction SilentlyContinue) {
        return "docker"
    }
    else {
        Write-Error "Not found Docker or Podman. Please install container runtime first."
        exit 1
    }
}

$RUNTIME = Get-ContainerRuntime

function Write-Header {
    param([string]$Message)
    Write-Host ""
    Write-Host "============================================================" -ForegroundColor Cyan
    Write-Host "  $Message" -ForegroundColor Cyan
    Write-Host "============================================================" -ForegroundColor Cyan
    Write-Host ""
}

function Test-NetworkExists {
    if ($RUNTIME -eq "podman") {
        $result = & $RUNTIME network exists $NETWORK_NAME 2>$null
        return $LASTEXITCODE -eq 0
    }
    else {
        $networks = & $RUNTIME network ls --filter "name=$NETWORK_NAME" --format '{{.Name}}' 2>$null
        return $networks -contains $NETWORK_NAME
    }
}

function New-Network {
    Write-Header "Creating Docker Network"
    if (Test-NetworkExists) {
        Write-Host "Network '$NETWORK_NAME' already exists." -ForegroundColor Yellow
    }
    else {
        & $RUNTIME network create $NETWORK_NAME | Out-Null
        Write-Host "Network '$NETWORK_NAME' created successfully." -ForegroundColor Green
    }
}

function Test-ContainerExists {
    param([string]$Name)
    $containers = & $RUNTIME ps -a --filter "name=$Name" --format '{{.Names}}' 2>$null
    return $containers -contains $Name
}

function Test-ContainerRunning {
    param([string]$Name)
    $containers = & $RUNTIME ps --filter "name=$Name" --format '{{.Names}}' 2>$null
    return $containers -contains $Name
}

function Remove-Container {
    param([string]$Name)
    if (Test-ContainerExists $Name) {
        Write-Host "Removing container $Name..." -ForegroundColor Yellow
        & $RUNTIME rm -f $Name 2>&1 | Out-Null
    }
}

function Start-Redis {
    Write-Header "Deploying Redis"
    Remove-Container "go-nomads-redis"
    
    & $RUNTIME run -d `
        --name go-nomads-redis `
        --network $NETWORK_NAME `
        -p 6379:6379 `
        redis:7-alpine | Out-Null
    
    Write-Host "Redis running at: redis://localhost:6379" -ForegroundColor Green
}

function Start-PostgreSQL {
    Write-Header "Deploying PostgreSQL"
    Remove-Container "go-nomads-postgres"
    
    & $RUNTIME run -d `
        --name go-nomads-postgres `
        --network $NETWORK_NAME `
        -p 5432:5432 `
        -e POSTGRES_DB=postgres `
        -e POSTGRES_USER=postgres `
        -e POSTGRES_PASSWORD=Walden@19830527 `
        postgres:16-alpine | Out-Null
    
    Write-Host "PostgreSQL running at: localhost:5432" -ForegroundColor Green
    Write-Host "  Database: postgres" -ForegroundColor Gray
    Write-Host "  User: postgres" -ForegroundColor Gray
    Write-Host "  Password: Walden@19830527" -ForegroundColor Gray
}

function Start-Consul {
    Write-Header "Deploying Consul"
    Remove-Container "go-nomads-consul"
    
    $consulDir = Join-Path $SCRIPT_DIR "consul"
    New-Item -ItemType Directory -Path $consulDir -Force | Out-Null
    
    # Create Consul config file using PowerShell hashtable
    $consulConfig = @{
        datacenter = "dc1"
        server = $true
        bootstrap_expect = 1
        data_dir = "/consul/data"
        ui_config = @{ enabled = $true }
        log_level = "INFO"
        ports = @{
            http = 8500
            grpc = 8502
            dns = 8600
        }
        addresses = @{
            http = "0.0.0.0"
            grpc = "0.0.0.0"
            dns = "0.0.0.0"
        }
    }
    $consulConfigContent = $consulConfig | ConvertTo-Json -Depth 5 -Compress
    
    $consulConfigPath = Join-Path $consulDir "consul-local.json"
    [System.IO.File]::WriteAllText($consulConfigPath, $consulConfigContent)
    
    # For Docker on Windows, use the absolute path without conversion
    & $RUNTIME run -d `
        --name go-nomads-consul `
        --network $NETWORK_NAME `
        -p 8500:8500 `
        -p 8502:8502 `
        -p 8600:8600/udp `
        -v "${consulConfigPath}:/consul/config/consul.json:ro" `
        hashicorp/consul:latest agent -config-file /consul/config/consul.json | Out-Null
    
    Write-Host "Consul UI available at: http://localhost:8500" -ForegroundColor Green
}

function Start-Zipkin {
    Write-Header "Deploying Zipkin"
    Remove-Container "go-nomads-zipkin"
    
    & $RUNTIME run -d `
        --name go-nomads-zipkin `
        --network $NETWORK_NAME `
        -p 9411:9411 `
        openzipkin/zipkin:latest | Out-Null
    
    Write-Host "Zipkin UI available at: http://localhost:9411" -ForegroundColor Green
}

function Start-Prometheus {
    Write-Header "Deploying Prometheus"
    Remove-Container "go-nomads-prometheus"
    
    $promDir = Join-Path $SCRIPT_DIR "prometheus"
    New-Item -ItemType Directory -Path $promDir -Force | Out-Null
    
    # Create Prometheus config file using array of strings
    $promConfigLines = @(
        'global:',
        '  scrape_interval: 15s',
        '',
        'scrape_configs:',
        '  - job_name: ''prometheus''',
        '    static_configs:',
        '      - targets: [''localhost:9090'']',
        '  ',
        '  # Consul service discovery - auto discover all registered services',
        '  - job_name: ''consul-services''',
        '    metrics_path: /metrics',
        '    consul_sd_configs:',
        '      - server: ''go-nomads-consul:8500''',
        '        # Auto discover all services without specifying names',
        '    relabel_configs:',
        '      # Only scrape services with metrics_path metadata',
        '      - source_labels: [__meta_consul_service_metadata_metrics_path]',
        '        action: keep',
        '        regex: /.+',
        '      ',
        '      # Use custom metrics path if provided',
        '      - source_labels: [__meta_consul_service_metadata_metrics_path]',
        '        target_label: __metrics_path__',
        '        regex: (.+)',
        '        replacement: $1',
        '      ',
        '      # Service name label',
        '      - source_labels: [__meta_consul_service]',
        '        target_label: service',
        '      ',
        '      # Version label',
        '      - source_labels: [__meta_consul_service_metadata_version]',
        '        target_label: version',
        '      ',
        '      # Protocol label',
        '      - source_labels: [__meta_consul_service_metadata_protocol]',
        '        target_label: protocol',
        '      ',
        '      # Instance label',
        '      - source_labels: [__address__]',
        '        target_label: instance'
    )
    $promConfigContent = $promConfigLines -join "`n"
    
    $promConfigPath = Join-Path $promDir "prometheus-local.yml"
    [System.IO.File]::WriteAllText($promConfigPath, $promConfigContent)
    $promConfigPath = $promConfigPath -replace '\\', '/'
    
    & $RUNTIME run -d `
        --name go-nomads-prometheus `
        --network $NETWORK_NAME `
        -p 9090:9090 `
        -v "${promConfigPath}:/etc/prometheus/prometheus.yml:ro" `
        prom/prometheus:latest | Out-Null
    
    Write-Host "Prometheus UI available at: http://localhost:9090" -ForegroundColor Green
}

function Start-Grafana {
    Write-Header "Deploying Grafana"
    Remove-Container "go-nomads-grafana"
    
    $grafanaDir = Join-Path $SCRIPT_DIR "grafana"
    $grafanaProvisioningPath = Join-Path $grafanaDir "provisioning"
    $grafanaProvisioningPath = $grafanaProvisioningPath -replace '\\', '/'
    
    & $RUNTIME run -d `
        --name go-nomads-grafana `
        --network $NETWORK_NAME `
        -p 3000:3000 `
        -e GF_SECURITY_ADMIN_PASSWORD=admin `
        -v "${grafanaProvisioningPath}:/etc/grafana/provisioning:ro" `
        grafana/grafana:latest | Out-Null
    
    Write-Host "Grafana UI available at: http://localhost:3000 (admin/admin)" -ForegroundColor Green
}

function Start-Nginx {
    Write-Header "Deploying Nginx"
    Remove-Container "go-nomads-nginx"
    
    & $RUNTIME run -d `
        --name go-nomads-nginx `
        --network $NETWORK_NAME `
        -p 80:80 `
        -p 443:443 `
        nginx:latest | Out-Null
    
    Write-Host "Nginx running at: http://localhost" -ForegroundColor Green
    Write-Host "  Note: Mount custom nginx.conf using volumes if needed" -ForegroundColor Gray
}

function Start-Infrastructure {
    Write-Header "Starting Go-Nomads Infrastructure"
    
    New-Network
    Start-Redis
    Start-Consul
    Start-Zipkin
    Start-Prometheus
    Start-Grafana
    Start-Nginx
    
    Write-Host "`nAll infrastructure containers started!" -ForegroundColor Green
    Write-Host "`nAccess URLs:" -ForegroundColor Cyan
    Write-Host "  Nginx:       http://localhost" -ForegroundColor White
    Write-Host "  Redis:       redis://localhost:6379" -ForegroundColor White
    Write-Host "  Consul:      http://localhost:8500" -ForegroundColor White
    Write-Host "  Zipkin:      http://localhost:9411" -ForegroundColor White
    Write-Host "  Prometheus:  http://localhost:9090" -ForegroundColor White
    Write-Host "  Grafana:     http://localhost:3000 (admin/admin)" -ForegroundColor White
}

function Stop-Infrastructure {
    Write-Header "Stopping Infrastructure Containers"
    
    $containers = @(
        "go-nomads-nginx",
        "go-nomads-grafana",
        "go-nomads-prometheus",
        "go-nomads-zipkin",
        "go-nomads-consul",
        "go-nomads-redis"
    )
    
    foreach ($container in $containers) {
        if (Test-ContainerRunning $container) {
            Write-Host "Stopping $container..." -ForegroundColor Yellow
            & $RUNTIME stop $container 2>&1 | Out-Null
        }
    }
    
    Write-Host "All infrastructure containers stopped." -ForegroundColor Green
}

function Restart-Infrastructure {
    Stop-Infrastructure
    Start-Sleep -Seconds 2
    Start-Infrastructure
}

function Remove-Infrastructure {
    Write-Header "Cleaning Up Infrastructure"
    
    Stop-Infrastructure
    
    $containers = @(
        "go-nomads-nginx",
        "go-nomads-grafana",
        "go-nomads-prometheus",
        "go-nomads-zipkin",
        "go-nomads-consul",
        "go-nomads-redis"
    )
    
    foreach ($container in $containers) {
        Remove-Container $container
    }
    
    if (Test-NetworkExists) {
        Write-Host "Removing network $NETWORK_NAME..." -ForegroundColor Yellow
        & $RUNTIME network rm $NETWORK_NAME 2>&1 | Out-Null
    }
    
    $consulDir = Join-Path $SCRIPT_DIR "consul"
    $promDir = Join-Path $SCRIPT_DIR "prometheus"
    
    if (Test-Path $consulDir) {
        Write-Host "Removing Consul config directory..." -ForegroundColor Yellow
        Remove-Item -Path $consulDir -Recurse -Force
    }
    
    if (Test-Path $promDir) {
        Write-Host "Removing Prometheus config directory..." -ForegroundColor Yellow
        Remove-Item -Path $promDir -Recurse -Force
    }
    
    Write-Host "Cleanup complete." -ForegroundColor Green
}

function Show-Status {
    Write-Header "Infrastructure Status"
    
    Write-Host "`nInfrastructure containers status:" -ForegroundColor Cyan
    & $RUNTIME ps --filter "name=go-nomads" --format 'table {{.Names}}`t{{.Status}}`t{{.Ports}}'
    
    Write-Host "`nAccess URLs:" -ForegroundColor Cyan
    Write-Host "  Redis:       redis://localhost:6379" -ForegroundColor White
    Write-Host "  Consul:      http://localhost:8500" -ForegroundColor White
    Write-Host "  Zipkin:      http://localhost:9411" -ForegroundColor White
    Write-Host "  Prometheus:  http://localhost:9090" -ForegroundColor White
    Write-Host "  Grafana:     http://localhost:3000 (admin/admin)" -ForegroundColor White
}

function Show-Help {
    Write-Host ""
    Write-Host "Go-Nomads Local Infrastructure Deployment (Windows PowerShell)" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Usage:" -ForegroundColor Cyan
    Write-Host "  .\deploy-infrastructure-local.ps1 [command]" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Commands:" -ForegroundColor Cyan
    Write-Host "  start    Deploy all infrastructure containers (default)" -ForegroundColor Cyan
    Write-Host "  stop     Stop infrastructure containers" -ForegroundColor Cyan
    Write-Host "  restart  Restart infrastructure containers" -ForegroundColor Cyan
    Write-Host "  status   Show running status" -ForegroundColor Cyan
    Write-Host "  clean    Remove containers, network and config files" -ForegroundColor Cyan
    Write-Host "  help     Show this help message" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Cyan
    Write-Host "  .\deploy-infrastructure-local.ps1" -ForegroundColor Cyan
    Write-Host "  .\deploy-infrastructure-local.ps1 status" -ForegroundColor Cyan
    Write-Host "  .\deploy-infrastructure-local.ps1 clean" -ForegroundColor Cyan
    Write-Host ""
}

# Main logic
switch ($Action) {
    'start' { Start-Infrastructure }
    'stop' { Stop-Infrastructure }
    'restart' { Restart-Infrastructure }
    'status' { Show-Status }
    'clean' { Remove-Infrastructure }
    'help' { Show-Help }
}
