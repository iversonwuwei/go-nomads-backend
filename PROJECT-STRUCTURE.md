# Go-Nomads Backend 项目结构说明

本文档详细说明了 Go-Nomads Backend 微服务项目的架构和文件组织结构。

## 总体架构

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   外部客户端    │    │   移动应用      │    │   Web前端       │
└─────────┬───────┘    └─────────┬───────┘    └─────────┬───────┘
          │                      │                      │
          └──────────────────────┼──────────────────────┘
                                 │
                          HTTP REST API
                                 │
                    ┌─────────────▼─────────────┐
                    │     YARP Gateway         │
                    │   (端口: 5000)           │
                    └─────────────┬─────────────┘
                                 │
                         Dapr 服务发现
                                 │
                    ┌─────────────▼─────────────┐
                    │     服务网格              │
                    └─────────────┬─────────────┘
                                 │
          ┌──────────────────────┼──────────────────────┐
          │                      │                      │
    ┌─────▼─────┐          ┌─────▼─────┐          ┌─────▼─────┐
    │UserService│          │ProductSvc │          │ 未来服务  │
    │(端口:5001)│◄────────►│(端口:5002)│          │           │
    └───────────┘   gRPC   └───────────┘          └───────────┘
          │                      │                      │
          └──────────────────────┼──────────────────────┘
                                 │
                        ┌────────▼────────┐
                        │  Redis (Dapr)   │
                        │  状态存储/消息   │
                        └─────────────────┘
```

## 项目文件结构

### 根目录

```
go-nomads-backend/
├── go-nomads-backend.sln          # Visual Studio 解决方案文件
├── README.md                       # 项目主文档
├── PROJECT-STRUCTURE.md            # 本文档 - 项目结构说明
├── docker-compose.yml              # Docker 编排配置 (兼容性保留)
├── podman-compose.yml              # Podman 编排配置
└── .gitignore                      # Git 忽略文件配置
```

### 源代码结构 (src/)

```
src/
├── Gateway/                        # API 网关层
│   └── Gateway/                    # YARP 网关项目
│       ├── Gateway.csproj          # 项目文件
│       ├── Program.cs              # 程序入口点
│       ├── appsettings.json        # 应用配置
│       └── appsettings.Development.json # 开发环境配置
│
├── Services/                       # 微服务层
│   ├── UserService/                # 用户服务
│   │   └── UserService/
│   │       ├── UserService.csproj  # 项目文件
│   │       ├── Program.cs          # 程序入口
│   │       ├── Controllers/        # REST API 控制器
│   │       │   └── UsersController.cs
│   │       ├── Services/           # gRPC 服务实现
│   │       │   └── UserGrpcService.cs
│   │       └── appsettings.json    # 服务配置
│   │
│   └── ProductService/             # 产品服务
│       └── ProductService/
│           ├── ProductService.csproj
│           ├── Program.cs
│           ├── Controllers/
│           │   └── ProductsController.cs
│           ├── Services/
│           │   └── ProductGrpcService.cs
│           └── appsettings.json
│
└── Shared/                         # 共享库
    └── Shared/
        ├── Shared.csproj           # 共享项目文件
        ├── Models/                 # 数据模型
        │   ├── User.cs             # 用户模型
        │   ├── Product.cs          # 产品模型
        │   └── Common.cs           # 通用响应模型
        └── Protos/                 # gRPC 协议定义
            ├── user.proto          # 用户服务协议
            └── product.proto       # 产品服务协议
```

### 配置文件 (dapr/)

```
dapr/
├── components.yaml                 # Dapr 组件配置
│   ├── Redis 状态存储配置
│   ├── Redis 发布/订阅配置
│   └── 本地文件密钥存储配置
└── config.yaml                    # Dapr 运行时配置
    ├── 追踪配置 (Zipkin)
    ├── 指标配置
    └── 中间件配置
```

### 脚本文件 (scripts/)

```
scripts/
├── start-all.ps1                  # 启动所有服务
├── stop-all.ps1                   # 停止所有服务
├── dev-tools.ps1                  # 开发辅助工具
└── test-api.ps1                   # API 测试脚本
```

## 详细组件说明

### 1. Gateway (API 网关)

**职责:**
- 统一入口点，处理所有外部请求
- 请求路由到对应的微服务
- 负载均衡和故障转移
- 认证和授权 (未来扩展)
- 请求/响应日志记录

**技术栈:**
- ASP.NET Core 9.0
- YARP (Yet Another Reverse Proxy)
- Dapr SDK for .NET

**关键文件:**
- `Program.cs`: 配置 YARP 反向代理和 Dapr 集成
- `appsettings.json`: 路由规则和集群配置

**路由配置:**
```json
{
  "ReverseProxy": {
    "Routes": {
      "user-route": {
        "ClusterId": "user-cluster",
        "Match": { "Path": "/api/users/{**catch-all}" }
      },
      "product-route": {
        "ClusterId": "product-cluster", 
        "Match": { "Path": "/api/products/{**catch-all}" }
      }
    }
  }
}
```

### 2. UserService (用户服务)

**职责:**
- 用户信息管理 (CRUD)
- 用户认证和授权
- 用户资料维护
- 提供 gRPC 和 REST API

**技术栈:**
- ASP.NET Core 9.0
- gRPC
- Dapr SDK
- Protocol Buffers

**API 端点:**
```
GET    /api/users          # 获取用户列表
GET    /api/users/{id}     # 获取用户详情
POST   /api/users          # 创建用户
PUT    /api/users/{id}     # 更新用户
DELETE /api/users/{id}     # 删除用户
```

**gRPC 服务:**
```protobuf
service UserService {
  rpc GetUser (GetUserRequest) returns (UserResponse);
  rpc CreateUser (CreateUserRequest) returns (UserResponse);
  rpc UpdateUser (UpdateUserRequest) returns (UserResponse);
  rpc DeleteUser (DeleteUserRequest) returns (DeleteUserResponse);
  rpc ListUsers (ListUsersRequest) returns (ListUsersResponse);
}
```

### 3. ProductService (产品服务)

**职责:**
- 产品信息管理 (CRUD)
- 产品分类管理
- 库存管理 (未来扩展)
- 与用户服务协作验证用户

**技术栈:**
- ASP.NET Core 9.0
- gRPC (客户端和服务端)
- Dapr SDK
- Protocol Buffers

**服务依赖:**
- 依赖 UserService 进行用户验证
- 通过 Dapr 服务调用进行服务间通信

**API 端点:**
```
GET    /api/products               # 获取产品列表
GET    /api/products/{id}          # 获取产品详情
GET    /api/products/user/{userId} # 获取用户的产品
POST   /api/products               # 创建产品
PUT    /api/products/{id}          # 更新产品
DELETE /api/products/{id}          # 删除产品
```

### 4. Shared (共享库)

**职责:**
- 定义通用数据模型
- gRPC 协议定义
- 共享业务逻辑
- 通用响应格式

**组件:**

#### Models (数据模型)
```csharp
// User.cs - 用户模型
public class User
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Product.cs - 产品模型
public class Product
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public string UserId { get; set; }
    public string Category { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Common.cs - 通用响应
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
    public List<string> Errors { get; set; }
}
```

#### Protos (协议定义)
- `user.proto`: 用户服务 gRPC 协议
- `product.proto`: 产品服务 gRPC 协议

### 5. Dapr 配置

#### components.yaml (组件配置)
```yaml
# Redis 状态存储
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: statestore
spec:
  type: state.redis
  version: v1
  metadata:
  - name: redisHost
    value: localhost:6379

# Redis 发布/订阅
apiVersion: dapr.io/v1alpha1  
kind: Component
metadata:
  name: pubsub
spec:
  type: pubsub.redis
  version: v1
  metadata:
  - name: redisHost
    value: localhost:6379

# 本地密钥存储
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: localsecretstore
spec:
  type: secretstores.local.file
  version: v1
  metadata:
  - name: secretsFile
    value: ./secrets.json
```

#### config.yaml (运行时配置)
```yaml
apiVersion: dapr.io/v1alpha1
kind: Configuration
metadata:
  name: appconfig
spec:
  tracing:
    samplingRate: "1"
    zipkin:
      endpointAddress: "http://localhost:9411/api/v2/spans"
  metric:
    enabled: true
```

## 通信模式

### 1. 外部通信 (客户端 → Gateway)
- **协议**: HTTP/HTTPS REST API
- **端口**: 5000
- **格式**: JSON
- **认证**: Bearer Token (未来实现)

### 2. 内部通信 (服务间)
- **协议**: gRPC + Dapr 服务调用
- **发现**: Dapr 名称解析
- **格式**: Protocol Buffers
- **重试**: Dapr 自动重试机制

### 3. 异步通信
- **协议**: Redis Pub/Sub (通过 Dapr)
- **用途**: 事件通知、异步处理
- **格式**: JSON 消息

## 部署架构

### 本地开发
```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│   Gateway   │    │ UserService │    │ProductSvc   │
│   (5000)    │    │   (5001)    │    │  (5002)     │
│   + Dapr    │    │   + Dapr    │    │   + Dapr    │
│  (3000)     │    │  (3001)     │    │  (3002)     │
└─────────────┘    └─────────────┘    └─────────────┘
       │                   │                   │
       └───────────────────┼───────────────────┘
                           │
                  ┌────────▼────────┐
                  │     Redis       │
                  │    (6379)       │
                  └─────────────────┘
```

### Podman 部署
```
┌─────────────────────────────────────────────────────┐
│                Podman Network                        │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐    │
│  │   gateway   │ │ user-service│ │product-svc  │    │
│  │  container  │ │  container  │ │ container   │    │
│  └─────────────┘ └─────────────┘ └─────────────┘    │
│         │               │               │           │
│         └───────────────┼───────────────┘           │
│                         │                           │
│                ┌────────▼────────┐                  │
│                │ redis container │                  │
│                └─────────────────┘                  │
└─────────────────────────────────────────────────────┘
```

## 数据流

### 创建产品的完整流程
```
1. 客户端 → Gateway (POST /api/products)
   ↓
2. Gateway → ProductService (路由转发)
   ↓
3. ProductService → UserService (gRPC 用户验证)
   ↓
4. UserService → Redis (查询用户信息)
   ↓
5. Redis → UserService (返回用户数据)
   ↓
6. UserService → ProductService (返回验证结果)
   ↓
7. ProductService → Redis (保存产品信息)
   ↓
8. ProductService → Event Bus (发布产品创建事件)
   ↓
9. ProductService → Gateway (返回结果)
   ↓
10. Gateway → 客户端 (返回响应)
```

## 扩展性考虑

### 1. 水平扩展
- 每个服务都可以独立扩展
- Dapr 提供负载均衡和服务发现
- Redis 可以配置为集群模式

### 2. 数据库分离
- 每个服务可以使用独立的数据库
- 通过 Dapr 状态存储抽象化存储层
- 支持多种数据库类型

### 3. 添加新服务
1. 在 `src/Services/` 创建新服务项目
2. 在 `src/Shared/Protos/` 添加协议定义
3. 更新 Gateway 路由配置
4. 添加 Dapr 配置
5. 更新 Podman Compose 文件

### 4. 跨数据中心部署
- Dapr 支持多区域配置
- Redis 可以配置为跨区域复制
- 支持服务网格集成

## 监控和可观测性

### 1. 日志
- 结构化日志记录
- 分布式追踪 ID
- 统一日志格式

### 2. 指标
- Dapr 内置指标
- 自定义业务指标
- Prometheus 兼容格式

### 3. 追踪
- Zipkin 分布式追踪
- 请求链路跟踪
- 性能瓶颈识别

### 4. 健康检查
- 每个服务提供健康检查端点
- Dapr 健康检查集成
- 自动故障恢复

## 安全考虑

### 1. 网络安全
- 服务间 mTLS 通信 (Dapr)
- API Gateway 统一认证
- 网络隔离和防火墙

### 2. 数据安全
- 敏感数据加密存储
- Dapr 密钥管理
- 数据访问审计

### 3. 认证授权
- JWT Token 认证
- RBAC 权限控制
- API 限流和防护

## 最佳实践

### 1. 代码组织
- 单一职责原则
- 依赖注入
- 配置外部化

### 2. API 设计
- RESTful 风格
- 版本控制
- 统一错误处理

### 3. 数据一致性
- 最终一致性
- 补偿事务
- 幂等性设计

### 4. 容错设计
- 断路器模式
- 重试机制
- 优雅降级

## 故障排除

### 常见问题和解决方案

1. **服务无法启动**
   - 检查端口占用
   - 验证 Dapr 配置
   - 查看服务日志

2. **服务间通信失败**
   - 检查 Dapr sidecar 状态
   - 验证服务发现配置
   - 查看网络连接

3. **数据库连接问题**
   - 检查 Redis 连接状态
   - 验证组件配置
   - 检查网络访问权限

4. **性能问题**
   - 分析追踪数据
   - 检查资源使用情况
   - 优化查询和缓存策略