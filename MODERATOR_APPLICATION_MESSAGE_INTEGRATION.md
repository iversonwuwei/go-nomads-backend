# 版主申请消息系统集成方案

## 架构设计

### 服务通信方式
- **Dapr Service Invocation** - 微服务间通过 Dapr 进行 HTTP 调用
- CityService 通过 `DaprClient.InvokeMethodAsync` 调用 MessageService
- 不使用传统 gRPC，而是使用 Dapr 的服务调用机制

## 流程设计

### 1. 用户申请成为版主
```
用户提交申请
    ↓
CityService.ModeratorApplicationService.ApplyAsync()
    ↓
├─ 创建 moderator_applications 记录 ✅
    ↓
└─ 调用 MessageService 创建通知消息 ✅
       ↓
       ├─ 为每个管理员创建 notifications 记录
       └─ 通过 SignalR 实时推送给在线管理员
```

### 2. 管理员处理申请
```
管理员批准/拒绝
    ↓
CityService.ModeratorApplicationService.HandleApplicationAsync()
    ↓
├─ 更新 moderator_applications.status ✅
├─ (如批准) 创建 city_moderators 记录 ✅
├─ (如批准) 调用 UserService 修改用户角色 ✅
    ↓
└─ 调用 MessageService 通知申请人 ✅
       ↓
       ├─ 创建 notifications 记录 (approved/rejected)
       └─ 通过 SignalR 实时推送给申请人
```

## 数据库表

### moderator_applications (申请记录表)
```sql
CREATE TABLE moderator_applications (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,          -- 申请人
    city_id INT NOT NULL,            -- 申请的城市
    reason TEXT,                     -- 申请理由
    status VARCHAR(20),              -- pending/approved/rejected
    processed_by UUID,               -- 处理人
    processed_at TIMESTAMP,
    rejection_reason TEXT,
    created_at TIMESTAMP,
    updated_at TIMESTAMP
);
```

### notifications (消息/通知表)
```sql
CREATE TABLE notifications (
    id UUID PRIMARY KEY,
    user_id TEXT NOT NULL,           -- 接收者
    title VARCHAR(200) NOT NULL,     -- 标题
    message TEXT,                    -- 消息内容
    type VARCHAR(50) NOT NULL,       -- 类型
    related_id TEXT,                 -- 关联ID (如 application_id)
    metadata JSONB,                  -- 元数据
    is_read BOOLEAN DEFAULT false,
    created_at TIMESTAMP,
    read_at TIMESTAMP
);
```

## MessageService API 端点

### 创建通知 (供其他服务调用)
```http
POST /api/v1/notifications
Content-Type: application/json

{
  "userId": "uuid",
  "title": "新的版主申请",
  "message": "用户 xxx 申请成为 xxx 的版主",
  "type": "moderator_application",
  "relatedId": "application_id",
  "metadata": "{...}"
}
```

### 批量发送给管理员
```http
POST /api/v1/notifications/admins
Content-Type: application/json

{
  "title": "新的版主申请",
  "message": "用户 xxx 申请成为 xxx 的版主",
  "type": "moderator_application",
  "relatedId": "application_id",
  "metadata": "{...}"
}
```

## Dapr 调用示例

### CityService 调用 MessageService

```csharp
// 发送给单个管理员
var notification = new
{
    UserId = adminId.ToString(),
    Title = "新的版主申请",
    Message = $"用户 {applicantName} 申请成为 {cityName} 的版主",
    Type = "moderator_application",
    RelatedId = application.Id.ToString(),
    Metadata = JsonSerializer.Serialize(new { ... })
};

await _daprClient.InvokeMethodAsync(
    HttpMethod.Post,
    "message-service",              // Dapr app-id
    "api/v1/notifications",         // API 路径
    notification
);
```

## 消息类型

### 版主申请相关消息类型
- `moderator_application` - 新的版主申请 (发给管理员)
- `moderator_approved` - 申请已批准 (发给申请人)
- `moderator_rejected` - 申请已拒绝 (发给申请人)

## 实现状态

### ✅ 已完成
1. **CityService - 申请记录管理**
   - ModeratorApplicationController ✅
   - ModeratorApplicationService ✅
   - ModeratorApplicationRepository ✅
   - 数据库表 `moderator_applications` ✅

2. **MessageService - 通知系统**
   - NotificationsController (REST API) ✅
   - NotificationService ✅
   - NotificationRepository ✅
   - 数据库表 `notifications` ✅
   - SignalR 实时推送 ✅

3. **服务间通信**
   - Dapr 集成 ✅
   - CityService → MessageService 调用 ✅
   - CityService → UserService 调用 (修改角色) ✅

4. **通知发送逻辑**
   - 申请提交时通知所有管理员 ✅
   - 申请批准时通知申请人 ✅
   - 申请拒绝时通知申请人 ✅

### ⏳ 需要优化

1. **批量通知优化**
   当前实现是循环为每个管理员单独创建通知,可以优化为批量创建:
   
   ```csharp
   // 当前实现 (在 ModeratorApplicationService.cs)
   foreach (var adminId in adminIds)
   {
       await _daprClient.InvokeMethodAsync(
           HttpMethod.Post,
           "message-service",
           "api/v1/notifications",
           notification
       );
   }
   
   // 优化建议: 使用批量接口
   await _daprClient.InvokeMethodAsync(
       HttpMethod.Post,
       "message-service",
       "api/v1/notifications/batch",  // 新增批量接口
       new { 
           UserIds = adminIds,
           Title = "...",
           Message = "...",
           ...
       }
   );
   ```

2. **消息持久化**
   当前通知直接通过 SignalR 推送,建议:
   - ✅ 通知已持久化到 `notifications` 表
   - ✅ 支持离线消息 (用户上线后可查看)
   - ✅ 支持消息已读/未读状态

## Dapr 配置

### message-service (Dapr app-id)
```yaml
# deployment/message-service.yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: message-service
spec:
  type: serviceinvocation
  version: v1
```

### 环境变量
```bash
# MessageService
DAPR_APP_ID=message-service
DAPR_HTTP_PORT=3500
DAPR_GRPC_PORT=50001

# CityService
DAPR_APP_ID=city-service
DAPR_HTTP_PORT=3501
DAPR_GRPC_PORT=50002
```

## 测试场景

### 场景 1: 用户申请成为版主
1. 用户在 Flutter 客户端提交申请
2. Gateway → CityService: POST /api/v1/cities/moderator/apply
3. CityService 创建 `moderator_applications` 记录
4. CityService → MessageService: POST /api/v1/notifications (为每个管理员)
5. MessageService 创建 `notifications` 记录
6. MessageService 通过 SignalR 推送给在线管理员
7. 管理员收到实时通知

### 场景 2: 管理员批准申请
1. 管理员在 Flutter 客户端批准申请
2. Gateway → CityService: POST /api/v1/cities/moderator/handle
3. CityService 更新 `moderator_applications.status = 'approved'`
4. CityService 创建 `city_moderators` 记录
5. CityService → UserService: 修改用户 role_id
6. CityService → MessageService: POST /api/v1/notifications
7. MessageService 创建 `notifications` 记录 (type='moderator_approved')
8. MessageService 通过 SignalR 推送给申请人
9. 申请人收到批准通知

## 关键代码位置

### CityService
- `src/Services/CityService/CityService/Application/Services/ModeratorApplicationService.cs`
  - `ApplyAsync()` - 创建申请 + 通知管理员
  - `HandleApplicationAsync()` - 处理申请 + 通知申请人
  - `NotifyAdminsAboutNewApplicationAsync()` - 通知管理员
  - `NotifyApplicantApprovedAsync()` - 通知批准
  - `NotifyApplicantRejectedAsync()` - 通知拒绝

### MessageService
- `src/Services/MessageService/MessageService/API/Controllers/NotificationsController.cs`
  - `CreateNotification()` - 创建单个通知
  - `SendToAdmins()` - 发送给所有管理员
- `src/Services/MessageService/MessageService/Application/Services/NotificationApplicationService.cs`
- `src/Services/MessageService/MessageService/Infrastructure/Repositories/NotificationRepository.cs`

## 总结

✅ **版主申请既是一个申请记录,也会生成消息记录**
- 申请记录存储在 `moderator_applications` 表 ✅
- 消息记录存储在 `notifications` 表 ✅
- 通过 Dapr 实现服务间解耦 ✅
- 支持实时推送和离线消息 ✅
