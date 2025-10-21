# 问题解决记录 - 2025-10-20

## ✅ 已解决: Gateway 路由 404 问题

### 问题症状
使用 `deploy-services-local.sh` 部署后，访问服务返回 404

### 根本原因
**Gateway 自己也注册到了 Consul，导致重复路由**

```
Consul 中的服务：
- Gateway (大写)
- gateway (小写)
- user-service
- product-service
- document-service

YARP 路由：
- Gateway-route ❌ 重复
- gateway-route ❌ 重复
- user-service-route ✅
- product-service-route ✅
- document-service-route ✅

错误：
System.AggregateException: The proxy config is invalid. 
(Duplicate route 'gateway-route') 
(Duplicate route 'gateway-exact-route') 
(Duplicate cluster 'gateway-cluster'.)
```

### 解决方案
修改 `ConsulProxyConfigProvider.cs`，过滤掉 Gateway 自己：

```csharp
// Skip consul and gateway itself (avoid self-routing loops)
if (serviceName == "consul" || 
    serviceName.Equals("gateway", StringComparison.OrdinalIgnoreCase))
    continue;
```

### 验证结果
```bash
# 路由加载成功
✅ Loaded 6 routes from Consul
✅ Route: user-service-route, Path: /api/users/{**remainder}
✅ Route: product-service-route, Path: /api/products/{**remainder}
✅ Route: document-service-route, Path: /api/document-service/{**remainder}

# 测试成功
✅ curl http://localhost:5000/health
   返回: {"status":"healthy",...}

✅ curl http://localhost:5000/api/users
   返回: 401 Unauthorized（需要认证，说明路由工作）

✅ curl -X POST http://localhost:5000/api/users/login
   返回: {"success":false,"message":"登录失败,请稍后重试"}
   （不再是 404）
```

---

## ❌ 待解决: 限流功能未触发

### 问题症状
连续10次快速请求 `/api/test/login`，全部返回 200，没有触发 429

### 已排查项目
1. ✅ 限流代码存在于 DLL 中
   - `strings Gateway.dll | grep RateLimitConfig` - 找到
   
2. ✅ Program.cs 配置正确
   ```csharp
   builder.Services.AddRateLimiter(RateLimitConfig.ConfigureRateLimiter);
   app.UseRateLimiter();
   ```

3. ✅ TestController 有限流特性
   ```csharp
   [EnableRateLimiting(RateLimitConfig.LoginPolicy)]
   public IActionResult TestLogin([FromBody] TestRequest request)
   ```

4. ✅ TestController 被访问
   - 返回了正确的响应内容
   
5. ✅ 中间件顺序调整
   - 将 `MapControllers()` 移到 `MapReverseProxy()` 之前

### 可能原因
1. **IP 地址获取问题**: 
   - Docker 容器内可能无法正确获取客户端 IP
   - 所有请求被认为来自不同 IP

2. **限流分区键问题**:
   - `GetClientIpAddress()` 可能返回不同的值

3. **YARP 代理干扰**:
   - 虽然 Controllers 在前，但可能还有其他问题

### 下一步调试
1. 添加日志查看实际的 IP 地址和分区键
2. 简化测试：不使用 IP 分区，用固定键
3. 检查是否是 GlobalLimiter 覆盖了策略限流
4. 测试不通过 Gateway，直接访问容器内的 TestController

### 临时解决方案
限流代码已经集成，只是运行时未触发。路由功能正常，可以继续使用。

---

## 📊 当前状态

### 工作正常 ✅
- Gateway 健康检查
- Consul 服务发现
- YARP 反向代理路由
- JWT 认证（401 响应）
- 服务间通信

### 待完善 ⚠️
- 限流功能（代码存在但未触发）

### 部署配置 ✅
- Gateway 使用 `Production` 环境
- 连接容器化 Consul（`go-nomads-consul:8500`）
- 路由过滤掉 Gateway 自身

---

## 🚀 快速部署

```bash
cd /Users/walden/Workspaces/WaldenProjects/go-noma/deployment
./deploy-services-local.sh
```

## 🔍 验证命令

```bash
# 1. 检查所有容器
docker ps --filter "name=go-nomads-"

# 2. 测试 Gateway
curl http://localhost:5000/health
curl http://localhost:5000/api/users

# 3. 查看 Gateway 日志
docker logs go-nomads-gateway | grep -E "Loaded|Route:"

# 4. 查看 Consul 服务
curl -s http://localhost:8500/v1/catalog/services | jq
```

---

**日期**: 2025-10-20
**状态**: Gateway 路由问题已解决 ✅，限流调试中 ⚠️
