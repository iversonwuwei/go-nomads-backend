# 管理员离线消息处理方案

## 现状分析

### ✅ 已实现的离线消息支持

当前架构**已经完整支持离线消息**:

```
用户申请版主
    ↓
CityService.ApplyAsync()
    ↓
通过 Dapr 调用 MessageService API
    ↓
POST /api/v1/notifications
    ↓
NotificationService.CreateNotificationAsync()
    ↓
NotificationRepository.CreateAsync()
    ↓
✅ 消息持久化到 notifications 表
```

**关键点**: 
- 消息**先保存到数据库**,无论管理员是否在线
- SignalR 只是额外的实时推送渠道
- 如果 SignalR 推送失败(管理员离线),**不影响消息保存**

### 工作流程

#### 1. 管理员离线时
```
用户申请 → 消息保存到数据库 (is_read=false) ✅
         → SignalR 推送 (失败,但不影响主流程) ⚠️
```

#### 2. 管理员上线后
```
管理员登录 → 调用 GET /api/v1/notifications?isRead=false
          → 获取所有未读消息 ✅
          → 显示红点Badge提示 ✅
```

## API 端点支持

### 1. 获取未读消息列表
```http
GET /api/v1/notifications?userId={adminId}&isRead=false&page=1&pageSize=20
```

**响应**:
```json
{
  "success": true,
  "data": {
    "notifications": [
      {
        "id": "uuid",
        "userId": "admin-uuid",
        "title": "新的版主申请",
        "message": "用户 xxx 申请成为 xxx 的版主",
        "type": "moderator_application",
        "relatedId": "application-uuid",
        "isRead": false,
        "createdAt": "2025-11-25T10:00:00Z"
      }
    ],
    "totalCount": 5,
    "page": 1,
    "pageSize": 20
  }
}
```

### 2. 获取未读消息数量
```http
GET /api/v1/notifications/unread/count?userId={adminId}
```

**响应**:
```json
{
  "success": true,
  "data": {
    "unreadCount": 5
  }
}
```

### 3. 标记已读
```http
PUT /api/v1/notifications/{notificationId}/read
```

### 4. 标记所有已读
```http
PUT /api/v1/notifications/read/all?userId={adminId}
```

## Flutter 客户端集成

### 1. 应用启动时拉取未读消息
```dart
class NotificationStateController extends GetxController {
  @override
  void onInit() {
    super.onInit();
    // 应用启动时加载未读消息
    loadUnreadNotifications();
    loadUnreadCount();
  }

  Future<void> loadUnreadNotifications() async {
    final result = await _repository.getUserNotifications(
      isRead: false,
      limit: 50,
    );
    
    result.fold(
      (failure) => print('加载未读消息失败'),
      (notifications) {
        // 显示未读消息列表
        _notifications.assignAll(notifications);
      },
    );
  }

  Future<void> loadUnreadCount() async {
    final result = await _repository.getUnreadCount();
    
    result.fold(
      (failure) => print('加载未读数量失败'),
      (count) {
        // 更新红点Badge
        _unreadCount.value = count;
      },
    );
  }
}
```

### 2. 定期轮询未读消息
```dart
class NotificationStateController extends GetxController {
  Timer? _pollTimer;

  @override
  void onInit() {
    super.onInit();
    // 启动定期轮询 (每30秒)
    _startPolling();
  }

  void _startPolling() {
    _pollTimer = Timer.periodic(
      const Duration(seconds: 30),
      (_) => loadUnreadCount(),
    );
  }

  @override
  void onClose() {
    _pollTimer?.cancel();
    super.onClose();
  }
}
```

### 3. SignalR 连接状态管理
```dart
class SignalRService extends GetxService {
  HubConnection? _connection;
  final _isConnected = false.obs;

  Future<void> connect() async {
    try {
      _connection = HubConnectionBuilder()
          .withUrl('$baseUrl/hubs/notifications')
          .build();

      _connection!.on('ReceiveNotification', (arguments) {
        // 实时接收通知
        _handleNotification(arguments);
      });

      await _connection!.start();
      _isConnected.value = true;
      
      // 连接成功后,拉取可能错过的离线消息
      await Get.find<NotificationStateController>()
          .loadUnreadNotifications();
      
    } catch (e) {
      print('SignalR 连接失败: $e');
      _isConnected.value = false;
      
      // 连接失败时,仍然可以通过轮询获取消息
      // 轮询已在 NotificationStateController 中启动
    }
  }

  Future<void> reconnect() async {
    if (!_isConnected.value) {
      await connect();
      
      // 重连成功后,拉取重连期间的消息
      await Get.find<NotificationStateController>()
          .loadUnreadNotifications();
    }
  }
}
```

## 优化方案

### 1. 批量通知接口 (推荐实现)

**现状**: 循环为每个管理员单独调用 API
```csharp
foreach (var adminId in adminIds)
{
    await _daprClient.InvokeMethodAsync(
        HttpMethod.Post,
        "message-service",
        "api/v1/notifications",
        notification
    );
}
```

**优化**: 批量创建接口
```http
POST /api/v1/notifications/batch
Content-Type: application/json

{
  "userIds": ["admin1-uuid", "admin2-uuid", "admin3-uuid"],
  "title": "新的版主申请",
  "message": "用户 xxx 申请成为 xxx 的版主",
  "type": "moderator_application",
  "relatedId": "application-uuid",
  "metadata": "{...}"
}
```

**实现**:
```csharp
// MessageService.API.Controllers.NotificationsController.cs

[HttpPost("batch")]
public async Task<ActionResult<ApiResponse<BatchNotificationResponse>>> CreateBatchNotifications(
    [FromBody] CreateBatchNotificationDto request,
    CancellationToken cancellationToken = default)
{
    var notifications = request.UserIds.Select(userId => new Notification
    {
        UserId = userId,
        Title = request.Title,
        Message = request.Message,
        Type = request.Type,
        RelatedId = request.RelatedId,
        Metadata = request.Metadata
    }).ToList();

    var created = await _repository.CreateBatchAsync(notifications, cancellationToken);

    return Ok(new ApiResponse<BatchNotificationResponse>
    {
        Success = true,
        Data = new BatchNotificationResponse
        {
            CreatedCount = created.Count,
            NotificationIds = created.Select(n => n.Id.ToString()).ToList()
        }
    });
}
```

### 2. 推送失败重试机制

**使用 MassTransit 重试策略**:
```csharp
// MessageService/Program.cs

cfg.ReceiveEndpoint("notifications-queue", e =>
{
    e.ConfigureConsumer<NotificationConsumer>(context);
    e.PrefetchCount = 16;
    
    // 配置重试策略
    e.UseMessageRetry(r => 
    {
        r.Interval(5, TimeSpan.FromSeconds(30));  // 5次重试,每次间隔30秒
        r.Ignore<ArgumentException>();             // 忽略参数错误
    });
    
    // 配置死信队列
    e.UseInMemoryOutbox();
});
```

### 3. 消息优先级队列

**高优先级消息** (如版主申请):
```csharp
await _daprClient.InvokeMethodAsync(
    HttpMethod.Post,
    "message-service",
    "api/v1/notifications",
    new {
        UserId = adminId,
        Title = "新的版主申请",
        Priority = "high",  // 添加优先级字段
        ...
    }
);
```

### 4. 邮件/短信补充通知 (可选)

当管理员长时间离线时,发送邮件提醒:

```csharp
// ModeratorApplicationService.cs

private async Task NotifyAdminsAboutNewApplicationAsync(...)
{
    // 1. 创建数据库通知 (立即执行)
    await CreateNotificationsAsync(...);
    
    // 2. 发送实时推送 (尽力而为)
    await SendSignalRNotificationsAsync(...);
    
    // 3. 如果管理员12小时未读,发送邮件 (后台任务)
    await ScheduleEmailReminderAsync(adminIds, 12 * 60);  // 12小时后
}
```

## 测试场景

### 场景 1: 管理员完全离线
```
1. 用户提交申请
2. 消息保存到数据库 ✅
3. SignalR 推送失败 (管理员离线) ⚠️
4. 管理员次日登录
5. 客户端调用 GET /api/v1/notifications?isRead=false
6. 显示昨天的未读申请 ✅
```

### 场景 2: 管理员断网重连
```
1. 用户提交申请时管理员在线
2. 消息保存到数据库 ✅
3. SignalR 推送成功 ✅
4. 管理员突然断网
5. 用户又提交一个申请
6. 消息保存到数据库 ✅
7. SignalR 推送失败 ⚠️
8. 管理员网络恢复,SignalR 重连
9. 客户端自动调用 loadUnreadNotifications() ✅
10. 显示重连期间错过的申请 ✅
```

### 场景 3: 多设备登录
```
1. 管理员在手机和电脑同时登录
2. 用户提交申请
3. 消息保存到数据库 ✅
4. SignalR 推送到两个设备 ✅
5. 管理员在手机上标记已读
6. 电脑端自动同步已读状态 ✅ (通过轮询或 SignalR)
```

## 监控和日志

### 1. 消息投递成功率监控
```csharp
_logger.LogInformation(
    "通知创建成功: NotificationId={Id}, UserId={UserId}, Type={Type}",
    notification.Id, notification.UserId, notification.Type
);

_logger.LogWarning(
    "SignalR 推送失败,但消息已保存: UserId={UserId}",
    userId
);
```

### 2. 未读消息统计
```sql
-- 查询各管理员的未读消息数量
SELECT 
    user_id,
    COUNT(*) as unread_count
FROM notifications
WHERE is_read = false
  AND type IN ('moderator_application', 'system_announcement')
GROUP BY user_id
ORDER BY unread_count DESC;
```

## 总结

### ✅ 现有方案已经支持离线消息
1. **消息持久化** - 所有通知都保存到数据库
2. **离线可查** - 管理员登录后可查询未读消息
3. **实时推送** - SignalR 作为额外的实时渠道
4. **不丢失消息** - SignalR 失败不影响消息保存

### 🚀 建议优化
1. **批量通知接口** - 减少 API 调用次数
2. **客户端轮询** - 补充 SignalR 的不可靠性
3. **重连拉取** - SignalR 重连后主动拉取未读消息
4. **邮件补充** - 长时间未读发送邮件提醒 (可选)

### 📝 实现优先级
1. **P0 (已完成)**: 消息持久化 + REST API 查询 ✅
2. **P1 (推荐)**: 批量通知接口 + 客户端轮询
3. **P2 (可选)**: 邮件提醒 + 推送通知
