# Dapr 部署指南

## 部署日期
2025-10-20

## 架构概述

现在系统同时使用 **Dapr** 和 **Consul**：
- **Dapr**: 用于服务间通信、状态管理、pub/sub
- **Consul**: 用于服务注册、发现和健康检查

## 部署的组件

### 1. Dapr Placement 服务
- **容器名**: `go-nomads-dapr-placement`
- **端口**: `50006`
- **作用**: Actor 放置服务，管理 Actor 的分布和调度

### 2. Dapr Sidecar 容器

每个应用服务都有一个对应的 Dapr sidecar：

| 服务 | Sidecar 容器名 | HTTP 端口 | gRPC 端口 | Metrics |
|------|---------------|----------|-----------|---------|
| user-service | go-nomads-user-service-dapr | 3501 | 50011 | 9091 |
| product-service | go-nomads-product-service-dapr | 3502 | 50012 | 9091 |
| document-service | go-nomads-document-service-dapr | 3503 | 50013 | 9091 |
| gateway | go-nomads-gateway-dapr | 3500 | 50010 | 9091 |

## Dapr 配置文件

### Components 目录: `deployment/dapr/components/`

1. **statestore-redis.yaml** - Redis 状态存储
   ```yaml
   type: state.redis
   metadata:
     - name: redisHost
       value: go-nomads-redis:6379
     - name: actorStateStore
       value: "true"
   ```

2. **pubsub-redis.yaml** - Redis 发布/订阅
   ```yaml
   type: pubsub.redis
   metadata:
     - name: redisHost
       value: go-nomads-redis:6379
   ```

3. **configuration-redis.yaml** - Redis 配置存储
   ```yaml
   type: configuration.redis
   metadata:
     - name: redisHost
       value: go-nomads-redis:6379
   ```

### Config 目录: `deployment/dapr/config/`

**config.yaml** - Dapr 运行时配置
```yaml
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

## 网络架构

### 容器网络配置

```
┌─────────────────────────────────────────────┐
│ Docker Network: go-nomads-network           │
│                                             │
│  ┌──────────────────────────────────┐      │
│  │ Application Container            │      │
│  │ - Port 8080 (App)                │      │
│  │ - Port 3500 (Dapr HTTP)          │      │
│  │ - Port 50001 (Dapr gRPC)         │      │
│  └──────────────────────────────────┘      │
│           │                                 │
│           │ localhost                       │
│           ▼                                 │
│  ┌──────────────────────────────────┐      │
│  │ Dapr Sidecar Container           │      │
│  │ - Shares network with app        │      │
│  │ - Access app via localhost:8080  │      │
│  └──────────────────────────────────┘      │
└─────────────────────────────────────────────┘
```

关键点：
- Dapr sidecar 使用 `--network "container:go-nomads-{service}"` 
- Dapr 通过 `localhost:8080` 访问应用
- 应用通过 `localhost:3500` 访问 Dapr HTTP API
- 应用通过 `localhost:50001` 访问 Dapr gRPC API

## 使用示例

### 1. 服务间调用（通过 Dapr）

在 ProductService 中调用 UserService：

```csharp
// 注入 DaprClient
private readonly DaprClient _daprClient;

// 通过 Dapr 调用
var userResponse = await _daprClient.InvokeMethodAsync<GetUserRequest, UserResponse>(
    "user-service",      // 目标服务的 app-id
    "GetUser",           // 方法名
    new GetUserRequest { Id = request.UserId }
);
```

### 2. 状态管理

```csharp
// 保存状态
await _daprClient.SaveStateAsync("statestore", "key", value);

// 获取状态
var value = await _daprClient.GetStateAsync<MyType>("statestore", "key");

// 删除状态
await _daprClient.DeleteStateAsync("statestore", "key");
```

### 3. 发布/订阅

```csharp
// 发布消息
await _daprClient.PublishEventAsync("pubsub", "topic-name", data);

// 订阅（在 Startup/Program.cs）
app.MapSubscribeHandler();

// 订阅处理器
[Topic("pubsub", "topic-name")]
public async Task<IActionResult> HandleEvent([FromBody] MyEventData data)
{
    // 处理事件
    return Ok();
}
```

### 4. 直接 HTTP 调用 Dapr API

```bash
# 获取服务元数据
curl http://localhost:3501/v1.0/metadata

# 通过 Dapr 调用服务
curl -X POST http://localhost:3501/v1.0/invoke/user-service/method/api/users \
  -H "Content-Type: application/json" \
  -d '{"name": "John"}'

# 状态操作
curl -X POST http://localhost:3501/v1.0/state/statestore \
  -H "Content-Type: application/json" \
  -d '[{"key": "mykey", "value": "myvalue"}]'

# 获取状态
curl http://localhost:3501/v1.0/state/statestore/mykey

# 发布事件
curl -X POST http://localhost:3501/v1.0/publish/pubsub/topic-name \
  -H "Content-Type: application/json" \
  -d '{"data": "message"}'
```

## 部署流程

### 1. 自动部署（推荐）

```bash
cd deployment
./deploy-services-local.sh
```

脚本会自动：
1. 检查并启动 Dapr Placement 服务
2. 为每个服务构建镜像
3. 启动应用容器
4. 启动 Dapr sidecar 容器
5. 等待服务注册到 Consul

### 2. 手动部署单个服务

```bash
# 1. 启动 Dapr Placement（如果未运行）
docker run -d \
  --name go-nomads-dapr-placement \
  --network go-nomads-network \
  -p 50006:50006 \
  daprio/dapr:latest \
  ./placement --port 50006

# 2. 启动应用容器
docker run -d \
  --name go-nomads-user-service \
  --network go-nomads-network \
  -p 5001:8080 \
  -p 3501:3500 \
  -p 50011:50001 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ASPNETCORE_URLS=http://+:8080 \
  go-nomads-user-service:latest

# 3. 启动 Dapr sidecar
docker run -d \
  --name go-nomads-user-service-dapr \
  --network "container:go-nomads-user-service" \
  -v "$(pwd)/dapr/components:/components:ro" \
  -v "$(pwd)/dapr/config:/config:ro" \
  daprio/daprd:latest \
  ./daprd \
  --app-id user-service \
  --app-protocol http \
  --app-port 8080 \
  --dapr-http-port 3500 \
  --dapr-grpc-port 50001 \
  --components-path /components \
  --config /config/config.yaml \
  --placement-host-address go-nomads-dapr-placement:50006 \
  --log-level info \
  --metrics-port 9091 \
  --enable-metrics=true
```

## 验证部署

### 1. 检查容器状态

```bash
# 查看所有 Dapr 相关容器
docker ps --format "table {{.Names}}\t{{.Status}}" | grep dapr

# 预期输出：
# go-nomads-gateway-dapr            Up X seconds
# go-nomads-document-service-dapr   Up X seconds
# go-nomads-product-service-dapr    Up X seconds
# go-nomads-user-service-dapr       Up X seconds
# go-nomads-dapr-placement          Up X minutes
```

### 2. 测试 Dapr 功能

```bash
# 检查 user-service 的 Dapr 元数据
curl -s http://localhost:3501/v1.0/metadata | jq

# 预期输出包含：
# - id: "user-service"
# - components: ["configstore", "pubsub", "statestore"]
```

### 3. 验证服务注册

```bash
# 检查 Consul 服务列表
curl -s http://localhost:8500/v1/agent/services | jq 'keys'

# 预期输出所有 4 个服务
```

### 4. 测试服务间通信

```bash
# 通过 Dapr 调用 user-service
curl -X POST http://localhost:3501/v1.0/invoke/user-service/method/health

# 应该返回健康检查响应
```

## 监控

### 1. Dapr Metrics

每个 Dapr sidecar 在端口 9091 暴露 Prometheus metrics：

```bash
curl http://localhost:5001/metrics  # 从应用容器访问（共享网络）
```

### 2. Dapr Dashboard (可选)

```bash
# 启动 Dapr Dashboard
docker run -d \
  --name dapr-dashboard \
  --network go-nomads-network \
  -p 8080:8080 \
  daprio/dashboard:latest

# 访问: http://localhost:8080
```

### 3. Zipkin 跟踪

Dapr 自动发送跟踪数据到 Zipkin：
- 访问: http://localhost:9411
- 查看服务间调用链路

## 故障排查

### 1. Dapr Sidecar 退出

```bash
# 查看日志
docker logs go-nomads-user-service-dapr

# 常见问题：
# - Component 配置错误
# - 重复的 actor state store
# - 无法连接到 Placement
```

### 2. 服务无法通过 Dapr 调用

```bash
# 检查 Dapr 是否能访问应用
docker exec go-nomads-user-service-dapr curl -s http://localhost:8080/health

# 检查 Dapr HTTP 端口
curl http://localhost:3501/v1.0/healthz
```

### 3. 组件初始化失败

```bash
# 检查 Redis 连接
docker exec go-nomads-user-service-dapr \
  curl -s telnet://go-nomads-redis:6379

# 检查组件配置
docker exec go-nomads-user-service-dapr ls -la /components
```

## 清理

```bash
# 停止所有服务
./stop-services.sh

# 删除所有 Dapr 相关容器
docker rm -f $(docker ps -aq --filter "name=dapr")

# 删除 Placement 服务
docker rm -f go-nomads-dapr-placement
```

## 端口映射总结

| 服务 | 应用端口 | Dapr HTTP | Dapr gRPC | Consul 健康检查 |
|------|---------|-----------|-----------|----------------|
| Gateway | 5000 | 3500 | 50010 | ✅ |
| UserService | 5001 | 3501 | 50011 | ✅ |
| ProductService | 5002 | 3502 | 50012 | ✅ |
| DocumentService | 5003 | 3503 | 50013 | ✅ |
| Dapr Placement | - | - | 50006 | - |

## 优势

1. **服务间通信简化**: 通过 Dapr SDK，无需关心服务地址
2. **状态管理**: 统一的状态存储抽象
3. **发布/订阅**: 解耦的消息传递
4. **分布式跟踪**: 自动集成 Zipkin
5. **重试和超时**: 内置弹性机制
6. **双重服务发现**: Dapr + Consul 互补

## 下一步

- [ ] 配置 Dapr 重试策略
- [ ] 添加更多 Dapr Components（如密钥存储）
- [ ] 实现 Actor 模式
- [ ] 配置 Dapr Dashboard
- [ ] 添加 Workflow 支持
