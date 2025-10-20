# Dapr gRPC 配置指南

本文档说明如何在 go-nomads 项目中使用 Dapr 的 gRPC 通信模式。

## 🚀 为什么选择 gRPC？

### 性能对比

| 指标 | HTTP/JSON | gRPC/Protobuf | 性能提升 |
|------|-----------|---------------|----------|
| 序列化速度 | 慢 | **快 5-10 倍** | ⚡⚡⚡ |
| 网络传输 | 文本（大） | **二进制（小 30-50%）** | ⚡⚡ |
| 连接方式 | 短连接 | **长连接复用** | ⚡⚡⚡ |
| 延迟 | 高 | **低 2-3 倍** | ⚡⚡ |
| 吞吐量 | 低 | **高 2-3 倍** | ⚡⚡⚡ |

### 适用场景

✅ **推荐使用 gRPC**：
- 微服务间高频调用
- 需要低延迟
- 大数据量传输
- 内网通信

❌ **不推荐 gRPC**：
- 浏览器直接调用（浏览器不支持 gRPC）
- 需要人类可读的调试信息
- 与老旧系统集成

---

## ⚙️ 配置步骤

### 1. Program.cs 配置

```csharp
// 配置 DaprClient 使用 gRPC
builder.Services.AddDaprClient(daprClientBuilder =>
{
    // 使用 gRPC 端点（性能更好，默认端口 50001）
    daprClientBuilder.UseGrpcEndpoint("http://localhost:50001");
    
    // 可选：配置 HTTP 端点作为备份
    // daprClientBuilder.UseHttpEndpoint("http://localhost:3502");
    
    // 可选：配置超时
    // daprClientBuilder.UseTimeout(TimeSpan.FromSeconds(30));
    
    // 可选：配置重试策略
    // daprClientBuilder.UseJsonSerializationOptions(new JsonSerializerOptions { ... });
});
```

### 2. 服务调用方式

#### ❌ 旧方式（HTTP）
```csharp
var products = await _daprClient.InvokeMethodAsync<object>(
    httpMethod: HttpMethod.Get,  // 指定 HTTP 方法
    appId: "product-service",
    methodName: "api/products/user/123",
    cancellationToken: cancellationToken);
```

#### ✅ 新方式（gRPC）
```csharp
var products = await _daprClient.InvokeMethodAsync<object>(
    appId: "product-service",
    methodName: "api/products/user/123",  // 移除 httpMethod，自动使用 gRPC
    cancellationToken: cancellationToken);
```

### 3. Pub/Sub 和 State Store

Pub/Sub 和 State Store **已经自动使用 gRPC**，无需额外配置：

```csharp
// Pub/Sub - 自动使用 gRPC
await _daprClient.PublishEventAsync(
    pubsubName: "pubsub",
    topicName: "user-created",
    data: userCreatedEvent);

// State Store - 自动使用 gRPC
await _daprClient.SaveStateAsync(
    storeName: "statestore",
    key: "user:123",
    value: user);
```

---

## 🐳 Docker 部署配置

### Dapr Sidecar 端口

在 Docker Compose 或部署脚本中，需要暴露 Dapr gRPC 端口：

```yaml
services:
  user-service:
    image: go-nomads-user-service:latest
    ports:
      - "5002:8080"  # 应用 HTTP 端口
    networks:
      - go-nomads

  user-service-dapr:
    image: daprio/daprd:latest
    command: [
      "./daprd",
      "-app-id", "user-service",
      "-app-port", "8080",
      "-dapr-http-port", "3502",   # Dapr HTTP 端口（可选）
      "-dapr-grpc-port", "50001",  # Dapr gRPC 端口（推荐）
      "-components-path", "/components",
      "-config", "/configuration/config.yaml"
    ]
    depends_on:
      - user-service
    network_mode: "service:user-service"  # 共享网络栈
    volumes:
      - ./components:/components
      - ./configuration:/configuration
```

### 环境变量配置

如果使用环境变量配置 Dapr 端点：

```bash
# .env 文件
DAPR_GRPC_ENDPOINT=http://localhost:50001
DAPR_HTTP_ENDPOINT=http://localhost:3502  # 可选备份
```

```csharp
// Program.cs
builder.Services.AddDaprClient(daprClientBuilder =>
{
    var grpcEndpoint = builder.Configuration["DAPR_GRPC_ENDPOINT"];
    if (!string.IsNullOrEmpty(grpcEndpoint))
    {
        daprClientBuilder.UseGrpcEndpoint(grpcEndpoint);
    }
});
```

---

## 🔍 验证 gRPC 配置

### 1. 检查日志

启动应用后，查看日志确认使用 gRPC：

```
info: Dapr.Client.DaprClientGrpc[0]
      Creating gRPC channel for endpoint: http://localhost:50001
```

### 2. 使用 Dapr CLI 查看

```bash
# 查看 Dapr 运行时信息
dapr list

# 输出示例
APP ID         HTTP PORT  GRPC PORT  APP PORT  COMMAND
user-service   3502       50001      8080      dotnet UserService.dll
```

### 3. 性能测试

使用 gRPC 前后对比：

```bash
# 测试 HTTP 方式
ab -n 1000 -c 10 http://localhost:5002/api/users/123/products

# 测试 gRPC 方式（配置后）
ab -n 1000 -c 10 http://localhost:5002/api/users/123/products
```

**预期结果**：gRPC 方式响应时间降低 40-60%

---

## 📊 端口规划

| 服务 | 应用端口 | Dapr HTTP | Dapr gRPC |
|------|---------|-----------|-----------|
| Gateway | 8080 | 3500 | 50000 |
| ProductService | 8080 | 3501 | 50001 |
| UserService | 8080 | 3502 | 50002 |
| DocumentService | 8080 | 3503 | 50003 |

### 注意事项

1. **Dapr gRPC 端口** 通常从 50000 开始，避免与应用端口冲突
2. **容器内访问** 使用 `localhost` 或 `127.0.0.1`
3. **跨容器访问** 需要使用服务名称（如 `user-service-dapr`）

---

## 🛠️ 高级配置

### 1. gRPC 通道选项

```csharp
builder.Services.AddDaprClient(daprClientBuilder =>
{
    daprClientBuilder.UseGrpcEndpoint("http://localhost:50001");
    
    // 配置 gRPC 通道选项
    daprClientBuilder.UseGrpcChannelOptions(new GrpcChannelOptions
    {
        MaxReceiveMessageSize = 16 * 1024 * 1024, // 16MB
        MaxSendMessageSize = 16 * 1024 * 1024,
        
        // 启用 gRPC 保活（推荐）
        HttpHandler = new SocketsHttpHandler
        {
            PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
            KeepAlivePingDelay = TimeSpan.FromSeconds(60),
            KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
            EnableMultipleHttp2Connections = true
        }
    });
});
```

### 2. 超时和重试

```csharp
// 方法级别超时
var products = await _daprClient.InvokeMethodAsync<object>(
    appId: "product-service",
    methodName: "api/products/user/123",
    cancellationToken: new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);

// 全局超时配置
builder.Services.AddDaprClient(daprClientBuilder =>
{
    daprClientBuilder.UseGrpcEndpoint("http://localhost:50001");
    daprClientBuilder.UseTimeout(TimeSpan.FromSeconds(30));
});
```

### 3. TLS/SSL 配置（生产环境）

```csharp
builder.Services.AddDaprClient(daprClientBuilder =>
{
    // 使用 HTTPS（生产环境推荐）
    daprClientBuilder.UseGrpcEndpoint("https://dapr-sidecar:50001");
    
    // 配置证书验证
    daprClientBuilder.UseGrpcChannelOptions(new GrpcChannelOptions
    {
        HttpHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = 
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        }
    });
});
```

---

## 📈 性能优化建议

### 1. 启用 HTTP/2 连接复用

```csharp
var socketHandler = new SocketsHttpHandler
{
    PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
    EnableMultipleHttp2Connections = true, // 允许多个并发流
    MaxConnectionsPerServer = 100
};
```

### 2. 使用连接池

```csharp
// DaprClient 是单例，自动复用连接
builder.Services.AddSingleton<DaprClient>(sp => 
{
    var daprClient = DaprClient.CreateInvokeHttpClient();
    return daprClient;
});
```

### 3. 批量操作

```csharp
// 使用 gRPC 流式传输（如果 Dapr 服务支持）
var tasks = userIds.Select(userId => 
    _daprClient.InvokeMethodAsync<object>(
        appId: "product-service",
        methodName: $"api/products/user/{userId}"));

var results = await Task.WhenAll(tasks);
```

---

## 🧪 测试和调试

### 1. 使用 grpcurl 测试

```bash
# 安装 grpcurl
go install github.com/fullstorydev/grpcurl/cmd/grpcurl@latest

# 列出 Dapr gRPC 服务
grpcurl -plaintext localhost:50001 list

# 调用方法
grpcurl -plaintext -d '{"appId":"product-service","methodName":"api/products/user/123"}' \
  localhost:50001 dapr.proto.runtime.v1.Dapr/InvokeService
```

### 2. 启用 Dapr 调试日志

```bash
# 启动 Dapr 时启用详细日志
dapr run \
  --app-id user-service \
  --app-port 8080 \
  --dapr-grpc-port 50001 \
  --log-level debug \
  -- dotnet run
```

### 3. 使用 Dapr Dashboard

```bash
# 启动 Dapr Dashboard
dapr dashboard

# 浏览器访问
http://localhost:8080
```

---

## 🔧 故障排查

### 问题 1: 连接被拒绝

**症状**：
```
Grpc.Core.RpcException: Status(StatusCode="Unavailable", Detail="failed to connect to all addresses")
```

**解决方案**：
1. 确认 Dapr sidecar 正在运行：`dapr list`
2. 检查端口是否正确：`netstat -ano | findstr 50001`
3. 检查防火墙设置

### 问题 2: 超时

**症状**：
```
System.Threading.Tasks.TaskCanceledException: The operation was canceled.
```

**解决方案**：
1. 增加超时时间
2. 检查目标服务是否响应
3. 启用 KeepAlive

### 问题 3: gRPC 未生效

**症状**：日志显示仍在使用 HTTP

**解决方案**：
1. 确认移除了 `httpMethod` 参数
2. 检查 `UseGrpcEndpoint` 配置
3. 重启应用和 Dapr sidecar

---

## 📚 相关资源

- [Dapr gRPC 官方文档](https://docs.dapr.io/developing-applications/building-blocks/service-invocation/howto-invoke-services-grpc/)
- [.NET gRPC 最佳实践](https://docs.microsoft.com/en-us/aspnet/core/grpc/performance)
- [gRPC 性能调优](https://grpc.io/docs/guides/performance/)

---

## ✅ 总结

- ✅ gRPC 比 HTTP 快 **2-3 倍**
- ✅ 序列化效率高，payload 小 **30-50%**
- ✅ 长连接复用，减少握手开销
- ✅ Dapr 官方推荐使用 gRPC
- ✅ 配置简单，只需移除 `httpMethod` 参数

**推荐所有内部服务间通信都使用 gRPC！**
