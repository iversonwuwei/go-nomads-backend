#!/bin/bash
#
# Go-Nomads Infrastructure Helm 部署脚本
# 仅用于基础设施服务 (docker-compose-infras.yml 对应的组件)
# 业务服务请继续使用 Kustomize (k8s/)
#
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
RELEASE_NAME="go-nomads-infra"

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

# 安装/升级
helm_install() {
    local values_file=$(get_values_file)
    local namespace="go-nomads"
    
    if [ "$ENVIRONMENT" = "cce" ]; then
        namespace="default"
    fi
    
    print_info "========== Helm 部署基础设施: $ENVIRONMENT 环境 =========="
    print_info "Release: $RELEASE_NAME"
    print_info "Namespace: $namespace"
    print_info "Values: $values_file"
    print_info ""
    print_info "包含组件:"
    print_info "  - Redis"
    print_info "  - RabbitMQ"
    print_info "  - Elasticsearch (可选)"
    print_info "  - Zipkin (可选)"
    print_info "  - Prometheus (可选)"
    print_info "  - Grafana (可选)"
    
    # 创建命名空间（如果不存在）
    if [ "$namespace" != "default" ]; then
        kubectl create namespace "$namespace" --dry-run=client -o yaml | kubectl apply -f -
    fi
    
    # Helm 安装/升级
    helm upgrade --install "$RELEASE_NAME" "$CHART_DIR" \
        -f "$values_file" \
        -n "$namespace" \
        --create-namespace \
        --wait \
        --timeout 10m
    
    print_success "基础设施 Helm 部署完成！"
    print_info ""
    print_info "接下来请使用 Kustomize 部署业务服务:"
    print_info "  kubectl apply -k k8s/overlays/$ENVIRONMENT"
    print_info "  或"
    print_info "  ./k8s/deploy.sh $ENVIRONMENT"
    
    # 显示状态
    print_info ""
    print_info "查看基础设施状态:"
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
    print_success "基础设施卸载完成"
}

# 模板渲染（调试用）
helm_template() {
    local values_file=$(get_values_file)
    local output_file="$SCRIPT_DIR/k8s/manifests/helm-infra-$ENVIRONMENT.yaml"
    
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
    print_info "Go-Nomads 基础设施 Helm 部署工具"
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
