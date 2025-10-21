# UserService 登录 API 文档

## 概述

UserService 现已集成 Supabase Auth 服务,提供完整的用户认证功能。

## API 端点

### 1. 用户登录

**端点**: `POST /api/users/login`

**请求体**:
```json
{
  "email": "user@example.com",
  "password": "password123"
}
```

**成功响应** (200 OK):
```json
{
  "success": true,
  "message": "登录成功",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "v1:abc123...",
    "tokenType": "Bearer",
    "expiresIn": 3600,
    "user": {
      "id": "123e4567-e89b-12d3-a456-426614174000",
      "name": "Test User",
      "email": "user@example.com",
      "phone": "+1234567890",
      "createdAt": "2024-01-01T00:00:00Z",
      "updatedAt": "2024-01-01T00:00:00Z"
    }
  }
}
```

**错误响应** (401 Unauthorized):
```json
{
  "success": false,
  "message": "登录失败,请检查邮箱和密码"
}
```

**验证错误** (400 Bad Request):
```json
{
  "success": false,
  "message": "验证失败",
  "errors": [
    "邮箱不能为空",
    "密码长度至少为6位"
  ]
}
```

### 2. 刷新访问令牌

**端点**: `POST /api/users/refresh`

**请求体**:
```json
{
  "refreshToken": "v1:abc123..."
}
```

**成功响应** (200 OK):
```json
{
  "success": true,
  "message": "令牌刷新成功",
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "v1:def456...",
    "tokenType": "Bearer",
    "expiresIn": 3600,
    "user": {
      "id": "123e4567-e89b-12d3-a456-426614174000",
      "name": "Test User",
      "email": "user@example.com",
      "phone": "+1234567890",
      "createdAt": "2024-01-01T00:00:00Z",
      "updatedAt": "2024-01-01T00:00:00Z"
    }
  }
}
```

### 3. 用户登出

**端点**: `POST /api/users/logout`

**成功响应** (200 OK):
```json
{
  "success": true,
  "message": "登出成功"
}
```

## 使用 JWT 令牌

### 在请求头中使用令牌

登录成功后,您会收到一个 `accessToken`。在后续请求中,将此令牌添加到请求头:

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### 令牌过期处理

1. `accessToken` 的有效期为 `expiresIn` 秒(通常为 3600 秒 = 1 小时)
2. 当访问令牌过期时,使用 `refreshToken` 获取新的令牌
3. 如果刷新令牌也过期,需要重新登录

## 通过 Dapr 调用

其他服务可以通过 Dapr Service Invocation 调用登录 API:

```bash
# 通过 Dapr sidecar 调用
curl -X POST http://localhost:3502/v1.0/invoke/user-service/method/api/users/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "password123"
  }'
```

## 架构

```
┌─────────────┐
│   Client    │
└──────┬──────┘
       │ HTTP Request
       ▼
┌─────────────────────┐
│  UsersController    │
│  /api/users/login   │
└──────┬──────────────┘
       │
       ▼
┌─────────────────────┐
│    AuthService      │
│ (业务逻辑层)         │
└──────┬──────────────┘
       │
       ├─────────────────────────┐
       │                         │
       ▼                         ▼
┌──────────────┐      ┌──────────────────────┐
│   Supabase   │      │ SupabaseUserRepository│
│     Auth     │      │   (数据库访问)        │
│  (认证服务)   │      └──────────────────────┘
└──────────────┘
       │
       ▼
┌──────────────────────┐
│  Supabase Backend    │
│  (Auth + Database)   │
└──────────────────────┘
```

## 注意事项

### SSL 连接问题

如果遇到 Supabase SSL 连接错误,请确保:

1. **连接字符串配置正确**:
   ```json
   "ConnectionStrings": {
     "SupabaseDb": "Host=db.lcfbajrocmjlqndkrsao.supabase.co;Port=6543;Database=postgres;Username=postgres.lcfbajrocmjlqndkrsao;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true;Pooling=true"
   }
   ```

2. **Supabase 配置正确**:
   ```json
   "Supabase": {
     "Url": "https://lcfbajrocmjlqndkrsao.supabase.co",
     "Key": "YOUR_ANON_KEY",
     "Schema": "public"
   }
   ```

3. **网络连接正常**: 确保可以访问 Supabase 服务

### 用户创建

在使用登录功能之前,需要先在 Supabase 中创建用户:

1. **通过 Supabase Dashboard 创建**:
   - 访问 Supabase Dashboard
   - 进入 Authentication > Users
   - 添加新用户

2. **通过 API 注册** (需要实现注册端点):
   ```json
   POST /api/users/register
   {
     "email": "user@example.com",
     "password": "password123",
     "name": "Test User"
   }
   ```

## 测试

使用提供的 `UserService-Auth.http` 文件进行测试:

1. 打开 `UserService-Auth.http`
2. 更新测试变量 (email, password)
3. 点击 "Send Request" 发送请求
4. 查看响应结果

## 依赖包

- `Dapr.AspNetCore` (1.16.0) - Dapr 集成
- `supabase-csharp` (0.16.2) - Supabase 客户端
- `Npgsql.EntityFrameworkCore.PostgreSQL` (9.0.2) - PostgreSQL 数据库访问

## 相关文件

- `/Controllers/UsersController.cs` - 登录 API 端点
- `/Services/IAuthService.cs` - 认证服务接口
- `/Services/AuthService.cs` - 认证服务实现
- `/DTOs/LoginDto.cs` - 登录请求 DTO
- `/DTOs/AuthResponseDto.cs` - 认证响应 DTO
- `/DTOs/RefreshTokenDto.cs` - 刷新令牌请求 DTO
- `Program.cs` - 服务注册配置
