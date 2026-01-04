#!/usr/bin/env bash

# ============================================================
# 构建 Docker 镜像并推送到华为云 SWR 仓库
# ============================================================

set -e

# ============================================================
# 配置区域 - 请根据实际情况修改
# ============================================================
# SWR 仓库地址格式: swr.<region>.myhuaweicloud.com/<organization>
SWR_REGISTRY="${SWR_REGISTRY:-swr.ap-southeast-3.myhuaweicloud.com}"
SWR_ORGANIZATION="${SWR_ORGANIZATION:-go-nomads}"
IMAGE_TAG="${IMAGE_TAG:-latest}"

# 项目根目录
PROJECT_ROOT="$(cd "$(dirname "$0")/.." && pwd)"

# 服务列表 - 服务名:Dockerfile路径
SERVICES_LIST="
gateway:src/Gateway/Gateway/Dockerfile
user-service:src/Services/UserService/UserService/Dockerfile
city-service:src/Services/CityService/CityService/Dockerfile
coworking-service:src/Services/CoworkingService/CoworkingService/Dockerfile
accommodation-service:src/Services/AccommodationService/AccommodationService/Dockerfile
event-service:src/Services/EventService/EventService/Dockerfile
ai-service:src/Services/AIService/AIService/Dockerfile
cache-service:src/Services/CacheService/CacheService/Dockerfile
document-service:src/Services/DocumentService/DocumentService/Dockerfile
ecommerce-service:src/Services/EcommerceService/EcommerceService/Dockerfile
innovation-service:src/Services/InnovationService/InnovationService/Dockerfile
message-service:src/Services/MessageService/MessageService/API/Dockerfile
product-service:src/Services/ProductService/ProductService/Dockerfile
travel-planning-service:src/Services/TravelPlanningService/TravelPlanningService/Dockerfile
"

# 获取服务的 Dockerfile 路径
get_dockerfile_path() {
    local service_name=$1
    echo "$SERVICES_LIST" | grep "^${service_name}:" | cut -d: -f2
}

# 获取所有服务名
get_all_services() {
    echo "$SERVICES_LIST" | grep -v '^$' | cut -d: -f1
}

# ============================================================
# 函数定义
# ============================================================

# 打印帮助信息
print_help() {
    echo "用法: $0 [选项] [服务名...]"
    echo ""
    echo "选项:"
    echo "  -h, --help              显示帮助信息"
    echo "  -l, --login             登录到 SWR 仓库"
    echo "  -b, --build-only        只构建镜像，不推送"
    echo "  -p, --push-only         只推送镜像，不构建"
    echo "  -a, --all               构建并推送所有服务"
    echo "  -t, --tag <tag>         指定镜像标签 (默认: latest)"
    echo "  --list                  列出所有可用的服务"
    echo ""
    echo "环境变量:"
    echo "  SWR_REGISTRY            SWR 仓库地址 (默认: swr.cn-north-4.myhuaweicloud.com)"
    echo "  SWR_ORGANIZATION        SWR 组织名称 (默认: go-nomads)"
    echo "  SWR_AK                  华为云 Access Key (用于登录)"
    echo "  SWR_SK                  华为云 Secret Key (用于登录)"
    echo "  IMAGE_TAG               镜像标签 (默认: latest)"
    echo ""
    echo "示例:"
    echo "  $0 --login                          # 登录到 SWR"
    echo "  $0 event-service                    # 构建并推送 event-service"
    echo "  $0 -t v1.0.0 gateway user-service   # 使用 v1.0.0 标签构建并推送多个服务"
    echo "  $0 -a                               # 构建并推送所有服务"
    echo "  $0 -b event-service                 # 只构建 event-service"
}

# 列出所有服务
list_services() {
    echo "可用的服务列表:"
    echo "==============="
    for service in $(get_all_services); do
        echo "  - $service"
    done
}

# 登录到 SWR
login_swr() {
    echo "================================================"
    echo "登录到华为云 SWR: $SWR_REGISTRY"
    echo "================================================"
    
    if [ -n "$SWR_AK" ] && [ -n "$SWR_SK" ]; then
        # 使用 AK/SK 登录
        echo "使用 AK/SK 进行登录..."
        # 华为云 SWR 登录命令
        # 密码格式: 区域项目名@AK@SK 或直接使用临时登录指令
        docker login -u "${SWR_REGION:-cn-north-4}@${SWR_AK}" -p "${SWR_SK}" "$SWR_REGISTRY"
    else
        echo "请设置 SWR_AK 和 SWR_SK 环境变量，或手动执行登录命令。"
        echo ""
        echo "方法1: 使用 AK/SK 登录"
        echo "  export SWR_AK=<your-access-key>"
        echo "  export SWR_SK=<your-secret-key>"
        echo "  $0 --login"
        echo ""
        echo "方法2: 使用华为云 CLI 获取临时登录指令"
        echo "  在华为云控制台 -> 容器镜像服务 -> 我的镜像 -> 客户端上传"
        echo "  复制并执行登录指令"
        echo ""
        echo "方法3: 手动 docker login"
        echo "  docker login $SWR_REGISTRY"
        exit 1
    fi
}

# 构建单个服务镜像
build_service() {
    local service_name=$1
    local dockerfile_path=$(get_dockerfile_path "$service_name")
    
    if [ -z "$dockerfile_path" ]; then
        echo "错误: 未知的服务 '$service_name'"
        echo "使用 --list 查看可用的服务列表"
        return 1
    fi
    
    local full_image_name="$SWR_REGISTRY/$SWR_ORGANIZATION/$service_name:$IMAGE_TAG"
    
    echo "================================================"
    echo "构建镜像: $service_name"
    echo "Dockerfile: $dockerfile_path"
    echo "镜像名称: $full_image_name"
    echo "================================================"
    
    cd "$PROJECT_ROOT"
    
    # 使用 --platform linux/amd64 确保镜像兼容 x86_64 服务器
    # 使用 --provenance=false 和 --sbom=false 避免生成多平台 manifest
    docker build \
        --platform linux/amd64 \
        --provenance=false \
        --sbom=false \
        -t "$full_image_name" \
        -f "$dockerfile_path" \
        .
    
    echo "✅ 镜像构建成功: $full_image_name"
}

# 推送单个服务镜像
push_service() {
    local service_name=$1
    local full_image_name="$SWR_REGISTRY/$SWR_ORGANIZATION/$service_name:$IMAGE_TAG"
    
    echo "================================================"
    echo "推送镜像: $full_image_name"
    echo "================================================"
    
    docker push "$full_image_name"
    
    echo "✅ 镜像推送成功: $full_image_name"
}

# 构建并推送单个服务
build_and_push_service() {
    local service_name=$1
    
    if [ "$BUILD_ONLY" = true ]; then
        build_service "$service_name"
    elif [ "$PUSH_ONLY" = true ]; then
        push_service "$service_name"
    else
        build_service "$service_name"
        push_service "$service_name"
    fi
}

# ============================================================
# 主逻辑
# ============================================================

BUILD_ONLY=false
PUSH_ONLY=false
DO_LOGIN=false
BUILD_ALL=false
SERVICES_TO_BUILD=""

# 解析命令行参数
while [[ $# -gt 0 ]]; do
    case $1 in
        -h|--help)
            print_help
            exit 0
            ;;
        -l|--login)
            DO_LOGIN=true
            shift
            ;;
        -b|--build-only)
            BUILD_ONLY=true
            shift
            ;;
        -p|--push-only)
            PUSH_ONLY=true
            shift
            ;;
        -a|--all)
            BUILD_ALL=true
            shift
            ;;
        -t|--tag)
            IMAGE_TAG="$2"
            shift 2
            ;;
        --list)
            list_services
            exit 0
            ;;
        -*)
            echo "未知选项: $1"
            print_help
            exit 1
            ;;
        *)
            SERVICES_TO_BUILD="$SERVICES_TO_BUILD $1"
            shift
            ;;
    esac
done

# 执行登录
if [ "$DO_LOGIN" = true ]; then
    login_swr
    if [ -z "$SERVICES_TO_BUILD" ] && [ "$BUILD_ALL" = false ]; then
        exit 0
    fi
fi

# 确定要构建的服务
if [ "$BUILD_ALL" = true ]; then
    SERVICES_TO_BUILD=$(get_all_services)
fi

# 检查是否有服务需要构建
if [ -z "$SERVICES_TO_BUILD" ]; then
    echo "错误: 请指定要构建的服务，或使用 -a 构建所有服务"
    echo ""
    print_help
    exit 1
fi

# 显示构建信息
echo "================================================"
echo "SWR 仓库配置"
echo "================================================"
echo "Registry:     $SWR_REGISTRY"
echo "Organization: $SWR_ORGANIZATION"
echo "Tag:          $IMAGE_TAG"
echo "================================================"
echo ""

# 构建并推送每个服务
for service in $SERVICES_TO_BUILD; do
    build_and_push_service "$service"
    echo ""
done

echo "================================================"
echo "🎉 所有操作完成!"
echo "================================================"
