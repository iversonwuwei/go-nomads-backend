# Go-Nomads + Dapr 部署成功报告

## 📊 部署摘要

✅ **所有组件已成功部署!**

- 使用 Podman 在 Windows 上部署完整的微服务架构
- 集成了 Dapr 运行时用于服务间通信和状态管理
- 集成了 Zipkin 用于分布式追踪
- 使用 Redis 作为状态存储和消息队列

## 🏗️ 架构组件

### 1. 基础设施服务

| 服务 | 镜像 | 端口 | 状态 |
|------|------|------|------|
| Redis | redis:7-alpine | 6379 | ✅ 运行中 |
| Zipkin | openzipkin/zipkin:latest | 9411 | ✅ 运行中 (健康) |

### 2. 应用服务

| 服务 | 容器名称 | 端口 | Dapr Sidecar | 状态 |
|------|----------|------|-------------|------|
| Gateway | go-nomads-gateway | 5000, 50003 | dapr-gateway (HTTP:3502, gRPC:51003) | ✅ 运行中 |
| Product Service | go-nomads-product-service | 5001, 50001 | dapr-product-service (HTTP:3500, gRPC:51001) | ✅ 运行中 |
| User Service | go-nomads-user-service | 5002, 50002 | dapr-user-service (HTTP:3501, gRPC:51002) | ✅ 运行中 |

### 3. Dapr 组件

所有服务的 Dapr sidecar 已成功加载以下组件:

- **State Store**: Redis (state.redis/v1)
  - 连接到: `go-nomads-redis:6379`
  - 用于: 状态持久化、Actor 状态存储

- **Pub/Sub**: Redis (pubsub.redis/v1)
  - 连接到: `go-nomads-redis:6379`
  - 用于: 服务间异步消息传递

- **Tracing**: Zipkin
  - 端点: `http://go-nomads-zipkin:9411/api/v2/spans`
  - 采样率: 100%

## 🌐 访问端点

### 应用 API

```bash
# Gateway (聚合API)
http://localhost:5000/api/products
http://localhost:5000/api/users

# Product Service (直接访问)
http://localhost:5001/api/products

# User Service (直接访问)
http://localhost:5002/api/users
```

### Dapr API (容器内部)

每个服务都有自己的 Dapr sidecar,可通过 localhost 访问:

```bash
# Gateway Dapr HTTP API
http://localhost:3502/v1.0/...

# Product Service Dapr HTTP API
http://localhost:3500/v1.0/...

# User Service Dapr HTTP API
http://localhost:3501/v1.0/...
```

### 监控和追踪

```bash
# Zipkin UI - 查看分布式追踪
http://localhost:9411
```

## 🧪 测试验证

### 1. 测试 Gateway API

```powershell
Invoke-RestMethod -Uri "http://localhost:5000/api/products" -Method Get
```

**预期响应:**
```json
{
  "success": true,
  "message": "Products retrieved successfully",
  "data": {
    "items": [...],
    "totalCount": 2,
    "page": 1,
    "pageSize": 10
  }
}
```

### 2. 测试 Dapr 状态存储

```powershell
# 保存状态
Invoke-RestMethod -Uri "http://localhost:3500/v1.0/state/statestore" `
  -Method Post `
  -ContentType "application/json" `
  -Body '[{"key":"mykey","value":"myvalue"}]'

# 获取状态
Invoke-RestMethod -Uri "http://localhost:3500/v1.0/state/statestore/mykey"
```

### 3. 查看 Zipkin 追踪

1. 打开浏览器访问: http://localhost:9411
2. 点击 "Run Query" 查看最近的追踪
3. 点击任意追踪查看详细信息

## 📦 部署文件

### 配置文件位置

```
deployment/
├── dapr/
│   ├── components/
│   │   ├── pubsub.yaml              # Redis pub/sub 组件
│   │   ├── statestore.yaml          # Redis 状态存储组件
│   │   ├── pubsub-memory.yaml.bak   # 内存 pub/sub (备份)
│   │   └── statestore-memory.yaml.bak # 内存状态存储 (备份)
│   └── config/
│       └── config.yaml              # Dapr 配置 (追踪、metrics等)
├── deploy-podman.ps1                # 应用服务部署脚本
└── deploy-dapr-podman.ps1           # Dapr sidecars 部署脚本
```

### 关键配置

**Dapr 配置** (`deployment/dapr/config/config.yaml`):
```yaml
apiVersion: dapr.io/v1alpha1
kind: Configuration
metadata:
  name: daprConfig
spec:
  tracing:
    samplingRate: "1"
    zipkin:
      endpointAddress: "http://go-nomads-zipkin:9411/api/v2/spans"
  metric:
    enabled: true
  mtls:
    enabled: false
```

**Redis 状态存储** (`deployment/dapr/components/statestore.yaml`):
```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: statestore
spec:
  type: state.redis
  version: v1
  metadata:
  - name: redisHost
    value: go-nomads-redis:6379
  - name: actorStateStore
    value: "true"
```

## 🚀 部署步骤回顾

### 成功使用的方法

1. **镜像拉取**: 使用华为云镜像源成功绕过了 Docker Hub 网络限制
   ```powershell
   podman pull swr.cn-north-4.myhuaweicloud.com/ddn-k8s/docker.io/redis:7-alpine
   podman pull swr.cn-north-4.myhuaweicloud.com/ddn-k8s/docker.io/openzipkin/zipkin:latest
   podman pull swr.cn-north-4.myhuaweicloud.com/ddn-k8s/docker.io/daprio/dapr:1.14.4
   ```

2. **镜像重标记**: 重命名为标准镜像名
   ```powershell
   podman tag swr.cn-north-4.myhuaweicloud.com/ddn-k8s/docker.io/redis:7-alpine redis:7-alpine
   ```

3. **容器部署**: 
   - 基础设施: Redis 和 Zipkin
   - 应用服务: Gateway, ProductService, UserService
   - Dapr Sidecars: 使用 `--network container:` 模式共享网络栈

4. **组件配置**: 
   - 更新 Redis 主机名为容器名称
   - 禁用内存组件以避免命名冲突

## 🛠️ 管理命令

### 启动所有服务

```powershell
# 启动应用服务
cd E:\Workspaces\WaldenProjects\go-nomads\deployment
.\deploy-podman.ps1 -Action start

# 启动 Dapr sidecars
.\deploy-dapr-podman.ps1 -Action start
```

### 停止所有服务

```powershell
# 停止 Dapr sidecars
.\deploy-dapr-podman.ps1 -Action stop

# 停止应用服务
.\deploy-podman.ps1 -Action stop
```

### 查看状态

```powershell
# 查看所有容器
podman ps

# 查看特定服务日志
podman logs go-nomads-gateway
podman logs dapr-gateway

# 查看 Dapr 状态
.\deploy-dapr-podman.ps1 -Action status
```

### 重启服务

```powershell
# 重启特定容器
podman restart go-nomads-gateway

# 完全重新部署
.\deploy-podman.ps1 -Action stop
.\deploy-podman.ps1 -Action start
.\deploy-dapr-podman.ps1 -Action start
```

## 🔍 故障排查

### 常见问题

1. **端口冲突**: 
   - Dapr gRPC 端口从 50001-50003 改为 51001-51003
   - 应用服务使用原有端口

2. **组件重复**: 
   - 禁用了内存组件 (pubsub-memory.yaml, statestore-memory.yaml)
   - 仅使用 Redis 组件

3. **网络连接**: 
   - 使用 `--network container:` 模式让 Dapr sidecar 与应用共享网络
   - 基础设施服务使用桥接网络 `go-nomads-network`

### 检查清单

- [ ] 所有容器都在运行: `podman ps`
- [ ] Redis 可访问: `podman logs go-nomads-redis`
- [ ] Zipkin 健康: `podman ps --filter "name=zipkin"`
- [ ] Dapr 组件已加载: `podman logs dapr-product-service | grep "Component loaded"`
- [ ] API 响应正常: `Invoke-RestMethod -Uri "http://localhost:5000/api/products"`

## 📈 后续改进

### 当前限制

1. **Dapr Placement 服务**: 未部署 (镜像不在华为云)
   - 影响: Actor 模型功能受限
   - 解决: 手动拉取 `daprio/placement:1.14.4` (需要解决网络问题)

2. **服务发现**: 当前使用容器名称硬编码
   - 改进: 使用 Dapr 服务调用 API 实现动态服务发现

3. **监控**: 仅有基本的 Zipkin 追踪
   - 改进: 添加 Prometheus + Grafana 进行 metrics 监控

### 建议的下一步

1. **集成 Dapr 服务调用**: 
   - 修改服务间调用使用 Dapr HTTP/gRPC API
   - 示例: `http://localhost:3500/v1.0/invoke/user-service/method/api/users`

2. **使用 Pub/Sub 实现异步通信**:
   - 商品创建事件发布
   - 用户通知订阅

3. **状态管理**:
   - 购物车状态存储
   - 用户会话管理

4. **添加 Dapr Placement**:
   - 下载离线镜像或配置镜像加速
   - 启用 Actor 支持

## 🎉 结论

Go-Nomads 项目已成功使用 Podman 部署完整的微服务架构,集成了:

✅ 3个 .NET 微服务 (Gateway, ProductService, UserService)  
✅ Dapr 运行时 (每个服务都有 sidecar)  
✅ Redis (状态存储 + 消息队列)  
✅ Zipkin (分布式追踪)  
✅ 容器网络 (Podman bridge network)  

所有服务正常运行,API 可访问,Dapr 组件已加载!

---

**部署时间**: 2025-01-09  
**Dapr 版本**: 1.14.4  
**Podman 版本**: 4.x  
**.NET 版本**: 9.0  
