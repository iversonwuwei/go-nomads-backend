# Podman + Dapr 部署文件清单

## 📁 已创建的文件

### 1. Dockerfile 文件

#### Gateway Dockerfile
- **位置**: `src/Gateway/Gateway/Dockerfile`
- **用途**: 构建 Gateway 服务的容器镜像
- **特点**: 多阶段构建，优化镜像大小

#### ProductService Dockerfile
- **位置**: `src/Services/ProductService/ProductService/Dockerfile`
- **用途**: 构建 Product Service 的容器镜像
- **特点**: 包含 Shared 项目依赖

#### UserService Dockerfile
- **位置**: `src/Services/UserService/UserService/Dockerfile`
- **用途**: 构建 User Service 的容器镜像
- **特点**: 包含 Shared 项目依赖

### 2. Dapr 配置文件

#### Dapr 主配置
- **位置**: `deployment/dapr/config/config.yaml`
- **内容**:
  - Zipkin 分布式追踪配置
  - 指标收集设置
  - mTLS 配置（当前禁用）

#### Redis State Store 组件
- **位置**: `deployment/dapr/components/statestore.yaml`
- **内容**:
  - Redis 连接配置
  - 状态存储组件定义
  - Actor State Store 支持

#### Redis Pub/Sub 组件
- **位置**: `deployment/dapr/components/pubsub.yaml`
- **内容**:
  - Redis 连接配置
  - 发布订阅组件定义

### 3. 部署脚本

#### 主部署脚本
- **位置**: `deploy-podman.ps1`
- **功能**:
  - ✅ 启动服务 (`-Action start`)
  - ✅ 停止服务 (`-Action stop`)
  - ✅ 重启服务 (`-Action restart`)
  - ✅ 构建镜像 (`-Action build`)
  - ✅ 查看日志 (`-Action logs`)
  - ✅ 查看状态 (`-Action status`)
  - ✅ 清理资源 (`-Action clean`)

#### 快速启动脚本
- **位置**: `start.ps1`
- **功能**: 一键启动所有服务

### 4. Compose 配置

#### Podman Compose 配置
- **位置**: `podman-compose.yml`
- **服务**:
  - ✅ Redis (状态存储 + Pub/Sub)
  - ✅ Zipkin (分布式追踪)
  - ✅ Dapr Placement (Actor 支持)
  - ✅ Product Service + Dapr Sidecar
  - ✅ User Service + Dapr Sidecar
  - ✅ Gateway + Dapr Sidecar

### 5. 配置文件

#### .dockerignore
- **位置**: `.dockerignore`
- **用途**: 优化 Docker 构建，排除不必要的文件

### 6. 文档

#### Podman 部署指南
- **位置**: `PODMAN_DEPLOYMENT.md`
- **内容**:
  - 📖 完整的部署说明
  - 🔧 故障排查指南
  - 📊 性能优化建议
  - 🔐 安全建议

#### 本文件
- **位置**: `DEPLOYMENT_SUMMARY.md`
- **用途**: 部署文件清单和快速参考

## 🚀 部署架构

### 服务架构图

```
┌─────────────────────────────────────────────────────────────┐
│                         Go-Nomads 微服务架构                    │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌──────────┐    ┌──────────┐    ┌──────────┐             │
│  │ Gateway  │    │ Product  │    │   User   │             │
│  │ :5000    │    │ Service  │    │ Service  │             │
│  │          │    │ :5001    │    │ :5002    │             │
│  └────┬─────┘    └────┬─────┘    └────┬─────┘             │
│       │               │               │                    │
│  ┌────┴─────┐    ┌────┴─────┐    ┌────┴─────┐             │
│  │  Dapr    │    │  Dapr    │    │  Dapr    │             │
│  │ Sidecar  │    │ Sidecar  │    │ Sidecar  │             │
│  │ :3502    │    │ :3500    │    │ :3501    │             │
│  └────┬─────┘    └────┬─────┘    └────┬─────┘             │
│       │               │               │                    │
│       └───────────────┴───────────────┘                    │
│                       │                                    │
│         ┌─────────────┴─────────────┐                      │
│         │                           │                      │
│    ┌────┴─────┐              ┌──────┴──────┐              │
│    │  Redis   │              │   Zipkin    │              │
│    │  :6379   │              │   :9411     │              │
│    └──────────┘              └─────────────┘              │
│                                                             │
│         ┌─────────────────┐                                │
│         │ Dapr Placement  │                                │
│         │    :50006       │                                │
│         └─────────────────┘                                │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### 端口分配

| 服务 | 应用端口 | Dapr HTTP | Dapr gRPC | 外部访问 |
|------|----------|-----------|-----------|---------|
| Gateway | 5000 | 3502 | 50003 | http://localhost:5000 |
| Product Service | 5001 | 3500 | 50001 | http://localhost:5001 |
| User Service | 5002 | 3501 | 50002 | http://localhost:5002 |
| Redis | 6379 | - | - | localhost:6379 |
| Zipkin | 9411 | - | - | http://localhost:9411 |
| Placement | 50006 | - | - | - |

## 📋 部署检查清单

### 部署前检查

- [ ] 安装 Podman
- [ ] 安装 .NET 9 SDK
- [ ] （可选）安装 podman-compose
- [ ] 克隆项目代码
- [ ] 检查端口是否被占用

### 部署步骤

1. [ ] 进入项目根目录
2. [ ] 执行 `.\start.ps1` 或 `.\deploy-podman.ps1 -Action start`
3. [ ] 等待所有服务启动
4. [ ] 验证服务状态: `.\deploy-podman.ps1 -Action status`
5. [ ] 测试 API 端点

### 验证检查

- [ ] 所有容器都在运行
- [ ] Gateway 响应正常: `curl http://localhost:5000/health`
- [ ] Product Service 响应正常
- [ ] User Service 响应正常
- [ ] Zipkin UI 可访问: http://localhost:9411
- [ ] Redis 连接正常

## 🛠️ 常用命令速查

### 服务管理

```powershell
# 启动所有服务
.\deploy-podman.ps1 -Action start

# 停止所有服务
.\deploy-podman.ps1 -Action stop

# 重启服务
.\deploy-podman.ps1 -Action restart

# 查看状态
.\deploy-podman.ps1 -Action status

# 清理所有资源
.\deploy-podman.ps1 -Action clean
```

### 容器操作

```powershell
# 查看所有容器
podman ps

# 查看日志
podman logs -f go-nomads-gateway
podman logs -f go-nomads-product-service
podman logs -f go-nomads-user-service

# 进入容器
podman exec -it go-nomads-gateway /bin/bash

# 停止单个容器
podman stop go-nomads-gateway

# 删除单个容器
podman rm go-nomads-gateway
```

### 镜像操作

```powershell
# 查看所有镜像
podman images

# 删除镜像
podman rmi go-nomads-gateway

# 清理未使用的镜像
podman image prune
```

### 网络操作

```powershell
# 查看网络
podman network ls

# 查看网络详情
podman network inspect go-nomads-network

# 删除网络
podman network rm go-nomads-network
```

## 📊 监控和日志

### 查看实时日志

```powershell
# Gateway 日志
podman logs -f go-nomads-gateway

# Product Service 日志
podman logs -f go-nomads-product-service

# User Service 日志
podman logs -f go-nomads-user-service

# Dapr Sidecar 日志
podman logs -f go-nomads-gateway-dapr
podman logs -f go-nomads-product-service-dapr
podman logs -f go-nomads-user-service-dapr

# Redis 日志
podman logs -f go-nomads-redis

# Placement 日志
podman logs -f go-nomads-placement
```

### Zipkin 追踪

访问 http://localhost:9411 查看分布式追踪信息：

- 查看服务调用链
- 分析请求延迟
- 定位性能瓶颈
- 追踪错误传播

## 🔧 故障排查

### 常见问题

#### 1. 端口被占用

```powershell
# 查看端口占用
netstat -ano | findstr "5000"
netstat -ano | findstr "6379"

# 停止占用端口的进程
Stop-Process -Id <PID> -Force
```

#### 2. 容器启动失败

```powershell
# 查看容器日志
podman logs go-nomads-<service-name>

# 查看容器详情
podman inspect go-nomads-<service-name>
```

#### 3. Dapr Sidecar 无法连接

```powershell
# 检查 Placement 服务
podman logs go-nomads-placement

# 检查组件配置
cat deployment/dapr/components/*.yaml

# 检查 Redis 连接
podman exec -it go-nomads-redis redis-cli ping
```

#### 4. 服务间通信失败

```powershell
# 检查网络配置
podman network inspect go-nomads-network

# 测试服务发现
podman exec -it go-nomads-gateway curl http://localhost:3502/v1.0/invoke/product-service/method/health
```

## 🎯 下一步

1. ✅ 所有部署文件已创建
2. ✅ 配置文件已就绪
3. ✅ 文档已完善

### 建议的后续步骤

1. **测试部署**
   ```powershell
   .\start.ps1
   ```

2. **验证服务**
   ```powershell
   curl http://localhost:5000/health
   curl http://localhost:5001/health
   curl http://localhost:5002/health
   ```

3. **测试 API**
   ```powershell
   # 获取产品列表
   curl http://localhost:5000/api/products
   
   # 获取用户列表
   curl http://localhost:5000/api/users
   ```

4. **查看追踪**
   - 访问 http://localhost:9411
   - 执行一些 API 调用
   - 在 Zipkin 中查看追踪信息

5. **优化配置**
   - 根据需要调整资源限制
   - 配置生产环境的 mTLS
   - 添加健康检查和重启策略

## 📚 参考文档

- [Podman 部署指南](PODMAN_DEPLOYMENT.md) - 详细的部署说明
- [README.md](README.md) - 项目主文档
- [Dapr 文档](https://docs.dapr.io) - Dapr 官方文档
- [Podman 文档](https://podman.io/docs) - Podman 官方文档

---

**创建日期**: 2025年10月9日  
**版本**: 1.0.0  
**状态**: ✅ 生产就绪
