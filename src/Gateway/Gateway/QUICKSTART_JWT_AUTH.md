# Gateway JWT 认证快速启动指南

## 概述

本指南将帮助你快速启动和测试 Gateway 的 JWT 认证功能。

## 前置条件

### 1. 确保所有服务已启动

```bash
cd /Users/walden/Workspaces/WaldenProjects/go-noma/deployment
./deploy-services-local.sh
```

### 2. 验证服务状态

```bash
# 检查 Docker 容器状态
docker ps | grep go-nomads

# 应该看到:
# - go-nomads-gateway (端口 5003)
# - go-nomads-user-service (端口 5000)
# - go-nomads-product-service (端口 5001)
# - go-nomads-consul
# - go-nomads-redis
# - go-nomads-zipkin
```

### 3. 在 Supabase 创建测试用户

访问 [Supabase Dashboard](https://app.supabase.com/project/lcfbajrocmjlqndkrsao):

1. 进入 **Authentication** > **Users**
2. 点击 **Add User**
3. 输入:
   - Email: `test@example.com`
   - Password: `Test@123456`
4. 点击 **Create User**

## 测试步骤

### 步骤 1: 通过 UserService 登录获取 JWT 令牌

```bash
# 直接访问 UserService（绕过 Gateway）
curl -X POST http://localhost:5000/api/users/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test@123456"
  }'
```

**期望响应**:
```json
{
  "success": true,
  "message": "登录成功",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "v1:abc123...",
    "tokenType": "Bearer",
    "expiresIn": 3600
  }
}
```

**复制 accessToken** 以便后续使用。

### 步骤 2: 通过 Gateway 访问公开路由（不需要令牌）

```bash
# 测试健康检查
curl http://localhost:5003/health

# 通过 Gateway 登录
curl -X POST http://localhost:5003/api/users/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test@123456"
  }'
```

**期望结果**: ✅ 成功返回（200 OK）

### 步骤 3: 访问受保护路由 - 无令牌（应该失败）

```bash
# 尝试获取用户列表（无令牌）
curl http://localhost:5003/api/users \
  -H "Content-Type: application/json"
```

**期望响应**: ❌ 401 Unauthorized
```json
{
  "success": false,
  "message": "Unauthorized. Please provide a valid JWT token.",
  "error": "Missing or invalid Authorization header"
}
```

### 步骤 4: 访问受保护路由 - 有效令牌（应该成功）

```bash
# 替换 YOUR_ACCESS_TOKEN 为步骤 1 中获取的令牌
export TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."

# 获取用户列表（带令牌）
curl http://localhost:5003/api/users \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json"
```

**期望结果**: ✅ 成功返回用户列表（200 OK）

### 步骤 5: 验证用户信息头传递

检查 Gateway 是否将用户信息传递给下游服务。

查看 UserService 日志:
```bash
docker logs go-nomads-user-service --tail 50
```

你应该看到类似的日志:
```
[Debug] JWT Authentication - User authenticated: UserId=123..., Email=test@example.com, Role=authenticated
```

### 步骤 6: 测试无效令牌

```bash
# 使用伪造的令牌
curl http://localhost:5003/api/users \
  -H "Authorization: Bearer invalid.token.here" \
  -H "Content-Type: application/json"
```

**期望响应**: ❌ 401 Unauthorized

## 使用 VS Code REST Client 测试

### 1. 打开测试文件

在 VS Code 中打开:
```
/Users/walden/Workspaces/WaldenProjects/go-noma/src/Gateway/Gateway/Gateway-Auth-Test.http
```

### 2. 运行测试

1. 点击 "Send Request" 运行第一个请求（登录）
2. 令牌会自动保存到变量 `@accessToken`
3. 后续请求会自动使用这个令牌
4. 按顺序运行所有测试场景

### 3. 测试场景覆盖

- ✅ 登录获取令牌
- ✅ 公开路由访问
- ✅ 受保护路由 - 无令牌（401）
- ✅ 受保护路由 - 有效令牌（200）
- ✅ 无效令牌（401）
- ✅ 刷新令牌
- ✅ 产品服务路由

## 验证认证流程

### 完整流程图

```
1. Client → Gateway: POST /api/users/login
   └─> Gateway → UserService: 转发登录请求（公开路由，无需认证）
       └─> UserService → Supabase Auth: 验证用户凭据
           └─> 返回 JWT Token

2. Client → Gateway: GET /api/users (with JWT Token)
   └─> Gateway: 验证 JWT 令牌
       ├─> ✅ 令牌有效
       │   └─> 提取用户信息 (userId, email, role)
       │       └─> Gateway → UserService: 转发请求 + 用户信息头
       │           └─> UserService: 处理请求（可以从请求头读取用户信息）
       │               └─> 返回响应
       │
       └─> ❌ 令牌无效/缺失
           └─> 返回 401 Unauthorized
```

## 常见问题

### Q1: 登录成功但访问受保护路由返回 401

**检查**:
1. 确认令牌是否正确复制（没有多余空格）
2. 检查令牌是否过期（默认 1 小时）
3. 验证 Gateway 的 JWT 配置是否正确

**解决**:
```bash
# 查看 Gateway 配置
docker exec go-nomads-gateway cat /app/appsettings.json

# 检查 JWT Secret 是否匹配
# 应该与 Supabase Dashboard 中的 JWT Secret 一致
```

### Q2: Gateway 日志显示 "JWT Authentication failed"

**检查**:
```bash
# 查看详细日志
docker logs go-nomads-gateway --tail 100

# 常见错误:
# - "IDX10214: Audience validation failed" → Audience 配置错误
# - "IDX10205: Issuer validation failed" → Issuer 配置错误
# - "IDX10223: Lifetime validation failed" → 令牌已过期
```

**解决**:
检查 `appsettings.json` 中的 JWT 配置:
```json
{
  "Jwt": {
    "Issuer": "https://lcfbajrocmjlqndkrsao.supabase.co/auth/v1",
    "Audience": "authenticated",
    "Secret": "YOUR_SUPABASE_JWT_SECRET"
  }
}
```

### Q3: 下游服务收不到用户信息头

**检查**:
```bash
# 在下游服务中添加日志
// UserService Controller
[HttpGet]
public IActionResult GetUsers()
{
    var userId = Request.Headers["X-User-Id"].ToString();
    var email = Request.Headers["X-User-Email"].ToString();
    _logger.LogInformation("Received headers: UserId={UserId}, Email={Email}", userId, email);
    // ...
}
```

**验证**:
```bash
# 查看 UserService 日志
docker logs go-nomads-user-service --tail 50 -f
```

### Q4: 公开路由被拦截

**检查** `RouteAuthorizationConfig.cs`:
```csharp
public static readonly HashSet<string> PublicRoutes = new(StringComparer.OrdinalIgnoreCase)
{
    "/api/users/login",    // ✅ 登录路由应该在这里
    "/api/users/register", // ✅ 注册路由应该在这里
    "/health",
    "/metrics"
};
```

## 性能测试

### 基准测试

```bash
# 使用 wrk 进行压力测试
# 安装: brew install wrk (macOS)

# 测试公开路由
wrk -t4 -c100 -d30s http://localhost:5003/health

# 测试认证路由（需要先设置令牌）
wrk -t4 -c100 -d30s \
  -H "Authorization: Bearer $TOKEN" \
  http://localhost:5003/api/users
```

**期望结果**:
- 公开路由: ~20,000-30,000 req/s
- 认证路由: ~15,000-25,000 req/s（JWT 验证有轻微开销）

### 查看 Prometheus 指标

```bash
# 访问 Gateway 指标
curl http://localhost:5003/metrics | grep http_request

# 查看认证相关指标
curl http://localhost:5003/metrics | grep jwt
```

## 生产环境配置

### 1. 启用 HTTPS

```csharp
// Program.cs
options.RequireHttpsMetadata = true; // 生产环境必须启用
```

### 2. 使用环境变量

```bash
# 不要在代码中硬编码密钥
export JWT_SECRET="your-production-secret"
export JWT_ISSUER="your-issuer"
export JWT_AUDIENCE="your-audience"
```

### 3. 设置合理的超时

```json
{
  "Jwt": {
    "ValidateLifetime": true,
    "ClockSkew": "00:05:00"  // 5 分钟时钟偏差
  }
}
```

### 4. 添加速率限制

```csharp
// 防止暴力破解登录
services.AddRateLimiter(options => {
    options.AddFixedWindowLimiter("login", opt => {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 5; // 每分钟最多 5 次登录尝试
    });
});
```

## 下一步

- [ ] 实现用户注册端点
- [ ] 添加刷新令牌逻辑
- [ ] 实现管理员权限检查
- [ ] 添加 API 限流
- [ ] 集成审计日志
- [ ] 配置 HTTPS
- [ ] 设置 CORS 策略

## 相关文档

- [完整 JWT 认证文档](./JWT_AUTH_README.md)
- [YARP 官方文档](https://microsoft.github.io/reverse-proxy/)
- [Supabase Auth 文档](https://supabase.com/docs/guides/auth)
- [JWT 最佳实践](https://tools.ietf.org/html/rfc8725)

## 技术支持

遇到问题？查看:
1. Gateway 日志: `docker logs go-nomads-gateway`
2. UserService 日志: `docker logs go-nomads-user-service`
3. Consul UI: http://localhost:8500
4. Zipkin UI: http://localhost:9411
5. Prometheus: http://localhost:9090
