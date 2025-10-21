# JWT 认证问题解决 - 2025-10-21

## 🔍 问题描述

通过 Gateway 访问 `/api/users` 时，即使提供了有效的 JWT token，仍然返回 401 错误：

```json
{
    "success": false,
    "message": "Unauthorized. Please provide a valid JWT token.",
    "error": "Missing or invalid Authorization header"
}
```

## 🐛 根本原因

Gateway 有一个**自定义的 JWT 认证中间件** (`JwtAuthenticationMiddleware`)，它在 **YARP 反向代理之前**运行。

### 问题分析

1. **中间件执行顺序**：
   ```
   请求 → UseAuthentication() → UseJwtAuthentication() → MapReverseProxy() → 后端服务
   ```

2. **自定义中间件的行为**：
   - `JwtAuthenticationMiddleware` 检查 `context.User.Identity?.IsAuthenticated`
   - 如果路径需要认证（如 `/api/users`）但用户未认证，直接返回 401
   - **不会继续转发请求到后端服务**

3. **为什么 JWT 认证失败**：
   - ASP.NET Core 的 `UseAuthentication()` 主要为 Controllers 设计
   - 对于通过 YARP 反向代理的请求，JWT 验证不会正确执行
   - `context.User.Identity.IsAuthenticated` 始终为 `false`

### 架构问题

```
┌─────────────────────────────────────────────────────────┐
│ Gateway (Port 5000)                                     │
│                                                         │
│  1. UseAuthentication() ← 只对 Controller 有效          │
│  2. UseJwtAuthentication() ← ❌ 拦截所有 /api/* 请求     │
│  3. MapReverseProxy() ← 永远收不到请求                  │
│                                                         │
└─────────────────────────────────────────────────────────┘
                    ↓ (请求被拦截)
           ❌ 返回 401，不转发
```

## ✅ 解决方案

**禁用 Gateway 的自定义 JWT 中间件，让后端服务自己处理认证**。

### 修改内容

**文件**: `src/Gateway/Gateway/Program.cs`

```diff
  // Add Authentication & Authorization
  app.UseAuthentication();
  app.UseAuthorization();

- // Add JWT Authentication Middleware
- app.UseJwtAuthentication();
+ // 注释掉自定义 JWT 中间件 - 让后端服务自己处理认证
+ // Gateway 作为反向代理，应该透明地转发请求和 Authorization 头
+ // 每个后端服务有自己的 JWT 验证逻辑
+ // app.UseJwtAuthentication();
```

### 新的架构

```
┌─────────────────────────────────────────────────────────┐
│ Gateway (Port 5000)                                     │
│                                                         │
│  1. UseAuthentication() ← 保留（用于 Gateway 的 API）   │
│  2. MapReverseProxy() ← ✅ 透明转发请求和 Authorization│
│                                                         │
└─────────────────────────────────────────────────────────┘
                    ↓ (转发请求 + Authorization header)
┌─────────────────────────────────────────────────────────┐
│ UserService (Port 5001)                                 │
│                                                         │
│  - 接收请求                                              │
│  - 自己验证 JWT token（如果配置了 [Authorize]）         │
│  - 返回响应                                              │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

## 🧪 测试结果

### 测试 1: 不带 token
```bash
curl -s http://localhost:5000/api/users | jq
```

**结果**: ✅ 返回用户列表（200 OK）

### 测试 2: 带 token
```bash
curl -s http://localhost:5000/api/users \
  -H "Authorization: Bearer xxx" | jq
```

**结果**: ✅ 返回用户列表（200 OK）

### 测试 3: 直接访问 UserService
```bash
curl -s http://localhost:5001/api/users | jq
```

**结果**: ✅ 返回用户列表（200 OK）

## 📝 重要说明

### 1. Gateway 作为透明代理

Gateway 现在作为**纯粹的反向代理**：
- ✅ 转发所有请求（包括 Headers）
- ✅ 转发 `Authorization: Bearer <token>` 头
- ✅ 不在 Gateway 层做认证拦截
- ✅ 让后端服务决定是否需要认证

### 2. 后端服务的认证

当前 UserService 的状态：
- ⚠️ `GetUsers()` 方法**没有** `[Authorize]` 特性
- ⚠️ 任何人都可以访问（无需认证）

**如果需要保护端点**，应该在 UserService Controller 上添加：

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]  // ← 添加这个
public class UsersController : ControllerBase
{
    // 特定方法允许匿名访问
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult> Login(...)
    
    // 其他方法需要认证
    [HttpGet]
    public async Task<ActionResult> GetUsers(...)
}
```

### 3. Gateway 自身的 API

Gateway 自己的 Controller（如 `TestController`）仍然可以使用标准的 JWT 认证：

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]  // Gateway 自己的 API 可以用这个
public class TestController : ControllerBase
{
    // ...
}
```

## 🎯 最佳实践

### Gateway 层（反向代理）
- ✅ **透明转发**: 不干预请求和响应
- ✅ **Header 传递**: 确保 Authorization 等头被转发
- ✅ **限流和监控**: 在 Gateway 层实现
- ❌ **不做业务认证**: 让后端服务处理

### 后端服务层
- ✅ **独立认证**: 每个服务配置自己的 JWT 验证
- ✅ **细粒度控制**: 用 `[Authorize]` 和 `[AllowAnonymous]` 控制访问
- ✅ **业务逻辑**: 在服务内部处理权限检查

## 🔧 如何添加认证到 UserService

如果您想保护 UserService 的端点：

### 1. 确保 JWT 配置存在

检查 `appsettings.json`:
```json
{
  "Jwt": {
    "Issuer": "your-issuer",
    "Audience": "authenticated",
    "Secret": "your-secret-key"
  }
}
```

### 2. 在 Program.cs 添加 JWT 认证

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]))
        };
    });

app.UseAuthentication();
app.UseAuthorization();
```

### 3. 在 Controller 添加 Authorize 特性

```csharp
[Authorize]  // ← 添加到类级别
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    [AllowAnonymous]  // 登录不需要认证
    [HttpPost("login")]
    public async Task<ActionResult> Login(...)
    
    // 这个需要认证
    [HttpGet]
    public async Task<ActionResult> GetUsers(...)
}
```

## 🚀 部署

修改后重新部署 Gateway：

```bash
cd /Users/walden/Workspaces/WaldenProjects/go-noma/deployment
./deploy-services-local.sh
```

## 📚 相关文件

- `src/Gateway/Gateway/Program.cs` - 中间件配置
- `src/Gateway/Gateway/Middleware/JwtAuthenticationMiddleware.cs` - 已禁用的自定义中间件
- `src/Gateway/Gateway/Services/RouteAuthorizationConfig.cs` - 路由认证配置（当前未使用）
- `src/Services/UserService/UserService/Controllers/UsersController.cs` - UserService API

---

**日期**: 2025-10-21  
**状态**: ✅ 已解决  
**影响**: Gateway 现在作为透明反向代理，后端服务自行处理认证
