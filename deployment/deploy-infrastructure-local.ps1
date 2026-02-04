# Go-Nomads Local Infrastructure Deployment (Docker only)
# Usage: .\deploy-infrastructure-local.ps1 [start|stop|restart|status|clean|help]

param(
    [Parameter(Position=0)]
    [ValidateSet('start', 'stop', 'restart', 'status', 'clean', 'help')]
    [string]$Action = 'start'
)

$ErrorActionPreference = 'Stop'

$NETWORK_NAME = "go-nomads-network"
$SCRIPT_DIR = Split-Path -Parent $MyInvocation.MyCommand.Path

# Require Docker
function Require-Docker {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        Write-Error "[ERROR] Docker is required for this script."
        exit 1
    }
}

$RUNTIME = "docker"

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
        --label "com.docker.compose.project=go-nomads-infras" `
        --label "com.docker.compose.service=redis" `
        -p 6379:6379 `
        redis:latest redis-server --appendonly yes | Out-Null
    
    Write-Host "Redis running at: redis://localhost:6379" -ForegroundColor Green
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
            http = 7500
            grpc = 7502
            dns = 7600
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
        --label "com.docker.compose.project=go-nomads-infras" `
        --label "com.docker.compose.service=consul" `
        -p 7500:7500 `
        -p 7502:7502 `
        -p 7600:7600/udp `
        -v "${consulConfigPath}:/consul/config/consul.json:ro" `
        hashicorp/consul:latest agent -config-file /consul/config/consul.json | Out-Null
    
    Write-Host "Consul UI available at: http://localhost:7500" -ForegroundColor Green
}

function Start-Jaeger {
    Write-Header "Deploying Jaeger (Distributed Tracing)"
    Remove-Container "go-nomads-jaeger"
    
    & $RUNTIME run -d `
        --name go-nomads-jaeger `
        --network $NETWORK_NAME `
        --label "com.docker.compose.project=go-nomads-infras" `
        --label "com.docker.compose.service=jaeger" `
        -e COLLECTOR_ZIPKIN_HOST_PORT=:9411 `
        -e COLLECTOR_OTLP_ENABLED=true `
        -e SPAN_STORAGE_TYPE=memory `
        -e MEMORY_MAX_TRACES=100000 `
        -p 16686:16686 `
        -p 4317:4317 `
        -p 4318:4318 `
        -p 9411:9411 `
        -p 6831:6831/udp `
        jaegertracing/all-in-one:1.54 | Out-Null
    
    Write-Host "Jaeger UI available at: http://localhost:16686" -ForegroundColor Green
    Write-Host "Jaeger OTLP (gRPC): localhost:4317" -ForegroundColor Green
    Write-Host "Jaeger OTLP (HTTP): localhost:4318" -ForegroundColor Green
}

function Start-Prometheus {
    Write-Header "Deploying Prometheus"
    Remove-Container "go-nomads-prometheus"
    
    $promDir = Join-Path $SCRIPT_DIR "prometheus"
    New-Item -ItemType Directory -Path $promDir -Force | Out-Null
    
    # Create Prometheus config file with OpenTelemetry compatible settings
    $promConfigLines = @(
        'global:',
        '  scrape_interval: 15s',
        '  evaluation_interval: 15s',
        '  external_labels:',
        '    cluster: ''go-nomads''',
        '    env: ''development''',
        '',
        'scrape_configs:',
        '  - job_name: ''prometheus''',
        '    static_configs:',
        '      - targets: [''localhost:9090'']',
        '',
        '  # Gateway',
        '  - job_name: ''gateway''',
        '    metrics_path: /metrics',
        '    static_configs:',
        '      - targets: [''host.docker.internal:5000'']',
        '        labels:',
        '          service: ''gateway''',
        '',
        '  # User Service',
        '  - job_name: ''user-service''',
        '    metrics_path: /metrics',
        '    static_configs:',
        '      - targets: [''host.docker.internal:5001'']',
        '        labels:',
        '          service: ''user-service''',
        '',
        '  # City Service',
        '  - job_name: ''city-service''',
        '    metrics_path: /metrics',
        '    static_configs:',
        '      - targets: [''host.docker.internal:8002'']',
        '        labels:',
        '          service: ''city-service''',
        '',
        '  # Accommodation Service',
        '  - job_name: ''accommodation-service''',
        '    metrics_path: /metrics',
        '    static_configs:',
        '      - targets: [''host.docker.internal:8003'']',
        '        labels:',
        '          service: ''accommodation-service''',
        '',
        '  # Coworking Service',
        '  - job_name: ''coworking-service''',
        '    metrics_path: /metrics',
        '    static_configs:',
        '      - targets: [''host.docker.internal:8004'']',
        '        labels:',
        '          service: ''coworking-service''',
        '',
        '  # Event Service',
        '  - job_name: ''event-service''',
        '    metrics_path: /metrics',
        '    static_configs:',
        '      - targets: [''host.docker.internal:8005'']',
        '        labels:',
        '          service: ''event-service''',
        '',
        '  # AI Service',
        '  - job_name: ''ai-service''',
        '    metrics_path: /metrics',
        '    static_configs:',
        '      - targets: [''host.docker.internal:8006'']',
        '        labels:',
        '          service: ''ai-service''',
        '',
        '  # Message Service',
        '  - job_name: ''message-service''',
        '    metrics_path: /metrics',
        '    static_configs:',
        '      - targets: [''host.docker.internal:8007'']',
        '        labels:',
        '          service: ''message-service''',
        '',
        '  # Search Service',
        '  - job_name: ''search-service''',
        '    metrics_path: /metrics',
        '    static_configs:',
        '      - targets: [''host.docker.internal:8008'']',
        '        labels:',
        '          service: ''search-service''',
        '',
        '  # Jaeger',
        '  - job_name: ''jaeger''',
        '    static_configs:',
        '      - targets: [''go-nomads-jaeger:14269'']',
        '        labels:',
        '          service: ''jaeger''',
        '',
        '  # Consul service discovery (optional)',
        '  # - job_name: ''consul-services''',
        '  #   metrics_path: /metrics',
        '  #   consul_sd_configs:',
        '  #     - server: ''go-nomads-consul:7500'''
    )
    $promConfigContent = $promConfigLines -join "`n"
    
    $promConfigPath = Join-Path $promDir "prometheus-local.yml"
    [System.IO.File]::WriteAllText($promConfigPath, $promConfigContent)
    $promConfigPath = $promConfigPath -replace '\\', '/'
    
    & $RUNTIME run -d `
        --name go-nomads-prometheus `
        --network $NETWORK_NAME `
        --label "com.docker.compose.project=go-nomads-infras" `
        --label "com.docker.compose.service=prometheus" `
        --add-host host.docker.internal:host-gateway `
        -p 9090:9090 `
        -v "${promConfigPath}:/etc/prometheus/prometheus.yml:ro" `
        prom/prometheus:v2.49.1 `
        --config.file=/etc/prometheus/prometheus.yml `
        --storage.tsdb.path=/prometheus `
        --web.enable-lifecycle | Out-Null
    
    Write-Host "Prometheus UI available at: http://localhost:9090" -ForegroundColor Green
}

function Start-Grafana {
    Write-Header "Deploying Grafana"
    Remove-Container "go-nomads-grafana"
    
    $grafanaDir = Join-Path $SCRIPT_DIR "grafana"
    $grafanaProvisioningPath = Join-Path $grafanaDir "provisioning"
    $grafanaDatasourcesPath = Join-Path $grafanaProvisioningPath "datasources"
    
    # Create directories
    New-Item -ItemType Directory -Path $grafanaDatasourcesPath -Force | Out-Null
    
    # Create datasources config for Prometheus and Jaeger
    $datasourcesConfig = @"
apiVersion: 1
datasources:
  - name: Prometheus
    type: prometheus
    access: proxy
    url: http://go-nomads-prometheus:9090
    isDefault: true
    editable: false

  - name: Jaeger
    type: jaeger
    access: proxy
    url: http://go-nomads-jaeger:16686
    editable: false
    jsonData:
      tracesToLogsV2:
        datasourceUid: ''
      tracesToMetrics:
        datasourceUid: Prometheus
        tags:
          - key: service.name
            value: service
"@
    
    $datasourcesFilePath = Join-Path $grafanaDatasourcesPath "datasources.yml"
    [System.IO.File]::WriteAllText($datasourcesFilePath, $datasourcesConfig)
    
    $grafanaProvisioningPath = $grafanaProvisioningPath -replace '\\', '/'
    
    & $RUNTIME run -d `
        --name go-nomads-grafana `
        --network $NETWORK_NAME `
        --label "com.docker.compose.project=go-nomads-infras" `
        --label "com.docker.compose.service=grafana" `
        --add-host host.docker.internal:host-gateway `
        -p 3000:3000 `
        -e GF_SECURITY_ADMIN_PASSWORD=admin `
        -e GF_INSTALL_PLUGINS=grafana-clock-panel,grafana-simple-json-datasource `
        -v "${grafanaProvisioningPath}:/etc/grafana/provisioning:ro" `
        grafana/grafana:10.3.1 | Out-Null
    
    Write-Host "Grafana UI available at: http://localhost:3000 (admin/admin)" -ForegroundColor Green
}

function Start-Elasticsearch {
    Write-Header "Deploying Elasticsearch"
    Remove-Container "go-nomads-elasticsearch"
    
    & $RUNTIME run -d `
        --name go-nomads-elasticsearch `
        --network $NETWORK_NAME `
        --label "com.docker.compose.project=go-nomads-infras" `
        --label "com.docker.compose.service=elasticsearch" `
        -p 9200:9200 `
        -p 9300:9300 `
        -e "discovery.type=single-node" `
        -e "xpack.security.enabled=false" `
        -e "ES_JAVA_OPTS=-Xms512m -Xmx512m" `
        docker.elastic.co/elasticsearch/elasticsearch:8.16.1 | Out-Null
    
    Write-Host "Elasticsearch available at: http://localhost:9200" -ForegroundColor Green
}

function Start-RabbitMQ {
    Write-Header "Deploying RabbitMQ"
    Remove-Container "go-nomads-rabbitmq"
    
    & $RUNTIME run -d `
        --name go-nomads-rabbitmq `
        --network $NETWORK_NAME `
        --label "com.docker.compose.project=go-nomads-infras" `
        --label "com.docker.compose.service=rabbitmq" `
        -p 5672:5672 `
        -p 15672:15672 `
        -e RABBITMQ_DEFAULT_USER=walden `
        -e RABBITMQ_DEFAULT_PASS=walden `
        rabbitmq:3-management-alpine | Out-Null
    
    Write-Host "RabbitMQ running at: amqp://localhost:5672" -ForegroundColor Green
    Write-Host "RabbitMQ Management UI: http://localhost:15672 (walden/walden)" -ForegroundColor Green
}

function Start-Nginx {
    Write-Header "Deploying Nginx"
    Remove-Container "go-nomads-nginx"
    
    $nginxConf = Join-Path $SCRIPT_DIR "nginx\nginx.conf"
    if (-not (Test-Path $nginxConf)) {
        Write-Host "[WARNING] Nginx config not found: $nginxConf" -ForegroundColor Yellow
        Write-Host "Skipping Nginx deployment. Run deploy-services-local.ps1 to deploy Nginx with gateway." -ForegroundColor Yellow
        return
    }
    
    $nginxConfPath = $nginxConf -replace '\\', '/'
    
    & $RUNTIME run -d `
        --name go-nomads-nginx `
        --network $NETWORK_NAME `
        --label "com.docker.compose.project=go-nomads-infras" `
        --label "com.docker.compose.service=nginx" `
        -p 80:80 `
        -p 443:443 `
        -v "${nginxConfPath}:/etc/nginx/conf.d/default.conf:ro" `
        --restart unless-stopped `
        nginx:alpine | Out-Null
    
    Write-Host "Nginx running at: http://localhost" -ForegroundColor Green
}

function Start-Infrastructure {
    Write-Header "Go-Nomads Local Infrastructure"
    Require-Docker
    
    New-Network
    Start-Redis
    Start-RabbitMQ
    Start-Consul
    Start-Jaeger
    Start-Prometheus
    Start-Grafana
    Start-Elasticsearch
    Start-Nginx
    
    Show-Status
    Write-Host "Infrastructure ready." -ForegroundColor Green
}

function Stop-Infrastructure {
    Write-Header "Stopping local infrastructure"
    Require-Docker
    
    $containers = @(
        "go-nomads-nginx",
        "go-nomads-elasticsearch",
        "go-nomads-grafana",
        "go-nomads-prometheus",
        "go-nomads-jaeger",
        "go-nomads-consul",
        "go-nomads-rabbitmq",
        "go-nomads-redis"
    )
    
    foreach ($container in $containers) {
        if (Test-ContainerRunning $container) {
            Write-Host "Stopping $container..." -ForegroundColor Yellow
            & $RUNTIME stop $container 2>&1 | Out-Null
        }
    }
}

function Restart-Infrastructure {
    Stop-Infrastructure
    Start-Sleep -Seconds 2
    Start-Infrastructure
}

function Remove-Infrastructure {
    Write-Header "Cleaning local infrastructure"
    Require-Docker
    
    Stop-Infrastructure
    
    $containers = @(
        "go-nomads-nginx",
        "go-nomads-elasticsearch",
        "go-nomads-grafana",
        "go-nomads-prometheus",
        "go-nomads-jaeger",
        "go-nomads-consul",
        "go-nomads-rabbitmq",
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
    $grafanaDir = Join-Path $SCRIPT_DIR "grafana"
    
    if (Test-Path $consulDir) {
        Remove-Item -Path $consulDir -Recurse -Force
    }
    
    if (Test-Path $promDir) {
        Remove-Item -Path $promDir -Recurse -Force
    }
    
    if (Test-Path $grafanaDir) {
        Remove-Item -Path $grafanaDir -Recurse -Force
    }
    
    Write-Host "Clean complete." -ForegroundColor Green
}

function Show-Status {
    Write-Header "Infrastructure status"
    Require-Docker
    
    & $RUNTIME ps --filter "name=go-nomads" --format 'table {{.Names}}`t{{.Status}}`t{{.Ports}}'
    
    Write-Host ""
    Write-Host "Access URLs:" -ForegroundColor Cyan
    Write-Host "  Nginx:          http://localhost"
    Write-Host "  Redis:          redis://localhost:6379"
    Write-Host "  Consul:         http://localhost:7500"
    Write-Host "  Jaeger UI:      http://localhost:16686" -ForegroundColor Green
    Write-Host "  Jaeger OTLP:    http://localhost:4317 (gRPC), http://localhost:4318 (HTTP)"
    Write-Host "  Prometheus:     http://localhost:9090"
    Write-Host "  Grafana:        http://localhost:3000 (admin/admin)"
    Write-Host "  Elasticsearch:  http://localhost:9200"
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
