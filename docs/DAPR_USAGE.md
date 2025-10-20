# Dapr 使用指南 - UserService

本文档说明 UserService 中如何使用 Dapr 的各种功能。

## 📋 目录

1. [Pub/Sub（发布订阅）](#pubsub发布订阅)
2. [Service Invocation（服务调用）](#service-invocation服务调用)
3. [State Management（状态管理）](#state-management状态管理)

---

## 🔔 Pub/Sub（发布订阅）

### 功能说明
当用户创建或删除时，通过 Dapr 发布事件到消息队列，其他服务可以订阅这些事件。

### 使用场景

#### 1. 用户创建事件
```csharp
// POST /api/users
// 创建用户后自动发布事件

var userCreatedEvent = new UserCreatedEvent
{
    UserId = user.Id,
    Name = user.Name,
    Email = user.Email,
    CreatedAt = user.CreatedAt
};

await _daprClient.PublishEventAsync(
    pubsubName: "pubsub",
    topicName: "user-created",
    data: userCreatedEvent,
    cancellationToken: cancellationToken);
```

**订阅示例**（其他服务）：
```csharp
[Topic("pubsub", "user-created")]
[HttpPost("user-created")]
public async Task<ActionResult> HandleUserCreated(UserCreatedEvent evt)
{
    // 处理用户创建事件
    // 例如：发送欢迎邮件、创建用户档案等
}
```

#### 2. 用户删除事件
```csharp
// DELETE /api/users/{id}
// 删除用户后自动发布事件

var userDeletedEvent = new UserDeletedEvent
{
    UserId = id,
    DeletedAt = DateTime.UtcNow
};

await _daprClient.PublishEventAsync(
    pubsubName: "pubsub",
    topicName: "user-deleted",
    data: userDeletedEvent,
    cancellationToken: cancellationToken);
```

### 事件模型

```csharp
public class UserCreatedEvent
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class UserDeletedEvent
{
    public string UserId { get; set; } = string.Empty;
    public DateTime DeletedAt { get; set; }
}
```

### 配置要求

需要在 Dapr components 中配置 pubsub：

```yaml
# components/pubsub.yaml
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
  - name: redisPassword
    value: ""
```

---

## 🔗 Service Invocation（服务调用）

### 功能说明
通过 Dapr 调用其他微服务的 API，无需硬编码服务地址。

### 使用场景

#### 获取用户的产品列表
```csharp
// GET /api/users/{userId}/products
// 跨服务调用 ProductService

var products = await _daprClient.InvokeMethodAsync<object>(
    httpMethod: HttpMethod.Get,
    appId: "product-service",
    methodName: $"api/products/user/{userId}",
    cancellationToken: cancellationToken);
```

### 测试示例

```powershell
# 获取用户 ID 为 c626573b-484c-4b61-b0b6-1e817716846a 的产品
Invoke-WebRequest -Uri "http://localhost:5002/api/users/c626573b-484c-4b61-b0b6-1e817716846a/products"
```

### 优势

✅ **服务发现自动化** - 无需知道 ProductService 的具体地址  
✅ **负载均衡** - Dapr 自动处理多实例负载均衡  
✅ **重试和超时** - 可配置重试策略  
✅ **可观测性** - 自动追踪跨服务调用

---

## 💾 State Management（状态管理）

### 功能说明
使用 Dapr State Store 缓存用户数据，提高查询性能。

### 使用场景

#### 缓存用户信息
```csharp
// GET /api/users/{id}/cached
// 先从缓存获取，未命中则查数据库并缓存

// 1. 从缓存读取
var cachedUser = await _daprClient.GetStateAsync<User>(
    storeName: "statestore",
    key: $"user:{id}",
    cancellationToken: cancellationToken);

if (cachedUser != null)
{
    return cachedUser; // 缓存命中
}

// 2. 缓存未命中，从数据库获取
var user = await _userService.GetUserByIdAsync(id, cancellationToken);

// 3. 保存到缓存（5分钟过期）
await _daprClient.SaveStateAsync(
    storeName: "statestore",
    key: $"user:{id}",
    value: user,
    metadata: new Dictionary<string, string>
    {
        { "ttlInSeconds", "300" } // 5分钟 TTL
    },
    cancellationToken: cancellationToken);
```

### 测试示例

```powershell
# 第一次请求 - 从数据库获取并缓存
Invoke-WebRequest -Uri "http://localhost:5002/api/users/c626573b-484c-4b61-b0b6-1e817716846a/cached"
# 响应: "User retrieved from database and cached"

# 第二次请求（5分钟内）- 从缓存获取
Invoke-WebRequest -Uri "http://localhost:5002/api/users/c626573b-484c-4b61-b0b6-1e817716846a/cached"
# 响应: "User retrieved from cache"
```

### 缓存策略

- **TTL（Time To Live）**: 5分钟自动过期
- **Key 格式**: `user:{userId}`
- **更新策略**: 用户更新/删除时应清除缓存

### 配置要求

需要在 Dapr components 中配置 statestore：

```yaml
# components/statestore.yaml
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
  - name: redisPassword
    value: ""
  - name: actorStateStore
    value: "true"
```

---

## 🚀 完整 API 列表

| 端点 | 方法 | Dapr 功能 | 说明 |
|------|------|-----------|------|
| `/api/users` | POST | Pub/Sub | 创建用户 + 发布 `user-created` 事件 |
| `/api/users/{id}` | DELETE | Pub/Sub | 删除用户 + 发布 `user-deleted` 事件 |
| `/api/users/{userId}/products` | GET | Service Invocation | 调用 ProductService 获取用户产品 |
| `/api/users/{id}/cached` | GET | State Management | 从缓存获取用户（未命中则查数据库） |

---

## 📝 最佳实践

### 1. 事件发布失败处理
```csharp
try
{
    await _daprClient.PublishEventAsync(...);
    _logger.LogInformation("Event published successfully");
}
catch (Exception ex)
{
    _logger.LogWarning(ex, "Failed to publish event");
    // 不影响主流程，继续返回成功
}
```

### 2. 服务调用错误处理
```csharp
try
{
    var result = await _daprClient.InvokeMethodAsync(...);
}
catch (Dapr.DaprException ex) when (ex.InnerException is HttpRequestException)
{
    _logger.LogError(ex, "Service unavailable");
    return StatusCode(503, "Dependent service unavailable");
}
```

### 3. 缓存失效策略
```csharp
// 更新用户时清除缓存
await _daprClient.DeleteStateAsync("statestore", $"user:{id}");
```

---

## 🔧 本地开发配置

### 1. 启动 Redis（Dapr 依赖）
```powershell
docker run -d --name redis -p 6379:6379 redis:7-alpine
```

### 2. 启动 UserService with Dapr
```powershell
dapr run `
  --app-id user-service `
  --app-port 8080 `
  --dapr-http-port 3502 `
  --components-path ./components `
  -- dotnet run
```

### 3. 测试 Dapr 功能
```powershell
# 测试 Pub/Sub
Invoke-WebRequest -Uri "http://localhost:5002/api/users" -Method Post -Body '{"name":"test","email":"test@example.com"}' -ContentType "application/json"

# 测试 Service Invocation
Invoke-WebRequest -Uri "http://localhost:5002/api/users/{userId}/products"

# 测试 State Management
Invoke-WebRequest -Uri "http://localhost:5002/api/users/{userId}/cached"
```

---

## 📚 相关文档

- [Dapr 官方文档](https://docs.dapr.io/)
- [Dapr .NET SDK](https://docs.dapr.io/developing-applications/sdks/dotnet/)
- [Supabase 共享模块文档](./SUPABASE_SHARED_MODULE.md)

---

## 🎯 下一步

- [ ] 在 ProductService 中订阅 `user-deleted` 事件
- [ ] 在 DocumentService 中订阅 `user-created` 事件
- [ ] 实现缓存自动失效机制
- [ ] 添加 Dapr 可观测性（Zipkin/Jaeger）
