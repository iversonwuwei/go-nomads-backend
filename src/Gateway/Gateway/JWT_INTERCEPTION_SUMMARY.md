# ✅ JWT 认证拦截 - 完成总结

## 🎯 问题描述

之前 Gateway 无论 token 是否有效都会转发请求到后端服务,导致安全问题。

## 🔧 解决方案

在 Gateway 层面实现 JWT 认证拦截:
- ✅ Token 有效 → 提取用户信息 → 添加到请求头 → 转发请求
- ❌ Token 无效/缺失 → 返回 401 → 不转发请求
- ⚪ 公开路径 → 跳过认证 → 直接转发

## 📁 修改的文件

### 1. 新增文件

**`src/Gateway/Gateway/Middleware/JwtAuthenticationInterceptor.cs`**
- JWT 认证拦截中间件
- 在 YARP 转发前验证 token
- 从配置读取公开路径白名单
- Token 有效时提取用户信息并添加到请求头 (X-User-Id, X-User-Email, X-User-Role)

### 2. 修改文件

**`src/Gateway/Gateway/Program.cs`**
```csharp
// 之前: 注释掉了认证中间件
// app.UseJwtAuthentication();

// 现在: 使用新的拦截中间件
app.UseJwtAuthenticationInterceptor();
```

**`src/Gateway/Gateway/appsettings.json`**
```json
{
  "Authentication": {
    "PublicPaths": [
      "/health",
      "/metrics",
      "/api/users/login",
      "/api/users/register",
      "/api/users/refresh",
      "/api/roles",
      "/openapi",
      "/scalar"
    ]
  }
}
```

### 3. 撤销的修改

撤销了对 UserService 的修改,保持后端服务不需要自己验证 JWT:
- ❌ 移除了 `AddAuthentication` 配置
- ❌ 移除了 `UseAuthentication` 和 `UseAuthorization`
- ❌ 移除了 `[Authorize]` 特性

后端服务只需通过 `UserContext` 获取用户信息即可。

## 🔒 认证流程

```
客户端请求
    ↓
Gateway 接收请求
    ↓
检查路径是否在白名单?
    ├─ 是 → 跳过认证 → 转发请求 → 后端服务
    └─ 否 → 验证 JWT Token
            ├─ 有效 → 提取用户信息
            │         ↓
            │    添加请求头:
            │    - X-User-Id
            │    - X-User-Email
            │    - X-User-Role
            │         ↓
            │    转发请求 → 后端服务
            │
            └─ 无效/缺失 → 返回 401 (不转发)
```

## 📝 公开路径配置

不需要认证的路径 (白名单):
- `/health` - 健康检查
- `/metrics` - Prometheus 指标
- `/api/users/login` - 用户登录
- `/api/users/register` - 用户注册
- `/api/users/refresh` - 刷新 token
- `/api/roles` - 获取角色列表
- `/openapi` - OpenAPI 文档
- `/scalar` - Scalar UI

**添加新路径**: 编辑 `appsettings.json` 的 `Authentication:PublicPaths` 配置

## 🧪 测试方法

### 1. 测试公开路径 (不需要 token)
```bash
curl http://localhost:5000/api/users/login \
  -H "Content-Type: application/json" \
  -d '{"email": "test@example.com", "password": "password123"}'
```

### 2. 测试受保护路径 (缺失 token)
```bash
curl http://localhost:5000/api/users
# 返回: 401 Unauthorized
```

### 3. 测试受保护路径 (有效 token)
```bash
# 获取 token
TOKEN=$(curl -X POST http://localhost:5000/api/users/login \
  -H "Content-Type: application/json" \
  -d '{"email": "test@example.com", "password": "password123"}' \
  | jq -r '.data.accessToken')

# 使用 token 访问
curl http://localhost:5000/api/users \
  -H "Authorization: Bearer $TOKEN"
# 返回: 200 OK
```

## 🚀 部署

```bash
# 重启 Gateway
docker-compose restart gateway

# 或使用部署脚本
cd deployment
./deploy-services-local.sh
```

## 📊 日志示例

Gateway 会记录详细的认证日志:

```
🔓 Public paths configured: /health, /metrics, /api/users/login, ...
⚪ Public path: /api/users/login - Skipping authentication
❌ Missing Authorization header for path: /api/users
❌ JWT validation failed for path: /api/users - Error: Invalid token
✅ JWT validated - UserId: 123, Email: test@example.com, Role: user, Path: /api/users
```

## 🎁 优势

1. **统一认证**: Gateway 统一处理认证,后端服务专注业务逻辑
2. **性能提升**: 避免每个服务重复验证 JWT
3. **安全性**: 无效请求在 Gateway 层就被拦截,不会到达后端
4. **灵活配置**: 通过配置文件管理白名单,易于维护
5. **用户上下文**: 自动提取用户信息并传递给后端服务

## 📖 更多文档

详细测试指南: `JWT_AUTHENTICATION_GUIDE.md`
