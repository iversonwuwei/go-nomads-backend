# Go-Nomads 快速部署指南

## 🎯 一键部署所有服务

### 1. 部署基础设施
```bash
cd deployment
./deploy-infrastructure.sh
```

这会部署：
- Redis (配置中心)
- Consul (服务发现)
- Zipkin (链路追踪)
- Prometheus (监控)
- Grafana (可视化)

### 2. 部署微服务
```bash
cd deployment
./deploy-services-local.sh
```

这会部署：
- Gateway (API 网关) - 端口 5000
- User Service (用户服务) - 端口 5001
- Product Service (产品服务) - 端口 5002
- Document Service (文档服务) - 端口 5003

## 🌐 访问地址

**微服务**
- Gateway: http://localhost:5000
- User Service: http://localhost:5001
- Product Service: http://localhost:5002
- Document Service: http://localhost:5003
- Document API 文档: http://localhost:5003/scalar/v1

**基础设施**
- Consul UI: http://localhost:8500
- Zipkin: http://localhost:9411
- Prometheus: http://localhost:9090
- Grafana: http://localhost:3000 (admin/admin)

## 🛑 停止服务

```bash
# 停止微服务
cd deployment
./stop-services.sh

# 停止基础设施
./deploy-infrastructure.sh stop
```

## 📋 查看状态

```bash
# 查看所有容器
/opt/podman/bin/podman ps

# 查看服务日志
/opt/podman/bin/podman logs go-nomads-gateway
/opt/podman/bin/podman logs -f go-nomads-user-service  # 实时日志
```

## 📚 详细文档

- [完整部署文档](DEPLOYMENT_SUCCESS.md)
- [部署架构](deployment/ARCHITECTURE.md)
- [端口指南](deployment/PORT_GUIDE.md)
