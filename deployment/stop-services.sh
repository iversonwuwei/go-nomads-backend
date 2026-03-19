#!/bin/bash

# ============================================================
# Go-Nomads Services Stop Script
# 停止所有微服务容器
# ============================================================

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# 容器运行时检测
CONTAINER_RUNTIME=""
if command -v podman &> /dev/null; then
    CONTAINER_RUNTIME="podman"
elif [ -x "/opt/podman/bin/podman" ]; then
    CONTAINER_RUNTIME="/opt/podman/bin/podman"
elif command -v docker &> /dev/null; then
    CONTAINER_RUNTIME="docker"
else
    echo -e "${RED}错误: 未找到 Podman 或 Docker${NC}"
    exit 1
fi

echo -e "${BLUE}============================================================${NC}"
echo -e "${BLUE}  停止 Go-Nomads 服务 (使用 $CONTAINER_RUNTIME)${NC}"
echo -e "${BLUE}============================================================${NC}"
echo ""

# 服务列表
SERVICES=(
    "go-nomads-gateway"
    "go-nomads-user-service"
    "go-nomads-product-service"
    "go-nomads-document-service"
)

# 停止服务
for service in "${SERVICES[@]}"; do
    if $CONTAINER_RUNTIME ps -a --filter "name=$service" --format "{{.Names}}" | grep -q "^$service$"; then
        echo -e "${YELLOW}停止容器: $service${NC}"
        $CONTAINER_RUNTIME stop "$service" &> /dev/null
        $CONTAINER_RUNTIME rm "$service" &> /dev/null
        echo -e "${GREEN}✓ $service 已停止并删除${NC}"
    else
        echo -e "${BLUE}- $service 不存在，跳过${NC}"
    fi
done

echo ""
echo -e "${GREEN}所有服务已停止! ✓${NC}"
echo ""
echo -e "${BLUE}注意: 基础设施服务 (Redis, etc.) 仍在运行${NC}"
echo -e "${BLUE}如需停止基础设施，请运行: ./deploy-infrastructure.sh stop${NC}"
