# Gateway JWT 认证拦截器文档

## 概述

Gateway 使用 **YARP (Yet Another Reverse Proxy)** 作为反向代理服务，并集成了 **JWT 认证拦截器**，用于验证所有通过网关的请求。

## 架构

```
┌─────────────┐
│   Client    │
│  (Browser/  │
│   Mobile)   │
└──────┬──────┘
       │
       │ HTTP Request + JWT Token
       │ Authorization: Bearer <token>
       ▼
┌────────────────────────────────────────────┐
│            Gateway (YARP)                  │
│                                            │
│  1. ┌────────────────────────────────┐    │
│     │ JWT Authentication Middleware  │    │
│     │  - 检查路由是否需要认证         │    │
│     │  - 验证 JWT 令牌               │    │
│     │  - 检查用户权限                │    │
│     └────────┬───────────────────────┘    │
│              │ (认证通过)                  │
│              ▼                             │
│  2. ┌────────────────────────────────┐    │
│     │ JwtAuthenticationTransform     │    │
│     │  - 提取用户信息                │    │
│     │  - 添加自定义请求头             │    │
│     │    * X-User-Id                │    │
│     │    * X-User-Email             │    │
│     │    * X-User-Role              │    │
│     └────────┬───────────────────────┘    │
│              │                             │
│              ▼                             │
│  3. ┌────────────────────────────────┐    │
│     │    YARP Reverse Proxy         │    │
│     │  - Consul 服务发现             │    │
│     │  - 负载均衡 (RoundRobin)       │    │
│     │  - 健康检查                    │    │
│     └────────┬───────────────────────┘    │
└──────────────┼────────────────────────────┘
               │
               │ 转发请求 + 用户信息头
               ▼
    ┌──────────────────────────┐
    │   Backend Services       │
    │  - UserService           │
    │  - ProductService        │
    │  - DocumentService       │
    └──────────────────────────┘
```

## 核心组件

### 1. JWT Authentication Middleware

**文件**: `Middleware/JwtAuthenticationMiddleware.cs`

**功能**:
- 拦截所有请求
- 检查路由是否需要认证（基于 RouteAuthorizationConfig）
- 验证用户身份
- 检查管理员权限
- 返回 401 Unauthorized 或 403 Forbidden

**工作流程**:
```csharp
请求 → 检查路由 → 需要认证？
                    ↓ 是
                 已认证？
                    ↓ 是
                需要管理员？
                    ↓ 否/是且权限足够
                 继续处理请求
```

### 2. JwtAuthenticationTransform

**文件**: `Middleware/JwtAuthenticationTransform.cs`

**功能**:
- YARP 转换器，在请求转发前处理
- 提取 JWT 中的用户信息
- 添加自定义请求头传递给下游服务:
  - `X-User-Id`: 用户 ID
  - `X-User-Email`: 用户邮箱
  - `X-User-Role`: 用户角色
- 保留原始 `Authorization` 头

**下游服务使用示例**:
```csharp
// 在下游服务的 Controller 中
[HttpGet]
public IActionResult GetProtectedData()
{
    var userId = Request.Headers["X-User-Id"].ToString();
    var email = Request.Headers["X-User-Email"].ToString();
    var role = Request.Headers["X-User-Role"].ToString();
    
    // 使用用户信息进行业务处理
    return Ok(new { userId, email, role });
}
```

### 3. RouteAuthorizationConfig

**文件**: `Services/RouteAuthorizationConfig.cs`

**功能**:
- 定义公开路由（不需要认证）
- 定义管理员路由（需要 admin 角色）
- 提供路由检查方法

**公开路由** (无需认证):
```csharp
/api/users/login       // 登录
/api/users/register    // 注册
/api/users/refresh     // 刷新令牌
/health                // 健康检查
/metrics               // Prometheus 指标
/scalar/v1             // API 文档
```

**受保护路由** (需要认证):
- 所有以 `/api/` 开头的路由（除了公开路由）

**管理员路由** (需要 admin 角色):
```csharp
/api/users/admin       // 用户管理
// 可以添加更多...
```

## JWT 配置

### appsettings.json

```json
{
  "Jwt": {
    "Issuer": "https://lcfbajrocmjlqndkrsao.supabase.co/auth/v1",
    "Audience": "authenticated",
    "Secret": "YOUR_SUPABASE_JWT_SECRET",
    "ValidateIssuerSigningKey": true,
    "ValidateIssuer": true,
    "ValidateAudience": true,
    "ValidateLifetime": true
  }
}
```

### JWT 密钥说明

**获取 Supabase JWT Secret**:
1. 访问 [Supabase Dashboard](https://app.supabase.com)
2. 选择项目
3. 进入 **Settings** > **API**
4. 复制 **JWT Secret**（在 "Config" 部分）

**重要**: 
- JWT Secret 用于验证令牌签名
- 必须与 Supabase 后端使用的密钥一致
- 生产环境应使用环境变量或密钥管理服务

## 使用流程

### 1. 客户端登录

```http
POST http://localhost:5003/api/users/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password123"
}
```

**响应**:
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "v1:abc123...",
    "expiresIn": 3600
  }
}
```

### 2. 使用令牌访问受保护资源

```http
GET http://localhost:5003/api/users
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Gateway 处理流程**:
1. ✅ 验证 JWT 令牌（签名、过期时间、issuer、audience）
2. ✅ 检查路由权限（/api/users 需要认证）
3. ✅ 提取用户信息并添加到请求头
4. ✅ 转发到 UserService（带上用户信息头）
5. ✅ 返回响应给客户端

### 3. 下游服务接收用户信息

UserService 收到的请求头:
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
X-User-Id: 123e4567-e89b-12d3-a456-426614174000
X-User-Email: user@example.com
X-User-Role: user
```

## 错误处理

### 401 Unauthorized - 未认证

**场景**:
- 没有提供 Authorization 头
- JWT 令牌无效或格式错误
- JWT 令牌已过期
- JWT 签名验证失败

**响应**:
```json
{
  "success": false,
  "message": "Unauthorized. Please provide a valid JWT token.",
  "error": "Missing or invalid Authorization header"
}
```

### 403 Forbidden - 权限不足

**场景**:
- 访问管理员路由但不是 admin 角色

**响应**:
```json
{
  "success": false,
  "message": "Forbidden. Admin access required.",
  "error": "Insufficient permissions"
}
```

## 配置路由权限

### 添加公开路由

编辑 `Services/RouteAuthorizationConfig.cs`:

```csharp
public static readonly HashSet<string> PublicRoutes = new(StringComparer.OrdinalIgnoreCase)
{
    "/api/users/login",
    "/api/users/register",
    "/api/products/public",  // 新增：公开产品列表
    "/health",
    "/metrics"
};
```

### 添加管理员路由

```csharp
public static readonly HashSet<string> AdminRoutes = new(StringComparer.OrdinalIgnoreCase)
{
    "/api/users/admin",
    "/api/products/admin",    // 新增：产品管理
    "/api/settings"           // 新增：系统设置
};
```

## JWT Payload 结构

Supabase JWT 令牌包含以下 claims:

```json
{
  "sub": "123e4567-e89b-12d3-a456-426614174000",  // 用户 ID
  "email": "user@example.com",                     // 邮箱
  "role": "authenticated",                         // 角色
  "iss": "https://lcfbajrocmjlqndkrsao.supabase.co/auth/v1",
  "aud": "authenticated",
  "iat": 1640000000,                               // 签发时间
  "exp": 1640003600                                // 过期时间
}
```

## 测试

### 使用提供的测试文件

1. 打开 `Gateway-Auth-Test.http`
2. 确保服务已启动:
   ```bash
   cd deployment
   ./deploy-services-local.sh
   ```
3. 运行测试场景

### 测试场景

| 场景 | 端点 | 是否需要令牌 | 期望结果 |
|------|------|------------|---------|
| 健康检查 | GET /health | ❌ | 200 OK |
| 登录 | POST /api/users/login | ❌ | 200 OK |
| 获取用户列表（无令牌） | GET /api/users | ❌ | 401 Unauthorized |
| 获取用户列表（有效令牌） | GET /api/users | ✅ | 200 OK |
| 访问管理员路由（普通用户） | GET /api/users/admin | ✅ | 403 Forbidden |
| 使用无效令牌 | GET /api/users | ❌ (无效) | 401 Unauthorized |

## 性能考虑

### JWT 验证性能

- JWT 验证是**无状态**的（不需要数据库查询）
- 验证时间: ~1-2ms
- 建议缓存公钥（如果使用 RSA）

### YARP 性能

- YARP 是高性能的反向代理
- 支持 HTTP/2 和 gRPC
- 内置连接池和负载均衡

### 建议

1. **使用 HTTP/2**: 减少连接开销
2. **启用响应缓存**: 对于不变的数据
3. **设置合理的超时**: 避免长时间等待
4. **监控指标**: 使用 Prometheus 监控延迟

## 安全最佳实践

### 1. HTTPS

生产环境**必须**使用 HTTPS:
```csharp
options.RequireHttpsMetadata = true; // 生产环境
```

### 2. 令牌过期时间

设置合理的过期时间:
- Access Token: 15-60 分钟
- Refresh Token: 7-30 天

### 3. 密钥管理

**不要**在代码中硬编码密钥:
```bash
# 使用环境变量
export JWT_SECRET="your-secret-key"

# 或使用 Azure Key Vault / AWS Secrets Manager
```

### 4. 速率限制

添加速率限制防止暴力破解:
```csharp
// 可以集成 AspNetCoreRateLimit 包
services.AddRateLimiter(...);
```

### 5. 日志

记录认证失败但**不要**记录令牌内容:
```csharp
_logger.LogWarning("Authentication failed for user {UserId}", userId);
// 不要: _logger.LogWarning("Token: {Token}", token);
```

## 扩展功能

### 1. 支持多种认证方式

```csharp
builder.Services.AddAuthentication()
    .AddJwtBearer("Supabase", options => { ... })
    .AddJwtBearer("Auth0", options => { ... });
```

### 2. 基于角色的访问控制 (RBAC)

```csharp
[Authorize(Roles = "admin,manager")]
public IActionResult AdminOnly() { ... }
```

### 3. 自定义 Claims

```csharp
// 在 JwtAuthenticationTransform 中添加更多 claims
transformContext.ProxyRequest.Headers.Add("X-User-Tenant", tenantId);
transformContext.ProxyRequest.Headers.Add("X-User-Plan", subscriptionPlan);
```

## 故障排查

### 问题 1: 401 Unauthorized - 令牌有效但仍失败

**检查**:
1. JWT Secret 是否正确
2. Issuer 和 Audience 是否匹配
3. 时钟偏差（ClockSkew）设置

### 问题 2: 令牌验证慢

**解决**:
1. 检查是否有网络请求（JWKS 获取）
2. 缓存公钥
3. 减少 ClockSkew

### 问题 3: 下游服务收不到用户信息头

**检查**:
1. JwtAuthenticationTransform 是否正确注册
2. 请求是否经过 Gateway
3. 查看 Gateway 日志

## 相关文件

```
Gateway/
├── Gateway.csproj                          # NuGet 包配置
├── Program.cs                              # JWT 认证配置
├── appsettings.json                        # JWT 配置
├── Gateway-Auth-Test.http                  # 测试文件
├── Middleware/
│   ├── JwtAuthenticationMiddleware.cs      # 认证中间件
│   └── JwtAuthenticationTransform.cs       # YARP 转换器
└── Services/
    ├── RouteAuthorizationConfig.cs         # 路由权限配置
    └── ConsulProxyConfigProvider.cs        # Consul 服务发现
```

## 部署

### Docker Compose

Gateway 环境变量:
```yaml
environment:
  - Jwt__Secret=${JWT_SECRET}
  - Jwt__Issuer=${JWT_ISSUER}
  - Jwt__Audience=${JWT_AUDIENCE}
```

### Kubernetes

使用 Secret 管理 JWT 密钥:
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: gateway-jwt-secret
type: Opaque
stringData:
  secret: "your-jwt-secret-here"
```

## 总结

✅ **已实现功能**:
- JWT 令牌验证
- 路由级别的访问控制
- 用户信息传递到下游服务
- 公开路由和受保护路由分离
- 管理员权限检查
- 详细的日志记录

🔄 **可选增强**:
- API 限流
- OAuth2/OpenID Connect 集成
- 多租户支持
- 审计日志
- 动态权限配置

📚 **相关文档**:
- [YARP 官方文档](https://microsoft.github.io/reverse-proxy/)
- [JWT 最佳实践](https://tools.ietf.org/html/rfc8725)
- [Supabase Auth 文档](https://supabase.com/docs/guides/auth)
