# Innovation 模块 API 集成完成

## 概述

本次更新完成了创意项目（Innovation）模块从静态数据到真实后端 API 的完整集成。

## 后端变更

### 1. InnovationsController.cs (新建)
位置: `src/Services/InnovationService/InnovationService/Controllers/InnovationsController.cs`

API 端点:
- `GET /api/innovations` - 获取项目列表（支持分页、筛选、搜索）
- `GET /api/innovations/popular` - 获取热门项目
- `GET /api/innovations/featured` - 获取精选项目
- `GET /api/innovations/my` - 获取当前用户的项目
- `GET /api/innovations/user/{userId}` - 获取指定用户的项目
- `GET /api/innovations/{id}` - 获取项目详情
- `POST /api/innovations` - 创建新项目
- `PUT /api/innovations/{id}` - 更新项目
- `DELETE /api/innovations/{id}` - 删除项目
- `POST /api/innovations/{id}/like` - 点赞/取消点赞
- `GET /api/innovations/{id}/comments` - 获取评论
- `POST /api/innovations/{id}/comments` - 添加评论
- `DELETE /api/innovations/comments/{commentId}` - 删除评论
- `GET /api/innovations/{id}/team` - 获取团队成员
- `POST /api/innovations/{id}/team` - 添加团队成员
- `DELETE /api/innovations/{id}/team/{memberId}` - 移除团队成员
- `PUT /api/innovations/{id}/visibility` - 更新可见性

### 2. Program.cs (更新)
- 配置 Supabase 客户端
- 配置 JWT Bearer 认证
- 注册 Repository 依赖注入
- 配置 CORS 策略

### 3. InnovationService.csproj (更新)
新增依赖:
- Microsoft.AspNetCore.Authentication.JwtBearer
- postgrest-csharp
- supabase-csharp
- Serilog.AspNetCore

### 4. 数据库迁移 (更新)
位置: `migrations/20241225_extend_innovations_table.sql`

新增表:
- `innovation_likes` - 点赞记录
- `innovation_comments` - 评论记录

新增功能:
- RLS 安全策略
- updated_at 触发器
- 适当的索引

## Flutter 变更

### 1. InnovationProjectDto (重写)
位置: `lib/features/innovation_project/infrastructure/dtos/innovation_project_dto.dart`

变更:
- ID 类型从 `int` 改为 `String` (UUID)
- 字段名从 `projectName` 改为 `title`
- 新增字段: category, stage, tags, imageUrl 等
- 新增 `InnovationListItemDto` 用于列表显示
- 新增 `CreateInnovationRequest` 用于创建项目

### 2. InnovationProjectRepository (重写)
位置: `lib/features/innovation_project/infrastructure/repositories/innovation_project_repository.dart`

变更:
- API 路径从 `/innovation-projects` 改为 `/innovations`
- 响应解析支持 `ApiResponse<T>` 格式
- 所有 ID 参数从 `int` 改为 `String`
- 新增 `getMyProjects()`, `getFeaturedProjects()` 方法

### 3. IInnovationProjectRepository (更新)
所有方法签名中的 `int projectId` 改为 `String projectId`

### 4. Use Cases (重写)
- 所有 ID 类型改为 `String`
- 新增 `GetMyProjectsUseCase`, `GetFeaturedProjectsUseCase`

### 5. InnovationProjectStateController (更新)
- 方法参数类型更新
- 新增 `getMyProjects()`, `getFeaturedProjects()` 方法

### 6. InnovationListPage (更新)
- 移除静态数据
- 集成 GetX Controller
- 添加加载状态和错误处理
- 支持下拉刷新

## 部署步骤

### 1. 执行数据库迁移
```sql
-- 在 Supabase SQL Editor 中执行
-- migrations/20241225_extend_innovations_table.sql
```

### 2. 启动后端服务
```bash
cd src/Services/InnovationService/InnovationService
dotnet run
```

服务端口: 8006

### 3. 测试 API
```bash
# 获取项目列表
curl http://localhost:8006/api/innovations

# 获取项目详情
curl http://localhost:8006/api/innovations/{id}

# 创建项目 (需要认证)
curl -X POST http://localhost:8006/api/innovations \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "测试项目",
    "elevatorPitch": "这是一个测试项目"
  }'
```

### 4. 运行 Flutter 应用
```bash
cd open-platform-app
flutter run
```

## API 响应格式

所有 API 响应遵循统一格式:

```json
{
  "success": true,
  "data": { ... },
  "message": null
}
```

错误响应:
```json
{
  "success": false,
  "data": null,
  "message": "错误信息"
}
```

## 注意事项

1. **认证**: 创建、更新、删除操作需要 JWT Bearer Token
2. **权限**: 只有项目创建者可以编辑/删除项目
3. **RLS**: 数据库层面有行级安全策略保护
4. **备用数据**: Flutter 端保留静态数据作为 API 失败时的备用方案

## 后续优化建议

1. 添加分页支持到列表页面
2. 实现项目搜索功能
3. 添加项目分类筛选
4. 实现点赞功能 UI
5. 实现评论功能 UI
6. 添加团队成员管理 UI
