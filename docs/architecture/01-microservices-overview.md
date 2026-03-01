# Go-Nomads 微服务架构详细设计

## 📋 文档目录

1. [微服务总览](./01-microservices-overview.md) ⬅️ 当前文档
2. [核心业务服务详细设计](./02-core-services-detail.md)
3. [基础服务详细设计](./03-infrastructure-services-detail.md)
4. [数据库设计](./04-database-design.md)
5. [API 网关设计](./05-api-gateway-design.md)
6. [部署架构](./06-deployment-architecture.md)
7. [安全架构](./07-security-architecture.md)

---

## 🎯 架构原则

### 设计原则
- **单一职责**: 每个服务只负责一个业务域
- **高内聚低耦合**: 服务内部高内聚，服务间通过明确接口交互
- **数据独立**: 每个服务独立管理自己的数据
- **无状态设计**: 服务实例可水平扩展
- **容错设计**: 服务降级、熔断、重试机制

### 技术选型原则
- **主语言**: C# (.NET 8+) - 已有 UserService, ProductService, DocumentService
- **辅助语言**: Node.js (实时服务), Go (高性能服务), Python (AI 服务)
- **统一框架**: ASP.NET Core Web API
- **服务通信**: Dapr (已集成)
- **数据存储**: PostgreSQL + Supabase

---

## 🏗️ 整体架构图

```
                                    ┌─────────────────────┐
                                    │   Load Balancer     │
                                    │   (Nginx/Caddy)     │
                                    └──────────┬──────────┘
                                               │
                            ┌──────────────────┼──────────────────┐
                            │                  │                  │
                    ┌───────▼────────┐ ┌──────▼──────┐  ┌───────▼────────┐
                    │  Web Client    │ │ Mobile App  │  │  Admin Portal  │
                    │  (Vue/React)   │ │  (Flutter)  │  │   (Flutter)    │
                    └───────┬────────┘ └──────┬──────┘  └───────┬────────┘
                            │                  │                  │
                            └──────────────────┼──────────────────┘
                                               │
                                    ┌──────────▼──────────┐
                                    │   API Gateway       │
                                    │   (Ocelot/YARP)     │
                                    │   - JWT Auth        │
                                    │   - Rate Limit      │
                                    │   - Request Log     │
                                    └──────────┬──────────┘
                                               │
              ┌────────────────────────────────┼────────────────────────────────┐
              │                                │                                │
    ┌─────────▼─────────┐          ┌─────────▼─────────┐          ┌──────────▼──────────┐
    │  Core Services    │          │  Infrastructure   │          │  Support Services   │
    │  (8 services)     │          │  Services         │          │  (Config/Registry)  │
    │  :8001-:8008      │          │  (6 services)     │          │                     │
    │                   │          │  :9001-:9006      │          │                     │
    └─────────┬─────────┘          └─────────┬─────────┘          └──────────┬──────────┘
              │                              │                                │
              └──────────────────────────────┼────────────────────────────────┘
                                             │
                          ┌──────────────────┼──────────────────┐
                          │                  │                  │
                  ┌───────▼────────┐ ┌──────▼──────┐  ┌───────▼────────┐
                  │  PostgreSQL    │ │   Redis     │  │   RabbitMQ     │
                  │  (Supabase)    │ │   Cache     │  │   Message      │
                  └────────────────┘ └─────────────┘  └────────────────┘
```

---

## 📦 服务清单

### 核心业务服务 (Core Services)

| 服务名称 | 端口 | 技术栈 | 状态 | 数据库 | 说明 |
|---------|------|--------|------|--------|------|
| User Service | 8001 | C# + ASP.NET | ✅ 已实现 | PostgreSQL | 用户认证、资料管理 |
| City Service | 8002 | C# + ASP.NET | 🟡 规划中 | PostgreSQL + PostGIS | 城市信息、地理位置 |
| Coworking Service | 8003 | C# + ASP.NET | 🟡 规划中 | PostgreSQL | 共享办公空间 |
| Accommodation Service | 8004 | C# + ASP.NET | 🟡 规划中 | PostgreSQL | 酒店民宿 |
| Event Service | 8005 | C# + ASP.NET | 🟡 规划中 | PostgreSQL | 活动管理 |
| Innovation Service | 8006 | C# + ASP.NET | 🟡 规划中 | PostgreSQL | 创新项目 |
| Travel Service | 8007 | C# + ASP.NET | 🟡 规划中 | PostgreSQL | 旅行规划 |
| Commerce Service | 8008 | C# + ASP.NET | 🟡 规划中 | PostgreSQL | 电商 |

### 基础服务 (Infrastructure Services)

| 服务名称 | 端口 | 技术栈 | 状态 | 数据库 | 说明 |
|---------|------|--------|------|--------|------|
| Location Service | 9001 | Node.js + PostGIS | 🟡 规划中 | PostgreSQL | 定位、地理服务 |
| Notification Service | 9002 | C# + SignalR | 🟡 规划中 | Redis | 推送通知 |
| File Service | 9003 | C# + MinIO | 🟡 规划中 | PostgreSQL | 文件存储 |
| Search Service | 9004 | C# + Elasticsearch | 🟡 规划中 | Elasticsearch | 全文搜索 |
| Payment Service | 9005 | C# + ASP.NET | 🟡 规划中 | PostgreSQL | 支付集成 |
| i18n Service | 9006 | C# + ASP.NET | 🟡 规划中 | Redis | 国际化 |

### 支撑服务 (Support Services)

| 服务名称 | 端口 | 技术栈 | 状态 | 说明 |
|---------|------|--------|------|------|
| API Gateway | 5000 | C# + YARP | ✅ 已实现 | 路由、认证、限流 |

---

## 🔗 服务依赖关系

### 依赖层级

```
Layer 1: 基础服务 (无依赖)
├── User Service
├── Location Service
├── File Service
└── Notification Service

Layer 2: 核心业务服务 (依赖 Layer 1)
├── City Service → Location Service, File Service
├── Coworking Service → Location Service, File Service, User Service
├── Accommodation Service → Location Service, File Service, User Service
├── Event Service → Location Service, Notification Service, User Service
├── Innovation Service → File Service, User Service
└── Commerce Service → Payment Service, User Service

Layer 3: 聚合服务 (依赖 Layer 2)
└── Travel Service → City, Coworking, Accommodation, Event
```

### 服务间通信方式

| 通信场景 | 方式 | 技术 | 示例 |
|---------|------|------|------|
| 同步调用 | HTTP/gRPC | Dapr Service Invocation | User → File |
| 异步消息 | Pub/Sub | Dapr Pub/Sub + RabbitMQ | Event → Notification |
| 状态管理 | State Store | Dapr State + Redis | User Session |
| 分布式锁 | Distributed Lock | Redis | Payment Lock |

---

## 📊 数据存储策略

### 数据库分配

```
PostgreSQL (Supabase):
├── public schema
│   ├── users (User Service)
│   ├── roles (User Service)
│   ├── user_roles (User Service)
│   └── ...
├── city schema
│   ├── cities (City Service)
│   ├── city_statistics (City Service)
│   └── ...
├── coworking schema
│   ├── spaces (Coworking Service)
│   ├── bookings (Coworking Service)
│   └── ...
└── ...

Redis (缓存):
├── user:session:{userId}
├── city:cache:{cityId}
├── rate_limit:{service}:{key}
└── ...

RabbitMQ (消息):
├── event.created → Notification Service
├── booking.confirmed → Notification Service
└── ...
```

### 数据一致性策略

| 场景 | 策略 | 说明 |
|-----|------|------|
| 用户注册 | 强一致性 | 事务保证 |
| 预订操作 | 最终一致性 | Saga 模式 |
| 通知发送 | 最终一致性 | 消息队列 |
| 缓存更新 | 最终一致性 | Cache Aside |

---

## 🔐 安全架构

### 认证流程

```
1. 用户登录
   Client → API Gateway → User Service
   ↓
   User Service 验证密码
   ↓
   生成 JWT Token (Access + Refresh)
   ↓
   返回 Token 给 Client

2. 访问资源
   Client (带 JWT) → API Gateway
   ↓
   Gateway 验证 JWT
   ↓
   提取 User Claims
   ↓
   转发到后端服务 (带 User Context)
```

### 授权策略

- **API Gateway**: JWT 验证 + Rate Limiting
- **服务内部**: 基于 Claims 的授权 (Role/Permission)
- **数据层**: Row Level Security (Supabase RLS)

---

## 📈 可观测性

### 监控指标

```
Prometheus 采集:
├── 服务健康: /health
├── 性能指标: /metrics
│   ├── http_requests_total
│   ├── http_request_duration_seconds
│   ├── dotnet_gc_collections_total
│   └── ...
└── 业务指标: custom metrics
    ├── user_registrations_total
    ├── booking_confirmations_total
    └── ...

Grafana 可视化:
├── 服务监控面板
├── 业务指标面板
└── 告警规则
```

### 链路追踪

```
Zipkin/Jaeger:
Client Request
  → API Gateway [Trace ID: xxx]
    → User Service [Span ID: yyy]
      → PostgreSQL Query [Span ID: zzz]
    → File Service [Span ID: aaa]
```

### 日志收集

```
Seq/ELK:
├── Structured Logging (Serilog)
├── Correlation ID
├── User Context
└── Request/Response Logging
```

---

## 🚀 部署策略

### 开发环境

```bash
# 使用 Docker Compose
docker-compose up

服务列表:
- gateway: http://localhost:5000
- user-service: http://localhost:8001
- city-service: http://localhost:8002
- ...
- postgres: localhost:5432
- redis: localhost:6379
```

### 生产环境

```yaml
Kubernetes 部署:
- Namespace: go-nomads
- Deployment: 每个服务 2-3 副本
- Service: ClusterIP (内部) / LoadBalancer (Gateway)
- Ingress: Nginx Ingress Controller
- ConfigMap: 配置管理
- Secret: 敏感信息
- HPA: 自动扩缩容
```

---

## 📝 下一步

详细设计文档:
- [核心业务服务详细设计](./02-core-services-detail.md)
- [基础服务详细设计](./03-infrastructure-services-detail.md)
- [数据库设计](./04-database-design.md)
- [API 网关设计](./05-api-gateway-design.md)

---

**文档版本**: v1.0  
**更新时间**: 2025-01-22  
**维护者**: Go-Nomads 架构团队
