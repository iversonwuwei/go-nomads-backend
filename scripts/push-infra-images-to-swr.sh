#!/usr/bin/env bash

# ============================================================
# 上传基础设施镜像到华为云 SWR 仓库
# ============================================================

set -e

# ============================================================
# 配置区域
# ============================================================
SWR_REGISTRY="${SWR_REGISTRY:-swr.ap-southeast-3.myhuaweicloud.com}"
SWR_ORGANIZATION="${SWR_ORGANIZATION:-go-nomads}"

# 基础设施镜像列表 - 源镜像:源标签:目标名称:目标标签
INFRA_IMAGES="
redis:7-alpine:redis:7-alpine
rabbitmq:3-management-alpine:rabbitmq:3-management-alpine
docker.elastic.co/elasticsearch/elasticsearch:8.11.0:elasticsearch:8.11.0
hashicorp/consul:latest:consul:latest
openzipkin/zipkin:latest:zipkin:latest
prom/prometheus:latest:prometheus:latest
grafana/grafana:latest:grafana:latest
daprio/dapr:latest:dapr:latest
daprio/daprd:latest:daprd:latest
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
    echo ""
    echo "环境变量:"
    echo "  SWR_REGISTRY      SWR 仓库地址 (默认: swr.ap-southeast-3.myhuaweicloud.com)"
    echo "  SWR_ORGANIZATION  SWR 组织名称 (默认: go-nomads)"
    echo ""
    echo "示例:"
    echo "  $0 --login      # 登录到 SWR"
    echo "  $0 --all        # 上传所有镜像"
}

list_images() {
    echo "将要上传的基础设施镜像列表:"
    echo "============================="
    echo "$INFRA_IMAGES" | grep -v '^$' | while IFS=':' read -r src_image src_tag dest_name dest_tag; do
        echo "  ${src_image}:${src_tag} -> ${SWR_REGISTRY}/${SWR_ORGANIZATION}/${dest_name}:${dest_tag}"
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
    echo "开始上传基础设施镜像到 SWR"
    echo "================================================"
    
    echo "$INFRA_IMAGES" | grep -v '^$' | while IFS=':' read -r src_image src_tag dest_name dest_tag; do
        local src="${src_image}:${src_tag}"
        local dest="${SWR_REGISTRY}/${SWR_ORGANIZATION}/${dest_name}:${dest_tag}"
        
        echo ""
        echo "处理镜像: $src -> $dest"
        echo "----------------------------------------"
        
        # 拉取源镜像 (指定 amd64 架构以确保服务器兼容性)
        echo "拉取源镜像: $src (linux/amd64)"
        docker pull --platform linux/amd64 "$src"
        
        # 打标签
        echo "打标签: $dest"
        docker tag "$src" "$dest"
        
        # 推送到 SWR
        echo "推送到 SWR: $dest"
        docker push "$dest"
        
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

case "$1" in
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
    *)
        print_help
        exit 1
        ;;
esac
