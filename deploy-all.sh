#!/bin/bash
#
# Go-Nomads 完整部署脚本
# 基础设施使用 Helm，业务服务使用 Kustomize
#
# 使用方法: ./deploy-all.sh [环境] [操作]
# 环境: dev, staging, prod, cce (默认: dev)
# 操作: deploy, delete (默认: deploy)
#

set -e

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

ENVIRONMENT=${1:-dev}
ACTION=${2:-deploy}
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

print_info() { echo -e "${BLUE}[INFO]${NC} $1"; }
print_success() { echo -e "${GREEN}[SUCCESS]${NC} $1"; }
print_warning() { echo -e "${YELLOW}[WARNING]${NC} $1"; }
print_error() { echo -e "${RED}[ERROR]${NC} $1"; }

deploy() {
    print_info "=========================================="
    print_info "Go-Nomads 完整部署"
    print_info "环境: $ENVIRONMENT"
    print_info "=========================================="
    
    # Step 1: 部署基础设施 (Helm)
    print_info ""
    print_info "Step 1/2: 部署基础设施 (Helm)"
    print_info "----------------------------------------"
    "$SCRIPT_DIR/helm-deploy.sh" "$ENVIRONMENT" install
    
    # 等待基础设施就绪
    print_info ""
    print_info "等待基础设施就绪..."
    sleep 10
    
    # Step 2: 部署业务服务 (Kustomize)
    print_info ""
    print_info "Step 2/2: 部署业务服务 (Kustomize)"
    print_info "----------------------------------------"
    
    local namespace="go-nomads"
    if [ "$ENVIRONMENT" = "cce" ]; then
        namespace="default"
    fi
    
    # 使用 services-only kustomization
    if [ -f "$SCRIPT_DIR/k8s/overlays/$ENVIRONMENT/kustomization.yaml" ]; then
        kubectl apply -k "$SCRIPT_DIR/k8s/overlays/$ENVIRONMENT" -n "$namespace"
    else
        # 使用基础 kustomization-services.yaml
        kubectl apply -k "$SCRIPT_DIR/k8s" --kustomization "$SCRIPT_DIR/k8s/kustomization-services.yaml" -n "$namespace" 2>/dev/null || \
        kubectl apply -f <(kubectl kustomize "$SCRIPT_DIR/k8s" --load-restrictor LoadRestrictionsNone) -n "$namespace"
    fi
    
    print_success ""
    print_success "=========================================="
    print_success "Go-Nomads 部署完成！"
    print_success "=========================================="
    print_info ""
    print_info "查看 Pod 状态:"
    print_info "  kubectl get pods -n $namespace"
    print_info ""
    print_info "查看服务:"
    print_info "  kubectl get svc -n $namespace"
}

delete() {
    print_info "=========================================="
    print_info "Go-Nomads 完整卸载"
    print_info "环境: $ENVIRONMENT"
    print_info "=========================================="
    
    local namespace="go-nomads"
    if [ "$ENVIRONMENT" = "cce" ]; then
        namespace="default"
    fi
    
    # Step 1: 删除业务服务
    print_info ""
    print_info "Step 1/2: 删除业务服务..."
    kubectl delete -k "$SCRIPT_DIR/k8s" -n "$namespace" --ignore-not-found || true
    
    # Step 2: 删除基础设施
    print_info ""
    print_info "Step 2/2: 删除基础设施 (Helm)..."
    "$SCRIPT_DIR/helm-deploy.sh" "$ENVIRONMENT" uninstall
    
    print_success "卸载完成"
}

case $ACTION in
    deploy)
        deploy
        ;;
    delete)
        delete
        ;;
    *)
        print_error "未知操作: $ACTION"
        print_info "可用操作: deploy, delete"
        exit 1
        ;;
esac
