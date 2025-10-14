#!/bin/bash

# ============================================================
# Go-Nomads Services Deployment Script (Local Build + Podman)
# 在本地构建，然后部署到容器中
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

# 本地构建并部署服务
deploy_service_local() {
    local service_name=$1
    local service_path=$2
    local app_port=$3
    local dll_name=$4
    
    show_header "部署 $service_name"
    
    # 本地构建
    echo -e "${YELLOW}  本地构建项目...${NC}"
    cd "$ROOT_DIR/$service_path"
    
    dotnet publish -c Release -o "$ROOT_DIR/publish/$service_name" > /dev/null 2>&1
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}  本地构建成功!${NC}"
    else
        echo -e "${RED}  [错误] 本地构建失败${NC}"
        dotnet publish -c Release -o "$ROOT_DIR/publish/$service_name"
        return 1
    fi
    
    # 创建简化的 Dockerfile
    cat > "$ROOT_DIR/publish/$service_name/Dockerfile" << EOF
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY . .
EXPOSE 8080
ENTRYPOINT ["dotnet", "$dll_name"]
EOF
    
    # 构建运行时镜像
    echo -e "${YELLOW}  构建运行时镜像...${NC}"
    $CONTAINER_RUNTIME build \
        -t "go-nomads-$service_name:latest" \
        "$ROOT_DIR/publish/$service_name" > /dev/null 2>&1
    
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
    $CONTAINER_RUNTIME run -d \
        --name "go-nomads-$service_name" \
        --network "$NETWORK_NAME" \
        -p "$app_port:8080" \
        -e ASPNETCORE_ENVIRONMENT=Development \
        -e ASPNETCORE_URLS=http://+:8080 \
        -e HTTP_PROXY= \
        -e HTTPS_PROXY= \
        -e NO_PROXY= \
        "go-nomads-$service_name:latest" > /dev/null
    
    if container_running "go-nomads-$service_name"; then
        echo -e "${GREEN}  $service_name 部署成功!${NC}"
        echo -e "${GREEN}  访问地址: http://localhost:$app_port${NC}"
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
    
    # 检查 .NET SDK
    if ! command -v dotnet &> /dev/null; then
        echo -e "${RED}  [错误] 未找到 .NET SDK${NC}"
        exit 1
    fi
    echo -e "${GREEN}  .NET SDK: $(dotnet --version)${NC}"
    
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
    show_header "Go-Nomads 服务部署 (本地构建 + $CONTAINER_RUNTIME)"
    
    echo -e "${BLUE}根目录: $ROOT_DIR${NC}"
    echo ""
    
    # 检查前置条件
    check_prerequisites
    echo ""
    
    # 清理旧的发布文件
    echo -e "${YELLOW}清理旧的发布文件...${NC}"
    rm -rf "$ROOT_DIR/publish"
    mkdir -p "$ROOT_DIR/publish"
    echo ""
    
    # 部署 UserService
    deploy_service_local \
        "user-service" \
        "src/Services/UserService/UserService" \
        "5001" \
        "UserService.dll"
    echo ""
    
    # 部署 ProductService
    deploy_service_local \
        "product-service" \
        "src/Services/ProductService/ProductService" \
        "5002" \
        "ProductService.dll"
    echo ""
    
    # 部署 DocumentService
    deploy_service_local \
        "document-service" \
        "src/Services/DocumentService/DocumentService" \
        "5003" \
        "DocumentService.dll"
    echo ""
    
    # 部署 Gateway
    deploy_service_local \
        "gateway" \
        "src/Gateway/Gateway" \
        "5000" \
        "Gateway.dll"
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
    echo ""
    echo -e "${BLUE}基础设施:${NC}"
    echo -e "  ${GREEN}Consul UI:        http://localhost:8500${NC}"
    echo -e "  ${GREEN}Zipkin:           http://localhost:9411${NC}"
    echo -e "  ${GREEN}Prometheus:       http://localhost:9090${NC}"
    echo -e "  ${GREEN}Grafana:          http://localhost:3000${NC}"
    echo ""
    echo -e "${BLUE}常用命令:${NC}"
    echo -e "  查看运行中的容器:  ${YELLOW}$CONTAINER_RUNTIME ps${NC}"
    echo -e "  查看服务日志:      ${YELLOW}$CONTAINER_RUNTIME logs go-nomads-gateway${NC}"
    echo -e "  停止所有服务:      ${YELLOW}./stop-services.sh${NC}"
    echo ""
    
    # 显示容器状态
    echo -e "${BLUE}容器状态:${NC}"
    $CONTAINER_RUNTIME ps --filter "name=go-nomads-" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
    echo ""
    
    echo -e "${GREEN}部署完成! 🚀${NC}"
}

# 运行主流程
main
