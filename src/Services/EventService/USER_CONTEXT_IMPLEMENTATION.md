# EventService UserContext 实现说明

## 📋 变更概述

将 EventService 的接口调整为从 **UserContext** 中间件获取用户信息，而不是通过 API 参数传递。这样做的好处是：

✅ **统一认证机制** - 所有微服务使用相同的用户上下文获取方式  
✅ **提高安全性** - 用户信息由 Gateway 统一验证和传递，微服务不能伪造  
✅ **简化接口** - 减少不必要的参数，接口更清晰  
✅ **符合微服务最佳实践** - 认证在网关层完成，服务层只关注业务逻辑

---

## 🔧 实现细节

### 1. 添加 UserContext 中间件

**文件**: `Program.cs`

```csharp
using GoNomads.Shared.Extensions;

// ... 其他代码

app.UseRouting();
app.UseHttpMetrics();

// 使用用户上下文中间件 - 从 Gateway 传递的请求头中提取用户信息
app.UseUserContext();

app.MapControllers();
```

**作用**: 从 Gateway 传递的请求头中提取用户信息（`X-User-Id`, `X-User-Email`, `X-User-Role`）并存储到 `HttpContext.Items` 中。

---

### 2. 修改 DTOs - 移除 UserId 参数

#### **CreateEventRequest** (创建 Event)

**之前**:
```csharp
public class CreateEventRequest
{
    [Required(ErrorMessage = "创建者ID不能为空")]
    public Guid OrganizerId { get; set; }
    // ... 其他字段
}
```

**之后**:
```csharp
public class CreateEventRequest
{
    // 移除 OrganizerId - 从 UserContext 获取
    // ... 其他字段
}
```

#### **JoinEventRequest** (参加 Event)

**之前**:
```csharp
public class JoinEventRequest
{
    [Required(ErrorMessage = "用户ID不能为空")]
    public Guid UserId { get; set; }
    public string? PaymentStatus { get; set; } = "pending";
}
```

**之后**:
```csharp
public class JoinEventRequest
{
    // 移除 UserId - 从 UserContext 获取
    public string? PaymentStatus { get; set; } = "pending";
}
```

#### **FollowEventRequest** (关注 Event)

**之前**:
```csharp
public class FollowEventRequest
{
    [Required(ErrorMessage = "用户ID不能为空")]
    public Guid UserId { get; set; }
    public bool NotificationEnabled { get; set; } = true;
}
```

**之后**:
```csharp
public class FollowEventRequest
{
    // 移除 UserId - 从 UserContext 获取
    public bool NotificationEnabled { get; set; } = true;
}
```

---

### 3. 修改 Application Service 接口

**文件**: `Application/Services/IEventService.cs`

**之前**:
```csharp
Task<EventResponse> CreateEventAsync(CreateEventRequest request);
Task<ParticipantResponse> JoinEventAsync(Guid eventId, JoinEventRequest request);
Task<FollowerResponse> FollowEventAsync(Guid eventId, FollowEventRequest request);
```

**之后**:
```csharp
Task<EventResponse> CreateEventAsync(CreateEventRequest request, Guid organizerId);
Task<ParticipantResponse> JoinEventAsync(Guid eventId, Guid userId, JoinEventRequest request);
Task<FollowerResponse> FollowEventAsync(Guid eventId, Guid userId, FollowEventRequest request);
```

**变更**: 将 `userId` 从 DTO 中移到方法参数，由 Controller 从 UserContext 提取后传入。

---

### 4. 修改 Application Service 实现

**文件**: `Application/Services/EventApplicationService.cs`

```csharp
public async Task<EventResponse> CreateEventAsync(CreateEventRequest request, Guid organizerId)
{
    _logger.LogInformation("📝 创建新 Event，Organizer: {OrganizerId}", organizerId);

    // 使用传入的 organizerId 而不是 request.OrganizerId
    var @event = Event.Create(
        title: request.Title,
        organizerId: organizerId,  // 从参数获取
        startTime: request.StartTime,
        // ... 其他字段
    );

    var createdEvent = await _eventRepository.CreateAsync(@event);
    return MapToResponse(createdEvent);
}

public async Task<ParticipantResponse> JoinEventAsync(Guid eventId, Guid userId, JoinEventRequest request)
{
    _logger.LogInformation("👥 用户 {UserId} 申请参加 Event {EventId}", userId, eventId);

    // 使用传入的 userId 而不是 request.UserId
    var participant = EventParticipant.Create(eventId, userId, request.PaymentStatus);
    // ...
}

public async Task<FollowerResponse> FollowEventAsync(Guid eventId, Guid userId, FollowEventRequest request)
{
    _logger.LogInformation("⭐ 用户 {UserId} 关注 Event {EventId}", userId, eventId);

    // 使用传入的 userId 而不是 request.UserId
    var follower = EventFollower.Create(eventId, userId, request.NotificationEnabled);
    // ...
}
```

---

### 5. 修改 Controller - 从 UserContext 获取用户信息

**文件**: `API/Controllers/EventsController.cs`

```csharp
using GoNomads.Shared.Middleware;

[ApiController]
[Route("api/v1/[controller]")]
public class EventsController : ControllerBase
{
    // ...

    /// <summary>
    /// 创建 Event
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request)
    {
        try
        {
            // 从 UserContext 获取当前用户信息
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
            {
                return Unauthorized(new { error = "用户未认证" });
            }

            var organizerId = Guid.Parse(userContext.UserId);
            var response = await _eventService.CreateEventAsync(request, organizerId);
            
            _logger.LogInformation("✅ 用户 {UserId} 成功创建 Event {EventId}", organizerId, response.Id);
            return CreatedAtAction(nameof(GetEvent), new { id = response.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建 Event 失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 参加 Event
    /// </summary>
    [HttpPost("{id}/join")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> JoinEvent(Guid id, [FromBody] JoinEventRequest request)
    {
        try
        {
            // 从 UserContext 获取当前用户信息
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
            {
                return Unauthorized(new { error = "用户未认证" });
            }

            var userId = Guid.Parse(userContext.UserId);
            var response = await _eventService.JoinEventAsync(id, userId, request);
            
            _logger.LogInformation("✅ 用户 {UserId} 成功参加 Event {EventId}", userId, id);
            return Ok(new { success = true, message = "成功加入 Event", participant = response });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "参加 Event 失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 取消参加 Event
    /// </summary>
    [HttpDelete("{id}/join")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LeaveEvent(Guid id)
    {
        try
        {
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
            {
                return Unauthorized(new { error = "用户未认证" });
            }

            var userId = Guid.Parse(userContext.UserId);
            await _eventService.LeaveEventAsync(id, userId);
            
            _logger.LogInformation("✅ 用户 {UserId} 成功取消参加 Event {EventId}", userId, id);
            return Ok(new { success = true, message = "已取消参加" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消参加 Event 失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 关注 Event
    /// </summary>
    [HttpPost("{id}/follow")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> FollowEvent(Guid id, [FromBody] FollowEventRequest request)
    {
        try
        {
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
            {
                return Unauthorized(new { error = "用户未认证" });
            }

            var userId = Guid.Parse(userContext.UserId);
            var response = await _eventService.FollowEventAsync(id, userId, request);
            
            _logger.LogInformation("✅ 用户 {UserId} 成功关注 Event {EventId}", userId, id);
            return Ok(new { success = true, message = "成功关注 Event", follower = response });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "关注 Event 失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 取消关注 Event
    /// </summary>
    [HttpDelete("{id}/follow")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UnfollowEvent(Guid id)
    {
        try
        {
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
            {
                return Unauthorized(new { error = "用户未认证" });
            }

            var userId = Guid.Parse(userContext.UserId);
            await _eventService.UnfollowEventAsync(id, userId);
            
            _logger.LogInformation("✅ 用户 {UserId} 成功取消关注 Event {EventId}", userId, id);
            return Ok(new { success = true, message = "已取消关注" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消关注 Event 失败");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
```

---

## 🔐 UserContext 工作原理

### Gateway → Microservice 流程

```
┌─────────────────────────────────────────────────────────┐
│                        Gateway                          │
│                                                         │
│  1. 验证 JWT Token                                       │
│  2. 提取用户信息 (UserId, Email, Role)                   │
│  3. 添加自定义请求头:                                     │
│     - X-User-Id: {userId}                               │
│     - X-User-Email: {email}                             │
│     - X-User-Role: {role}                               │
│     - Authorization: Bearer {token}                     │
│                                                         │
└────────────────────┬────────────────────────────────────┘
                     │ HTTP Request with headers
                     ▼
┌─────────────────────────────────────────────────────────┐
│                 EventService (Microservice)             │
│                                                         │
│  ┌───────────────────────────────────────────────┐     │
│  │      UserContextMiddleware                    │     │
│  │  1. 从请求头提取用户信息                       │     │
│  │  2. 创建 UserContext 对象                      │     │
│  │  3. 存储到 HttpContext.Items                   │     │
│  └───────────────┬───────────────────────────────┘     │
│                  │                                      │
│  ┌───────────────▼───────────────────────────────┐     │
│  │         EventsController                      │     │
│  │  1. 调用 UserContextMiddleware.GetUserContext │     │
│  │  2. 检查认证状态                               │     │
│  │  3. 提取 UserId                                │     │
│  │  4. 传递给 Application Service                 │     │
│  └───────────────┬───────────────────────────────┘     │
│                  │                                      │
│  ┌───────────────▼───────────────────────────────┐     │
│  │     EventApplicationService                   │     │
│  │  执行业务逻辑,使用传入的 userId                 │     │
│  └───────────────────────────────────────────────┘     │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### UserContext 数据结构

```csharp
public class UserContext
{
    public string? UserId { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
    public string? AuthorizationHeader { get; set; }
    
    public bool IsAuthenticated => !string.IsNullOrEmpty(UserId);
}
```

---

## 📝 API 使用示例

### 创建 Event

**请求**:
```http
POST /api/v1/events
Authorization: Bearer {jwt_token}
Content-Type: application/json

{
  "title": "周末徒步活动",
  "description": "一起去爬山",
  "startTime": "2025-10-30T09:00:00Z",
  "location": "香山公园",
  "maxParticipants": 20,
  "price": 0
}
```

**注意**: 不需要传 `organizerId`，Gateway 会自动从 JWT Token 提取用户 ID 并通过请求头传递。

**响应**:
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "title": "周末徒步活动",
  "organizerId": "user-from-token",
  "startTime": "2025-10-30T09:00:00Z",
  "status": "upcoming",
  ...
}
```

### 参加 Event

**请求**:
```http
POST /api/v1/events/{eventId}/join
Authorization: Bearer {jwt_token}
Content-Type: application/json

{
  "paymentStatus": "pending"
}
```

**注意**: 不需要传 `userId`，从 UserContext 自动获取。

### 关注 Event

**请求**:
```http
POST /api/v1/events/{eventId}/follow
Authorization: Bearer {jwt_token}
Content-Type: application/json

{
  "notificationEnabled": true
}
```

### 取消参加 Event

**请求**:
```http
DELETE /api/v1/events/{eventId}/join
Authorization: Bearer {jwt_token}
```

**注意**: 不需要传 `userId` 查询参数。

### 取消关注 Event

**请求**:
```http
DELETE /api/v1/events/{eventId}/follow
Authorization: Bearer {jwt_token}
```

---

## ✅ 优势总结

| 方面 | 之前 (参数传递) | 现在 (UserContext) |
|------|----------------|-------------------|
| **安全性** | ❌ 客户端可伪造 UserId | ✅ Gateway 统一验证,无法伪造 |
| **接口清晰度** | ❌ 多余参数,职责不清 | ✅ 参数精简,职责明确 |
| **一致性** | ❌ 每个服务实现不同 | ✅ 所有服务统一方式 |
| **维护性** | ❌ 认证逻辑分散 | ✅ 认证集中在 Gateway |
| **错误率** | ❌ 易忘记传递 UserId | ✅ 自动获取,不易出错 |

---

## 🔍 参考实现

- **UserService**: `/src/Services/UserService/UserService/Controllers/UsersController.cs`
- **UserContextMiddleware**: `/src/Shared/Shared/Middleware/UserContextMiddleware.cs`
- **UserContext 模型**: `/src/Shared/Shared/Models/UserContext.cs`

---

## 📚 相关文档

- [EventService 三层架构 + DDD 文档](./ARCHITECTURE_DDD.md)
- [Gateway 配置文档](../../Gateway/README.md)
