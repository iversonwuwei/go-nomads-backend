# Go-Nomads Backend

基于.NET 9的微服务架构后端系统，使用YARP网关、Dapr服务发现和gRPC通信。

## 项目结构

```
go-nomads-backend/
├── src/
│   ├── Gateway/                    # YARP API网关
│   │   └── Gateway/
│   ├── Services/                   # 微服务
│   │   ├── UserService/           # 用户服务
│   │   └── ProductService/        # 产品服务
│   └── Shared/                    # 共享库
│       └── Shared/
│           ├── Models/            # 共享数据模型
│           └── Protos/           # gRPC协议定义
├── dapr/                          # Dapr配置文件
│   ├── components.yaml           # Dapr组件配置
│   └── config.yaml               # Dapr运行时配置
├── scripts/                       # 启动脚本
├── docker-compose.yml            # Docker编排文件 (兼容性保留)
├── podman-compose.yml            # Podman编排文件
└── README.md                     # 项目文档
```

## 技术栈

- **.NET 9**: 主要开发框架
- **YARP**: 反向代理网关
- **Dapr**: 服务发现和运行时
- **gRPC**: 服务间通信
- **Redis**: 状态存储和消息发布/订阅
- **Podman**: 容器化部署 (Docker兼容)

## 架构特点

### 1. 微服务架构
- **Gateway Service**: YARP反向代理，统一入口
- **User Service**: 用户管理服务
- **Product Service**: 产品管理服务，演示服务间gRPC调用

### 2. 服务发现
- 使用Dapr实现服务发现
- 支持本地开发和容器化部署
- 基于mDNS的名称解析

### 3. 通信方式
- **外部通信**: HTTP REST API (通过Gateway)
- **内部通信**: gRPC (服务间直接调用)
- **异步通信**: Redis pub/sub (通过Dapr)

### 4. 状态管理
- Redis作为状态存储
- 支持分布式缓存
- 统一的状态管理接口

## 服务端口分配

| 服务 | HTTP端口 | gRPC端口 | 说明 |
|------|----------|----------|------|
| Gateway | 5000 | - | YARP网关 |
| UserService | 5001 | 5001 | 用户服务 |
| ProductService | 5002 | 5002 | 产品服务 |

## 快速开始

### 前置要求

1. **.NET 9 SDK**
2. **Dapr CLI** (v1.12+)
3. **Podman** 和 **Podman Compose** (或Docker)
4. **Redis** (可通过Podman运行)

### 安装依赖

```powershell
# 安装Dapr CLI
powershell -Command "iwr -useb https://raw.githubusercontent.com/dapr/cli/master/install/install.ps1 | iex"

# 初始化Dapr
dapr init
```

### 本地开发运行

#### 方式一：使用启动脚本 (推荐)

```powershell
# 运行所有服务
.\scripts\start-all.ps1

# 停止所有服务
.\scripts\stop-all.ps1
```

#### 方式二：手动启动各服务

```powershell
# 1. 启动Redis
podman run -d --name redis -p 6379:6379 redis:alpine

# 2. 启动UserService
cd src\Services\UserService\UserService
dapr run --app-id user-service --app-port 5001 --dapr-http-port 3001 --dapr-grpc-port 50001 --components-path ..\..\..\..\dapr -- dotnet run

# 3. 启动ProductService
cd src\Services\ProductService\ProductService
dapr run --app-id product-service --app-port 5002 --dapr-http-port 3002 --dapr-grpc-port 50002 --components-path ..\..\..\..\dapr -- dotnet run

# 4. 启动Gateway
cd src\Gateway\Gateway
dapr run --app-id gateway --app-port 5000 --dapr-http-port 3000 --dapr-grpc-port 50000 --components-path ..\..\..\dapr -- dotnet run
```

### Podman部署 (推荐)

#### 快速开始
```powershell
# 一键启动所有服务
.\start.ps1

# 或使用完整部署脚本
.\deploy-podman.ps1 -Action start
```

#### 详细步骤
```powershell
# 1. 启动服务 (自动构建镜像、创建网络、启动容器)
.\deploy-podman.ps1 -Action start

# 2. 查看服务状态
.\deploy-podman.ps1 -Action status

# 3. 查看日志
podman logs -f go-nomads-gateway
podman logs -f go-nomads-product-service
podman logs -f go-nomads-user-service

# 4. 停止服务
.\deploy-podman.ps1 -Action stop

# 5. 清理所有资源
.\deploy-podman.ps1 -Action clean
```

#### 使用 Podman Compose
```powershell
# 构建并启动所有服务
podman-compose -f podman-compose.yml up --build -d

# 查看运行状态
podman-compose -f podman-compose.yml ps

# 查看日志
podman-compose -f podman-compose.yml logs -f

# 停止服务
podman-compose -f podman-compose.yml down
```

📖 **详细文档**: [Podman部署指南](PODMAN_DEPLOYMENT.md)

### Docker部署 (兼容)

```powershell
# 使用Podman Compose构建并启动所有服务
podman-compose -f podman-compose.yml up --build

# 后台运行
podman-compose -f podman-compose.yml up -d --build

# 停止服务
podman-compose -f podman-compose.yml down

# 或者使用Docker兼容模式
docker-compose up --build
```

## API端点

### Gateway (端口 5000)

- `GET /health` - 健康检查
- `GET /api/users` - 获取用户列表
- `GET /api/users/{id}` - 获取用户详情
- `POST /api/users` - 创建用户
- `PUT /api/users/{id}` - 更新用户
- `DELETE /api/users/{id}` - 删除用户

- `GET /api/products` - 获取产品列表
- `GET /api/products/{id}` - 获取产品详情
- `GET /api/products/user/{userId}` - 获取用户的产品
- `POST /api/products` - 创建产品
- `PUT /api/products/{id}` - 更新产品
- `DELETE /api/products/{id}` - 删除产品

### 示例API调用

```powershell
# 创建用户
curl -X POST http://localhost:5000/api/users \
  -H "Content-Type: application/json" \
  -d '{"name":"John Doe","email":"john@example.com","phone":"123-456-7890"}'

# 获取用户列表
curl http://localhost:5000/api/users

# 创建产品
curl -X POST http://localhost:5000/api/products \
  -H "Content-Type: application/json" \
  -d '{"name":"Laptop","description":"Gaming laptop","price":1299.99,"userId":"1","category":"Electronics"}'
```

## gRPC服务

### UserService gRPC

```proto
service UserService {
  rpc GetUser (GetUserRequest) returns (UserResponse);
  rpc CreateUser (CreateUserRequest) returns (UserResponse);
  rpc UpdateUser (UpdateUserRequest) returns (UserResponse);
  rpc DeleteUser (DeleteUserRequest) returns (DeleteUserResponse);
  rpc ListUsers (ListUsersRequest) returns (ListUsersResponse);
}
```

### ProductService gRPC

```proto
service ProductService {
  rpc GetProduct (GetProductRequest) returns (ProductResponse);
  rpc CreateProduct (CreateProductRequest) returns (ProductResponse);
  rpc UpdateProduct (UpdateProductRequest) returns (ProductResponse);
  rpc DeleteProduct (DeleteProductRequest) returns (DeleteProductResponse);
  rpc ListProducts (ListProductsRequest) returns (ListProductsResponse);
  rpc GetProductsByUserId (GetProductsByUserIdRequest) returns (ListProductsResponse);
}
```

## 配置说明

### YARP配置 (Gateway/appsettings.json)

```json
{
  "ReverseProxy": {
    "Routes": {
      "user-route": {
        "ClusterId": "user-cluster",
        "Match": {
          "Path": "/api/users/{**catch-all}"
        }
      }
    },
    "Clusters": {
      "user-cluster": {
        "Destinations": {
          "user-service": {
            "Address": "http://localhost:5001/"
          }
        }
      }
    }
  }
}
```

### Dapr组件配置

- **State Store**: Redis状态存储
- **Pub/Sub**: Redis消息队列
- **Secret Store**: 本地文件密钥存储

## 开发指南

### 添加新的微服务

1. 在`src/Services/`创建新服务项目
2. 添加gRPC服务定义到`src/Shared/Shared/Protos/`
3. 更新Gateway路由配置
4. 添加Dapr配置
5. 更新Podman Compose文件

### 代码结构约定

- **Controllers**: REST API控制器
- **Services**: gRPC服务实现
- **Models**: 数据模型 (在Shared项目中)
- **Protos**: gRPC协议定义 (在Shared项目中)

### 错误处理

所有API返回统一的响应格式：

```json
{
  "success": true,
  "message": "操作成功",
  "data": {...},
  "errors": []
}
```

## 监控和调试

### Dapr Dashboard

```powershell
# 启动Dapr Dashboard
dapr dashboard
```

访问 http://localhost:8080 查看服务状态

### 分布式追踪

系统集成了Zipkin进行分布式追踪：

- Zipkin UI: http://localhost:9411
- 自动收集gRPC和HTTP调用链

### 日志

各服务日志级别配置：

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Grpc": "Debug",
      "Yarp": "Information"
    }
  }
}
```

## 故障排除

### 常见问题

1. **端口冲突**: 确保端口5000-5002和3000-3002没有被占用
2. **Dapr未启动**: 运行`dapr --version`检查Dapr是否正确安装
3. **Redis连接失败**: 确保Redis服务在6379端口运行
4. **gRPC通信失败**: 检查防火墙设置和端口配置

### 调试命令

```powershell
# 检查Dapr状态
dapr list

# 查看Dapr日志
dapr logs --app-id user-service

# 检查服务健康状态
curl http://localhost:5000/health
curl http://localhost:5001/health
curl http://localhost:5002/health
```

## 贡献指南

1. Fork项目
2. 创建功能分支
3. 提交代码更改
4. 创建Pull Request

## 许可证

MIT License

## 联系方式

如有问题，请创建Issue或联系项目维护者。