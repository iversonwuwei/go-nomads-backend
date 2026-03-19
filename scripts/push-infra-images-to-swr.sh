#!/usr/bin/env bash

# ============================================================
# 上传基础设施镜像到华为云 SWR 仓库
# ============================================================

set -euo pipefail

# ============================================================
# 配置区域
# ============================================================
SWR_REGISTRY="${SWR_REGISTRY:-swr.ap-southeast-3.myhuaweicloud.com}"
SWR_ORGANIZATION="${SWR_ORGANIZATION:-go-nomads}"
PLATFORMS="${PLATFORMS:-linux/amd64,linux/arm64}"

REDIS_SOURCE_IMAGE="${REDIS_SOURCE_IMAGE:-redis:7-alpine}"
RABBITMQ_SOURCE_IMAGE="${RABBITMQ_SOURCE_IMAGE:-rabbitmq:3-management-alpine}"
ELASTICSEARCH_SOURCE_IMAGE="${ELASTICSEARCH_SOURCE_IMAGE:-docker.elastic.co/elasticsearch/elasticsearch:8.11.0}"
CONSUL_SOURCE_IMAGE="${CONSUL_SOURCE_IMAGE:-hashicorp/consul:latest}"
NGINX_SOURCE_IMAGE="${NGINX_SOURCE_IMAGE:-nginx:latest}"

# 基础设施镜像列表 - 源镜像|目标名称|目标标签
INFRA_IMAGES="
${REDIS_SOURCE_IMAGE}|redis|7-alpine
${RABBITMQ_SOURCE_IMAGE}|rabbitmq|3-management-alpine
${ELASTICSEARCH_SOURCE_IMAGE}|elasticsearch|8.11.0
${CONSUL_SOURCE_IMAGE}|consul|latest
${NGINX_SOURCE_IMAGE}|nginx|latest
"

# ============================================================
# 函数定义
# ============================================================

print_help() {
    echo "用法: $0 [选项]"
    echo ""
    echo "选项:"
    echo "  -h, --help      显示帮助信息"
    echo "  -l, --login     登录到 SWR 仓库"
    echo "  -a, --all       上传所有基础设施镜像"
    echo "  --list          列出所有要上传的镜像"
    echo "  --platforms     指定同步的平台列表 (默认: linux/amd64,linux/arm64)"
    echo ""
    echo "环境变量:"
    echo "  SWR_REGISTRY      SWR 仓库地址 (默认: swr.ap-southeast-3.myhuaweicloud.com)"
    echo "  SWR_ORGANIZATION  SWR 组织名称 (默认: go-nomads)"
    echo "  PLATFORMS         多架构平台列表 (默认: linux/amd64,linux/arm64)"
    echo "  REDIS_SOURCE_IMAGE          Redis 源镜像"
    echo "  RABBITMQ_SOURCE_IMAGE       RabbitMQ 源镜像"
    echo "  ELASTICSEARCH_SOURCE_IMAGE  Elasticsearch 源镜像"
    echo "  CONSUL_SOURCE_IMAGE         Consul 源镜像"
    echo "  NGINX_SOURCE_IMAGE          Nginx 源镜像"
    echo ""
    echo "示例:"
    echo "  $0 --login      # 登录到 SWR"
    echo "  $0 --all        # 上传所有镜像"
    echo "  REDIS_SOURCE_IMAGE=swr.cn-north-4.myhuaweicloud.com/library/redis:7-alpine $0 --all"
}

list_images() {
    echo "将要上传的基础设施镜像列表:"
    echo "============================="
    echo "目标平台: $PLATFORMS"
    echo "$INFRA_IMAGES" | grep -v '^$' | while IFS='|' read -r src dest_name dest_tag; do
        echo "  ${src} -> ${SWR_REGISTRY}/${SWR_ORGANIZATION}/${dest_name}:${dest_tag}"
    done
}

login_swr() {
    echo "================================================"
    echo "请手动登录到华为云 SWR: $SWR_REGISTRY"
    echo "================================================"
    echo ""
    echo "登录方式:"
    echo "1. 使用华为云控制台获取临时登录指令"
    echo "2. 在 SWR 控制台 -> 我的镜像 -> 客户端上传 -> 生成临时登录指令"
    echo ""
    echo "示例命令:"
    echo "docker login -u [区域项目名]@[AK] -p [临时密码] $SWR_REGISTRY"
}

push_all_images() {
    echo "================================================"
    echo "开始上传基础设施镜像到 SWR (多架构)"
    echo "================================================"

    if ! docker buildx version >/dev/null 2>&1; then
        echo "需要 Docker buildx 才能同步多架构 manifest。" >&2
        exit 1
    fi

    local platform_args=()
    local platform
    IFS=',' read -r -a platforms <<< "$PLATFORMS"
    for platform in "${platforms[@]}"; do
        platform_args+=(--platform "$platform")
    done
    
    echo "$INFRA_IMAGES" | grep -v '^$' | while IFS='|' read -r src dest_name dest_tag; do
        local dest="${SWR_REGISTRY}/${SWR_ORGANIZATION}/${dest_name}:${dest_tag}"
        
        echo ""
        echo "处理镜像: $src -> $dest"
        echo "----------------------------------------"

        echo "同步多架构 manifest: $PLATFORMS"
        docker buildx imagetools create \
            "${platform_args[@]}" \
            --tag "$dest" \
            "$src"

        echo "校验目标 manifest: $dest"
        docker buildx imagetools inspect "$dest" >/dev/null
        
        echo "✓ 完成: $dest"
    done
    
    echo ""
    echo "================================================"
    echo "所有基础设施镜像上传完成!"
    echo "================================================"
}

# ============================================================
# 主程序
# ============================================================

command="${1:-}"

case "$command" in
    -h|--help)
        print_help
        ;;
    -l|--login)
        login_swr
        ;;
    --list)
        list_images
        ;;
    -a|--all)
        push_all_images
        ;;
    --platforms)
        if [[ $# -lt 2 ]]; then
            echo "--platforms 需要传入平台列表，例如: linux/amd64,linux/arm64" >&2
            exit 1
        fi
        PLATFORMS="$2"
        shift 2
        case "${1:-}" in
            --list)
                list_images
                ;;
            -a|--all)
                push_all_images
                ;;
            *)
                print_help
                exit 1
                ;;
        esac
        ;;
    *)
        print_help
        exit 1
        ;;
esac
