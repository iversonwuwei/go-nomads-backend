# EventService Status 功能实现文档

## 概述
为 EventService 添加了完整的活动状态管理功能,支持状态筛选、状态转换和取消活动操作。

## 1. 状态枚举定义

### EventStatus (Domain/Enums/EventStatus.cs)

```csharp
public static class EventStatus
{
    public const string Upcoming = "upcoming";    // 即将开始
    public const string Ongoing = "ongoing";      // 进行中
    public const string Completed = "completed";  // 已结束
    public const string Cancelled = "cancelled";  // 已取消
}
```

## 2. Event 实体新增方法

### 状态转换方法

#### 2.1 取消活动
```csharp
public void Cancel(Guid userId)
```
- 验证状态:已取消或已结束的活动不能再次取消
- 将状态设置为 `cancelled`
- 记录操作用户和时间

#### 2.2 标记为进行中
```csharp
public void MarkAsOngoing()
```
- 只有 `upcoming` 状态的活动可以标记为进行中
- 将状态设置为 `ongoing`

#### 2.3 标记为已结束
```csharp
public void MarkAsCompleted()
```
- 已取消的活动不能标记为已结束
- 将状态设置为 `completed`

#### 2.4 根据时间自动更新状态
```csharp
public void UpdateStatusByTime()
```
- 已取消的活动不自动更新
- 活动开始后自动标记为 `ongoing`
- 活动结束后自动标记为 `completed`
- 无结束时间的活动默认开始后3小时自动结束

### 业务逻辑更新

#### 2.5 检查是否可以参加
```csharp
public bool CanJoin()
```
- 只有 `upcoming` 或 `ongoing` 状态的活动可以参加
- `completed` 和 `cancelled` 状态的活动不能参加

## 3. Application Service 新增方法

### EventApplicationService

#### 3.1 取消活动
```csharp
public async Task<EventResponse> CancelEventAsync(Guid id, Guid userId)
```

**功能:**
- 验证活动存在
- 验证权限:只有组织者可以取消
- 调用领域方法取消活动
- 返回更新后的活动信息

**异常:**
- `KeyNotFoundException`: 活动不存在
- `UnauthorizedAccessException`: 非组织者尝试取消
- `InvalidOperationException`: 活动状态不允许取消

## 4. API 端点

### 4.1 取消活动

**端点:** `POST /api/v1/events/{id}/cancel`

**请求:**
```http
POST /api/v1/events/550e8400-e29b-41d4-a716-446655440000/cancel
Authorization: Bearer <token>
```

**响应:**
```json
{
  "success": true,
  "message": "活动已取消",
  "data": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "title": "Digital Nomad Meetup",
    "status": "cancelled",
    ...
  }
}
```

**状态码:**
- `200 OK`: 取消成功
- `401 Unauthorized`: 用户未认证
- `403 Forbidden`: 无权限(非组织者)
- `404 Not Found`: 活动不存在
- `400 Bad Request`: 活动状态不允许取消

### 4.2 按状态筛选活动

**端点:** `GET /api/v1/events?status={status}`

**请求:**
```http
GET /api/v1/events?status=upcoming&page=1&pageSize=20
```

**支持的状态值:**
- `upcoming` - 即将开始的活动
- `ongoing` - 进行中的活动
- `completed` - 已结束的活动
- `cancelled` - 已取消的活动

**示例:**
```http
# 获取所有即将开始的活动
GET /api/v1/events?status=upcoming

# 获取某个城市进行中的活动
GET /api/v1/events?cityId=xxx&status=ongoing

# 获取已结束的活动
GET /api/v1/events?status=completed

# 获取已取消的活动
GET /api/v1/events?status=cancelled
```

## 5. Flutter 集成指南

### 5.1 MeetupDto 映射

确保 `MeetupDto` 正确映射 status 字段:

```dart
factory MeetupDto.fromJson(Map<String, dynamic> json) {
  return MeetupDto(
    // ... 其他字段
    status: json['status'] as String? ?? 'upcoming',
    // ...
  );
}
```

### 5.2 状态枚举

```dart
enum MeetupStatus {
  upcoming,   // 即将开始
  ongoing,    // 进行中
  completed,  // 已结束
  cancelled;  // 已取消

  static MeetupStatus fromString(String status) {
    return MeetupStatus.values.firstWhere(
      (e) => e.name == status,
      orElse: () => MeetupStatus.upcoming,
    );
  }
}
```

### 5.3 API 调用示例

#### 取消活动
```dart
Future<void> cancelMeetup(String meetupId) async {
  final response = await httpService.post(
    '/events/$meetupId/cancel',
  );
  
  if (response.statusCode == 200) {
    // 处理成功
    final data = response.data;
    print('活动已取消: ${data['status']}');
  }
}
```

#### 按状态筛选
```dart
Future<List<Meetup>> getMeetupsByStatus(String status) async {
  final response = await httpService.get(
    '/events',
    queryParameters: {
      'status': status,
      'page': 1,
      'pageSize': 20,
    },
  );
  
  final data = response.data;
  final events = (data['items'] as List)
      .map((json) => MeetupDto.fromJson(json).toDomain())
      .toList();
  
  return events;
}
```

### 5.4 UI 状态显示

```dart
Widget buildStatusBadge(MeetupStatus status) {
  final config = switch (status) {
    MeetupStatus.upcoming => (
      text: '即将开始',
      color: Colors.blue,
      icon: Icons.upcoming
    ),
    MeetupStatus.ongoing => (
      text: '进行中',
      color: Colors.green,
      icon: Icons.play_circle
    ),
    MeetupStatus.completed => (
      text: '已结束',
      color: Colors.grey,
      icon: Icons.check_circle
    ),
    MeetupStatus.cancelled => (
      text: '已取消',
      color: Colors.red,
      icon: Icons.cancel
    ),
  };
  
  return Chip(
    label: Text(config.text),
    backgroundColor: config.color.withOpacity(0.1),
    labelStyle: TextStyle(color: config.color),
    avatar: Icon(config.icon, color: config.color),
  );
}
```

### 5.5 按钮逻辑

```dart
Widget buildActionButton() {
  // 组织者显示取消按钮
  if (_meetup.isOrganizer) {
    if (_meetup.status == MeetupStatus.upcoming || 
        _meetup.status == MeetupStatus.ongoing) {
      return ElevatedButton(
        onPressed: _cancelMeetup,
        child: Text('取消活动'),
      );
    }
  }
  
  // 参与者显示加入/退出按钮
  if (_meetup.status == MeetupStatus.upcoming || 
      _meetup.status == MeetupStatus.ongoing) {
    if (_meetup.isJoined) {
      return ElevatedButton(
        onPressed: _leaveMeetup,
        child: Text('退出活动'),
      );
    } else {
      return ElevatedButton(
        onPressed: _joinMeetup,
        child: Text('加入活动'),
      );
    }
  }
  
  // 已结束或已取消的活动不显示按钮
  return SizedBox.shrink();
}
```

### 5.6 Tab 筛选示例

```dart
class MeetupListPage extends StatefulWidget {
  @override
  State<MeetupListPage> createState() => _MeetupListPageState();
}

class _MeetupListPageState extends State<MeetupListPage> {
  MeetupStatus _selectedStatus = MeetupStatus.upcoming;
  
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('活动列表'),
        bottom: TabBar(
          tabs: [
            Tab(text: '即将开始'),
            Tab(text: '进行中'),
            Tab(text: '已结束'),
            Tab(text: '已取消'),
          ],
          onTap: (index) {
            setState(() {
              _selectedStatus = MeetupStatus.values[index];
            });
            _loadMeetups();
          },
        ),
      ),
      body: FutureBuilder(
        future: getMeetupsByStatus(_selectedStatus.name),
        builder: (context, snapshot) {
          // 显示列表
        },
      ),
    );
  }
  
  Future<void> _loadMeetups() async {
    // 根据 _selectedStatus 加载数据
    final meetups = await getMeetupsByStatus(_selectedStatus.name);
    setState(() {
      // 更新状态
    });
  }
}
```

## 6. 状态转换规则

```
创建活动 → upcoming

upcoming → ongoing (活动开始时自动或手动)
upcoming → cancelled (组织者取消)

ongoing → completed (活动结束时自动)
ongoing → cancelled (组织者取消)

completed → (终态,不可转换)
cancelled → (终态,不可转换)
```

## 7. 权限控制

| 操作 | 组织者 | 参与者 | 游客 |
|------|--------|--------|------|
| 取消活动 | ✅ | ❌ | ❌ |
| 加入活动 (upcoming/ongoing) | ❌ | ✅ | ✅ |
| 退出活动 (upcoming/ongoing) | ❌ | ✅ | ❌ |
| 查看活动 | ✅ | ✅ | ✅ |

## 8. 数据库字段

Event 表的 status 字段:
- 类型: `VARCHAR(20)`
- 默认值: `'upcoming'`
- 索引: 建议添加索引以优化按状态筛选的查询

## 9. 测试建议

### 单元测试
1. 测试状态转换逻辑
2. 测试权限验证
3. 测试异常处理

### 集成测试
1. 测试 API 端点
2. 测试状态筛选
3. 测试并发取消

### E2E 测试
1. 组织者取消流程
2. 状态筛选 UI
3. 按钮显示逻辑

## 10. 注意事项

1. **状态同步**: 建议添加定时任务自动更新过期活动的状态
2. **通知**: 活动取消时应通知所有参与者
3. **退款**: 如涉及付费活动,取消时需要处理退款逻辑
4. **数据一致性**: 取消活动时考虑是否需要清理相关数据(参与记录等)
5. **UI 刷新**: 状态变更后及时刷新前端显示

## 11. 后续优化建议

1. 添加取消原因字段
2. 支持活动恢复(从 cancelled 恢复到 upcoming)
3. 添加活动延期功能
4. 支持批量状态更新
5. 添加状态变更历史记录
