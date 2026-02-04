#!/bin/bash

# ============================================================
# Go-Nomads Backend - 部署到阿里云服务器
# 从华为云 SWR 拉取镜像并部署到阿里云 ECS
# ============================================================

set -e

# 颜色输出
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# 加载环境变量
if [ -f ".env.deploy" ]; then
    source .env.deploy
fi

# 检查必要的环境变量
check_env_vars() {
    local missing=0
    
    # 华为云 SWR
    for var in SWR_REGION SWR_AK SWR_SK SWR_ORGANIZATION SWR_LOGIN_SERVER; do
        if [ -z "${!var}" ]; then
            echo -e "${RED}错误: ${var} 未设置${NC}"
            missing=1
        fi
    done
    
    # 阿里云服务器
    for var in ALIYUN_SERVER_HOST ALIYUN_SERVER_USER ALIYUN_DEPLOY_PATH; do
        if [ -z "${!var}" ]; then
            echo -e "${RED}错误: ${var} 未设置${NC}"
            missing=1
        fi
    done
    
    if [ $missing -eq 1 ]; then
        echo ""
        echo "请设置以下环境变量或创建 .env.deploy 文件"
        exit 1
    fi
}

# SSH 密钥路径
SSH_KEY="${ALIYUN_SSH_KEY:-~/.ssh/id_rsa}"

# 获取镜像标签
get_image_tag() {
    if [ -n "$IMAGE_TAG" ]; then
        echo "$IMAGE_TAG"
    elif git rev-parse --short HEAD &> /dev/null; then
        echo $(git rev-parse --short HEAD)
    else
        echo "latest"
    fi
}

# 部署到阿里云
deploy() {
    local tag=$(get_image_tag)
    
    echo -e "${BLUE}===== 部署到阿里云服务器 =====${NC}"
    echo "服务器: ${ALIYUN_SERVER_USER}@${ALIYUN_SERVER_HOST}"
    echo "部署路径: ${ALIYUN_DEPLOY_PATH}"
    echo "镜像标签: ${tag}"
    echo ""
    
    ssh -i ${SSH_KEY} ${ALIYUN_SERVER_USER}@${ALIYUN_SERVER_HOST} << DEPLOY_SCRIPT
        set -e
        
        echo "===== 登录华为云 SWR ====="
        docker login -u "${SWR_REGION}@${SWR_AK}" -p "${SWR_SK}" ${SWR_LOGIN_SERVER}
        
        echo "===== 进入部署目录 ====="
        cd ${ALIYUN_DEPLOY_PATH}
        
        echo "===== 设置环境变量 ====="
        export IMAGE_TAG="${tag}"
        export SWR_LOGIN_SERVER="${SWR_LOGIN_SERVER}"
        export SWR_ORGANIZATION="${SWR_ORGANIZATION}"
        
        echo "===== 拉取最新镜像 ====="
        docker-compose -f docker-compose-services-swr.yml pull
        
        echo "===== 停止旧服务 ====="
        docker-compose -f docker-compose-services-swr.yml down --remove-orphans || true
        
        echo "===== 启动新服务 ====="
        docker-compose -f docker-compose-services-swr.yml up -d
        
        echo "===== 清理旧镜像 ====="
        docker image prune -f
        
        echo "===== 检查服务状态 ====="
        docker-compose -f docker-compose-services-swr.yml ps
DEPLOY_SCRIPT
    
    echo -e "${GREEN}✓ 部署完成${NC}"
}

# 显示帮助
show_help() {
    echo "Go-Nomads - 部署到阿里云服务器"
    echo ""
    echo "使用方式:"
    echo "  $0 [选项]"
    echo ""
    echo "选项:"
    echo "  deploy      部署服务到阿里云"
    echo "  status      查看服务状态"
    echo "  logs        查看服务日志"
    echo "  help        显示帮助"
    echo ""
    echo "环境变量:"
    echo "  # 华为云 SWR"
    echo "  SWR_REGION, SWR_AK, SWR_SK, SWR_ORGANIZATION, SWR_LOGIN_SERVER"
    echo ""
    echo "  # 阿里云服务器"
    echo "  ALIYUN_SERVER_HOST, ALIYUN_SERVER_USER, ALIYUN_DEPLOY_PATH"
    echo "  ALIYUN_SSH_KEY (可选, 默认 ~/.ssh/id_rsa)"
    echo ""
    echo "  # 镜像标签"
    echo "  IMAGE_TAG (可选, 默认使用 Git SHA)"
}

# 查看状态
check_status() {
    echo -e "${BLUE}===== 检查服务状态 =====${NC}"
    ssh -i ${SSH_KEY} ${ALIYUN_SERVER_USER}@${ALIYUN_SERVER_HOST} << STATUS_SCRIPT
        cd ${ALIYUN_DEPLOY_PATH}
        docker-compose -f docker-compose-services-swr.yml ps
STATUS_SCRIPT
}

# 查看日志
view_logs() {
    local service=${1:-}
    echo -e "${BLUE}===== 查看服务日志 =====${NC}"
    ssh -i ${SSH_KEY} ${ALIYUN_SERVER_USER}@${ALIYUN_SERVER_HOST} << LOGS_SCRIPT
        cd ${ALIYUN_DEPLOY_PATH}
        if [ -n "${service}" ]; then
            docker-compose -f docker-compose-services-swr.yml logs -f --tail=100 ${service}
        else
            docker-compose -f docker-compose-services-swr.yml logs -f --tail=50
        fi
LOGS_SCRIPT
}

# 主函数
main() {
    local action=${1:-help}
    
    case $action in
        deploy)
            check_env_vars
            deploy
            ;;
        status)
            check_env_vars
            check_status
            ;;
        logs)
            check_env_vars
            view_logs "$2"
            ;;
        help|--help|-h)
            show_help
            ;;
        *)
            echo -e "${RED}未知命令: ${action}${NC}"
            show_help
            exit 1
            ;;
    esac
}

main "$@"
