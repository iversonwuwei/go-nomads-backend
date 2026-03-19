#!/bin/bash

# ============================================================
# Go-Nomads Services Deployment Script (Podman)
# 部署所有微服务到容器中
# ============================================================

set -e

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# 脚本目录
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"

# 容器运行时检测
CONTAINER_RUNTIME=""
if command -v podman &> /dev/null; then
    CONTAINER_RUNTIME="podman"
elif [ -x "/opt/podman/bin/podman" ]; then
    CONTAINER_RUNTIME="/opt/podman/bin/podman"
elif command -v docker &> /dev/null; then
    CONTAINER_RUNTIME="docker"
else
    echo -e "${RED}错误: 未找到 Podman 或 Docker${NC}"
    exit 1
fi

# 网络名称
NETWORK_NAME="go-nomads-network"

# 显示标题
show_header() {
    echo -e "${BLUE}"
    echo "============================================================"
    echo "  $1"
    echo "============================================================"
    echo -e "${NC}"
}

# 检查容器是否运行
container_running() {
    $CONTAINER_RUNTIME ps --filter "name=$1" --filter "status=running" --format "{{.Names}}" | grep -q "^$1$"
}

# 删除容器（如果存在）
remove_container_if_exists() {
    local container_name=$1
    if $CONTAINER_RUNTIME ps -a --filter "name=$container_name" --format "{{.Names}}" | grep -q "^$container_name$"; then
        echo -e "${YELLOW}  删除已存在的容器: $container_name${NC}"
        $CONTAINER_RUNTIME stop "$container_name" &> /dev/null || true
        $CONTAINER_RUNTIME rm "$container_name" &> /dev/null || true
    fi
}

# 等待服务就绪
wait_for_service() {
    local service_name=$1
    local url=$2
    local max_attempts=30
    local attempt=1
    
    echo -e "${YELLOW}  等待 $service_name 就绪...${NC}"
    
    while [ $attempt -le $max_attempts ]; do
        if curl -s "$url" &> /dev/null; then
            echo -e "${GREEN}  $service_name 已就绪!${NC}"
            return 0
        fi
        echo -n "."
        sleep 2
        ((attempt++))
    done
    
    echo -e "${RED}  [警告] $service_name 启动超时${NC}"
    return 1
}

# 构建并部署服务
deploy_service() {
    local service_name=$1
    local service_path=$2
    local dockerfile_path=$3
    local app_port=$4
    
    show_header "部署 $service_name"
    
    # 构建镜像
    echo -e "${YELLOW}  构建 Docker 镜像...${NC}"
    cd "$ROOT_DIR"
    
    $CONTAINER_RUNTIME build \
        --platform linux/amd64 \
        -f "$dockerfile_path" \
        -t "go-nomads-$service_name:latest" \
        . --quiet
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}  镜像构建成功!${NC}"
    else
        echo -e "${RED}  [错误] 镜像构建失败${NC}"
        return 1
    fi
    
    # 删除旧容器
    remove_container_if_exists "go-nomads-$service_name"
    
    # 启动容器
    echo -e "${YELLOW}  启动容器...${NC}"
    
    # Gateway 使用生产配置
    # 其他服务继续使用 Development 环境
    local env_config=()
    if [[ "$service_name" == "gateway" ]]; then
        # Gateway 使用生产配置
        env_config+=("-e" "ASPNETCORE_ENVIRONMENT=Production")
    else
        # 其他服务使用 Development 环境
        env_config+=("-e" "ASPNETCORE_ENVIRONMENT=Development")
    fi
    
    $CONTAINER_RUNTIME run -d \
        --name "go-nomads-$service_name" \
        --network "$NETWORK_NAME" \
        -p "$app_port:8080" \
        "${env_config[@]}" \
        -e ASPNETCORE_URLS=http://+:8080 \
        "go-nomads-$service_name:latest" > /dev/null
    
    if container_running "go-nomads-$service_name"; then
        echo -e "${GREEN}  $service_name 部署成功!${NC}"
        echo -e "${GREEN}  容器端口: $app_port${NC}"
        return 0
    else
        echo -e "${RED}  [错误] $service_name 启动失败${NC}"
        echo -e "${YELLOW}  查看日志: $CONTAINER_RUNTIME logs go-nomads-$service_name${NC}"
        return 1
    fi
}

# 检查前置条件
check_prerequisites() {
    show_header "检查前置条件"
    
    # 检查网络是否存在
    if ! $CONTAINER_RUNTIME network exists "$NETWORK_NAME" &> /dev/null; then
        echo -e "${RED}  [错误] 网络 '$NETWORK_NAME' 不存在${NC}"
        echo -e "${YELLOW}  请先运行基础设施部署脚本: ./deploy-infrastructure.sh${NC}"
        exit 1
    fi
    echo -e "${GREEN}  网络检查通过${NC}"
    
    # 检查 Redis
    if ! container_running "go-nomads-redis"; then
        echo -e "${RED}  [错误] Redis 未运行${NC}"
        echo -e "${YELLOW}  请先运行基础设施部署脚本: ./deploy-infrastructure.sh${NC}"
        exit 1
    fi
    echo -e "${GREEN}  Redis 运行正常${NC}"
    
    echo -e "${GREEN}  前置条件检查完成${NC}"
}

# 主部署流程
main() {
    show_header "Go-Nomads 服务部署 (使用 $CONTAINER_RUNTIME)"
    
    echo -e "${BLUE}根目录: $ROOT_DIR${NC}"
    echo ""
    
    # 检查前置条件
    check_prerequisites
    echo ""
    
    # 部署 UserService
    deploy_service \
        "user-service" \
        "src/Services/UserService/UserService" \
        "src/Services/UserService/UserService/Dockerfile" \
        "5001" \
        "3001" \
        "50001"
    echo ""
    
    # 部署 ProductService
    deploy_service \
        "product-service" \
        "src/Services/ProductService/ProductService" \
        "src/Services/ProductService/ProductService/Dockerfile" \
        "5002" \
        "3002" \
        "50002"
    echo ""
    
    # 部署 DocumentService
    deploy_service \
        "document-service" \
        "src/Services/DocumentService/DocumentService" \
        "src/Services/DocumentService/DocumentService/Dockerfile" \
        "5003" \
        "3003" \
        "50003"
    echo ""
    
    # 部署 Gateway
    deploy_service \
        "gateway" \
        "src/Gateway/Gateway" \
        "src/Gateway/Gateway/Dockerfile" \
        "5000" \
        "3000" \
        "50000"
    echo ""
    
    # 部署 MessageService
    deploy_service \
        "messageservice" \
        "src/Services/MessageService/API" \
        "src/Services/MessageService/API/Dockerfile" \
        "5005" \
        "3005" \
        "50005"
    echo ""
    
    # 显示部署摘要
    show_header "部署摘要"
    
    echo -e "${GREEN}所有服务部署完成!${NC}"
    echo ""
    echo -e "${BLUE}服务访问地址:${NC}"
    echo -e "  ${GREEN}Gateway:          http://localhost:5000${NC}"
    echo -e "  ${GREEN}User Service:     http://localhost:5001${NC}"
    echo -e "  ${GREEN}Product Service:  http://localhost:5002${NC}"
    echo -e "  ${GREEN}Document Service: http://localhost:5003${NC}"
    echo -e "  ${GREEN}Document API:     http://localhost:5003/scalar/v1${NC}"
    echo -e "  ${GREEN}Message Service:  http://localhost:5005${NC}"
    echo -e "  ${GREEN}Message Swagger:  http://localhost:5005/swagger${NC}"
    echo ""
    echo -e "${BLUE}基础设施:${NC}"
    echo -e "  ${GREEN}RabbitMQ UI:      http://localhost:15672 (guest/guest)${NC}"
    echo ""
    echo -e "${BLUE}常用命令:${NC}"
    echo -e "  查看运行中的容器:  ${YELLOW}$CONTAINER_RUNTIME ps${NC}"
    echo -e "  查看服务日志:      ${YELLOW}$CONTAINER_RUNTIME logs go-nomads-gateway${NC}"
    echo -e "  停止所有服务:      ${YELLOW}./stop-services.sh${NC}"
    echo -e "  重启服务:          ${YELLOW}$CONTAINER_RUNTIME restart go-nomads-gateway${NC}"
    echo ""
    
    # 显示容器状态
    echo -e "${BLUE}容器状态:${NC}"
    $CONTAINER_RUNTIME ps --filter "name=go-nomads-" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
    echo ""
    
    echo -e "${GREEN}部署完成! 🚀${NC}"
}

# 运行主流程
main
