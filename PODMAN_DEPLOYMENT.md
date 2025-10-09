# Go-Nomads Podman 部署指南

本指南说明如何使用 Podman 和 Dapr 部署 Go-Nomads 微服务应用。

## 📋 前提条件

### 必需工具
- **Podman**: 容器引擎 (替代 Docker)
  ```powershell
  # Windows 安装 Podman
  winget install -e --id RedHat.Podman
  ```

- **Podman Compose** (可选，用于 compose 方式部署)
  ```powershell
  pip install podman-compose
  ```

- **.NET 9.0 SDK**: 用于构建应用
  ```powershell
  winget install Microsoft.DotNet.SDK.9
  ```

### 验证安装
```powershell
podman --version
podman-compose --version  # 如果安装了
dotnet --version
```

## 🚀 快速开始

### 方法 1: 使用 PowerShell 脚本 (推荐)

#### 启动所有服务
```powershell
.\deploy-podman.ps1 -Action start
```

这将会：
1. 创建 Podman 网络
2. 启动基础设施服务 (Redis, Zipkin, Dapr Placement)
3. 构建应用镜像
4. 启动应用服务和 Dapr sidecars

#### 查看服务状态
```powershell
.\deploy-podman.ps1 -Action status
```

#### 查看日志
```powershell
# 查看所有可用服务
.\deploy-podman.ps1 -Action logs

# 查看特定服务日志
podman logs -f go-nomads-gateway
podman logs -f go-nomads-product-service
podman logs -f go-nomads-user-service
```

#### 停止所有服务
```powershell
.\deploy-podman.ps1 -Action stop
```

#### 重启服务
```powershell
.\deploy-podman.ps1 -Action restart
```

#### 重新构建镜像
```powershell
.\deploy-podman.ps1 -Action build
```

#### 清理所有资源
```powershell
.\deploy-podman.ps1 -Action clean
```

### 方法 2: 使用 Podman Compose

#### 启动服务
```powershell
podman-compose -f podman-compose.yml up -d --build
```

#### 停止服务
```powershell
podman-compose -f podman-compose.yml down
```

#### 查看日志
```powershell
podman-compose -f podman-compose.yml logs -f
```

## 🏗️ 架构说明

### 服务端口映射

| 服务 | 应用端口 | Dapr HTTP | Dapr gRPC | 说明 |
|------|----------|-----------|-----------|------|
| Gateway | 5000 | 3502 | 50003 | API 网关 |
| Product Service | 5001 | 3500 | 50001 | 产品服务 |
| User Service | 5002 | 3501 | 50002 | 用户服务 |
| Redis | 6379 | - | - | 状态存储/消息队列 |
| Zipkin | 9411 | - | - | 分布式追踪 |
| Placement | 50006 | - | - | Dapr Placement |

### Dapr 组件

部署包含以下 Dapr 组件：

1. **状态存储 (State Store)**: Redis
   - 配置文件: `deployment/dapr/components/statestore.yaml`
   - 用于持久化应用状态

2. **发布/订阅 (Pub/Sub)**: Redis
   - 配置文件: `deployment/dapr/components/pubsub.yaml`
   - 用于服务间异步通信

3. **分布式追踪**: Zipkin
   - 配置文件: `deployment/dapr/config/config.yaml`
   - 用于追踪跨服务的请求

## 📁 项目结构

```
go-nomads/
├── deployment/
│   └── dapr/
│       ├── components/          # Dapr 组件配置
│       │   ├── statestore.yaml  # Redis 状态存储
│       │   └── pubsub.yaml      # Redis 发布订阅
│       └── config/
│           └── config.yaml      # Dapr 配置
├── src/
│   ├── Gateway/
│   │   └── Gateway/
│   │       └── Dockerfile       # Gateway Dockerfile
│   ├── Services/
│   │   ├── ProductService/
│   │   │   └── ProductService/
│   │   │       └── Dockerfile   # Product Service Dockerfile
│   │   └── UserService/
│   │       └── UserService/
│   │           └── Dockerfile   # User Service Dockerfile
│   └── Shared/                  # 共享库
├── deploy-podman.ps1            # Podman 部署脚本
└── podman-compose.yml           # Compose 配置
```

## 🔍 验证部署

### 1. 检查容器状态
```powershell
podman ps
```

应该看到以下容器在运行：
- go-nomads-redis
- go-nomads-zipkin
- go-nomads-placement
- go-nomads-gateway
- go-nomads-gateway-dapr
- go-nomads-product-service
- go-nomads-product-service-dapr
- go-nomads-user-service
- go-nomads-user-service-dapr

### 2. 测试 API 端点

#### Gateway 健康检查
```powershell
curl http://localhost:5000/health
```

#### 通过 Gateway 访问服务
```powershell
# 获取产品列表
curl http://localhost:5000/api/products

# 获取用户列表
curl http://localhost:5000/api/users
```

#### 直接访问服务
```powershell
# Product Service
curl http://localhost:5001/health

# User Service
curl http://localhost:5002/health
```

### 3. 使用 Dapr API

#### 通过 Dapr 调用服务
```powershell
# 调用 Product Service
curl http://localhost:3500/v1.0/invoke/product-service/method/health

# 调用 User Service
curl http://localhost:3501/v1.0/invoke/user-service/method/health
```

### 4. 查看分布式追踪
访问 Zipkin UI: http://localhost:9411

## 🛠️ 常见操作

### 查看特定服务日志
```powershell
# Gateway
podman logs -f go-nomads-gateway
podman logs -f go-nomads-gateway-dapr

# Product Service
podman logs -f go-nomads-product-service
podman logs -f go-nomads-product-service-dapr

# User Service
podman logs -f go-nomads-user-service
podman logs -f go-nomads-user-service-dapr

# 基础设施
podman logs -f go-nomads-redis
podman logs -f go-nomads-placement
```

### 进入容器调试
```powershell
# 进入应用容器
podman exec -it go-nomads-gateway /bin/bash

# 进入 Redis
podman exec -it go-nomads-redis redis-cli
```

### 重新构建单个服务
```powershell
# 停止服务
podman stop go-nomads-gateway go-nomads-gateway-dapr
podman rm go-nomads-gateway go-nomads-gateway-dapr

# 重新构建
podman build -t go-nomads-gateway -f src/Gateway/Gateway/Dockerfile .

# 重新启动
.\deploy-podman.ps1 -Action start
```

### 清理未使用的资源
```powershell
# 清理停止的容器
podman container prune

# 清理未使用的镜像
podman image prune

# 清理所有未使用的资源
podman system prune -a
```

## 🐛 故障排查

### 容器无法启动

1. 检查端口占用
```powershell
netstat -ano | findstr "5000"
netstat -ano | findstr "6379"
```

2. 查看容器日志
```powershell
podman logs go-nomads-<service-name>
```

3. 检查网络连接
```powershell
podman network ls
podman network inspect go-nomads-network
```

### Dapr Sidecar 连接失败

1. 确认 Placement 服务运行正常
```powershell
podman logs go-nomads-placement
```

2. 检查 Dapr 配置文件
```powershell
Get-Content deployment/dapr/components/*.yaml
Get-Content deployment/dapr/config/config.yaml
```

3. 验证 Redis 连接
```powershell
podman exec -it go-nomads-redis redis-cli ping
```

### 服务间通信失败

1. 检查网络配置
```powershell
podman inspect go-nomads-gateway | Select-String -Pattern "NetworkMode|Networks"
```

2. 测试服务发现
```powershell
# 从 Gateway 容器内测试
podman exec -it go-nomads-gateway curl http://localhost:3502/v1.0/invoke/product-service/method/health
```

## 📊 性能优化

### 资源限制
修改 `podman-compose.yml` 添加资源限制：

```yaml
services:
  product-service:
    # ... 其他配置
    deploy:
      resources:
        limits:
          cpus: '0.5'
          memory: 512M
        reservations:
          cpus: '0.25'
          memory: 256M
```

### 构建优化
使用多阶段构建缓存加速：

```powershell
# 使用 buildah 缓存
podman build --layers --cache-to type=local,dest=/tmp/cache -t go-nomads-gateway .
```

## 🔐 安全建议

1. **生产环境配置**
   - 启用 Dapr mTLS
   - 使用环境变量管理敏感信息
   - 配置 Redis 密码

2. **网络隔离**
   - 为不同环境创建独立网络
   - 限制容器间通信

3. **镜像安全**
   - 定期更新基础镜像
   - 扫描镜像漏洞

## 📚 相关资源

- [Podman 官方文档](https://podman.io/docs)
- [Dapr 官方文档](https://docs.dapr.io)
- [.NET 容器化指南](https://learn.microsoft.com/en-us/dotnet/core/docker/introduction)

## 🤝 贡献

如有问题或改进建议，请提交 Issue 或 Pull Request。

---

**最后更新**: 2025年10月9日
