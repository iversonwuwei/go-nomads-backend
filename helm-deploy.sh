#!/bin/bash
#
# Go-Nomads Helm 部署脚本
# 使用方法: ./helm-deploy.sh [环境] [操作]
# 环境: dev, staging, prod, cce (默认: dev)
# 操作: install, upgrade, uninstall, template, dry-run (默认: install)
#

set -e

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# 默认参数
ENVIRONMENT=${1:-dev}
ACTION=${2:-install}
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CHART_DIR="$SCRIPT_DIR/helm/go-nomads"
RELEASE_NAME="go-nomads"

# 打印函数
print_info() { echo -e "${BLUE}[INFO]${NC} $1"; }
print_success() { echo -e "${GREEN}[SUCCESS]${NC} $1"; }
print_warning() { echo -e "${YELLOW}[WARNING]${NC} $1"; }
print_error() { echo -e "${RED}[ERROR]${NC} $1"; }

# 检查 Helm 是否安装
check_helm() {
    if ! command -v helm &> /dev/null; then
        print_error "Helm 未安装，请先安装 Helm"
        print_info "安装方法: brew install helm (macOS) 或 参考 https://helm.sh/docs/intro/install/"
        exit 1
    fi
    print_success "Helm 版本: $(helm version --short)"
}

# 检查 kubectl 是否安装
check_kubectl() {
    if ! command -v kubectl &> /dev/null; then
        print_error "kubectl 未安装"
        exit 1
    fi
    print_success "kubectl 已安装"
}

# 检查集群连接
check_cluster() {
    if ! kubectl cluster-info &> /dev/null; then
        print_error "无法连接到 Kubernetes 集群"
        exit 1
    fi
    print_success "已连接到 Kubernetes 集群"
}

# 选择 values 文件
get_values_file() {
    case $ENVIRONMENT in
        cce)
            echo "$CHART_DIR/values-cce.yaml"
            ;;
        prod|production)
            echo "$CHART_DIR/values-prod.yaml"
            ;;
        staging)
            echo "$CHART_DIR/values-staging.yaml"
            ;;
        *)
            echo "$CHART_DIR/values.yaml"
            ;;
    esac
}

# 创建 SWR 镜像拉取 Secret
create_image_pull_secret() {
    local namespace=${1:-default}
    
    if [ -z "$SWR_USERNAME" ] || [ -z "$SWR_PASSWORD" ]; then
        print_warning "SWR_USERNAME 或 SWR_PASSWORD 未设置，跳过创建镜像拉取 Secret"
        print_info "请设置环境变量后重新运行，或手动创建 Secret:"
        print_info "  kubectl create secret docker-registry docker-registry-secret \\"
        print_info "    --docker-server=swr.ap-southeast-3.myhuaweicloud.com \\"
        print_info "    --docker-username=<username> \\"
        print_info "    --docker-password=<password> \\"
        print_info "    -n $namespace"
        return
    fi
    
    print_info "创建镜像拉取 Secret..."
    kubectl create secret docker-registry docker-registry-secret \
        --docker-server=swr.ap-southeast-3.myhuaweicloud.com \
        --docker-username="$SWR_USERNAME" \
        --docker-password="$SWR_PASSWORD" \
        -n "$namespace" \
        --dry-run=client -o yaml | kubectl apply -f -
    print_success "镜像拉取 Secret 已创建/更新"
}

# 安装/升级
helm_install() {
    local values_file=$(get_values_file)
    local namespace="go-nomads"
    
    if [ "$ENVIRONMENT" = "cce" ]; then
        namespace="default"
    fi
    
    print_info "========== Helm 部署: $ENVIRONMENT 环境 =========="
    print_info "Release: $RELEASE_NAME"
    print_info "Namespace: $namespace"
    print_info "Values: $values_file"
    
    # 创建命名空间（如果不存在）
    if [ "$namespace" != "default" ]; then
        kubectl create namespace "$namespace" --dry-run=client -o yaml | kubectl apply -f -
    fi
    
    # 创建镜像拉取 Secret
    create_image_pull_secret "$namespace"
    
    # Helm 安装/升级
    helm upgrade --install "$RELEASE_NAME" "$CHART_DIR" \
        -f "$values_file" \
        -n "$namespace" \
        --create-namespace \
        --wait \
        --timeout 10m
    
    print_success "Helm 部署完成！"
    
    # 显示状态
    print_info "查看部署状态:"
    helm status "$RELEASE_NAME" -n "$namespace"
}

# 卸载
helm_uninstall() {
    local namespace="go-nomads"
    if [ "$ENVIRONMENT" = "cce" ]; then
        namespace="default"
    fi
    
    print_info "卸载 Helm Release: $RELEASE_NAME"
    helm uninstall "$RELEASE_NAME" -n "$namespace" || true
    print_success "卸载完成"
}

# 模板渲染（调试用）
helm_template() {
    local values_file=$(get_values_file)
    local output_file="$SCRIPT_DIR/k8s/manifests/helm-rendered-$ENVIRONMENT.yaml"
    
    print_info "渲染 Helm 模板..."
    helm template "$RELEASE_NAME" "$CHART_DIR" \
        -f "$values_file" \
        > "$output_file"
    
    print_success "模板已渲染到: $output_file"
}

# Dry Run
helm_dry_run() {
    local values_file=$(get_values_file)
    local namespace="go-nomads"
    if [ "$ENVIRONMENT" = "cce" ]; then
        namespace="default"
    fi
    
    print_info "Helm Dry Run..."
    helm upgrade --install "$RELEASE_NAME" "$CHART_DIR" \
        -f "$values_file" \
        -n "$namespace" \
        --dry-run \
        --debug
}

# 主函数
main() {
    print_info "=========================================="
    print_info "Go-Nomads Helm 部署工具"
    print_info "环境: $ENVIRONMENT"
    print_info "操作: $ACTION"
    print_info "=========================================="
    
    check_helm
    check_kubectl
    
    case $ACTION in
        install|upgrade)
            check_cluster
            helm_install
            ;;
        uninstall|delete)
            check_cluster
            helm_uninstall
            ;;
        template)
            helm_template
            ;;
        dry-run)
            check_cluster
            helm_dry_run
            ;;
        *)
            print_error "未知操作: $ACTION"
            print_info "可用操作: install, upgrade, uninstall, template, dry-run"
            exit 1
            ;;
    esac
}

main
