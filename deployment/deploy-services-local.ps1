# ============================================================
# Go-Nomads Services Deployment Script (Docker Compose Build)
# Usage: .\deploy-services-local.ps1 [-SkipBuild] [-Help]
# ============================================================

param(
    [switch]$SkipBuild,
    [switch]$Help
)

$ErrorActionPreference = 'Stop'

if ($Help) {
    Write-Host ""
    Write-Host "Usage: .\deploy-services-local.ps1 [options]"
    Write-Host ""
    Write-Host "Options:"
    Write-Host "  -SkipBuild    Skip docker build, use existing images"
    Write-Host "  -Help         Show this help message"
    Write-Host ""
    exit 0
}

# 脚本目录
$SCRIPT_DIR = Split-Path -Parent $MyInvocation.MyCommand.Path
$ROOT_DIR = Split-Path -Parent $SCRIPT_DIR
$COMPOSE_FILE = Join-Path $ROOT_DIR "docker-compose.yml"

# 网络名称
$NETWORK_NAME = "go-nomads-network"

function Show-Header($title) {
    Write-Host ""
    Write-Host "============================================================" -ForegroundColor Blue
    Write-Host "  $title" -ForegroundColor Blue
    Write-Host "============================================================" -ForegroundColor Blue
    Write-Host ""
}

# 确保 Docker 可用
function Test-Docker {
    try {
        $version = docker --version
        Write-Host "  Docker: $version" -ForegroundColor Green
    } catch {
        Write-Host "  [错误] 未找到 Docker" -ForegroundColor Red
        exit 1
    }
}

# 确保网络存在
function Ensure-Network {
    $existing = docker network ls --filter "name=$NETWORK_NAME" --format "{{.Name}}" | Where-Object { $_ -eq $NETWORK_NAME }
    if ($existing) {
        Write-Host "  网络已存在: $NETWORK_NAME" -ForegroundColor Green
    } else {
        Write-Host "  创建网络: $NETWORK_NAME" -ForegroundColor Yellow
        docker network create $NETWORK_NAME | Out-Null
        Write-Host "  网络创建完成" -ForegroundColor Green
    }
}

# 检查基础设施
function Test-Infrastructure {
    $infraServices = @("go-nomads-redis", "go-nomads-rabbitmq", "go-nomads-elasticsearch")
    foreach ($svc in $infraServices) {
        $running = docker ps --filter "name=$svc" --filter "status=running" --format "{{.Names}}" | Where-Object { $_ -eq $svc }
        if ($running) {
            Write-Host "  $svc 运行正常" -ForegroundColor Green
        } else {
            Write-Host "  [错误] $svc 未运行" -ForegroundColor Red
            Write-Host "  请先启动基础设施: docker compose -f docker-compose-infras-swr.yml up -d" -ForegroundColor Yellow
            exit 1
        }
    }
}

# 主部署流程
function Main {
    Show-Header "Go-Nomads 服务部署 (Docker Compose Build)"

    Write-Host "Compose 文件: $COMPOSE_FILE" -ForegroundColor Blue
    if ($SkipBuild) {
        Write-Host "构建模式: 跳过构建" -ForegroundColor Yellow
    } else {
        Write-Host "构建模式: Docker 构建" -ForegroundColor Blue
    }
    Write-Host ""

    # 检查前置条件
    Show-Header "检查前置条件"
    Test-Docker
    Ensure-Network
    Test-Infrastructure
    Write-Host "  前置条件检查完成" -ForegroundColor Green
    Write-Host ""

    # 停止并移除旧的服务容器
    Show-Header "停止旧容器"
    Write-Host "  停止并移除旧的服务容器..." -ForegroundColor Yellow
    Push-Location $ROOT_DIR

    # docker compose down（临时降低错误级别，避免 Docker 警告被 PowerShell 当作异常）
    $prevEAP = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    docker compose -f $COMPOSE_FILE down --remove-orphans 2>&1 | Out-Null
    $ErrorActionPreference = $prevEAP

    # 强制清理残留的同名容器（防止 down 未能完全清理）
    # 注意：只清理后端服务容器，不要误删 web/nginx 等其他项目的容器
    $staleContainers = docker ps -a --filter "name=go-nomads-" --format "{{.Names}}" 2>$null |
        Where-Object { $_ -match "^go-nomads-(gateway|.*-service|aspire-dashboard)$" }
    if ($staleContainers) {
        Write-Host "  发现残留容器，强制移除: $($staleContainers -join ', ')" -ForegroundColor Yellow
        docker rm -f $staleContainers 2>&1 | Out-Null
    }

    Write-Host "  旧容器已清理" -ForegroundColor Green
    Write-Host ""

    # 构建镜像
    if (-not $SkipBuild) {
        Show-Header "构建 Docker 镜像"
        Write-Host "  构建所有服务镜像..." -ForegroundColor Yellow
        docker compose -f $COMPOSE_FILE build
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  所有镜像构建成功!" -ForegroundColor Green
        } else {
            Write-Host "  [错误] 镜像构建失败" -ForegroundColor Red
            Pop-Location
            exit 1
        }
        Write-Host ""
    }

    # 启动服务
    Show-Header "启动服务"
    Write-Host "  启动所有服务容器..." -ForegroundColor Yellow
    docker compose -f $COMPOSE_FILE up -d
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  所有服务启动成功!" -ForegroundColor Green
    } else {
        Write-Host "  [错误] 服务启动失败" -ForegroundColor Red
        Pop-Location
        exit 1
    }
    Write-Host ""

    # 等待服务就绪
    Write-Host "  等待服务启动..." -ForegroundColor Yellow
    Start-Sleep -Seconds 5

    # 显示部署摘要
    Show-Header "部署摘要"

    Write-Host "所有服务部署完成!" -ForegroundColor Green
    Write-Host ""
    Write-Host "服务访问地址:" -ForegroundColor Blue
    Write-Host "  Gateway:               http://localhost:5080" -ForegroundColor Green
    Write-Host "  User Service:          http://localhost:5001" -ForegroundColor Green
    Write-Host "  Product Service:       http://localhost:5002" -ForegroundColor Green
    Write-Host "  Document Service:      http://localhost:5003" -ForegroundColor Green
    Write-Host "  City Service:          http://localhost:8002" -ForegroundColor Green
    Write-Host "  Event Service:         http://localhost:8005" -ForegroundColor Green
    Write-Host "  Coworking Service:     http://localhost:8006" -ForegroundColor Green
    Write-Host "  AI Service:            http://localhost:8009" -ForegroundColor Green
    Write-Host "  Cache Service:         http://localhost:8010" -ForegroundColor Green
    Write-Host "  Message Service:       http://localhost:5005" -ForegroundColor Green
    Write-Host "  Accommodation Service: http://localhost:8012" -ForegroundColor Green
    Write-Host "  Innovation Service:    http://localhost:8011" -ForegroundColor Green
    Write-Host "  Search Service:        http://localhost:8015" -ForegroundColor Green
    Write-Host ""
    Write-Host "常用命令:" -ForegroundColor Blue
    Write-Host "  查看运行中的容器:  docker compose -f docker-compose.yml ps" -ForegroundColor Yellow
    Write-Host "  查看服务日志:      docker compose -f docker-compose.yml logs -f gateway" -ForegroundColor Yellow
    Write-Host "  停止所有服务:      docker compose -f docker-compose.yml down" -ForegroundColor Yellow
    Write-Host "  重启单个服务:      docker compose -f docker-compose.yml restart gateway" -ForegroundColor Yellow
    Write-Host ""

    # 显示容器状态
    Write-Host "容器状态:" -ForegroundColor Blue
    docker compose -f $COMPOSE_FILE ps
    Write-Host ""

    Pop-Location
    Write-Host "部署完成! 🚀" -ForegroundColor Green
}

# 运行主流程
Main
