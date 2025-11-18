# JWT 认证拦截测试指南

## 📋 概述

Gateway 现在会在转发请求前验证 JWT token:

- ✅ **有效 token**: 提取用户信息,添加到请求头,转发到后端服务
- ❌ **无效 token**: 返回 401,不转发请求
- ❌ **缺失 token**: 返回 401,不转发请求
- ⚪ **公开路径**: 跳过认证,直接转发

## 🔓 公开路径白名单

以下路径不需要认证 (配置在 `appsettings.json`):

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

## 🧪 测试场景

### 1. 访问公开路径 (不需要 token)

```bash
# 健康检查
curl http://localhost:5000/health

# 用户登录
curl -X POST http://localhost:5000/api/users/login \
  -H "Content-Type: application/json" \
  -d '{"email": "test@example.com", "password": "password123"}'

# 用户注册
curl -X POST http://localhost:5000/api/users/register \
  -H "Content-Type: application/json" \
  -d '{
    "name": "测试用户",
    "email": "test@example.com",
    "password": "password123",
    "phone": "13800138000"
  }'

# 获取角色列表
curl http://localhost:5000/api/roles
```

**预期结果**: ✅ 200 OK - 正常返回数据

### 2. 访问受保护路径 (缺失 token)

```bash
# 获取用户列表 (需要认证)
curl http://localhost:5000/api/users
```

**预期结果**: ❌ 401 Unauthorized

```json
{
  "success": false,
  "message": "Missing Authorization header",
  "error": "Unauthorized"
}
```

### 3. 访问受保护路径 (无效 token)

```bash
# 使用无效的 token
curl http://localhost:5000/api/users \
  -H "Authorization: Bearer invalid_token_here"
```

**预期结果**: ❌ 401 Unauthorized

```json
{
  "success": false,
  "message": "Invalid or expired token",
  "error": "Unauthorized",
  "details": "..."
}
```

### 4. 访问受保护路径 (有效 token)

```bash
# 1. 先登录获取 token
TOKEN=$(curl -X POST http://localhost:5000/api/users/login \
  -H "Content-Type: application/json" \
  -d '{"email": "test@example.com", "password": "password123"}' \
  | jq -r '.data.accessToken')

# 2. 使用 token 访问受保护资源
curl http://localhost:5000/api/users \
  -H "Authorization: Bearer $TOKEN"

# 获取单个用户
curl http://localhost:5000/api/users/123 \
  -H "Authorization: Bearer $TOKEN"

# 创建用户
curl -X POST http://localhost:5000/api/users \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "新用户",
    "email": "newuser@example.com",
    "password": "password123",
    "phone": "13900139000"
  }'
```

**预期结果**: ✅ 200 OK - 正常返回数据

Gateway 会自动添加以下请求头到后端服务:

```
X-User-Id: <用户ID>
X-User-Email: <用户邮箱>
X-User-Role: <用户角色>
```

## 🔍 验证用户上下文

后端服务可以通过 `UserContext` 获取用户信息:

```csharp
// 在 Controller 中
var userContext = HttpContext.RequestServices.GetRequiredService<UserContext>();
var userId = userContext.UserId;      // 从 X-User-Id 头获取
var email = userContext.Email;        // 从 X-User-Email 头获取
var role = userContext.Role;          // 从 X-User-Role 头获取
```

## 📝 添加新的公开路径

编辑 `src/Gateway/Gateway/appsettings.json`:

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
      "/api/products",          // 新增: 产品列表公开
      "/api/documents/public",  // 新增: 公开文档
      "/openapi",
      "/scalar"
    ]
  }
}
```

重启 Gateway 服务后生效。

## 🚀 部署测试

```bash
# 重启 Gateway
docker-compose restart gateway

# 或使用部署脚本
cd deployment
./deploy-services-local.sh
```

## 📊 日志查看

Gateway 会记录认证日志:

```bash
# 查看 Gateway 日志
docker logs gateway -f
```

日志示例:

```
🔓 Public paths configured: /health, /metrics, /api/users/login, ...
⚪ Public path: /api/users/login - Skipping authentication
❌ Missing Authorization header for path: /api/users
❌ JWT validation failed for path: /api/users - Error: Invalid token
✅ JWT validated - UserId: 123, Email: test@example.com, Role: user, Path: /api/users
```

## ⚠️ 重要提醒

1. **Token 格式**: 必须使用 `Bearer <token>` 格式
2. **公开路径匹配**: 支持精确匹配和前缀匹配
3. **大小写不敏感**: 路径匹配不区分大小写
4. **后端服务**: 不需要自己验证 JWT,只需从 UserContext 获取用户信息
5. **性能**: Gateway 统一认证,避免每个服务重复验证

## 🐛 故障排查

### 问题: 始终返回 401

**检查项**:

1. Token 是否正确复制 (没有多余空格)
2. Token 是否过期
3. `appsettings.json` 中 JWT 配置是否正确
4. 检查 Gateway 日志查看具体错误

### 问题: 公开路径也返回 401

**检查项**:

1. 确认路径在 `PublicPaths` 配置中
2. 检查路径拼写 (注意大小写)
3. 重启 Gateway 确保配置生效

### 问题: 后端服务获取不到用户信息

**检查项**:

1. 确认后端服务使用了 `UseUserContext()` 中间件
2. 检查 Gateway 是否正确添加了 `X-User-*` 请求头
3. 查看后端服务日志
