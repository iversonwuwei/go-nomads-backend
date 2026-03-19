# UserService API 文档（DDD 重构后）

## 📋 概述

UserService 已完成 DDD（领域驱动设计）重构，API 现在分为两个控制器：

1. **AuthController** (`/api/auth`) - 认证相关 API
2. **UsersController** (`/api/users`) - 用户管理 API

所有 API 均遵循 RESTful 规范，使用统一的 `ApiResponse<T>` 响应格式。

---

## 🔐 AuthController - 认证 API

### 1. 用户注册

**POST** `/api/auth/register`

注册新用户并自动返回 JWT Token。

**Request Body:**

```json
{
  "name": "张三",
  "email": "zhangsan@example.com",
  "password": "password123",
  "phone": "13800138000"
}
```

**Response (200 OK):**

```json
{
  "success": true,
  "message": "注册成功",
  "data": {
    "accessToken": "eyJhbGc...",
    "refreshToken": "eyJhbGc...",
    "tokenType": "Bearer",
    "expiresIn": 3600,
    "user": {
      "id": "uuid-here",
      "name": "张三",
      "email": "zhangsan@example.com",
      "phone": "13800138000",
      "createdAt": "2024-01-01T00:00:00Z",
      "updatedAt": "2024-01-01T00:00:00Z"
    }
  }
}
```

**Error (400 Bad Request):**

```json
{
  "success": false,
  "message": "邮箱 'zhangsan@example.com' 已被注册"
}
```

---

### 2. 用户登录

**POST** `/api/auth/login`

用户登录并获取 JWT Token。

**Request Body:**

```json
{
  "email": "zhangsan@example.com",
  "password": "password123"
}
```

**Response (200 OK):**

```json
{
  "success": true,
  "message": "登录成功",
  "data": {
    "accessToken": "eyJhbGc...",
    "refreshToken": "eyJhbGc...",
    "tokenType": "Bearer",
    "expiresIn": 3600,
    "user": { /* 同注册响应 */ }
  }
}
```

**Error (401 Unauthorized):**

```json
{
  "success": false,
  "message": "用户名或密码错误"
}
```

---

### 3. 刷新令牌

**POST** `/api/auth/refresh`

使用 refresh token 获取新的 access token。

**Request Body:**

```json
{
  "refreshToken": "eyJhbGc..."
}
```

**Response (200 OK):**

```json
{
  "success": true,
  "message": "令牌刷新成功",
  "data": {
    "accessToken": "eyJhbGc...",
    "refreshToken": "eyJhbGc...",  // 新的 refresh token (token rotation)
    "tokenType": "Bearer",
    "expiresIn": 3600,
    "user": { /* 同登录响应 */ }
  }
}
```

**Error (401 Unauthorized):**

```json
{
  "success": false,
  "message": "刷新令牌无效或已过期,请重新登录"
}
```

---

### 4. 用户登出

**POST** `/api/auth/logout`

用户登出（需要认证）。

**Headers:**

```
Authorization: Bearer <access_token>
```

**Response (200 OK):**

```json
{
  "success": true,
  "message": "登出成功"
}
```

**注意**: JWT 是无状态的，客户端需要删除本地存储的 token。

---

### 5. 修改密码

**POST** `/api/auth/change-password`

修改当前用户密码（需要认证，使用 UserContext 获取用户 ID）。

**Headers:**

```
Authorization: Bearer <access_token>
X-User-Id: <user_id>  (来自 Gateway)
```

**Request Body:**

```json
{
  "oldPassword": "password123",
  "newPassword": "newPassword456"
}
```

**Response (200 OK):**

```json
{
  "success": true,
  "message": "密码修改成功"
}
```

**Error (400 Bad Request):**

```json
{
  "success": false,
  "message": "旧密码错误"
}
```

---

## 👥 UsersController - 用户管理 API

### 1. 获取用户列表（分页）

**GET** `/api/users?page=1&pageSize=10`

获取用户列表（支持分页）。

**Query Parameters:**

- `page` (int, optional): 页码，默认 1
- `pageSize` (int, optional): 每页数量，默认 10，最大 100

**Response (200 OK):**

```json
{
  "success": true,
  "message": "Users retrieved successfully",
  "data": {
    "items": [
      {
        "id": "uuid-1",
        "name": "张三",
        "email": "zhangsan@example.com",
        "phone": "13800138000",
        "createdAt": "2024-01-01T00:00:00Z",
        "updatedAt": "2024-01-01T00:00:00Z"
      }
    ],
    "totalCount": 100,
    "page": 1,
    "pageSize": 10
  }
}
```

---

### 2. 根据 ID 获取用户

**GET** `/api/users/{id}`

获取指定 ID 的用户信息。

**Response (200 OK):**

```json
{
  "success": true,
  "message": "User retrieved successfully",
  "data": {
    "id": "uuid-here",
    "name": "张三",
    "email": "zhangsan@example.com",
    "phone": "13800138000",
    "createdAt": "2024-01-01T00:00:00Z",
    "updatedAt": "2024-01-01T00:00:00Z"
  }
}
```

**Error (404 Not Found):**

```json
{
  "success": false,
  "message": "User not found"
}
```

---

### 3. 获取当前用户信息（使用 UserContext）

**GET** `/api/users/me`

获取当前登录用户信息（需要认证）。

**Headers:**

```
Authorization: Bearer <access_token>
X-User-Id: <user_id>  (来自 Gateway)
```

**Response (200 OK):**

```json
{
  "success": true,
  "message": "User retrieved successfully",
  "data": {
    "id": "uuid-here",
    "name": "张三",
    "email": "zhangsan@example.com",
    "phone": "13800138000",
    "createdAt": "2024-01-01T00:00:00Z",
    "updatedAt": "2024-01-01T00:00:00Z"
  }
}
```

**Error (401 Unauthorized):**

```json
{
  "success": false,
  "message": "未认证用户"
}
```

---

### 4. 创建用户（不带密码）

**POST** `/api/users`

创建用户（通常由管理员使用，不设置密码）。

**Request Body:**

```json
{
  "name": "李四",
  "email": "lisi@example.com",
  "phone": "13900139000"
}
```

**Response (201 Created):**

```json
{
  "success": true,
  "message": "User created successfully",
  "data": {
    "id": "uuid-here",
    "name": "李四",
    "email": "lisi@example.com",
    "phone": "13900139000",
    "createdAt": "2024-01-01T00:00:00Z",
    "updatedAt": "2024-01-01T00:00:00Z"
  }
}
```

**注意**: 该接口会通过消息总线发布 `user-created` 事件。

---

### 5. 更新用户信息（指定 ID）

**PUT** `/api/users/{id}`

更新指定用户信息。

**Request Body:**

```json
{
  "name": "张三（已更新）",
  "email": "zhangsan_new@example.com",
  "phone": "13800138001"
}
```

**Response (200 OK):**

```json
{
  "success": true,
  "message": "User updated successfully",
  "data": {
    "id": "uuid-here",
    "name": "张三（已更新）",
    "email": "zhangsan_new@example.com",
    "phone": "13800138001",
    "createdAt": "2024-01-01T00:00:00Z",
    "updatedAt": "2024-01-01T10:00:00Z"
  }
}
```

**Error (400 Bad Request):**

```json
{
  "success": false,
  "message": "邮箱 'zhangsan_new@example.com' 已被其他用户使用"
}
```

---

### 6. 更新当前用户信息（使用 UserContext）

**PUT** `/api/users/me`

更新当前登录用户信息（需要认证）。

**Headers:**

```
Authorization: Bearer <access_token>
X-User-Id: <user_id>  (来自 Gateway)
```

**Request Body:**

```json
{
  "name": "张三（已更新）",
  "email": "zhangsan_new@example.com",
  "phone": "13800138001"
}
```

**Response (200 OK):**

```json
{
  "success": true,
  "message": "User updated successfully",
  "data": { /* 同上 */ }
}
```

---

### 7. 删除用户

**DELETE** `/api/users/{id}`

删除指定用户。

**Response (200 OK):**

```json
{
  "success": true,
  "message": "User deleted successfully"
}
```

**Error (404 Not Found):**

```json
{
  "success": false,
  "message": "User not found"
}
```

**注意**: 该接口会通过消息总线发布 `user-deleted` 事件。

---

### 8. 健康检查

**GET** `/api/users/health`

服务健康检查端点。

**Response (200 OK):**

```json
{
  "status": "healthy",
  "service": "UserService",
  "timestamp": "2024-01-01T00:00:00Z"
}
```

---

### 9. 获取用户的产品列表（内部服务调用）

**GET** `/api/users/{userId}/products`

通过内部服务调用 ProductService 获取用户的产品列表。

**Response (200 OK):**

```json
{
  "success": true,
  "message": "User products retrieved successfully",
  "data": [ /* ProductService 返回的产品列表 */ ]
}
```

---

### 10. 获取缓存的用户信息（应用缓存）

**GET** `/api/users/{id}/cached`

使用缓存层保存用户数据（缓存 5 分钟）。

**Response (200 OK):**

```json
{
  "success": true,
  "message": "User retrieved from cache",  // 或 "User retrieved from database and cached"
  "data": { /* 用户信息 */ }
}
```

---

## 🔗 UserContext 集成

以下端点使用 **UserContext** 从 Gateway 传递的请求头中获取用户信息，**无需在路径或请求体中传递 userId**：

### AuthController

- ✅ `POST /api/auth/logout` - 从 `X-User-Id` header 获取 userId
- ✅ `POST /api/auth/change-password` - 从 `X-User-Id` header 获取 userId

### UsersController

- ✅ `GET /api/users/me` - 从 `X-User-Id` header 获取当前用户
- ✅ `PUT /api/users/me` - 从 `X-User-Id` header 更新当前用户

**Gateway 传递的 Headers:**

```
X-User-Id: <user_id>
X-User-Email: <user_email>
X-User-Role: <user_role>
```

---

## 🎯 统一响应格式

所有 API 响应均遵循 `ApiResponse<T>` 格式：

### 成功响应

```json
{
  "success": true,
  "message": "操作成功消息",
  "data": { /* 返回数据 */ }
}
```

### 错误响应

```json
{
  "success": false,
  "message": "错误消息",
  "errors": ["详细错误1", "详细错误2"]  // 可选
}
```

---

## 📊 HTTP 状态码

| 状态码                       | 说明    | 使用场景           |
|---------------------------|-------|----------------|
| 200 OK                    | 成功    | 获取数据、更新成功、删除成功 |
| 201 Created               | 已创建   | 创建用户成功         |
| 400 Bad Request           | 请求错误  | 验证失败、业务规则不满足   |
| 401 Unauthorized          | 未认证   | 未登录、Token 无效   |
| 404 Not Found             | 未找到   | 用户不存在          |
| 500 Internal Server Error | 服务器错误 | 系统异常           |

---

## 🔌 服务集成

UserService 集成了以下能力：

1. **消息发布**:
    - 发布 `user-created` 事件（用户创建时）
    - 发布 `user-deleted` 事件（用户删除时）

2. **内部服务调用**:
    - 调用 ProductService: `GET /api/users/{userId}/products`

3. **缓存**:
    - 缓存用户数据: `GET /api/users/{id}/cached`

---

## 🚀 API 测试示例

### 使用 cURL

**注册用户:**

```bash
curl -X POST http://localhost:5002/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "name": "张三",
    "email": "zhangsan@example.com",
    "password": "password123",
    "phone": "13800138000"
  }'
```

**登录:**

```bash
curl -X POST http://localhost:5002/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "zhangsan@example.com",
    "password": "password123"
  }'
```

**获取当前用户（需要 Token）:**

```bash
curl -X GET http://localhost:5002/api/users/me \
  -H "Authorization: Bearer <access_token>" \
  -H "X-User-Id: <user_id>"
```

---

## 📝 API 版本历史

### v2.0.0 (DDD 重构后)

- ✅ 完成 DDD 架构重构
- ✅ 分离 AuthController 和 UsersController
- ✅ 集成 UserContext（`/me` 路由）
- ✅ 统一 ApiResponse 响应格式
- ✅ 改进错误处理和 HTTP 状态码

### v1.0.0 (重构前)

- 基础 CRUD 功能
- 认证和授权
- 服务集成

---

**生成日期**: 2024-01-01  
**服务版本**: 2.0.0 (DDD)  
**作者**: GitHub Copilot
