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
    Write-Host "  RabbitMQ:       amqp://localhost:5672"
    Write-Host "  RabbitMQ UI:    http://localhost:15672"
    Write-Host "  Elasticsearch:  http://localhost:9200"
    Write-Host ""
    Write-Host "  Observability (Aspire Dashboard): 通过 dotnet run --project src/GoNomads.AppHost 启动" -ForegroundColor Yellow
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
