#!/bin/bash

# ============================================================
# MessageService 快速部署脚本
# 使用 docker-compose 部署 MessageService + RabbitMQ + Redis
# ============================================================

set -e

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# 脚本目录
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"

# 显示标题
show_header() {
    echo -e "${BLUE}"
    echo "============================================================"
    echo "  $1"
    echo "============================================================"
    echo -e "${NC}"
}

# 检查 Docker 或 Podman
check_runtime() {
    if command -v docker-compose &> /dev/null; then
        COMPOSE_CMD="docker-compose"
    elif command -v podman-compose &> /dev/null; then
        COMPOSE_CMD="podman-compose"
    elif command -v docker &> /dev/null && docker compose version &> /dev/null; then
        COMPOSE_CMD="docker compose"
    else
        echo -e "${RED}错误: 未找到 docker-compose 或 podman-compose${NC}"
        exit 1
    fi
    
    echo -e "${GREEN}使用容器运行时: $COMPOSE_CMD${NC}"
}

# 主部署流程
main() {
    show_header "MessageService 部署"
    
    echo -e "${BLUE}根目录: $ROOT_DIR${NC}"
    echo ""
    
    # 检查运行时
    check_runtime
    echo ""
    
    # 进入根目录
    cd "$ROOT_DIR"
    
    # 停止旧容器
    echo -e "${YELLOW}停止旧容器...${NC}"
    $COMPOSE_CMD -f docker-compose.messageservice.yml down 2>/dev/null || true
    echo ""
    
    # 构建并启动服务
    show_header "构建并启动服务"
    echo -e "${YELLOW}正在构建镜像...${NC}"
    $COMPOSE_CMD -f docker-compose.messageservice.yml build
    echo ""
    
    echo -e "${YELLOW}启动容器...${NC}"
    $COMPOSE_CMD -f docker-compose.messageservice.yml up -d
    echo ""
    
    # 等待服务就绪
    show_header "等待服务就绪"
    
    echo -e "${YELLOW}等待 Redis 就绪...${NC}"
    sleep 3
    
    echo -e "${YELLOW}等待 RabbitMQ 就绪...${NC}"
    for i in {1..30}; do
        if curl -s http://localhost:15672 > /dev/null 2>&1; then
            echo -e "${GREEN}RabbitMQ 已就绪!${NC}"
            break
        fi
        echo -n "."
        sleep 2
    done
    echo ""
    
    echo -e "${YELLOW}等待 MessageService 就绪...${NC}"
    for i in {1..30}; do
        if curl -s http://localhost:5005/swagger > /dev/null 2>&1; then
            echo -e "${GREEN}MessageService 已就绪!${NC}"
            break
        fi
        echo -n "."
        sleep 2
    done
    echo ""
    
    # 显示部署摘要
    show_header "部署成功!"
    
    echo -e "${BLUE}服务访问地址:${NC}"
    echo -e "  ${GREEN}MessageService API:  http://localhost:5005${NC}"
    echo -e "  ${GREEN}Swagger 文档:        http://localhost:5005/swagger${NC}"
    echo -e "  ${GREEN}AI Progress Hub:     ws://localhost:5005/hubs/ai-progress${NC}"
    echo -e "  ${GREEN}Notification Hub:    ws://localhost:5005/hubs/notifications${NC}"
    echo ""
    echo -e "${BLUE}基础设施:${NC}"
    echo -e "  ${GREEN}RabbitMQ 管理界面:   http://localhost:15672${NC}"
    echo -e "  ${GREEN}  用户名: guest${NC}"
    echo -e "  ${GREEN}  密码:   guest${NC}"
    echo -e "  ${GREEN}Redis:               redis://localhost:6379${NC}"
    echo ""
    echo -e "${BLUE}常用命令:${NC}"
    echo -e "  查看日志:       ${YELLOW}$COMPOSE_CMD -f docker-compose.messageservice.yml logs -f${NC}"
    echo -e "  查看容器状态:   ${YELLOW}$COMPOSE_CMD -f docker-compose.messageservice.yml ps${NC}"
    echo -e "  停止服务:       ${YELLOW}$COMPOSE_CMD -f docker-compose.messageservice.yml down${NC}"
    echo -e "  重启服务:       ${YELLOW}$COMPOSE_CMD -f docker-compose.messageservice.yml restart${NC}"
    echo ""
    
    # 显示容器状态
    echo -e "${BLUE}容器状态:${NC}"
    $COMPOSE_CMD -f docker-compose.messageservice.yml ps
    echo ""
    
    echo -e "${GREEN}部署完成! 🚀${NC}"
    echo ""
    echo -e "${YELLOW}提示: 访问 http://localhost:15672 查看 RabbitMQ 队列是否创建成功${NC}"
}

# 运行主流程
main
