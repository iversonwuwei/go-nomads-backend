# 登录 API 快速启动指南

## 1. 前置条件

在使用登录 API 之前,需要确保:

### 1.1 Supabase 配置已完成

检查 `appsettings.json` 中的 Supabase 配置:

```json
{
  "Supabase": {
    "Url": "https://lcfbajrocmjlqndkrsao.supabase.co",
    "Key": "YOUR_SUPABASE_ANON_KEY",
    "Schema": "public"
  }
}
```

### 1.2 在 Supabase 中创建测试用户

有两种方式创建测试用户:

**方式 1: 通过 Supabase Dashboard**
1. 访问 [Supabase Dashboard](https://app.supabase.com/project/lcfbajrocmjlqndkrsao)
2. 进入 **Authentication** > **Users**
3. 点击 **Add User**
4. 输入邮箱和密码 (例如: `test@example.com` / `Test@123456`)
5. 点击 **Create User**

**方式 2: 通过 SQL (如果 Dashboard 不可用)**
```sql
-- 注意: 这只是创建数据库记录,不会创建 Auth 用户
-- 推荐使用 Supabase Dashboard 创建完整的认证用户
INSERT INTO auth.users (email, encrypted_password)
VALUES ('test@example.com', crypt('Test@123456', gen_salt('bf')));
```

## 2. 启动服务

### 2.1 使用 Docker (推荐)

```bash
# 进入部署目录
cd /Users/walden/Workspaces/WaldenProjects/go-noma/deployment

# 启动所有服务 (包括 UserService 和 Dapr)
./deploy-services-local.sh
```

### 2.2 本地开发模式

```bash
# 进入 UserService 目录
cd /Users/walden/Workspaces/WaldenProjects/go-noma/src/Services/UserService/UserService

# 使用 Dapr 启动服务
dapr run \
  --app-id user-service \
  --app-port 8080 \
  --dapr-http-port 3500 \
  --dapr-grpc-port 50001 \
  --components-path ../../../../deployment/dapr/components \
  --config ../../../../deployment/dapr/config/config.yaml \
  -- dotnet run
```

## 3. 测试登录 API

### 3.1 使用 VS Code REST Client

1. 打开 `UserService-Auth.http` 文件
2. 更新测试变量:
   ```http
   @email = test@example.com
   @password = Test@123456
   ```
3. 点击 "Send Request" 发送登录请求

### 3.2 使用 curl

```bash
# 登录
curl -X POST http://localhost:5000/api/users/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test@123456"
  }'

# 期望响应:
# {
#   "success": true,
#   "message": "登录成功",
#   "data": {
#     "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
#     "refreshToken": "v1:abc123...",
#     "tokenType": "Bearer",
#     "expiresIn": 3600,
#     "user": {
#       "id": "...",
#       "name": "Test User",
#       "email": "test@example.com",
#       ...
#     }
#   }
# }
```

### 3.3 通过 Dapr 调用

```bash
# 通过 Dapr Service Invocation
curl -X POST http://localhost:3502/v1.0/invoke/user-service/method/api/users/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test@123456"
  }'
```

### 3.4 使用 Scalar API 文档

1. 启动 UserService
2. 访问 Scalar UI: http://localhost:5000/scalar/v1
3. 找到 **POST /api/users/login** 端点
4. 点击 "Try it out"
5. 输入测试数据并发送请求

## 4. 常见问题

### 4.1 登录失败: 401 Unauthorized

**原因**: 邮箱或密码错误

**解决方案**:
1. 确认用户已在 Supabase Dashboard 中创建
2. 确认邮箱和密码正确
3. 检查 Supabase Auth 配置

### 4.2 连接错误: SSL/TLS 问题

**错误信息**: "Received an unexpected EOF or 0 bytes from the transport stream"

**解决方案**:
1. 检查网络连接
2. 确认 Supabase URL 正确
3. 检查防火墙设置

### 4.3 验证错误: 400 Bad Request

**原因**: 请求数据格式不正确

**解决方案**:
- 确保邮箱格式正确 (example@domain.com)
- 密码长度至少 6 位
- Content-Type 必须是 application/json

## 5. 集成到其他服务

### 5.1 在 ProductService 中调用登录验证

```csharp
// 通过 Dapr Service Invocation 调用 UserService
var loginRequest = new 
{
    email = "user@example.com",
    password = "password123"
};

var authResponse = await daprClient.InvokeMethodAsync<object, AuthResponse>(
    HttpMethod.Post,
    "user-service",
    "api/users/login",
    loginRequest
);

if (authResponse.Success)
{
    var token = authResponse.Data.AccessToken;
    // 使用 token 进行后续操作
}
```

### 5.2 JWT 令牌验证 (后续实现)

```csharp
// 在其他服务中验证 JWT 令牌
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            // 从 Supabase 获取公钥
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("YOUR_SUPABASE_JWT_SECRET")
            )
        };
    });
```

## 6. 下一步

- [ ] 实现用户注册端点
- [ ] 添加 JWT 令牌验证中间件
- [ ] 实现密码重置功能
- [ ] 添加多因素认证 (MFA)
- [ ] 实现 OAuth 登录 (Google, GitHub 等)

## 7. 相关文档

- [完整 API 文档](./LOGIN_API_README.md)
- [Supabase Auth 文档](https://supabase.com/docs/guides/auth)
- [Dapr Service Invocation](https://docs.dapr.io/developing-applications/building-blocks/service-invocation/)
