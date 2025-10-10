# Go-Nomads 服务端口访问指南

## 🎯 核心问题:使用哪个端口访问服务?

**答案: 使用 Gateway 的端口作为统一入口!**

## 📍 端口映射总览

### 应用服务端口

| 服务 | 容器内部端口 | 主机映射端口 | 用途 | 是否直接访问 |
|------|-------------|-------------|------|------------|
| **Gateway** | 8080 | **5000** | API 网关(统一入口) | ✅ **推荐访问** |
| Product Service | 8080 | 5001 | 产品服务 | ❌ 不推荐 |
| User Service | 8080 | 5002 | 用户服务 | ❌ 不推荐 |

### Dapr Sidecar 端口

| 服务 | Dapr HTTP | Dapr gRPC | 主机映射 | 用途 |
|------|-----------|-----------|---------|------|
| Gateway | 3500 | 50003 | 50003 | Dapr API |
| Product Service | 3500 | 50001 | 50001 | Dapr API |
| User Service | 3500 | 50002 | 50002 | Dapr API |

### 基础设施端口

| 组件 | 端口 | 用途 |
|------|------|------|
| Consul | 8500 | 服务注册 & Web UI |
| Redis | 6379 | 配置中心 & 状态存储 |
| Prometheus | 9090 | 指标收集 & 查询 |
| Grafana | 3000 | 监控可视化 |
| Zipkin | 9411 | 分布式追踪 |

## 🚀 推荐访问方式

### 方式一: 通过 Gateway 访问(推荐)⭐

这是标准的微服务访问方式,所有请求通过 API 网关统一路由:

```bash
# 访问产品服务
curl http://localhost:5000/api/products

# 访问用户服务
curl http://localhost:5000/api/users

# Gateway 会自动路由到对应的后端服务
```

**优点:**

- ✅ 统一入口,便于管理
- ✅ 可以在 Gateway 实现认证、限流等功能
- ✅ 符合微服务最佳实践
- ✅ 服务间通过 Dapr 通信,不暴露内部端口

### 方式二: 直接访问服务端口(开发调试)

仅用于开发调试,不推荐生产使用:

```bash
# 直接访问产品服务
curl http://localhost:5001/api/products

# 直接访问用户服务
curl http://localhost:5002/api/users
```

**缺点:**

- ❌ 绕过了 Gateway 的统一管理
- ❌ 缺少认证和授权
- ❌ 不符合微服务架构设计

### 方式三: 通过 Dapr API 访问

使用 Dapr 的服务调用功能:

```bash
# 通过 Gateway 的 Dapr sidecar 调用产品服务
curl http://localhost:50003/v1.0/invoke/product-service/method/api/products

# 或者直接使用产品服务的 Dapr sidecar
curl http://localhost:50001/v1.0/invoke/product-service/method/api/products
```

**用途:**

- 🔧 测试 Dapr 服务调用功能
- 🔧 验证服务间通信
- 🔧 开发调试

## 📊 完整访问示例

### 1. 用户请求流程(推荐)

```
用户/客户端
    ↓
http://localhost:5000/api/products  ← 访问 Gateway
    ↓
Gateway (端口 8080 容器内部)
    ↓ (通过 Dapr 服务调用)
Product Service (端口 8080 容器内部)
    ↓
返回结果
```

### 2. 请求示例

```bash
# 获取所有产品
curl http://localhost:5000/api/products

# 获取单个产品
curl http://localhost:5000/api/products/1

# 创建产品 (POST)
curl -X POST http://localhost:5000/api/products \
  -H "Content-Type: application/json" \
  -d '{"name":"新产品","price":99.99}'

# 获取用户信息
curl http://localhost:5000/api/users
```

### 3. 浏览器访问

```
Gateway:          http://localhost:5000/api/products
Consul UI:        http://localhost:8500
Prometheus:       http://localhost:9090
Grafana:          http://localhost:3000
Zipkin:           http://localhost:9411
```

## 🔍 端口说明

### Gateway 端口详解

| 端口类型 | 端口号 | 映射 | 说明 |
|---------|--------|------|------|
| 应用端口 | 8080(容器内) | 5000(主机) | **主要访问端口** - API 网关入口 |
| Dapr HTTP | 3500(容器内) | 未映射 | Dapr HTTP API(内部使用) |
| Dapr gRPC | 50003(容器内) | 50003(主机) | Dapr gRPC API(调试用) |

### 访问建议

```bash
✅ 推荐: http://localhost:5000/api/*
   - 生产环境访问方式
   - 通过 Gateway 统一路由
   
⚠️  开发: http://localhost:5001/api/products
   - 直接访问 Product Service
   - 仅用于开发调试
   
🔧 调试: http://localhost:50003/v1.0/invoke/product-service/method/api/products
   - 测试 Dapr 服务调用
   - 验证服务间通信
```

## 🛠️ 常见问题

### Q1: 为什么有这么多端口?

**A:** 微服务架构下每个服务都有:

- **应用端口** (8080): 服务本身的 HTTP 端口
- **Dapr HTTP 端口** (3500): Dapr sidecar 的 HTTP API
- **Dapr gRPC 端口** (5000x): Dapr sidecar 的 gRPC API

### Q2: 我应该记住所有端口吗?

**A:** 不需要!作为用户,您只需要记住:

- **5000** - Gateway 入口(最重要!)
- **8500** - Consul UI
- **9090** - Prometheus
- **3000** - Grafana
- **9411** - Zipkin

### Q3: 服务之间如何通信?

**A:** 服务间通过 Dapr 的服务调用功能通信,使用容器名称:

```
Gateway → Dapr → product-service (容器名)
                  ↓
              go-nomads-product-service:8080
```

### Q4: 如果 Gateway 挂了怎么办?

**A:** 开发环境可以临时直接访问服务端口:

```bash
# 临时访问产品服务
curl http://localhost:5001/api/products
```

但生产环境应该:

1. 重启 Gateway
2. 配置 Gateway 的高可用(多实例)

## 📝 快速参考卡

```
┌─────────────────────────────────────────────┐
│        Go-Nomads 端口快速参考               │
├─────────────────────────────────────────────┤
│                                             │
│  🌐 应用访问                                │
│  ├─ Gateway:       http://localhost:5000   │
│  ├─ Product Svc:   http://localhost:5001   │
│  └─ User Svc:      http://localhost:5002   │
│                                             │
│  📊 监控工具                                │
│  ├─ Consul:        http://localhost:8500   │
│  ├─ Prometheus:    http://localhost:9090   │
│  ├─ Grafana:       http://localhost:3000   │
│  └─ Zipkin:        http://localhost:9411   │
│                                             │
│  🔧 Dapr 调试                               │
│  ├─ Gateway:       http://localhost:50003  │
│  ├─ Product Svc:   http://localhost:50001  │
│  └─ User Svc:      http://localhost:50002  │
│                                             │
│  ⭐ 推荐访问: localhost:5000 (Gateway)      │
└─────────────────────────────────────────────┘
```

## 🎯 总结

**使用端口: `http://localhost:5000` (Gateway)**

这是访问 Go-Nomads 微服务系统的统一入口。Gateway 会根据路由规则将请求转发到对应的后端服务。

**记住这个公式:**

```
客户端 → Gateway(5000) → 后端服务
```

其他端口主要用于:

- **开发调试** (5001, 5002)
- **监控管理** (8500, 9090, 3000, 9411)
- **Dapr 测试** (50001, 50002, 50003)
