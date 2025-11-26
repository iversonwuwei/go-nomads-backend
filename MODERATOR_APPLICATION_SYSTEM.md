# 版主申请系统实现方案

## 技术栈确认
- **后端**: ASP.NET Core 9.0
- **前端**: Flutter
- **实时通讯**: SignalR (已有 NotificationHub)
- **数据库**: Supabase (PostgreSQL)
- **消息系统**: MessageService (已存在)

## 数据库表结构

### 1. moderator_applications (已创建实体)
```sql
CREATE TABLE moderator_applications (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES auth.users(id),
    city_id UUID NOT NULL REFERENCES cities(id),
    reason TEXT NOT NULL,
    status VARCHAR(20) DEFAULT 'pending', -- pending, approved, rejected
    processed_by UUID REFERENCES auth.users(id),
    processed_at TIMESTAMP,
    rejection_reason TEXT,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);

CREATE INDEX idx_moderator_applications_user_id ON moderator_applications(user_id);
CREATE INDEX idx_moderator_applications_city_id ON moderator_applications(city_id);
CREATE INDEX idx_moderator_applications_status ON moderator_applications(status);
```

### 2. notifications (已存在)
表结构已满足需求，type 字段支持:
- `moderator_application` - 新的申请通知（发给管理员）
- `moderator_approved` - 申请通过通知（发给申请人）
- `moderator_rejected` - 申请拒绝通知（发给申请人）

## 业务流程

### 流程 1: 用户提交申请
```
用户点击"申请成为版主"
  ↓
POST /api/v1/cities/moderator/apply
  ↓
1. 检查用户是否已是该城市版主
2. 检查是否有待处理的申请
3. 创建申请记录（status=pending）
  ↓
4. 查询所有 admin 用户
5. 为每个 admin 创建通知记录
6. 通过 SignalR 实时推送给在线的 admin
  ↓
返回申请成功响应
```

### 流程 2: 管理员处理申请
```
Admin 收到 SignalR 推送
  ↓
Flutter 显示通知弹窗
  ↓
Admin 点击"同意"或"拒绝"
  ↓
POST /api/v1/cities/moderator/handle
  ↓
1. 验证 admin 权限
2. 更新申请状态
3. 如果同意：
   - 在 city_moderators 表中创建记录
   - 给用户赋予版主角色
4. 如果拒绝：
   - 记录拒绝原因
  ↓
5. 给申请人发送结果通知
6. 通过 SignalR 推送给申请人
  ↓
返回处理结果
```

## API 接口设计

### 1. 提交申请
```http
POST /api/v1/cities/moderator/apply
Authorization: Bearer {token}

Body:
{
  "cityId": "uuid",
  "reason": "我热爱这个城市，希望为社区贡献..."
}

Response:
{
  "success": true,
  "message": "申请已提交",
  "data": {
    "id": "uuid",
    "userId": "uuid",
    "cityId": "uuid",
    "status": "pending",
    "createdAt": "2025-01-25T10:00:00Z"
  }
}
```

### 2. 处理申请 (Admin Only)
```http
POST /api/v1/cities/moderator/handle
Authorization: Bearer {admin_token}

Body:
{
  "applicationId": "uuid",
  "action": "approve", // or "reject"
  "rejectionReason": "optional"
}

Response:
{
  "success": true,
  "message": "申请已批准",
  "data": {
    "id": "uuid",
    "status": "approved",
    "processedBy": "admin_uuid",
    "processedAt": "2025-01-25T11:00:00Z"
  }
}
```

### 3. 获取待处理申请 (Admin Only)
```http
GET /api/v1/cities/moderator/applications/pending?page=1&pageSize=20
Authorization: Bearer {admin_token}

Response:
{
  "success": true,
  "data": {
    "items": [...],
    "totalCount": 15,
    "page": 1,
    "pageSize": 20
  }
}
```

### 4. 获取我的申请
```http
GET /api/v1/cities/moderator/applications/my
Authorization: Bearer {token}

Response:
{
  "success": true,
  "data": [
    {
      "id": "uuid",
      "cityId": "uuid",
      "cityName": "Bangkok",
      "status": "pending",
      "reason": "...",
      "createdAt": "2025-01-25T10:00:00Z"
    }
  ]
}
```

## SignalR Hub 集成

### NotificationHub 方法（已存在，需扩展）

```csharp
// 客户端监听
connection.on("ReceiveNotification", (notification) => {
  // 处理通知
});

// 服务端发送（在 ModeratorApplicationService 中调用）
await _hubContext.Clients
    .User(adminUserId.ToString())
    .SendAsync("ReceiveNotification", notification);
```

## Flutter 客户端集成

### 1. SignalR 连接初始化
```dart
final connection = HubConnectionBuilder()
    .withUrl(
      "http://localhost:5005/notificationHub",
      HttpConnectionOptions(
        accessTokenFactory: () async => await getAuthToken(),
      ),
    )
    .build();

await connection.start();

// 监听版主申请通知
connection.on("ReceiveNotification", (args) {
  final notification = NotificationModel.fromJson(args![0]);
  
  if (notification.type == 'moderator_application') {
    _showModeratorApplicationDialog(notification);
  } else if (notification.type == 'moderator_approved') {
    _showSuccessMessage('恭喜！您已成为版主');
  } else if (notification.type == 'moderator_rejected') {
    _showRejectionMessage(notification.message);
  }
});
```

### 2. 申请提交
```dart
Future<void> applyForModerator(String cityId, String reason) async {
  final response = await http.post(
    Uri.parse('$baseUrl/api/v1/cities/moderator/apply'),
    headers: {'Authorization': 'Bearer $token'},
    body: jsonEncode({
      'cityId': cityId,
      'reason': reason,
    }),
  );
  
  if (response.statusCode == 200) {
    Get.snackbar('成功', '申请已提交，请等待管理员审核');
  }
}
```

### 3. 管理员处理申请
```dart
Future<void> handleApplication(String appId, String action) async {
  final response = await http.post(
    Uri.parse('$baseUrl/api/v1/cities/moderator/handle'),
    headers: {'Authorization': 'Bearer $adminToken'},
    body: jsonEncode({
      'applicationId': appId,
      'action': action,
    }),
  );
  
  if (response.statusCode == 200) {
    Get.back(); // 关闭弹窗
    _refreshApplications();
  }
}
```

## 通知消息格式

### 给管理员的申请通知
```json
{
  "id": "uuid",
  "userId": "admin_uuid",
  "title": "新的版主申请",
  "message": "用户 Walden 申请成为 Bangkok 的版主",
  "type": "moderator_application",
  "relatedId": "application_uuid",
  "metadata": {
    "applicationId": "uuid",
    "applicantId": "uuid",
    "applicantName": "Walden",
    "cityId": "uuid",
    "cityName": "Bangkok",
    "reason": "申请原因..."
  },
  "isRead": false,
  "createdAt": "2025-01-25T10:00:00Z"
}
```

### 给申请人的批准通知
```json
{
  "id": "uuid",
  "userId": "applicant_uuid",
  "title": "版主申请已通过",
  "message": "恭喜！您已成为 Bangkok 的版主",
  "type": "moderator_approved",
  "relatedId": "application_uuid",
  "metadata": {
    "cityId": "uuid",
    "cityName": "Bangkok"
  },
  "isRead": false,
  "createdAt": "2025-01-25T11:00:00Z"
}
```

## 权限控制

### 中间件检查
```csharp
[Authorize(Roles = "admin")]
public async Task<IActionResult> HandleApplication(...)
{
    // 只有 admin 可以处理申请
}
```

### 业务逻辑检查
```csharp
// 检查是否已是版主
var existingModerator = await _moderatorRepo.GetAsync(cityId, userId);
if (existingModerator != null)
{
    throw new InvalidOperationException("您已经是该城市的版主");
}

// 检查是否有待处理申请
var hasPending = await _applicationRepo.HasPendingApplicationAsync(userId, cityId);
if (hasPending)
{
    throw new InvalidOperationException("您已有待处理的申请");
}
```

## 实现步骤

✅ 1. 创建 ModeratorApplication 实体
✅ 2. 创建 IModeratorApplicationRepository 接口
✅ 3. 实现 ModeratorApplicationRepository
✅ 4. 创建 DTO 类
✅ 5. 创建 IModeratorApplicationService 接口
⬜ 6. 实现 ModeratorApplicationService（需要集成 MessageService）
⬜ 7. 创建 ModeratorApplicationController
⬜ 8. 注册依赖注入
⬜ 9. 在 MessageService 中添加发送通知的方法
⬜ 10. Flutter 端集成 SignalR
⬜ 11. Flutter 端创建申请 UI
⬜ 12. Flutter 端创建管理员审批 UI

## 下一步

由于代码量较大，我建议分步骤实现：

1. **先完成后端核心逻辑** - Controller + Service
2. **测试 API 接口** - 使用 Postman/Swagger
3. **实现 SignalR 推送** - 确保实时通知工作
4. **Flutter 客户端开发** - UI + SignalR 集成

需要我继续实现哪个部分？
