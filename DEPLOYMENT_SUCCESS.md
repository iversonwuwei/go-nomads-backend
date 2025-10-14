# Go-Nomads Podman 部署完成

## ✅ 部署状态

所有服务已成功使用 Podman 部署！

## 🚀 已部署的服务

### 微服务
- **Gateway (API网关)**: http://localhost:5000
- **User Service (用户服务)**: http://localhost:5001  
- **Product Service (产品服务)**: http://localhost:5002
- **Document Service (文档服务)**: http://localhost:5003
  - API 文档: http://localhost:5003/scalar/v1

### 基础设施
- **Redis** (配置中心 & 状态存储): `localhost:6379`
- **Consul** (服务注册与发现): http://localhost:8500
- **Zipkin** (分布式追踪): http://localhost:9411
- **Prometheus** (监控): http://localhost:9090
- **Grafana** (可视化): http://localhost:3000 (用户名/密码: admin/admin)

## 📝 可用脚本

### 基础设施管理
```bash
# 部署基础设施
./deployment/deploy-infrastructure.sh

# 查看基础设施状态
./deployment/deploy-infrastructure.sh status

# 停止基础设施
./deployment/deploy-infrastructure.sh stop

# 清理基础设施
./deployment/deploy-infrastructure.sh clean
```

### 服务管理
```bash
# 部署所有服务 (本地构建方式 - 推荐)
./deployment/deploy-services-local.sh

# 部署所有服务 (Docker构建方式 - 较慢)
./deployment/deploy-services.sh

# 停止所有服务
./deployment/stop-services.sh
```

## 🔧 常用命令

### 查看容器状态
```bash
/opt/podman/bin/podman ps
```

### 查看服务日志
```bash
# Gateway 日志
/opt/podman/bin/podman logs go-nomads-gateway

# User Service 日志
/opt/podman/bin/podman logs go-nomads-user-service

# Product Service 日志
/opt/podman/bin/podman logs go-nomads-product-service

# Document Service 日志
/opt/podman/bin/podman logs go-nomads-document-service
```

### 实时查看日志
```bash
/opt/podman/bin/podman logs -f go-nomads-gateway
```

### 重启单个服务
```bash
/opt/podman/bin/podman restart go-nomads-gateway
```

## 🐛 问题排查

### 服务无法访问
1. 检查容器状态：`/opt/podman/bin/podman ps`
2. 查看容器日志：`/opt/podman/bin/podman logs <container-name>`
3. 检查端口占用：`lsof -i :<port>`

### 重新部署单个服务
```bash
# 停止并删除容器
/opt/podman/bin/podman stop go-nomads-gateway
/opt/podman/bin/podman rm go-nomads-gateway

# 重新运行部署脚本
./deployment/deploy-services-local.sh
```

### 完全清理并重新部署
```bash
# 停止所有服务
./deployment/stop-services.sh

# 停止并清理基础设施
./deployment/deploy-infrastructure.sh clean

# 重新部署基础设施
./deployment/deploy-infrastructure.sh

# 重新部署服务
./deployment/deploy-services-local.sh
```

## 🏗️ 架构说明

### 部署方式
本项目使用**本地构建 + Podman 部署**的混合方式：

1. **本地构建**：使用本地 .NET SDK 构建和发布项目
   - 避免了 ARM64 架构下的 protobuf 工具问题
   - 构建速度更快
   - 可以利用本地缓存

2. **容器部署**：将发布的文件打包到运行时镜像
   - 使用轻量级的 ASP.NET Core 运行时镜像
   - 容器化运行，隔离环境
   - 易于管理和扩展

### 网络架构
- 所有容器运行在 `go-nomads-network` 网络中
- 容器间可以通过容器名互相访问
- 主机可以通过映射的端口访问服务

## 📊 性能优化建议

1. **启用健康检查**：在容器启动脚本中添加健康检查
2. **资源限制**：为每个容器设置 CPU 和内存限制
3. **日志轮转**：配置日志轮转避免日志文件过大
4. **持久化数据**：为 Redis 添加数据卷实现持久化

## 🔐 安全建议

1. **修改默认密码**：Grafana 等服务的默认密码应该修改
2. **网络隔离**：生产环境中应使用更严格的网络隔离
3. **TLS/SSL**：生产环境应启用 HTTPS
4. **密钥管理**：使用密钥管理服务存储敏感信息

## 📚 相关文档

- [部署架构文档](deployment/ARCHITECTURE.md)
- [快速开始指南](deployment/QUICKSTART.md)
- [端口指南](deployment/PORT_GUIDE.md)
- [Scalar 文档](SCALAR_README.md)

## ✨ 下一步

1. 访问 Consul UI 查看服务注册状态
2. 访问 Zipkin 查看分布式追踪
3. 配置 Grafana 仪表盘监控服务指标
4. 测试 API 端点功能

---

**部署时间**: 2025年10月14日  
**部署方式**: Podman (本地构建)  
**平台**: macOS (Apple Silicon)
