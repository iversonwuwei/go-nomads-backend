#!/bin/bash

# Go Nomads 可观测性基础设施启动脚本

set -e

echo "=========================================="
echo "  Go Nomads Observability Stack"
echo "  OpenTelemetry + Jaeger + Prometheus"
echo "=========================================="

# 检查 Docker 网络
if ! docker network ls | grep -q "go-nomads-network"; then
    echo "创建 Docker 网络: go-nomads-network"
    docker network create go-nomads-network
fi

# 启动可观测性基础设施
echo ""
echo "启动可观测性服务..."
docker-compose -f docker-compose-observability.yml up -d

echo ""
echo "等待服务启动..."
sleep 10

# 检查服务状态
echo ""
echo "检查服务状态..."
echo ""

# Jaeger
if curl -s http://localhost:16686 > /dev/null; then
    echo "✅ Jaeger UI: http://localhost:16686"
else
    echo "⏳ Jaeger UI 正在启动..."
fi

# Prometheus
if curl -s http://localhost:9090/-/healthy > /dev/null; then
    echo "✅ Prometheus: http://localhost:9090"
else
    echo "⏳ Prometheus 正在启动..."
fi

# Grafana
if curl -s http://localhost:3000/api/health > /dev/null; then
    echo "✅ Grafana: http://localhost:3000 (admin/admin)"
else
    echo "⏳ Grafana 正在启动..."
fi

echo ""
echo "=========================================="
echo "  可观测性服务已启动!"
echo "=========================================="
echo ""
echo "访问地址:"
echo "  - Jaeger UI:    http://localhost:16686"
echo "  - Prometheus:   http://localhost:9090"
echo "  - Grafana:      http://localhost:3000"
echo ""
echo "Grafana 默认登录: admin / admin"
echo ""
echo "如需启动 OpenTelemetry Collector (可选):"
echo "  docker-compose -f docker-compose-observability.yml --profile otel-collector up -d otel-collector"
echo ""
