# Event 参与者信息集成完成

## 📋 变更摘要

成功将 EventService 的参与者信息集成到前端 Meetup Detail 页面,通过 gRPC 调用 UserService 获取完整的用户信息。

## 🎯 实现目标

将活动参与者的**完整用户信息**(姓名、邮箱、头像、电话)集成到 Event 详情 API 中,前端无需单独调用 `/participants` 端点。

## 🔧 后端修改

### 1. UserGrpcClient.cs - 添加完整用户信息获取方法

**文件**: `go-nomads/src/Services/EventService/EventService/Infrastructure/GrpcClients/UserGrpcClient.cs`

**新增接口**:
```csharp
public interface IUserGrpcClient
{
    // 新增方法:获取包含 Avatar 和 Phone 的完整用户信息
    Task<Dictionary<Guid, UserInfo>> GetUsersInfoByIdsAsync(
        IEnumerable<Guid> userIds, 
        CancellationToken cancellationToken = default);
}
```

**实现要点**:
- 扩展了 `UserDto` 内部类,添加 `Phone` 和 `Avatar` 字段
- 通过 Dapr Service Invocation 批量调用 UserService
- 并行请求优化性能
- 返回 `Dictionary<Guid, UserInfo>` 供应用服务使用

### 2. EventApplicationService.cs - 在 GetEventAsync 中填充参与者用户信息

**文件**: `go-nomads/src/Services/EventService/EventService/Application/Services/EventApplicationService.cs`

**修改逻辑**:
```csharp
public async Task<EventResponse> GetEventAsync(Guid id, Guid? userId = null)
{
    // ... 获取 event 基本信息 ...
    
    // 获取参与者列表
    var participants = await GetParticipantsAsync(id);
    
    // 🔧 通过 gRPC 填充用户信息
    if (participants.Any())
    {
        var userIds = participants.Select(p => p.UserId).Distinct().ToList();
        var users = await _userGrpcClient.GetUsersInfoByIdsAsync(userIds);
        
        foreach (var participant in participants)
        {
            if (users.TryGetValue(participant.UserId, out var userInfo))
            {
                participant.User = userInfo; // 填充完整用户信息
            }
        }
    }
    
    response.Participants = participants.ToList();
    // ...
}
```

**关键改进**:
- ✅ 一次 API 调用获取所有信息(Event + Participants + UserInfo)
- ✅ 批量 gRPC 调用优化性能
- ✅ 异常处理:即使 UserService 失败也不影响主流程
- ✅ 详细日志记录方便调试

## 📱 前端修改

### 1. meetup_detail_page.dart - 从 eventData 中提取参与者

**文件**: `df_admin_mobile/lib/pages/meetup_detail_page.dart`

**数据加载**:
```dart
Future<void> _loadEventDetails() async {
  final response = await _eventsApiService.getEvent(widget.meetup.id);
  final eventData = response;
  
  _meetup.value = _convertApiEventToMeetupModel(eventData);
  
  // 🔧 从 eventData['participants'] 中提取参与者信息
  final participantsData = eventData['participants'] as List?;
  if (participantsData != null) {
    _participants.value = participantsData
        .map((p) => p as Map<String, dynamic>)
        .toList();
  }
}
```

**参与者头像列表渲染**:
```dart
Widget _buildAttendeesList() {
  return ListView.builder(
    itemBuilder: (context, index) {
      final participant = _participants[index];
      
      // 从嵌套的 user 对象中获取信息
      final userInfo = participant['user'] as Map<String, dynamic>?;
      final userName = userInfo?['name'] as String? ?? 'User';
      final userAvatar = userInfo?['avatar'] as String?;
      
      return CircleAvatar(
        backgroundImage: NetworkImage(
          userAvatar ?? 'https://i.pravatar.cc/150?u=$userId',
        ),
      );
    },
  );
}
```

**View All 对话框**:
```dart
void _showAllAttendees() {
  Get.dialog(
    AlertDialog(
      content: Obx(() {
        return ListView.builder(
          itemCount: _participants.length,
          itemBuilder: (context, index) {
            final participant = _participants[index];
            final userInfo = participant['user'] as Map<String, dynamic>?;
            final userName = userInfo?['name'] ?? 'User ${index + 1}';
            final userEmail = userInfo?['email'];
            
            return ListTile(
              title: Text(userName),
              subtitle: Text(userEmail ?? 'Digital Nomad'),
            );
          },
        );
      }),
    ),
  );
}
```

## 📊 数据结构

### EventResponse 返回格式

```json
{
  "success": true,
  "message": "Event 获取成功",
  "data": {
    "id": "b3593196-9ee8-4171-bf31-aac5f06e51e4",
    "title": "Digital Nomad Meetup",
    "participants": [
      {
        "id": "participant-uuid",
        "eventId": "event-uuid",
        "userId": "user-uuid",
        "status": "registered",
        "registeredAt": "2025-10-25T12:00:00Z",
        "user": {
          "id": "user-uuid",
          "name": "walden",
          "email": "walden.wuwei@gmail.com",
          "avatar": "https://...",
          "phone": "+86138****"
        }
      }
    ]
  }
}
```

## ✅ 测试验证

### 后端 API 测试

```powershell
# 获取 Event 详情
$headers = @{ 
    'Authorization' = 'Bearer <token>'
    'X-User-Id' = '<user-id>' 
}
Invoke-RestMethod -Uri "http://localhost:8005/api/v1/events/<event-id>" `
    -Headers $headers -Method Get
```

**预期结果**:
- ✅ `data.participants` 数组不为空
- ✅ 每个 `participant.user` 包含 `name`, `email`, `avatar`, `phone`
- ✅ 日志显示: `✅ 成功为 N 个参与者填充用户信息`

### 前端测试步骤

1. **重启 Flutter app**
2. **打开任意活动详情页**
3. **验证参与者头像**:
   - 应显示真实用户头像(不是测试数据)
   - Tooltip 显示真实用户名
4. **点击 "View All" 按钮**:
   - 列表显示真实参与者姓名
   - 副标题显示真实邮箱地址
5. **控制台日志**:
   ```
   ✅ 成功从活动详情中加载 N 个参与者(包含用户信息)
   ```

## 🎯 优势对比

### 改进前
```
前端需要 2 次 API 调用:
1. GET /api/v1/events/{id}  → 获取活动基本信息
2. GET /api/v1/events/{id}/participants  → 获取参与者列表

问题:
❌ 多次网络请求
❌ 参与者信息可能不完整
❌ 前端需要复杂的数据合并逻辑
```

### 改进后
```
前端只需 1 次 API 调用:
1. GET /api/v1/events/{id}  → 获取所有信息(活动+参与者+用户详情)

优势:
✅ 单次请求获取完整数据
✅ 后端统一处理数据聚合
✅ 前端代码简化
✅ 用户体验更流畅
```

## 📝 技术要点

### 1. gRPC 批量调用优化
- 使用 `Task.WhenAll` 并行请求多个用户信息
- 避免 N+1 查询问题

### 2. 容错设计
- UserService 调用失败不影响主流程
- 返回部分数据优于完全失败

### 3. 数据嵌套结构
```
EventResponse
  └─ Participants[]
       └─ User{}  ← 嵌套的用户完整信息
```

### 4. 前端响应式更新
- 使用 `Obx()` 自动监听 `_participants` 变化
- 数据加载完成后自动刷新 UI

## 🚀 部署说明

### 后端重新部署
```powershell
cd go-nomads/deployment
.\deploy-services-local.ps1
```

### 前端无需额外操作
- 代码已更新,直接 Hot Reload 即可
- 或重启 app: `flutter run`

## 📌 注意事项

1. **Token 过期处理**: 确保前端 token 有效
2. **Avatar 默认值**: UserService 可能返回 `null`,前端需要 fallback
3. **性能监控**: 观察批量 gRPC 调用的耗时
4. **错误日志**: 检查 EventService 日志确认 UserService 调用成功

## 🔍 故障排查

### 如果参与者信息为空
1. 检查 Event 是否有参与者: 调用 `/api/v1/events/{id}/join`
2. 查看 EventService 日志: `docker logs go-nomads-event-service`
3. 确认 UserService 正常运行: `docker ps | grep user-service`

### 如果用户信息缺失
1. 检查 UserService 是否返回数据
2. 查看 gRPC 调用日志: 搜索 `GetUsersInfoByIdsAsync`
3. 确认 Dapr Sidecar 正常工作

## ✨ 总结

成功实现了 Event 详情 API 与 UserService 的集成,前端现在可以通过单次 API 调用获取包含完整用户信息的参与者列表,大大提升了开发效率和用户体验!
