#!/bin/bash

# ============================================================
# Go-Nomads Backend - 本地构建并推送到华为云 SWR
# 使用方式: ./scripts/gitee-swr-build.sh [服务名称|all]
# 示例:
#   ./scripts/gitee-swr-build.sh gateway
#   ./scripts/gitee-swr-build.sh user-service
#   ./scripts/gitee-swr-build.sh all
# ============================================================

set -e

# 颜色输出
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# 加载环境变量（如果存在 .env 文件）
if [ -f ".env.swr" ]; then
    source .env.swr
fi

# 检查必要的环境变量
check_env_vars() {
    local missing=0
    
    if [ -z "$SWR_REGION" ]; then
        echo -e "${RED}错误: SWR_REGION 未设置${NC}"
        missing=1
    fi
    
    if [ -z "$SWR_AK" ]; then
        echo -e "${RED}错误: SWR_AK 未设置${NC}"
        missing=1
    fi
    
    if [ -z "$SWR_SK" ]; then
        echo -e "${RED}错误: SWR_SK 未设置${NC}"
        missing=1
    fi
    
    if [ -z "$SWR_ORGANIZATION" ]; then
        echo -e "${RED}错误: SWR_ORGANIZATION 未设置${NC}"
        missing=1
    fi
    
    if [ -z "$SWR_LOGIN_SERVER" ]; then
        echo -e "${RED}错误: SWR_LOGIN_SERVER 未设置${NC}"
        missing=1
    fi
    
    if [ $missing -eq 1 ]; then
        echo ""
        echo "请设置以下环境变量或创建 .env.swr 文件:"
        echo "  export SWR_REGION=cn-north-4"
        echo "  export SWR_AK=your-access-key"
        echo "  export SWR_SK=your-secret-key"
        echo "  export SWR_ORGANIZATION=go-nomads"
        echo "  export SWR_LOGIN_SERVER=swr.cn-north-4.myhuaweicloud.com"
        exit 1
    fi
}

# 服务配置
declare -A SERVICES=(
    ["gateway"]="src/Gateway/Gateway/Dockerfile"
    ["user-service"]="src/Services/UserService/UserService/Dockerfile"
    ["city-service"]="src/Services/CityService/CityService/Dockerfile"
    ["accommodation-service"]="src/Services/AccommodationService/AccommodationService/Dockerfile"
    ["coworking-service"]="src/Services/CoworkingService/CoworkingService/Dockerfile"
    ["event-service"]="src/Services/EventService/EventService/Dockerfile"
    ["ai-service"]="src/Services/AIService/AIService/Dockerfile"
    ["message-service"]="src/Services/MessageService/MessageService/API/Dockerfile"
    ["search-service"]="src/Services/SearchService/SearchService/Dockerfile"
    ["cache-service"]="src/Services/CacheService/CacheService/Dockerfile"
    ["innovation-service"]="src/Services/InnovationService/InnovationService/Dockerfile"
    ["product-service"]="src/Services/ProductService/ProductService/Dockerfile"
    ["document-service"]="src/Services/DocumentService/DocumentService/Dockerfile"
)

# 获取 Git 提交 SHA
get_image_tag() {
    if [ -n "$IMAGE_TAG" ]; then
        echo "$IMAGE_TAG"
    elif git rev-parse --short HEAD &> /dev/null; then
        echo $(git rev-parse --short HEAD)
    else
        echo "latest"
    fi
}

# 登录 SWR
swr_login() {
    echo -e "${BLUE}===== 登录华为云 SWR =====${NC}"
    docker login -u "${SWR_REGION}@${SWR_AK}" -p "${SWR_SK}" ${SWR_LOGIN_SERVER}
    echo -e "${GREEN}✓ 登录成功${NC}"
}

# 构建并推送单个服务
build_and_push_service() {
    local service_name=$1
    local dockerfile=${SERVICES[$service_name]}
    local tag=$(get_image_tag)
    local image="${SWR_LOGIN_SERVER}/${SWR_ORGANIZATION}/${service_name}"
    
    if [ -z "$dockerfile" ]; then
        echo -e "${RED}错误: 未知服务 '${service_name}'${NC}"
        echo "可用服务: ${!SERVICES[@]}"
        return 1
    fi
    
    if [ ! -f "$dockerfile" ]; then
        echo -e "${RED}错误: Dockerfile 不存在: ${dockerfile}${NC}"
        return 1
    fi
    
    echo -e "${BLUE}===== 构建 ${service_name} =====${NC}"
    echo "  Dockerfile: ${dockerfile}"
    echo "  镜像: ${image}:${tag}"
    
    # 构建镜像
    docker build \
        -f ${dockerfile} \
        -t ${image}:${tag} \
        -t ${image}:latest \
        .
    
    echo -e "${BLUE}===== 推送 ${service_name} =====${NC}"
    docker push ${image}:${tag}
    docker push ${image}:latest
    
    echo -e "${GREEN}✓ ${service_name} 推送成功${NC}"
    echo ""
}

# 构建所有服务
build_all_services() {
    local success=0
    local failed=0
    
    for service_name in "${!SERVICES[@]}"; do
        if build_and_push_service "$service_name"; then
            ((success++))
        else
            ((failed++))
        fi
    done
    
    echo ""
    echo -e "${BLUE}===== 构建完成 =====${NC}"
    echo -e "${GREEN}成功: ${success}${NC}"
    if [ $failed -gt 0 ]; then
        echo -e "${RED}失败: ${failed}${NC}"
    fi
}

# 显示帮助
show_help() {
    echo "Go-Nomads Backend - 华为云 SWR 构建脚本"
    echo ""
    echo "使用方式:"
    echo "  $0 <服务名称|all|list>"
    echo ""
    echo "命令:"
    echo "  list        列出所有可用服务"
    echo "  all         构建并推送所有服务"
    echo "  <服务名称>   构建并推送指定服务"
    echo ""
    echo "可用服务:"
    for service_name in "${!SERVICES[@]}"; do
        echo "  - ${service_name}"
    done
    echo ""
    echo "环境变量:"
    echo "  SWR_REGION        华为云区域 (如: cn-north-4)"
    echo "  SWR_AK            Access Key"
    echo "  SWR_SK            Secret Key"
    echo "  SWR_ORGANIZATION  SWR 组织名称"
    echo "  SWR_LOGIN_SERVER  SWR 登录服务器"
    echo "  IMAGE_TAG         镜像标签 (可选，默认使用 Git SHA)"
}

# 主函数
main() {
    local target=${1:-help}
    
    case $target in
        help|--help|-h)
            show_help
            ;;
        list)
            echo "可用服务:"
            for service_name in "${!SERVICES[@]}"; do
                echo "  - ${service_name}"
            done
            ;;
        all)
            check_env_vars
            swr_login
            build_all_services
            ;;
        *)
            check_env_vars
            swr_login
            build_and_push_service "$target"
            ;;
    esac
}

# 执行主函数
main "$@"
