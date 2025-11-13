# 通知系统后端实现完成

## 📋 概述

已在 MessageService 中完成通知系统的后端实现，支持版主申请审批、系统消息等通知功能。

## 🎯 已完成的文件

### 1. Domain 层
- ✅ `Domain/Entities/Notification.cs` - 通知实体
- ✅ `Domain/Repositories/INotificationRepository.cs` - 通知仓储接口

### 2. Application 层
- ✅ `Application/DTOs/NotificationDto.cs` - 通知DTO（包含5个DTO类）
- ✅ `Application/Services/INotificationService.cs` - 通知服务接口
- ✅ `Application/Services/NotificationApplicationService.cs` - 通知服务实现

### 3. Infrastructure 层
- ✅ `Infrastructure/Repositories/NotificationRepository.cs` - 通知仓储实现（Supabase）

### 4. API 层
- ✅ `API/Controllers/NotificationsController.cs` - 通知控制器（8个端点）
- ✅ `API/Program.cs` - 依赖注入注册

### 5. Database 层
- ✅ `database/migrations/create-notifications-table.sql` - 数据库迁移SQL
- ✅ `database/migrations/execute-notifications-migration.sh` - 迁移执行脚本

## 📡 API 端点列表

### 1. GET `/api/v1/notifications`
获取用户通知列表（支持筛选）

**查询参数：**
- `userId` (必需) - 用户ID
- `isRead` (可选) - 筛选已读/未读
- `page` (可选) - 页码，默认1
- `pageSize` (可选) - 每页数量，默认20

**响应：**
```json
{
  "success": true,
  "message": "通知列表获取成功",
  "data": {
    "notifications": [...],
    "totalCount": 100,
    "page": 1,
    "pageSize": 20
  }
}
```

### 2. GET `/api/v1/notifications/unread/count`
获取未读通知数量

**查询参数：**
- `userId` (必需) - 用户ID

**响应：**
```json
{
  "success": true,
  "message": "未读数量获取成功",
  "data": {
    "unreadCount": 5
  }
}
```

### 3. POST `/api/v1/notifications`
创建通知

**请求体：**
```json
{
  "userId": "user-123",
  "title": "新通知",
  "message": "这是一条测试通知",
  "type": "system_announcement",
  "relatedId": "city-456",
  "metadata": {
    "cityName": "成都"
  }
}
```

### 4. POST `/api/v1/notifications/admins`
发送通知给所有管理员

**请求体：**
```json
{
  "title": "新的版主申请",
  "message": "用户申请成为成都的版主，请及时审核",
  "type": "moderator_application",
  "relatedId": "city-456",
  "metadata": {
    "cityName": "成都",
    "cityId": "city-456"
  }
}
```

### 5. PUT `/api/v1/notifications/{id}/read`
标记通知为已读

**路径参数：**
- `id` - 通知ID

### 6. PUT `/api/v1/notifications/read/batch`
批量标记通知为已读

**查询参数：**
- `userId` (必需) - 用户ID

**请求体：**
```json
{
  "notificationIds": ["id1", "id2", "id3"]
}
```

### 7. PUT `/api/v1/notifications/read/all`
标记所有通知为已读

**查询参数：**
- `userId` (必需) - 用户ID

### 8. DELETE `/api/v1/notifications/{id}`
删除通知

**路径参数：**
- `id` - 通知ID

## 🗄️ 数据库结构

### notifications 表

| 字段 | 类型 | 说明 |
|------|------|------|
| id | UUID | 主键 |
| user_id | TEXT | 接收用户ID |
| title | TEXT | 通知标题 |
| message | TEXT | 通知消息内容 |
| type | TEXT | 通知类型（6种） |
| related_id | TEXT | 关联资源ID |
| metadata | JSONB | 元数据（JSON） |
| is_read | BOOLEAN | 是否已读 |
| created_at | TIMESTAMP | 创建时间 |
| read_at | TIMESTAMP | 阅读时间 |

### 通知类型

1. `moderator_application` - 版主申请
2. `moderator_approved` - 版主批准
3. `moderator_rejected` - 版主拒绝
4. `city_update` - 城市更新
5. `system_announcement` - 系统公告
6. `other` - 其他

### 索引

- `idx_notifications_user_id` - 用户ID索引
- `idx_notifications_user_unread` - 未读通知索引
- `idx_notifications_created_at` - 创建时间索引
- `idx_notifications_user_created` - 用户+创建时间组合索引
- `idx_notifications_type` - 类型索引

### RPC 函数

- `get_admin_user_ids()` - 获取所有管理员用户ID列表

### 触发器

- `trigger_set_notification_read_at` - 自动设置read_at时间

### RLS 策略

1. 用户只能查看自己的通知
2. 用户只能更新自己的通知
3. 用户只能删除自己的通知
4. 服务端可以插入任何通知

## 🚀 部署步骤

### 1. 执行数据库迁移

```bash
cd /Users/walden/Workspaces/WaldenProjects/go-noma

# 设置数据库密码
export SUPABASE_DB_PASSWORD="your-password"

# 执行迁移
./database/migrations/execute-notifications-migration.sh
```

或者手动执行：

```bash
psql -h db.lcfbajrocmjlqndkrsao.supabase.co \
     -p 5432 \
     -U postgres \
     -d postgres \
     -f database/migrations/create-notifications-table.sql
```

### 2. 配置 Supabase 客户端

确保 MessageService 的 `appsettings.json` 中配置了 Supabase：

```json
{
  "Supabase": {
    "Url": "https://lcfbajrocmjlqndkrsao.supabase.co",
    "Key": "your-service-role-key"
  }
}
```

### 3. 添加 NuGet 包引用

MessageService 需要以下包：

```bash
cd src/Services/MessageService/MessageService
dotnet add package supabase-csharp
dotnet add package Postgrest
```

### 4. 构建和运行

```bash
cd src/Services/MessageService/MessageService/API
dotnet build
dotnet run
```

## 🧪 测试端点

### 测试发送通知给管理员

```bash
curl -X POST http://localhost:5005/api/v1/notifications/admins \
  -H "Content-Type: application/json" \
  -d '{
    "title": "新的版主申请",
    "message": "用户申请成为成都的版主，请及时审核",
    "type": "moderator_application",
    "relatedId": "city-123",
    "metadata": {
      "cityName": "成都",
      "cityId": "city-123"
    }
  }'
```

### 测试获取通知列表

```bash
curl "http://localhost:5005/api/v1/notifications?userId=admin-user-id&page=1&pageSize=20"
```

### 测试获取未读数量

```bash
curl "http://localhost:5005/api/v1/notifications/unread/count?userId=admin-user-id"
```

## ⚠️ 注意事项

### 1. 编译错误处理

当前存在一些编译错误，主要原因：

- ✅ 缺少 `Postgrest.Attributes` 和 `Postgrest.Models` using
- ✅ `Notification` 实体需要继承 `BaseModel`
- ✅ 需要添加 `[Table]` 和 `[Column]` 属性

**解决方案：**

需要在项目中添加 Supabase 和 Postgrest NuGet 包：

```bash
cd src/Services/MessageService/MessageService/Domain
dotnet add package Postgrest

cd ../Infrastructure
dotnet add package Postgrest
dotnet add package supabase-csharp
```

### 2. 管理员用户识别

RPC 函数 `get_admin_user_ids()` 假设：
- users 表在 `auth.users`
- 管理员角色存储在 `raw_user_meta_data->>'role'`
- 角色值为 `'admin'` 或 `'administrator'`

如果实际结构不同，需要修改 SQL 中的函数定义。

### 3. RLS 策略

- 客户端调用需要使用 `anon key`（受RLS限制）
- 后端服务调用应使用 `service_role key`（绕过RLS）

### 4. Flutter 前端集成

前端需要更新 API 调用：

```dart
// NotificationRepository 中的 API 端点
static const String baseUrl = 'http://localhost:5005/api/v1/notifications';

// 或通过 Gateway
static const String baseUrl = 'http://localhost:7000/api/v1/notifications';
```

## 📝 下一步

1. **添加 NuGet 包** - 解决编译错误
2. **执行数据库迁移** - 创建表和函数
3. **测试 API** - 验证所有端点
4. **集成 Gateway** - 配置路由转发
5. **Flutter 集成** - 更新前端 API 调用

## 🔗 相关文档

- [NOTIFICATION_SYSTEM_INTEGRATION_GUIDE.md](../../open-platform-app/NOTIFICATION_SYSTEM_INTEGRATION_GUIDE.md) - Flutter 前端集成指南
- [NOTIFICATION_SYSTEM_SUMMARY.md](../../open-platform-app/NOTIFICATION_SYSTEM_SUMMARY.md) - 系统总结

## ✅ 完成清单

- [x] 创建 Notification 实体
- [x] 创建通知 DTO
- [x] 创建通知仓储接口和实现
- [x] 创建通知服务接口和实现
- [x] 创建通知控制器（8个端点）
- [x] 注册依赖注入
- [x] 创建数据库迁移 SQL
- [x] 创建迁移执行脚本
- [ ] 添加 NuGet 包引用
- [ ] 执行数据库迁移
- [ ] 测试 API 端点
- [ ] 配置 Gateway 路由
- [ ] 更新 Flutter 前端 API 调用
