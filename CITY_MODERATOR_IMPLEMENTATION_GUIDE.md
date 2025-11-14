# 城市版主功能集成指南

## 后端实现总结

### 1. 数据库变更
✅ 创建了 `city_moderators` 表 (010_create_city_moderators_table.sql)
- 支持多对多关系：一个城市可以有多个版主，一个用户可以是多个城市的版主
- 包含细粒度权限控制（编辑城市、管理 coworks/costs/visas/chats）
- 支持激活/停用状态
- 记录指定人和指定时间

### 2. 后端 API 端点

#### CityService 版主管理 API
```
GET    /api/v1/cities/{id}/moderators           - 获取城市的所有版主
POST   /api/v1/cities/{id}/moderators           - 添加版主（仅 admin）
DELETE /api/v1/cities/{cityId}/moderators/{userId} - 删除版主（仅 admin）
PATCH  /api/v1/cities/{cityId}/moderators/{moderatorId} - 更新版主权限（仅 admin）
```

#### UserService 用户搜索 API
```
GET /api/v1/users/search?q={searchTerm}&role={role}&page=1&pageSize=10
```
- 支持按名称或邮箱搜索
- 支持按角色筛选
- 分页返回结果

### 3. 新增 DTO 类
- `CityModeratorDto` - 版主详细信息
- `ModeratorUserDto` - 版主用户信息
- `AddCityModeratorDto` - 添加版主请求
- `UpdateCityModeratorDto` - 更新版主权限请求

---

## Flutter 集成步骤

由于 city_detail_page.dart 文件已经很大（约2600行），建议在页面中添加一个新的版主管理对话框组件。

### 实现方式

在 `city_detail_page.dart` 中：

1. **在 AppBar actions 添加版主管理按钮**（仅 admin 可见）
```dart
// 在 _buildAppBar() 的 actions 中添加
if (isAdmin) IconButton(
  icon: Icon(Icons.admin_panel_settings),
  onPressed: () => _showModeratorsDialog(context),
)
```

2. **创建版主管理对话框**
```dart
void _showModeratorsDialog(BuildContext context) async {
  showDialog(
    context: context,
    builder: (context) => ModeratorManagementDialog(
      cityId: widget.city.id,
      cityName: widget.city.name,
    ),
  );
}
```

3. **创建新文件：`lib/pages/city_moderator_dialog.dart`**
这个独立组件包含：
- 版主列表显示
- 添加版主功能（用户搜索 + 权限设置）
- 删除版主功能
- 更新版主权限功能

### 需要创建的新文件

#### 1. Repository 层
`lib/repositories/city_moderator_repository.dart`
- `getModerators(cityId)` - 获取版主列表
- `addModerator(cityId, userId, permissions)` - 添加版主
- `removeModerator(cityId, userId)` - 删除版主
- `updateModerator(cityId, moderatorId, permissions)` - 更新权限

#### 2. Repository 层（用户搜索）
`lib/repositories/user_repository.dart` 或扩展现有的用户仓储
- `searchUsers(query, role)` - 搜索用户

#### 3. UI 组件
`lib/pages/city_moderator_dialog.dart`
- `ModeratorManagementDialog` - 主对话框
- `ModeratorListItem` - 版主列表项
- `AddModeratorDialog` - 添加版主对话框
- `UserSearchField` - 用户搜索输入框

---

## API 使用示例

### 1. 获取城市版主列表
```dart
final response = await http.get(
  Uri.parse('${ApiConfig.currentApiBaseUrl}/api/v1/cities/$cityId/moderators'),
);

if (response.statusCode == 200) {
  final data = json.decode(response.body);
  final moderators = (data['data'] as List)
      .map((json) => CityModerator.fromJson(json))
      .toList();
}
```

### 2. 搜索用户
```dart
final response = await http.get(
  Uri.parse('${ApiConfig.currentApiBaseUrl}/api/v1/users/search?q=$searchTerm&page=1&pageSize=20'),
  headers: {
    'X-User-Id': userId,
    'Content-Type': 'application/json',
  },
);
```

### 3. 添加版主
```dart
final response = await http.post(
  Uri.parse('${ApiConfig.currentApiBaseUrl}/api/v1/cities/$cityId/moderators'),
  headers: {
    'X-User-Id': adminUserId,
    'X-User-Role': 'admin',
    'Content-Type': 'application/json',
  },
  body: json.encode({
    'userId': selectedUserId,
    'canEditCity': true,
    'canManageCoworks': true,
    'canManageCosts': true,
    'canManageVisas': true,
    'canModerateChats': true,
    'notes': '版主说明',
  }),
);
```

### 4. 删除版主
```dart
final response = await http.delete(
  Uri.parse('${ApiConfig.currentApiBaseUrl}/api/v1/cities/$cityId/moderators/$userId'),
  headers: {
    'X-User-Id': adminUserId,
    'X-User-Role': 'admin',
  },
);
```

---

## 数据模型

### CityModerator 模型
```dart
class CityModerator {
  final String id;
  final String cityId;
  final String userId;
  final ModeratorUser user;
  final bool canEditCity;
  final bool canManageCoworks;
  final bool canManageCosts;
  final bool canManageVisas;
  final bool canModerateChats;
  final String? assignedBy;
  final DateTime assignedAt;
  final bool isActive;
  final String? notes;
  final DateTime createdAt;
  final DateTime updatedAt;

  factory CityModerator.fromJson(Map<String, dynamic> json) {
    return CityModerator(
      id: json['id'],
      cityId: json['cityId'],
      userId: json['userId'],
      user: ModeratorUser.fromJson(json['user']),
      canEditCity: json['canEditCity'] ?? true,
      canManageCoworks: json['canManageCoworks'] ?? true,
      canManageCosts: json['canManageCosts'] ?? true,
      canManageVisas: json['canManageVisas'] ?? true,
      canModerateChats: json['canModerateChats'] ?? true,
      assignedBy: json['assignedBy'],
      assignedAt: DateTime.parse(json['assignedAt']),
      isActive: json['isActive'] ?? true,
      notes: json['notes'],
      createdAt: DateTime.parse(json['createdAt']),
      updatedAt: DateTime.parse(json['updatedAt']),
    );
  }
}
```

---

## 部署步骤

### 1. 执行数据库迁移
```bash
cd /Users/walden/Workspaces/WaldenProjects/go-noma/database
psql postgresql://postgres:gonoma2024@localhost:54322/postgres -f migrations/010_create_city_moderators_table.sql
```

### 2. 重新部署后端服务
```bash
cd /Users/walden/Workspaces/WaldenProjects/go-noma/deployment
./deploy-services-local.sh
```

### 3. 测试 API
```bash
# 测试用户搜索
curl -X GET "http://localhost:5001/api/v1/users/search?q=walden" \
  -H "X-User-Id: your-user-id"

# 测试获取版主列表
curl -X GET "http://localhost:5003/api/v1/cities/{cityId}/moderators"

# 测试添加版主
curl -X POST "http://localhost:5003/api/v1/cities/{cityId}/moderators" \
  -H "X-User-Id: admin-user-id" \
  -H "X-User-Role: admin" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "user-guid",
    "canEditCity": true,
    "canManageCoworks": true,
    "canManageCosts": true,
    "canManageVisas": true,
    "canModerateChats": true
  }'
```

---

## 后续优化建议

1. **用户信息获取**：当前版主列表返回时 User 信息是占位符，需要通过 Dapr 调用 UserService 获取完整用户信息

2. **权限检查优化**：在各个 Cowork/Cost/Visa API 中检查版主权限，不仅仅检查 admin

3. **通知系统**：版主被添加/移除时发送通知

4. **审计日志**：记录版主权限变更历史

5. **前端状态管理**：使用 GetX 或 Provider 管理版主列表状态

---

## 文件清单

### 后端文件（已完成）
- ✅ `database/migrations/010_create_city_moderators_table.sql`
- ✅ `CityService/Domain/Entities/CityModerator.cs`
- ✅ `CityService/Domain/Repositories/ICityModeratorRepository.cs`
- ✅ `CityService/Infrastructure/Repositories/CityModeratorRepository.cs`
- ✅ `CityService/Application/DTOs/CityDtos.cs` (新增 DTO)
- ✅ `CityService/API/Controllers/CitiesController.cs` (新增端点)
- ✅ `CityService/Program.cs` (DI 注册)
- ✅ `UserService/Domain/Repositories/IUserRepository.cs` (SearchAsync)
- ✅ `UserService/Infrastructure/Repositories/UserRepository.cs` (SearchAsync 实现)
- ✅ `UserService/Application/Services/IUserService.cs` (SearchUsersAsync)
- ✅ `UserService/Application/Services/UserApplicationService.cs` (SearchUsersAsync 实现)
- ✅ `UserService/API/Controllers/UsersController.cs` (搜索端点)

### Flutter 文件（待实现）
- ⏳ `lib/models/city_moderator.dart` - 数据模型
- ⏳ `lib/repositories/city_moderator_repository.dart` - 版主 API 调用
- ⏳ `lib/repositories/user_repository.dart` - 用户搜索 API（或扩展现有）
- ⏳ `lib/pages/city_moderator_dialog.dart` - 版主管理 UI
- ⏳ `lib/pages/city_detail_page.dart` - 集成版主管理按钮

---

## 完成时间
2025-11-14

## 作者
GitHub Copilot (Claude Sonnet 4.5)
