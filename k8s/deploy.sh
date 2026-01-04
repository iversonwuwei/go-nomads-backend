#!/bin/bash
#
# Go-Nomads Kubernetes 部署脚本
# 使用方法: ./deploy.sh [环境] [操作]
# 环境: dev, staging, prod (默认: dev)
# 操作: deploy, delete, status (默认: deploy)
#

set -e

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# 默认参数
ENVIRONMENT=${1:-dev}
ACTION=${2:-deploy}
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
K8S_DIR="$SCRIPT_DIR"

# Docker Registry 配置 (请根据实际情况修改)
DOCKER_REGISTRY=${DOCKER_REGISTRY:-"your-registry.com"}
IMAGE_TAG=${IMAGE_TAG:-"latest"}

# 打印带颜色的消息
print_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# 检查 kubectl 是否安装
check_kubectl() {
    if ! command -v kubectl &> /dev/null; then
        print_error "kubectl 未安装，请先安装 kubectl"
        exit 1
    fi
    print_success "kubectl 已安装"
}

# 检查 Helm 是否安装
check_helm() {
    if ! command -v helm &> /dev/null; then
        print_error "Helm 未安装，Dapr 安装需要 Helm，请先安装 Helm"
        return 1
    fi
    print_success "Helm 已安装"
    return 0
}

# 检查集群连接
check_cluster() {
    if ! kubectl cluster-info &> /dev/null; then
        print_error "无法连接到 Kubernetes 集群"
        exit 1
    fi
    print_success "已连接到 Kubernetes 集群"
}

# 安装 Dapr
install_dapr() {
    print_info "========== 安装 Dapr =========="
    
    # 检查 Dapr 是否已安装
    if kubectl get pods -n dapr-system --no-headers 2>/dev/null | grep -q .; then
        print_success "Dapr 已安装，跳过安装步骤"
        return 0
    fi
    
    print_info "添加 Dapr Helm 仓库..."
    helm repo add dapr https://dapr.github.io/helm-charts/ 2>/dev/null || true
    helm repo update
    
    print_info "创建 dapr-system 命名空间..."
    kubectl create namespace dapr-system --dry-run=client -o yaml | kubectl apply -f -
    
    print_info "安装 Dapr..."
    helm upgrade --install dapr dapr/dapr \
        --namespace dapr-system \
        --set global.ha.enabled=false \
        --wait \
        --timeout 5m
    
    print_info "等待 Dapr 组件就绪..."
    max_retries=30
    retry_count=0
    while [ $retry_count -lt $max_retries ]; do
        running_pods=$(kubectl get pods -n dapr-system --no-headers 2>/dev/null | grep -c "Running" || echo "0")
        total_pods=$(kubectl get pods -n dapr-system --no-headers 2>/dev/null | wc -l)
        
        if [ "$running_pods" -eq "$total_pods" ] && [ "$total_pods" -gt 0 ]; then
            print_success "Dapr 所有组件已就绪 ($running_pods/$total_pods)"
            break
        fi
        
        retry_count=$((retry_count + 1))
        print_info "等待 Dapr 组件就绪... ($retry_count/$max_retries) - Running: $running_pods/$total_pods"
        sleep 10
    done
    
    if [ $retry_count -eq $max_retries ]; then
        print_warning "Dapr 组件启动超时，请手动检查状态: kubectl get pods -n dapr-system"
        return 1
    fi
    
    print_success "Dapr 安装完成"
    return 0
}

# 替换镜像变量
replace_variables() {
    local file=$1
    sed -e "s|\${DOCKER_REGISTRY}|$DOCKER_REGISTRY|g" \
        -e "s|\${IMAGE_TAG}|$IMAGE_TAG|g" \
        "$file"
}

# 应用配置文件
apply_config() {
    local file=$1
    local description=$2
    
    print_info "正在部署: $description"
    
    if [[ -f "$file" ]]; then
        if [[ "$file" == *"service"* ]] || [[ "$file" == *"gateway"* ]]; then
            # 对服务配置进行变量替换
            replace_variables "$file" | kubectl apply -f -
        else
            kubectl apply -f "$file"
        fi
        print_success "成功部署: $description"
    else
        print_warning "文件不存在: $file"
    fi
}

# 等待部署就绪
wait_for_deployment() {
    local deployment=$1
    local namespace=$2
    local timeout=${3:-300}
    
    print_info "等待 $deployment 就绪..."
    kubectl rollout status deployment/$deployment -n $namespace --timeout=${timeout}s
}

# 部署基础配置
deploy_base() {
    print_info "========== 部署基础配置 =========="
    
    apply_config "$K8S_DIR/base/namespace.yaml" "命名空间"
    apply_config "$K8S_DIR/base/configmap.yaml" "ConfigMap"
    apply_config "$K8S_DIR/base/secrets.yaml" "Secrets"
    
    print_success "基础配置部署完成"
}

# 部署基础设施
deploy_infrastructure() {
    print_info "========== 部署基础设施服务 =========="
    
    apply_config "$K8S_DIR/infrastructure/redis.yaml" "Redis"
    apply_config "$K8S_DIR/infrastructure/rabbitmq.yaml" "RabbitMQ"
    apply_config "$K8S_DIR/infrastructure/elasticsearch.yaml" "Elasticsearch"
    apply_config "$K8S_DIR/infrastructure/consul.yaml" "Consul"
    
    # 等待基础设施就绪
    print_info "等待基础设施服务就绪..."
    sleep 10
    
    wait_for_deployment "redis" "go-nomads" 120
    wait_for_deployment "rabbitmq" "go-nomads" 180
    wait_for_deployment "consul" "go-nomads" 120
    
    print_success "基础设施服务部署完成"
}

# 部署监控服务
deploy_monitoring() {
    print_info "========== 部署监控服务 =========="
    
    apply_config "$K8S_DIR/monitoring/prometheus.yaml" "Prometheus"
    apply_config "$K8S_DIR/monitoring/grafana.yaml" "Grafana"
    apply_config "$K8S_DIR/monitoring/zipkin.yaml" "Zipkin"
    
    print_success "监控服务部署完成"
}

# 部署业务服务
deploy_services() {
    print_info "========== 部署业务服务 =========="
    
    # Gateway
    apply_config "$K8S_DIR/services/gateway.yaml" "API Gateway"
    
    # 核心服务
    apply_config "$K8S_DIR/services/user-service.yaml" "User Service"
    apply_config "$K8S_DIR/services/city-service.yaml" "City Service"
    apply_config "$K8S_DIR/services/coworking-service.yaml" "Coworking Service"
    apply_config "$K8S_DIR/services/event-service.yaml" "Event Service"
    apply_config "$K8S_DIR/services/ai-service.yaml" "AI Service"
    apply_config "$K8S_DIR/services/message-service.yaml" "Message Service"
    apply_config "$K8S_DIR/services/cache-service.yaml" "Cache Service"
    
    print_success "业务服务部署完成"
}

# 完整部署
deploy_all() {
    print_info "开始完整部署 Go-Nomads 到 Kubernetes..."
    print_info "环境: $ENVIRONMENT"
    print_info "Docker Registry: $DOCKER_REGISTRY"
    print_info "镜像标签: $IMAGE_TAG"
    echo ""
    
    # 1. 安装 Dapr（如果尚未安装）
    if check_helm; then
        install_dapr
        echo ""
    else
        print_warning "Helm 未安装，跳过 Dapr 安装。业务服务将无法使用 Dapr sidecar。"
    fi
    
    # 2. 部署基础配置
    deploy_base
    echo ""
    
    # 3. 部署基础设施服务
    deploy_infrastructure
    echo ""
    
    # 4. 部署监控服务
    deploy_monitoring
    echo ""
    
    # 5. 部署业务服务
    deploy_services
    echo ""
    
    print_success "========== 部署完成 =========="
    print_info "使用 kubectl get pods -n go-nomads 查看 Pod 状态"
    print_info "使用 kubectl get services -n go-nomads 查看服务状态"
    print_info "使用 dapr list -k 查看 Dapr 应用状态"
}

# 删除所有资源
delete_all() {
    print_warning "即将删除 go-nomads 命名空间下的所有资源..."
    read -p "确定要继续吗? (y/N) " confirm
    
    if [[ "$confirm" == "y" || "$confirm" == "Y" ]]; then
        kubectl delete namespace go-nomads --ignore-not-found
        print_success "资源删除完成"
    else
        print_info "操作已取消"
    fi
}

# 查看状态
show_status() {
    print_info "========== Go-Nomads 部署状态 =========="
    echo ""
    
    print_info "Pods:"
    kubectl get pods -n go-nomads -o wide
    echo ""
    
    print_info "Services:"
    kubectl get services -n go-nomads
    echo ""
    
    print_info "Deployments:"
    kubectl get deployments -n go-nomads
    echo ""
    
    print_info "Ingress:"
    kubectl get ingress -n go-nomads
    echo ""
    
    print_info "PersistentVolumeClaims:"
    kubectl get pvc -n go-nomads
}

# 构建并推送 Docker 镜像
build_and_push() {
    print_info "========== 构建并推送 Docker 镜像 =========="
    
    local PROJECT_ROOT="$(dirname "$K8S_DIR")"
    
    # 服务列表
    local services=(
        "gateway:src/Gateway/Gateway/Dockerfile"
        "user-service:src/Services/UserService/UserService/Dockerfile"
        "city-service:src/Services/CityService/CityService/Dockerfile"
        "coworking-service:src/Services/CoworkingService/CoworkingService/Dockerfile"
        "event-service:src/Services/EventService/EventService/Dockerfile"
        "ai-service:src/Services/AIService/AIService/Dockerfile"
        "message-service:src/Services/MessageService/MessageService/API/Dockerfile"
        "cache-service:src/Services/CacheService/CacheService/Dockerfile"
    )
    
    for service in "${services[@]}"; do
        IFS=':' read -r name dockerfile <<< "$service"
        print_info "构建 $name..."
        
        docker build --platform linux/amd64 \
            -t "$DOCKER_REGISTRY/go-nomads-$name:$IMAGE_TAG" \
            -f "$PROJECT_ROOT/$dockerfile" \
            "$PROJECT_ROOT"
        
        print_info "推送 $name..."
        docker push "$DOCKER_REGISTRY/go-nomads-$name:$IMAGE_TAG"
        
        print_success "$name 构建并推送完成"
    done
    
    print_success "所有镜像构建并推送完成"
}

# 主函数
main() {
    echo ""
    echo "================================"
    echo "  Go-Nomads Kubernetes 部署工具"
    echo "================================"
    echo ""
    
    check_kubectl
    check_cluster
    
    case $ACTION in
        deploy)
            deploy_all
            ;;
        delete)
            delete_all
            ;;
        status)
            show_status
            ;;
        build)
            build_and_push
            ;;
        infrastructure)
            deploy_base
            deploy_infrastructure
            ;;
        services)
            deploy_services
            ;;
        monitoring)
            deploy_monitoring
            ;;
        dapr)
            check_helm || exit 1
            install_dapr
            ;;
        *)
            print_error "未知操作: $ACTION"
            echo "可用操作: deploy, delete, status, build, infrastructure, services, monitoring, dapr"
            exit 1
            ;;
    esac
}

# 执行主函数
main
