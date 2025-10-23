# EventService API 端点总结

## 📋 所有 API 端点（已完全移除 userId 参数传递）

### ✅ 端点列表

| HTTP 方法 | 路由 | 认证要求 | UserContext 使用 | 说明 |
|----------|------|---------|-----------------|------|
| **POST** | `/api/v1/events` | ✅ 必须 | ✅ 获取 organizerId | 创建 Event |
| **GET** | `/api/v1/events/{id}` | ⭕ 可选 | ⭕ 可选（用于判断关注/参与状态） | 获取 Event 详情 |
| **GET** | `/api/v1/events` | ❌ 不需要 | ❌ 不使用 | 获取 Event 列表 |
| **PUT** | `/api/v1/events/{id}` | ✅ 必须 | ✅ 获取 userId（权限验证） | 更新 Event |
| **POST** | `/api/v1/events/{id}/join` | ✅ 必须 | ✅ 获取 userId | 参加 Event |
| **DELETE** | `/api/v1/events/{id}/join` | ✅ 必须 | ✅ 获取 userId | 取消参加 Event |
| **POST** | `/api/v1/events/{id}/follow` | ✅ 必须 | ✅ 获取 userId | 关注 Event |
| **DELETE** | `/api/v1/events/{id}/follow` | ✅ 必须 | ✅ 获取 userId | 取消关注 Event |
| **GET** | `/api/v1/events/{id}/participants` | ❌ 不需要 | ❌ 不使用 | 获取参与者列表 |
| **GET** | `/api/v1/events/{id}/followers` | ❌ 不需要 | ❌ 不使用 | 获取关注者列表 |
| **GET** | `/api/v1/events/me/created` | ✅ 必须 | ✅ 获取 userId | 获取我创建的 Event |
| **GET** | `/api/v1/events/me/joined` | ✅ 必须 | ✅ 获取 userId | 获取我参加的 Event |
| **GET** | `/api/v1/events/me/following` | ✅ 必须 | ✅ 获取 userId | 获取我关注的 Event |

---

## 🔧 修改详情

### 1. **CreateEvent** - 创建 Event

**路由**: `POST /api/v1/events`

**之前**:
```http
POST /api/v1/events
{
  "organizerId": "user-123",  // ❌ 需要传递
  "title": "活动标题"
}
```

**现在**:
```http
POST /api/v1/events
Authorization: Bearer {token}

{
  "title": "活动标题"  // ✅ organizerId 从 UserContext 获取
}
```

**Controller 代码**:
```csharp
public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
{
    var userContext = UserContextMiddleware.GetUserContext(HttpContext);
    if (userContext?.IsAuthenticated != true) return Unauthorized();
    
    var organizerId = Guid.Parse(userContext.UserId);
    var response = await _eventService.CreateEventAsync(request, organizerId);
    return CreatedAtAction(nameof(GetEvent), new { id = response.Id }, response);
}
```

---

### 2. **GetEvent** - 获取 Event 详情

**路由**: `GET /api/v1/events/{id}`

**之前**:
```http
GET /api/v1/events/{id}?userId=xxx  // ❌ 需要传递 userId
```

**现在**:
```http
GET /api/v1/events/{id}
Authorization: Bearer {token}  // ⭕ 可选，如果传了则返回 isFollowing 等状态
```

**Controller 代码**:
```csharp
public async Task<IActionResult> GetEvent(Guid id)
{
    // 从 UserContext 获取用户信息（可选）
    var userContext = UserContextMiddleware.GetUserContext(HttpContext);
    Guid? userId = null;
    
    if (userContext?.IsAuthenticated == true && !string.IsNullOrEmpty(userContext.UserId))
    {
        userId = Guid.Parse(userContext.UserId);
    }

    var response = await _eventService.GetEventAsync(id, userId);
    return Ok(response);
}
```

**说明**: 
- 如果用户已登录，返回的 `EventResponse` 会包含 `isFollowing` 和 `isParticipant` 状态
- 如果用户未登录，这些字段为 `false`

---

### 3. **UpdateEvent** - 更新 Event

**路由**: `PUT /api/v1/events/{id}`

**之前**:
```http
PUT /api/v1/events/{id}?userId=xxx  // ❌ 需要传递 userId
{
  "title": "新标题"
}
```

**现在**:
```http
PUT /api/v1/events/{id}
Authorization: Bearer {token}

{
  "title": "新标题"  // ✅ userId 从 UserContext 获取
}
```

**Controller 代码**:
```csharp
public async Task<IActionResult> UpdateEvent(Guid id, [FromBody] UpdateEventRequest request)
{
    var userContext = UserContextMiddleware.GetUserContext(HttpContext);
    if (userContext?.IsAuthenticated != true) return Unauthorized();

    var userId = Guid.Parse(userContext.UserId);
    var response = await _eventService.UpdateEventAsync(id, request, userId);
    return Ok(response);
}
```

---

### 4. **JoinEvent** - 参加 Event

**路由**: `POST /api/v1/events/{id}/join`

**之前**:
```http
POST /api/v1/events/{id}/join
{
  "userId": "user-123",  // ❌ 需要传递
  "paymentStatus": "pending"
}
```

**现在**:
```http
POST /api/v1/events/{id}/join
Authorization: Bearer {token}

{
  "paymentStatus": "pending"  // ✅ userId 从 UserContext 获取
}
```

**Controller 代码**:
```csharp
public async Task<IActionResult> JoinEvent(Guid id, [FromBody] JoinEventRequest request)
{
    var userContext = UserContextMiddleware.GetUserContext(HttpContext);
    if (userContext?.IsAuthenticated != true) return Unauthorized();

    var userId = Guid.Parse(userContext.UserId);
    var response = await _eventService.JoinEventAsync(id, userId, request);
    return Ok(new { success = true, participant = response });
}
```

---

### 5. **LeaveEvent** - 取消参加 Event

**路由**: `DELETE /api/v1/events/{id}/join`

**之前**:
```http
DELETE /api/v1/events/{id}/join?userId=xxx  // ❌ 需要传递 userId
```

**现在**:
```http
DELETE /api/v1/events/{id}/join
Authorization: Bearer {token}  // ✅ userId 从 UserContext 获取
```

**Controller 代码**:
```csharp
public async Task<IActionResult> LeaveEvent(Guid id)
{
    var userContext = UserContextMiddleware.GetUserContext(HttpContext);
    if (userContext?.IsAuthenticated != true) return Unauthorized();

    var userId = Guid.Parse(userContext.UserId);
    await _eventService.LeaveEventAsync(id, userId);
    return Ok(new { success = true, message = "已取消参加" });
}
```

---

### 6. **FollowEvent** - 关注 Event

**路由**: `POST /api/v1/events/{id}/follow`

**之前**:
```http
POST /api/v1/events/{id}/follow
{
  "userId": "user-123",  // ❌ 需要传递
  "notificationEnabled": true
}
```

**现在**:
```http
POST /api/v1/events/{id}/follow
Authorization: Bearer {token}

{
  "notificationEnabled": true  // ✅ userId 从 UserContext 获取
}
```

---

### 7. **UnfollowEvent** - 取消关注 Event

**路由**: `DELETE /api/v1/events/{id}/follow`

**之前**:
```http
DELETE /api/v1/events/{id}/follow?userId=xxx  // ❌ 需要传递 userId
```

**现在**:
```http
DELETE /api/v1/events/{id}/follow
Authorization: Bearer {token}  // ✅ userId 从 UserContext 获取
```

---

### 8. **GetMyCreatedEvents** - 获取我创建的 Event（重要变更）

**路由变更**: `/user/{userId}/created` → `/me/created`

**之前**:
```http
GET /api/v1/events/user/xxx-user-id/created  // ❌ 需要在 URL 中传递 userId
```

**现在**:
```http
GET /api/v1/events/me/created
Authorization: Bearer {token}  // ✅ userId 从 UserContext 获取
```

**Controller 代码**:
```csharp
[HttpGet("me/created")]
public async Task<IActionResult> GetMyCreatedEvents()
{
    var userContext = UserContextMiddleware.GetUserContext(HttpContext);
    if (userContext?.IsAuthenticated != true) return Unauthorized();

    var userId = Guid.Parse(userContext.UserId);
    var events = await _eventService.GetUserCreatedEventsAsync(userId);
    return Ok(events);
}
```

---

### 9. **GetMyJoinedEvents** - 获取我参加的 Event（重要变更）

**路由变更**: `/user/{userId}/joined` → `/me/joined`

**之前**:
```http
GET /api/v1/events/user/xxx-user-id/joined  // ❌ 需要在 URL 中传递 userId
```

**现在**:
```http
GET /api/v1/events/me/joined
Authorization: Bearer {token}  // ✅ userId 从 UserContext 获取
```

---

### 10. **GetMyFollowingEvents** - 获取我关注的 Event（重要变更）

**路由变更**: `/user/{userId}/following` → `/me/following`

**之前**:
```http
GET /api/v1/events/user/xxx-user-id/following  // ❌ 需要在 URL 中传递 userId
```

**现在**:
```http
GET /api/v1/events/me/following
Authorization: Bearer {token}  // ✅ userId 从 UserContext 获取
```

---

## ✅ 验证清单

| 检查项 | 状态 | 说明 |
|--------|------|------|
| ❌ 移除所有 `[FromQuery] userId` 参数 | ✅ 完成 | UpdateEvent, LeaveEvent, UnfollowEvent 等 |
| ❌ 移除所有 `[FromBody]` 中的 `userId` 字段 | ✅ 完成 | CreateEventRequest, JoinEventRequest, FollowEventRequest |
| ❌ 移除所有 URL 路径中的 `{userId}` | ✅ 完成 | `/user/{userId}/created` → `/me/created` |
| ✅ 所有需要认证的端点检查 UserContext | ✅ 完成 | 返回 401 Unauthorized 如果未认证 |
| ✅ 可选认证的端点正确处理 | ✅ 完成 | GetEvent 允许未登录访问，但提供不同响应 |
| ✅ 添加日志记录用户操作 | ✅ 完成 | 所有操作都记录 userId |
| ✅ 添加 401 响应类型注解 | ✅ 完成 | `[ProducesResponseType(StatusCodes.Status401Unauthorized)]` |

---

## 🎯 RESTful API 设计最佳实践

### ✅ 符合 RESTful 规范

1. **资源路由清晰**
   - ✅ `/events` - Event 资源
   - ✅ `/events/{id}` - 特定 Event
   - ✅ `/events/{id}/join` - Event 的参与子资源
   - ✅ `/events/me/created` - 当前用户的 Event 集合

2. **HTTP 方法语义正确**
   - ✅ `POST` - 创建资源（创建 Event、参加、关注）
   - ✅ `GET` - 获取资源（查询 Event、列表）
   - ✅ `PUT` - 更新资源（更新 Event 信息）
   - ✅ `DELETE` - 删除资源（取消参加、取消关注）

3. **状态码使用规范**
   - ✅ `201 Created` - 创建成功
   - ✅ `200 OK` - 操作成功
   - ✅ `401 Unauthorized` - 未认证
   - ✅ `403 Forbidden` - 无权限
   - ✅ `404 Not Found` - 资源不存在
   - ✅ `400 Bad Request` - 请求参数错误
   - ✅ `500 Internal Server Error` - 服务器错误

4. **认证统一处理**
   - ✅ 通过 `Authorization: Bearer {token}` 请求头
   - ✅ Gateway 统一验证 JWT
   - ✅ 微服务从 UserContext 获取用户信息
   - ✅ 不信任客户端传递的 userId

---

## 📚 相关文档

- [UserContext 实现说明](./USER_CONTEXT_IMPLEMENTATION.md)
- [三层架构 + DDD 文档](./ARCHITECTURE_DDD.md)
- [Scalar API 文档](http://localhost:8005/scalar/v1)

---

## 🚀 部署状态

- ✅ 编译成功
- ✅ 服务运行正常
- ✅ 健康检查通过
- ✅ 所有端点已验证
