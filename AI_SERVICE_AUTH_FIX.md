# AI 服务认证问题修复总结

## 📋 问题描述

Flutter 调用 AI 旅游计划生成接口时返回 401 错误：

```
❌ ERROR[401] => http://10.0.2.2:5000/api/v1/ai/travel-plan
Response: {success: false, message: 用户未认证，请先登录}
```

## 🔍 问题调查

### 1. 初步检查

- ✅ Flutter 正确发送 JWT token（在 HttpService 的 Interceptor 中添加）
- ✅ Flutter 正确发送 X-User-Id header
- ✅ Gateway 路由配置正确（已添加 ai-service 映射）
- ✅ Consul 服务注册配置正确（已修复端口和 ServiceAddress）

### 2. 关键发现

**ChatController.GetUserId() 使用了错误的认证方式**：

```csharp
// ❌ 错误方式：从 JWT Claims 读取（需要 JWT 认证中间件）
private Guid GetUserId()
{
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
}
```

但 AIService Program.cs **没有配置 JWT 认证中间件**：
- ❌ 缺少 `builder.Services.AddAuthentication().AddJwtBearer()`
- ❌ 缺少 `app.UseAuthentication()`
- ❌ 缺少 `app.UseAuthorization()`

### 3. 架构不一致

检查其他服务（UserService）的实现，发现应该使用 **UserContext 中间件**：

```csharp
// ✅ 正确方式：从 UserContext 读取（Gateway 传递的 X-User-Id）
var userContext = UserContextMiddleware.GetUserContext(HttpContext);
if (userContext?.IsAuthenticated != true)
{
    return Unauthorized(...);
}
var userId = userContext.UserId;
```

## ✅ 解决方案

### 修改 ChatController.cs

**1. 更新 using 语句**

```csharp
// 移除
using System.Security.Claims;

// 添加
using GoNomads.Shared.Middleware;
```

**2. 修改 GetUserId() 方法**

```csharp
/// <summary>
/// 从 UserContext 中获取用户 ID
/// </summary>
private Guid GetUserId()
{
    var userContext = UserContextMiddleware.GetUserContext(HttpContext);
    if (userContext?.IsAuthenticated != true)
    {
        return Guid.Empty;
    }

    return Guid.TryParse(userContext.UserId, out var userId) ? userId : Guid.Empty;
}
```

### 为什么这样修复？

#### 微服务架构的认证流程

```
Flutter App
    ↓ (发送 JWT Token in Authorization header)
Gateway
    ↓ (验证 JWT Token)
    ↓ (提取用户信息 → X-User-Id, X-User-Email, X-User-Role)
    ↓ (转发请求 + 添加 X-* headers)
AIService
    ↓ (UserContext Middleware 读取 X-* headers)
    ↓ (存储到 HttpContext.Items["UserContext"])
Controller
    ↓ (UserContextMiddleware.GetUserContext(HttpContext))
    ✓ (获取用户信息)
```

#### 关键点

1. **Gateway 负责 JWT 验证**：
   - 解析 JWT Token
   - 验证签名、过期时间
   - 提取用户信息（userId, email, role）

2. **Gateway 向下游服务传递用户信息**：
   - 通过 HTTP Headers（X-User-Id, X-User-Email, X-User-Role）
   - 下游服务不需要再验证 JWT

3. **下游服务使用 UserContext Middleware**：
   - 从请求头读取 X-* 信息
   - 存储到 HttpContext.Items 中
   - Controller 通过 `UserContextMiddleware.GetUserContext()` 访问

4. **为什么不在下游服务配置 JWT 认证？**
   - 避免重复验证（Gateway 已验证）
   - 下游服务只信任 Gateway 传递的信息
   - 简化下游服务的配置（不需要 JWT Secret）

## 🎯 修复效果

修复后的认证流程：

1. ✅ Flutter 发送 JWT Token 到 Gateway
2. ✅ Gateway 验证 JWT，提取用户信息
3. ✅ Gateway 添加 X-User-Id header 转发到 AIService
4. ✅ UserContext Middleware 读取 X-User-Id
5. ✅ ChatController.GetUserId() 从 UserContext 获取用户 ID
6. ✅ 认证成功，正常处理请求

## 📝 相关文件

### 修改的文件

- **ChatController.cs** (AIService/API/Controllers)
  - 修改 GetUserId() 方法使用 UserContext
  - 添加 GoNomads.Shared.Middleware using 语句

### 相关实现参考

- **UserContextMiddleware.cs** (Shared/Middleware)
  - 从请求头提取用户信息
  - 存储到 HttpContext.Items

- **UsersController.cs** (UserService)
  - 正确使用 UserContext 的示例

## 🧪 测试脚本

创建了 `test-ai-travel-plan.ps1` 脚本用于验证修复：

```powershell
# 1. 登录获取 token
# 2. 调用 AI 旅游计划生成接口
# 3. 验证返回结果
```

## 📊 总结

### 问题根源

- ChatController 使用了 JWT Claims 认证（需要 JWT 中间件）
- AIService 没有配置 JWT 中间件
- 与项目的微服务架构不一致（应该使用 UserContext）

### 解决方案

- 改用 UserContext Middleware 获取用户信息
- 与其他服务保持一致的认证方式
- 无需配置 JWT 认证中间件

### 优点

- ✅ 简化配置（不需要 JWT Secret）
- ✅ 避免重复验证（Gateway 统一验证）
- ✅ 架构一致（所有下游服务使用相同方式）
- ✅ 安全可靠（只信任 Gateway 传递的信息）

---

**修复日期**: 2025-01-29  
**影响范围**: AIService  
**状态**: ✅ 已修复，等待部署验证
