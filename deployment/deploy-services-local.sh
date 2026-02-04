#!/bin/bash

# ============================================================
# Go-Nomads Services Deployment Script (Local Build + Container)
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
            echo "  --skip-build    Skip the build step and use existing published binaries"
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

# 容器运行时检测
CONTAINER_RUNTIME=""
CONSUL_HTTP_ADDR="${CONSUL_HTTP_ADDR:-http://localhost:8500}"

select_container_runtime() {
    local docker_bin="${DOCKER_BINARY:-$(command -v docker || true)}"
    local podman_bin="${PODMAN_BINARY:-$(command -v podman || true)}"

    if [[ -z "$docker_bin" && -x "/opt/podman/bin/podman" ]]; then
        podman_bin="/opt/podman/bin/podman"
    fi

    if [[ -n "$docker_bin" ]]; then
        if "$docker_bin" ps -a --filter "name=go-nomads-redis" --format "{{.Names}}" | grep -q "^go-nomads-redis$"; then
            CONTAINER_RUNTIME="$docker_bin"
            return
        fi
    fi

    if [[ -n "$podman_bin" ]]; then
        if "$podman_bin" ps -a --filter "name=go-nomads-redis" --format "{{.Names}}" | grep -q "^go-nomads-redis$"; then
            CONTAINER_RUNTIME="$podman_bin"
            return
        fi
    fi

    if [[ -n "$docker_bin" ]]; then
        CONTAINER_RUNTIME="$docker_bin"
        return
    fi

    if [[ -n "$podman_bin" ]]; then
        CONTAINER_RUNTIME="$podman_bin"
        return
    fi

    echo -e "${RED}错误: 未找到 Podman 或 Docker${NC}"
    exit 1
}

select_container_runtime

# 网络名称
NETWORK_NAME="go-nomads-network"

# 检查网络是否存在
network_exists() {
    local runtime_name
    runtime_name="$(basename "$CONTAINER_RUNTIME")"

    if [[ "$runtime_name" == "podman" ]]; then
        $CONTAINER_RUNTIME network exists "$NETWORK_NAME" &> /dev/null
    else
        $CONTAINER_RUNTIME network ls --filter "name=$NETWORK_NAME" --format "{{.Name}}" | grep -q "^$NETWORK_NAME$"
    fi
}

ensure_network() {
    if network_exists; then
        echo -e "${GREEN}  网络已存在: $NETWORK_NAME${NC}"
    else
        echo -e "${YELLOW}  创建网络: $NETWORK_NAME${NC}"
        $CONTAINER_RUNTIME network create "$NETWORK_NAME" > /dev/null
        echo -e "${GREEN}  网络创建完成${NC}"
    fi
}

# 注意：服务现在使用自动注册机制
# 每个服务在启动时会自动调用 RegisterWithConsulAsync() 注册到 Consul
# 无需手动注册配置文件

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

# 删除容器和镜像（如果存在）
remove_container_if_exists() {
    local container_name=$1
    if $CONTAINER_RUNTIME ps -a --filter "name=$container_name" --format "{{.Names}}" | grep -q "^$container_name$"; then
        echo -e "${YELLOW}  删除已存在的容器: $container_name${NC}"
        $CONTAINER_RUNTIME stop "$container_name" &> /dev/null || true
        $CONTAINER_RUNTIME rm "$container_name" &> /dev/null || true
    fi
    
    # 删除对应的镜像（如果存在）
    local image_name="$container_name"
    if $CONTAINER_RUNTIME images --filter "reference=${image_name}:latest" --format "{{.Repository}}" | grep -q "^${image_name}$"; then
        echo -e "${YELLOW}  删除已存在的镜像: ${image_name}:latest${NC}"
        $CONTAINER_RUNTIME rmi -f "${image_name}:latest" &> /dev/null || true
    fi
}

# 本地构建并部署服务（带 Dapr sidecar）- Container Sidecar 模式
deploy_service_local() {
    local service_name=$1
    local service_path=$2
    local app_port=$3
    local dll_name=$4
    local dapr_http_port=$5
    local app_id=$6
    
    show_header "部署 $service_name"
    
    # 本地构建
    if [ "$SKIP_BUILD" = false ]; then
        echo -e "${YELLOW}  本地构建项目...${NC}"
        cd "$ROOT_DIR/$service_path"
        
        dotnet publish -c Release --no-self-contained > /dev/null 2>&1
        
        if [ $? -eq 0 ]; then
            echo -e "${GREEN}  本地构建成功!${NC}"
        else
            echo -e "${RED}  [错误] 本地构建失败${NC}"
            dotnet publish -c Release --no-self-contained
            return 1
        fi
    else
        echo -e "${YELLOW}  跳过构建，使用已有的发布文件...${NC}"
    fi
    
    # 删除旧容器（应用容器和 Dapr sidecar）
    remove_container_if_exists "go-nomads-$service_name"
    remove_container_if_exists "go-nomads-$service_name-dapr"
    
    # 发布目录
    local publish_dir="$ROOT_DIR/$service_path/bin/Release/net9.0/publish"
    
    if [ ! -d "$publish_dir" ]; then
        echo -e "${RED}  [错误] 发布目录不存在: $publish_dir${NC}"
        return 1
    fi
    
    # 额外的环境变量
    local extra_env=()
    if [[ "$service_name" == "document-service" ]]; then
        extra_env+=(
            "-e" "Services__Gateway__Url=http://go-nomads-gateway:8080"
            "-e" "Services__Gateway__OpenApiUrl=http://go-nomads-gateway:8080/openapi/v1.json"
            "-e" "Services__ProductService__Url=http://go-nomads-product-service:8080"
            "-e" "Services__ProductService__OpenApiUrl=http://go-nomads-product-service:8080/openapi/v1.json"
            "-e" "Services__UserService__Url=http://go-nomads-user-service:8080"
            "-e" "Services__UserService__OpenApiUrl=http://go-nomads-user-service:8080/openapi/v1.json"
        )
    fi

    # city-service 需要 RabbitMQ 和 Elasticsearch 配置
    if [[ "$service_name" == "city-service" ]]; then
        extra_env+=(
            "-e" "ConnectionStrings__Elasticsearch=http://go-nomads-elasticsearch:9200"
            "-e" "RabbitMQ__Host=go-nomads-rabbitmq"
            "-e" "RabbitMQ__Username=walden"
            "-e" "RabbitMQ__Password=walden"
        )
    fi
    
    # message-service 需要指定 ServiceAddress 和 RabbitMQ 配置
    if [[ "$service_name" == "message-service" ]]; then
        extra_env+=(
            "-e" "Consul__ServiceAddress=go-nomads-$service_name"
            "-e" "Consul__ServicePort=8080"
            "-e" "RabbitMQ__HostName=go-nomads-rabbitmq"
            "-e" "RabbitMQ__Port=5672"
            "-e" "RabbitMQ__UserName=walden"
            "-e" "RabbitMQ__Password=walden"
        )
    fi

    # search-service 需要 Elasticsearch 和服务 URL 配置
    if [[ "$service_name" == "search-service" ]]; then
        extra_env+=(
            "-e" "Elasticsearch__Url=http://go-nomads-elasticsearch:9200"
            "-e" "ServiceUrls__CityService=http://go-nomads-city-service:8080"
            "-e" "ServiceUrls__CoworkingService=http://go-nomads-coworking-service:8080"
            "-e" "RabbitMQ__Host=go-nomads-rabbitmq"
            "-e" "RabbitMQ__Username=walden"
            "-e" "RabbitMQ__Password=walden"
        )
    fi

    # 启动应用容器
    echo -e "${YELLOW}  启动应用容器...${NC}"
    
    # Gateway 使用生产配置（appsettings.json 中的 go-nomads-consul:8500）
    # 其他服务使用 Development 环境
    local env_config=()
    if [[ "$service_name" == "gateway" ]]; then
        # Gateway 使用生产配置，不需要 Dapr
        env_config+=("-e" "ASPNETCORE_ENVIRONMENT=Production")
    else
        # 其他服务使用 Development 环境
        env_config+=("-e" "ASPNETCORE_ENVIRONMENT=Development")
    fi
    
    # Gateway 容器配置（不需要 Dapr）
    if [[ "$service_name" == "gateway" ]]; then
        $CONTAINER_RUNTIME run -d \
            --name "go-nomads-$service_name" \
            --network "$NETWORK_NAME" \
            --label "com.docker.compose.project=go-nomads" \
            --label "com.docker.compose.service=$service_name" \
            -p "$app_port:5000" \
            "${env_config[@]}" \
            -e ASPNETCORE_URLS="http://+:5000" \
            -e Consul__Address="http://go-nomads-consul:7500" \
            -e HTTP_PROXY= \
            -e HTTPS_PROXY= \
            -e NO_PROXY= \
            "${extra_env[@]}" \
            -v "${publish_dir}:/app:ro" \
            -w /app \
            mcr.microsoft.com/dotnet/aspnet:9.0 \
            dotnet "$dll_name" > /dev/null
    else
        # 其他服务需要 Dapr 支持
        $CONTAINER_RUNTIME run -d \
            --name "go-nomads-$service_name" \
            --network "$NETWORK_NAME" \
            --label "com.docker.compose.project=go-nomads" \
            --label "com.docker.compose.service=$service_name" \
            -p "$app_port:8080" \
            -p "$dapr_http_port:$dapr_http_port" \
            "${env_config[@]}" \
            -e ASPNETCORE_URLS="http://+:8080" \
            -e DAPR_GRPC_PORT="50001" \
            -e DAPR_HTTP_PORT="$dapr_http_port" \
            -e Consul__Address="http://go-nomads-consul:7500" \
            -e DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_HTTP2UNENCRYPTEDSUPPORT=true \
            -e HTTP_PROXY= \
            -e HTTPS_PROXY= \
            -e NO_PROXY= \
            "${extra_env[@]}" \
            -v "${publish_dir}:/app:ro" \
            -w /app \
            mcr.microsoft.com/dotnet/aspnet:9.0 \
            dotnet "$dll_name" > /dev/null
    fi
    
    if container_running "go-nomads-$service_name"; then
        echo -e "${GREEN}  应用容器启动成功!${NC}"
    else
        echo -e "${RED}  [错误] 应用容器启动失败${NC}"
        echo -e "${YELLOW}  查看日志: $CONTAINER_RUNTIME logs go-nomads-$service_name${NC}"
        return 1
    fi

    sleep 2

    # Gateway 不需要 Dapr sidecar（只使用 YARP 反向代理 + JWT 验证）
    if [[ "$service_name" == "gateway" ]]; then
        echo -e "${YELLOW}  跳过 Dapr sidecar ($service_name 不需要 Dapr)${NC}"
        echo -e "${GREEN}  $service_name 部署成功!${NC}"
        echo -e "${GREEN}  应用端口: http://localhost:$app_port${NC}"
        sleep 2
        return 0
    fi

    # 启动 Dapr sidecar（共享应用容器的网络命名空间）
    # 使用 --network container:<app-container> 实现真正的 sidecar 模式
    # 应用和 Dapr 通过 localhost 通信，端口已在应用容器暴露
    echo -e "${YELLOW}  启动 Dapr sidecar (container sidecar 模式)...${NC}"
    
    $CONTAINER_RUNTIME run -d \
        --name "go-nomads-$service_name-dapr" \
        --network "container:go-nomads-$service_name" \
        --label "com.docker.compose.project=go-nomads" \
        --label "com.docker.compose.service=$service_name-dapr" \
        daprio/daprd:latest \
        ./daprd \
        --app-id "$app_id" \
        --app-port 8080 \
        --dapr-http-port "$dapr_http_port" \
        --dapr-grpc-port 50001 \
        --log-level info > /dev/null
    
    if container_running "go-nomads-$service_name-dapr"; then
        echo -e "${GREEN}  Dapr sidecar 启动成功!${NC}"
        echo -e "${GREEN}  $service_name 部署成功!${NC}"
        echo -e "${GREEN}  应用端口: http://localhost:$app_port${NC}"
        echo -e "${GREEN}  Dapr HTTP: localhost:$dapr_http_port (通过应用容器暴露)${NC}"
        echo -e "${GREEN}  Dapr gRPC: localhost:50001 (container sidecar 模式)${NC}"
        sleep 2
        return 0
    else
        echo -e "${RED}  [错误] Dapr sidecar 启动失败${NC}"
        echo -e "${YELLOW}  查看日志: $CONTAINER_RUNTIME logs go-nomads-$service_name-dapr${NC}"
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
    
    # 确保网络存在
    ensure_network
    
    # 检查 Redis
    if ! container_running "go-nomads-redis"; then
        echo -e "${RED}  [错误] Redis 未运行${NC}"
        echo -e "${YELLOW}  请先运行基础设施部署脚本: ./deploy-infrastructure-local.sh${NC}"
        exit 1
    fi
    echo -e "${GREEN}  Redis 运行正常${NC}"

    # 检查 Consul
    if ! container_running "go-nomads-consul"; then
        echo -e "${RED}  [错误] Consul 未运行${NC}"
        echo -e "${YELLOW}  请先运行基础设施部署脚本: ./deploy-infrastructure-local.sh${NC}"
        exit 1
    fi
    echo -e "${GREEN}  Consul 运行正常${NC}"
    
    # 检查 Nginx
    if ! container_running "go-nomads-nginx"; then
        echo -e "${YELLOW}  [提示] Nginx 未运行，可通过 deploy-infrastructure-local.sh 部署${NC}"
    else
        echo -e "${GREEN}  Nginx 运行正常${NC}"
    fi
    
    echo -e "${GREEN}  前置条件检查完成${NC}"
}

# 主部署流程
main() {
    show_header "Go-Nomads 服务部署 (本地构建 + $CONTAINER_RUNTIME)"
    
    echo -e "${BLUE}使用容器运行时: $(basename "$CONTAINER_RUNTIME")${NC}"
    echo -e "${BLUE}根目录: $ROOT_DIR${NC}"
    if [ "$SKIP_BUILD" = true ]; then
        echo -e "${YELLOW}构建模式: 跳过构建${NC}"
    else
        echo -e "${BLUE}构建模式: 完整构建${NC}"
    fi
    echo ""
    
    # 检查前置条件
    check_prerequisites
    echo ""
    
    # 部署 Gateway
    deploy_service_local \
        "gateway" \
        "src/Gateway/Gateway" \
        "5080" \
        "Gateway.dll" \
        "3500" \
        "gateway"
    echo ""
    
    # 部署 ProductService
    deploy_service_local \
        "product-service" \
        "src/Services/ProductService/ProductService" \
        "5002" \
        "ProductService.dll" \
        "3501" \
        "product-service"
    echo ""
    
    # 部署 UserService
    deploy_service_local \
        "user-service" \
        "src/Services/UserService/UserService" \
        "5001" \
        "UserService.dll" \
        "3502" \
        "user-service"
    echo ""
    
    # 部署 DocumentService
    deploy_service_local \
        "document-service" \
        "src/Services/DocumentService/DocumentService" \
        "5003" \
        "DocumentService.dll" \
        "3503" \
        "document-service"
    echo ""
    
    # 部署 CityService
    deploy_service_local \
        "city-service" \
        "src/Services/CityService/CityService" \
        "8002" \
        "CityService.dll" \
        "3504" \
        "city-service"
    echo ""
    
    # 部署 EventService
    deploy_service_local \
        "event-service" \
        "src/Services/EventService/EventService" \
        "8005" \
        "EventService.dll" \
        "3505" \
        "event-service"
    echo ""
    
    # 部署 CoworkingService
    deploy_service_local \
        "coworking-service" \
        "src/Services/CoworkingService/CoworkingService" \
        "8006" \
        "CoworkingService.dll" \
        "3506" \
        "coworking-service"
    echo ""
    
    # 部署 AIService
    deploy_service_local \
        "ai-service" \
        "src/Services/AIService/AIService" \
        "8009" \
        "AIService.dll" \
        "3509" \
        "ai-service"
    echo ""
    
    # 部署 CacheService
    deploy_service_local \
        "cache-service" \
        "src/Services/CacheService/CacheService" \
        "8010" \
        "CacheService.dll" \
        "3512" \
        "cache-service"
    echo ""
    
    # 部署 MessageService
    deploy_service_local \
        "message-service" \
        "src/Services/MessageService/MessageService/API" \
        "5005" \
        "MessageService.dll" \
        "3511" \
        "message-service"
    echo ""
    
    # 部署 AccommodationService
    deploy_service_local \
        "accommodation-service" \
        "src/Services/AccommodationService/AccommodationService" \
        "8012" \
        "AccommodationService.dll" \
        "3513" \
        "accommodation-service"
    echo ""
    
    # 部署 InnovationService
    deploy_service_local \
        "innovation-service" \
        "src/Services/InnovationService/InnovationService" \
        "8011" \
        "InnovationService.dll" \
        "3514" \
        "innovation-service"
    echo ""
    
    # 部署 SearchService
    deploy_service_local \
        "search-service" \
        "src/Services/SearchService/SearchService" \
        "8015" \
        "SearchService.dll" \
        "3517" \
        "search-service"
    echo ""
    
    # 显示部署摘要
    show_header "部署摘要"
    
    echo -e "${GREEN}所有服务部署完成!${NC}"
    echo ""
    echo -e "${BLUE}反向代理:${NC}"
    echo -e "  ${GREEN}Nginx (推荐):      http://localhost${NC}"
    echo ""
    echo -e "${BLUE}服务访问地址:${NC}"
    echo -e "  ${GREEN}Gateway:             http://localhost:5080${NC}"
    echo -e "  ${GREEN}User Service:        http://localhost:5001${NC}"
    echo -e "  ${GREEN}Product Service:     http://localhost:5002${NC}"
    echo -e "  ${GREEN}Document Service:    http://localhost:5003${NC}"
    echo -e "  ${GREEN}City Service:        http://localhost:8002${NC}"
    echo -e "  ${GREEN}Event Service:       http://localhost:8005${NC}"
    echo -e "  ${GREEN}Coworking Service:   http://localhost:8006${NC}"
    echo -e "  ${GREEN}AI Service:          http://localhost:8009${NC}"
    echo -e "  ${GREEN}Cache Service:       http://localhost:8010${NC}"
    echo -e "  ${GREEN}Accommodation Service: http://localhost:8012${NC}"
    echo -e "  ${GREEN}Message Service:     http://localhost:5005${NC}"
    echo -e "  ${GREEN}Innovation Service:  http://localhost:8011${NC}"
    echo -e "  ${GREEN}Search Service:      http://localhost:8015${NC}"
    echo -e "  ${GREEN}Message Swagger:     http://localhost:5005/swagger${NC}"
    echo ""
    echo -e "${BLUE}Dapr 配置:${NC}"
    echo -e "  ${GREEN}模式:             Container Sidecar (共享网络命名空间)${NC}"
    echo -e "  ${GREEN}gRPC 端口:        50001 (通过 DAPR_GRPC_PORT 环境变量)${NC}"
    echo -e "  ${GREEN}HTTP 端口:        3500-3511 (各服务独立端口)${NC}"
    echo ""
    echo -e "${BLUE}基础设施:${NC}"
    echo -e "  ${GREEN}Consul UI:        http://localhost:8500${NC}"
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
