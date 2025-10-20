# Dapr gRPC 配置指南

## ✅ 成功配置 - UserService 使用 Dapr gRPC

### 配置概览

UserService 现已成功配置为通过 **Dapr gRPC** 与其他服务通信,而不是使用 HTTP。

### 关键配置

#### 1. Program.cs 配置

```csharp
// 配置 DaprClient 连接到 Dapr sidecar
// Dapr sidecar 与应用共享网络命名空间,通过 localhost 访问
// 使用 gRPC 端点(性能更好:2-3x 吞吐量,30-50% 更小的负载)
// 
// Dapr 环境变量:
// - DAPR_GRPC_PORT: gRPC 端口号(默认: 50001)
// - DAPR_HTTP_PORT: HTTP 端口号(默认: 3500)
builder.Services.AddDaprClient();
```

**要点:**
- 不需要显式调用 `UseGrpcEndpoint()` 或 `UseHttpEndpoint()`
- Dapr SDK 会自动读取环境变量 `DAPR_GRPC_PORT`
- 当设置了 `DAPR_GRPC_PORT` 时,DaprClient 自动使用 gRPC

#### 2. 环境变量配置

在 Docker 容器启动时设置:

```powershell
docker run -d \
  -e DAPR_GRPC_PORT="50001" \
  -e DAPR_HTTP_PORT="3502" \
  ...
```

#### 3. 服务调用代码

```csharp
// 使用 Dapr gRPC 调用其他服务
var products = await _daprClient.InvokeMethodAsync<object>(
    httpMethod: HttpMethod.Get,      // 必须指定 HTTP 方法
    appId: "product-service",         // 目标服务的 app-id
    methodName: $"/api/products/user/{userId}",  // API 路径
    cancellationToken: cancellationToken);
```

**重要提示:**
- 必须指定 `httpMethod` 参数(GET, POST, PUT, DELETE 等)
- 如果不指定,默认使用 POST 方法,可能导致 405 Method Not Allowed 错误

#### 4. Dapr Sidecar 配置

```bash
./daprd \
  --app-id user-service \
  --app-port 8080 \
  --dapr-http-port 3502 \
  --dapr-grpc-port 50001 \     # gRPC 端口
  --log-level info
```

### 网络配置

#### Sidecar 模式部署

```powershell
# 1. 启动应用容器(暴露所有需要的端口)
docker run -d \
  --name go-nomads-user-service \
  --network go-nomads-network \
  -p 5002:8080 \
  -p 3502:3502 \
  -e DAPR_GRPC_PORT="50001" \
  ...

# 2. 启动 Dapr sidecar(共享应用容器的网络命名空间)
docker run -d \
  --name go-nomads-user-service-dapr \
  --network "container:go-nomads-user-service" \
  daprio/daprd:latest ./daprd \
    --app-id user-service \
    --app-port 8080 \
    --dapr-http-port 3502 \
    --dapr-grpc-port 50001
```

**关键点:**
- 使用 `--network "container:<app-container>"` 让 Dapr sidecar 共享应用容器的网络
- 应用和 Dapr 通过 `localhost` 或 `127.0.0.1` 通信
- 端口映射只在应用容器配置,sidecar 不需要独立映射端口

### 性能优势

| 指标 | HTTP | gRPC | 提升 |
|------|------|------|------|
| **吞吐量** | 基准 | 2-3x | 200-300% |
| **负载大小** | 基准 | -30~50% | 减少 30-50% |
| **延迟** | 基准 | -20~40% | 降低 20-40% |
| **序列化速度** | JSON | Protobuf | 5-10x |
| **连接复用** | 单请求 | 多路复用 | ✅ |

### 验证配置

#### 检查环境变量

```powershell
docker exec go-nomads-user-service env | Select-String -Pattern "DAPR"
```

期望输出:
```
DAPR_GRPC_PORT=50001
DAPR_HTTP_PORT=3502
```

#### 检查 Dapr sidecar 日志

```powershell
docker logs go-nomads-user-service-dapr | Select-String -Pattern "grpc|gRPC"
```

期望输出:
```
level=info msg="gRPC server listening on TCP address: :50001"
level=info msg="API gRPC server is running on port 50001"
```

#### 测试服务调用

```powershell
Invoke-WebRequest -Uri "http://localhost:5002/api/users/{userId}/products"
```

期望响应:
```json
{
  "success": true,
  "message": "User products retrieved successfully",
  "data": { ... }
}
```

### 常见问题

#### 问题 1: Connection refused (localhost:3500)

**原因:** DaprClient 使用了默认的 HTTP 端口 3500,而没有使用 gRPC 端口

**解决方案:**
1. 确保设置了环境变量 `DAPR_GRPC_PORT=50001`
2. 检查容器是否正确传递了环境变量
3. 重启容器以应用新的环境变量

#### 问题 2: 405 Method Not Allowed

**原因:** `InvokeMethodAsync` 默认使用 POST 方法,但目标 API 需要 GET 方法

**解决方案:**
```csharp
// 显式指定 HTTP 方法
var result = await _daprClient.InvokeMethodAsync<object>(
    httpMethod: HttpMethod.Get,  // 添加这行
    appId: "service-name",
    methodName: "/api/path",
    cancellationToken: cancellationToken);
```

#### 问题 3: 应用监听了 50002 等额外端口

**原因:** `builder.Services.AddGrpc()` 导致 Kestrel 自动配置了 gRPC 端点

**解决方案:**
如果应用本身不提供 gRPC 服务,只是作为客户端调用 Dapr,则移除这行代码:

```csharp
// 删除此行(如果不提供 gRPC 服务)
// builder.Services.AddGrpc();
```

### 下一步

1. **应用到其他服务:**
   - ProductService
   - DocumentService
   - Gateway

2. **更新部署脚本:**
   - 在 `deploy-services-local.ps1` 中为所有服务添加 `DAPR_GRPC_PORT` 环境变量

3. **性能测试:**
   - 对比 HTTP vs gRPC 的实际性能差异
   - 验证延迟和吞吐量提升

4. **监控和日志:**
   - 配置 Prometheus 指标收集
   - 在 Grafana 中可视化 gRPC 调用性能

### 参考资料

- [Dapr Service Invocation](https://docs.dapr.io/developing-applications/building-blocks/service-invocation/)
- [Dapr gRPC API](https://docs.dapr.io/reference/api/service_invocation_api/)
- [Dapr .NET SDK](https://docs.dapr.io/developing-applications/sdks/dotnet/)

---

**配置完成日期:** 2025年10月20日  
**状态:** ✅ 成功运行  
**验证:** UserService → ProductService (Dapr gRPC)
