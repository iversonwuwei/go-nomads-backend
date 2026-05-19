#!/bin/bash

# ============================================================
# Go-Nomads Services Deployment Script (Docker Compose Build)
# Usage: bash deploy-services-local.sh [--skip-build] [--help]
# ============================================================

set -e

# 参数解析
SKIP_BUILD=false

while [[ $# -gt 0 ]]; do
    case $1 in
        --skip-build)
            SKIP_BUILD=true
            shift
            ;;
        --help|-h)
            echo ""
            echo "Usage: ./deploy-services-local.sh [options]"
            echo ""
            echo "Options:"
            echo "  --skip-build    Skip docker build, use existing images"
            echo "  --help, -h      Show this help message"
            echo ""
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            echo "Use --help for usage information"
            exit 1
            ;;
    esac
done

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# 脚本目录
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"
COMPOSE_FILE="$ROOT_DIR/docker-compose.yml"

# 网络名称
NETWORK_NAME="go-nomads-network"

show_header() {
    echo -e "${BLUE}"
    echo "============================================================"
    echo "  $1"
    echo "============================================================"
    echo -e "${NC}"
}

# 确保 Docker 可用
check_docker() {
    if ! command -v docker &> /dev/null; then
        echo -e "${RED}[错误] 未找到 Docker${NC}"
        exit 1
    fi
    echo -e "${GREEN}  Docker: $(docker --version | head -1)${NC}"
}

# 确保网络存在
ensure_network() {
    if docker network ls --filter "name=$NETWORK_NAME" --format "{{.Name}}" | grep -q "^$NETWORK_NAME$"; then
        echo -e "${GREEN}  网络已存在: $NETWORK_NAME${NC}"
    else
        echo -e "${YELLOW}  创建网络: $NETWORK_NAME${NC}"
        docker network create "$NETWORK_NAME" > /dev/null
        echo -e "${GREEN}  网络创建完成${NC}"
    fi
}

# 检查基础设施
check_infra() {
    local infra_services=("go-nomads-redis" "go-nomads-rabbitmq" "go-nomads-elasticsearch")
    for svc in "${infra_services[@]}"; do
        if docker ps --filter "name=$svc" --filter "status=running" --format "{{.Names}}" | grep -q "^$svc$"; then
            echo -e "${GREEN}  $svc 运行正常${NC}"
        else
            echo -e "${RED}  [错误] $svc 未运行${NC}"
            echo -e "${YELLOW}  请先启动基础设施: docker compose -f docker-compose-infras-swr.yml up -d${NC}"
            exit 1
        fi
    done
}

# 主部署流程
main() {
    show_header "Go-Nomads 服务部署 (Docker Compose Build)"

    echo -e "${BLUE}Compose 文件: $COMPOSE_FILE${NC}"
    if [ "$SKIP_BUILD" = true ]; then
        echo -e "${YELLOW}构建模式: 跳过构建${NC}"
    else
        echo -e "${BLUE}构建模式: Docker 构建${NC}"
    fi
    echo ""

    # 检查前置条件
    show_header "检查前置条件"
    check_docker
    ensure_network
    check_infra
    echo -e "${GREEN}  前置条件检查完成${NC}"
    echo ""

    # 停止并移除旧的服务容器
    show_header "停止旧容器"
    echo -e "${YELLOW}  停止并移除旧的服务容器...${NC}"
    cd "$ROOT_DIR"
    docker compose -f "$COMPOSE_FILE" down --remove-orphans 2>/dev/null || true
    echo -e "${GREEN}  旧容器已清理${NC}"
    echo ""

    # 构建镜像
    if [ "$SKIP_BUILD" = false ]; then
        show_header "构建 Docker 镜像"
        echo -e "${YELLOW}  构建所有服务镜像...${NC}"
        docker compose -f "$COMPOSE_FILE" build
        if [ $? -eq 0 ]; then
            echo -e "${GREEN}  所有镜像构建成功!${NC}"
        else
            echo -e "${RED}  [错误] 镜像构建失败${NC}"
            exit 1
        fi
        echo ""
    fi

    # 启动服务
    show_header "启动服务"
    echo -e "${YELLOW}  启动所有服务容器...${NC}"
    docker compose -f "$COMPOSE_FILE" up -d
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}  所有服务启动成功!${NC}"
    else
        echo -e "${RED}  [错误] 服务启动失败${NC}"
        exit 1
    fi
    echo ""

    # 等待服务就绪
    echo -e "${YELLOW}  等待服务启动...${NC}"
    sleep 5

    # 显示部署摘要
    show_header "部署摘要"

    echo -e "${GREEN}所有服务部署完成!${NC}"
    echo ""
    echo -e "${BLUE}服务访问地址:${NC}"
    echo -e "  ${GREEN}Gateway:               http://localhost:5080${NC}"
    echo -e "  ${GREEN}User Service:          http://localhost:5001${NC}"
    echo -e "  ${GREEN}Product Service:       http://localhost:5002${NC}"
    echo -e "  ${GREEN}Document Service:      http://localhost:5003${NC}"
    echo -e "  ${GREEN}City Service:          http://localhost:5202${NC}"
    echo -e "  ${GREEN}Event Service:         http://localhost:5205${NC}"
    echo -e "  ${GREEN}Coworking Service:     http://localhost:5203${NC}"
    echo -e "  ${GREEN}AI Service:            http://localhost:5209${NC}"
    echo -e "  ${GREEN}Cache Service:         http://localhost:5210${NC}"
    echo -e "  ${GREEN}Message Service:       http://localhost:5005${NC}"
    echo -e "  ${GREEN}Accommodation Service: http://localhost:5204${NC}"
    echo -e "  ${GREEN}Innovation Service:    http://localhost:5206${NC}"
    echo -e "  ${GREEN}Search Service:        http://localhost:5215${NC}"
    echo ""
    echo -e "${BLUE}常用命令:${NC}"
    echo -e "  查看运行中的容器:  ${YELLOW}docker compose -f docker-compose.yml ps${NC}"
    echo -e "  查看服务日志:      ${YELLOW}docker compose -f docker-compose.yml logs -f gateway${NC}"
    echo -e "  停止所有服务:      ${YELLOW}docker compose -f docker-compose.yml down${NC}"
    echo -e "  重启单个服务:      ${YELLOW}docker compose -f docker-compose.yml restart gateway${NC}"
    echo ""

    # 显示容器状态
    echo -e "${BLUE}容器状态:${NC}"
    docker compose -f "$COMPOSE_FILE" ps
    echo ""

    echo -e "${GREEN}部署完成! 🚀${NC}"
}

# 运行主流程
main
