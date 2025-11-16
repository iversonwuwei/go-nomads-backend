# 指定版主功能优化 - 服务端角色分配

## 📋 需求概述

将"指定版主"功能优化为：
- **前端**：只需要选择用户，不需要加载和选择角色列表
- **后端**：在指定版主时，自动为用户分配 `moderator` 角色

## ✅ 完成的修改

### 1. 后端修改 (go-noma)

#### 1.1 CitiesController 修改

**文件**: `/Users/walden/Workspaces/WaldenProjects/go-noma/src/Services/CityService/CityService/API/Controllers/CitiesController.cs`

**修改内容**:
在 `AddCityModerator` 方法中添加了自动分配角色的逻辑：

```csharp
// 1. 首先获取 moderator 角色
var roleResponse = await _daprClient.InvokeMethodAsync<ApiResponse<SimpleRoleDto>>(
    HttpMethod.Get,
    "user-service",
    "api/v1/roles/by-name/moderator");

// 2. 为用户分配 moderator 角色
var changeRoleRequest = new { roleId = moderatorRoleId };
var changeRoleResponse = await _daprClient.InvokeMethodAsync<object, ApiResponse<SimpleUserDto>>(
    HttpMethod.Patch,
    "user-service",
    $"api/v1/users/{dto.UserId}/role",
    changeRoleRequest);

// 3. 创建版主记录
var moderator = new CityModerator { ... };
var added = await _moderatorRepository.AddAsync(moderator);
```

**新增 DTO 类**:
```csharp
/// <summary>
/// 简单的用户 DTO - 用于 Dapr 服务间调用
/// </summary>
public class SimpleUserDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

/// <summary>
/// 简单的角色 DTO - 用于 Dapr 服务间调用
/// </summary>
public class SimpleRoleDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
```

#### 1.2 数据库迁移

**文件**: `/Users/walden/Workspaces/WaldenProjects/go-noma/src/Services/UserService/UserService/Database/migrations/003_add_moderator_role.sql`

**内容**:
```sql
-- 添加 moderator 角色
INSERT INTO public.roles (id, name, description) VALUES
    ('role_moderator', 'moderator', '城市版主角色 - 可以管理特定城市的内容')
ON CONFLICT (name) DO NOTHING;
```

### 2. 工作流程

#### 旧流程（前端需要选择角色）:
1. 前端加载所有角色列表 → `GET /api/v1/roles`
2. 用户选择要分配的角色
3. 提交用户ID和角色ID到后端
4. 后端创建版主记录

#### 新流程（服务端自动分配角色）:
1. 前端只需要选择用户
2. 提交用户ID到后端 → `POST /api/v1/cities/{id}/moderators`
3. **后端自动**：
   - 获取 `moderator` 角色 → `GET /api/v1/roles/by-name/moderator`
   - 为用户分配角色 → `PATCH /api/v1/users/{userId}/role`
   - 创建版主记录

### 3. API 调用链

```
CityService (POST /api/v1/cities/{id}/moderators)
    ↓ [Dapr]
UserService (GET /api/v1/roles/by-name/moderator)
    ↓ 获取 role_moderator ID
CityService
    ↓ [Dapr]
UserService (PATCH /api/v1/users/{userId}/role)
    ↓ 更新用户角色
CityService
    ↓
CityModeratorRepository (创建版主记录)
```

## 🔧 部署步骤

### 1. 执行数据库迁移

在 Supabase Dashboard → SQL Editor 中执行修复脚本：

**如果遇到错误: `foreign key constraint cannot be implemented - incompatible types`**

这说明 `users.role_id` 和 `roles.id` 字段类型不一致。请执行完整的修复脚本：

```sql
-- 修复字段类型不匹配并添加 moderator 角色

-- Step 1: 删除外键约束
ALTER TABLE public.users DROP CONSTRAINT IF EXISTS fk_users_role_id;
ALTER TABLE public.users DROP CONSTRAINT IF EXISTS users_role_id_fkey;

-- Step 2: 统一修改两个字段为 VARCHAR(50)
ALTER TABLE public.roles ALTER COLUMN id TYPE VARCHAR(50);

-- Step 3: 插入基础角色（确保存在）
INSERT INTO public.roles (id, name, description) VALUES
    ('role_user', 'user', '普通用户角色'),
    ('role_admin', 'admin', '管理员角色')
ON CONFLICT (id) DO UPDATE 
SET name = EXCLUDED.name, description = EXCLUDED.description;

-- Step 4: 修改 users.role_id 类型并设置默认值
ALTER TABLE public.users ALTER COLUMN role_id TYPE VARCHAR(50);
ALTER TABLE public.users ALTER COLUMN role_id SET DEFAULT 'role_user';

-- Step 5: 更新现有用户的 role_id
UPDATE public.users 
SET role_id = 'role_user'
WHERE role_id IS NOT NULL 
  AND role_id NOT IN ('role_user', 'role_admin', 'role_moderator');

UPDATE public.users 
SET role_id = 'role_user'
WHERE role_id IS NULL;

-- Step 6: 重新创建外键约束
ALTER TABLE public.users
ADD CONSTRAINT fk_users_role_id 
FOREIGN KEY (role_id) 
REFERENCES public.roles(id)
ON DELETE SET NULL;

-- Step 7: 插入 moderator 角色
INSERT INTO public.roles (id, name, description) VALUES
    ('role_moderator', 'moderator', '城市版主角色 - 可以管理特定城市的内容')
ON CONFLICT (id) DO UPDATE 
SET name = EXCLUDED.name, description = EXCLUDED.description;

-- Step 8: 验证结果
SELECT * FROM public.roles ORDER BY name;
```

**如果没有错误（正常情况）**，只需执行：

```sql
INSERT INTO public.roles (id, name, description) VALUES
    ('role_moderator', 'moderator', '城市版主角色 - 可以管理特定城市的内容')
ON CONFLICT (name) DO NOTHING;

-- 验证角色已创建
SELECT * FROM public.roles WHERE name = 'moderator';
```

### 2. 重新部署 CityService

```bash
cd src/Services/CityService
dotnet build
dotnet run
```

### 3. 验证功能

```bash
# 1. 验证 moderator 角色存在
curl http://localhost:5001/api/v1/roles/by-name/moderator

# 2. 测试添加版主（需要管理员权限）
curl -X POST http://localhost:5003/api/v1/cities/{cityId}/moderators \
  -H "Authorization: Bearer {admin_token}" \
  -H "Content-Type: application/json" \
  -d '{
    "cityId": "...",
    "userId": "...",
    "canEditCity": true,
    "canManageCoworks": true,
    "canManageCosts": true,
    "canManageVisas": true,
    "canModerateChats": true,
    "notes": "测试版主"
  }'
```

## 📝 前端影响

### 修改前
前端需要：
1. 调用 `/api/v1/roles` 获取角色列表
2. 在 UI 中显示角色选择器
3. 提交时包含 `roleId`

### 修改后
前端只需要：
1. ~~调用 `/api/v1/roles` 获取角色列表~~ ❌ 不需要了
2. ~~在 UI 中显示角色选择器~~ ❌ 不需要了
3. 提交时只需要 `userId` 和其他权限设置

**简化的前端代码示例**:
```dart
// 旧代码
Future<Result<bool>> assignModerator({
  required String cityId,
  required String userId,
  required String roleId,  // ❌ 不需要了
}) async {
  ...
}

// 新代码
Future<Result<bool>> assignModerator({
  required String cityId,
  required String userId,
  // roleId 参数已移除
}) async {
  // 后端会自动分配 moderator 角色
  ...
}
```

## ✨ 优势

1. **简化前端逻辑**: 不需要加载和管理角色列表
2. **减少网络请求**: 少一次 API 调用
3. **更好的用户体验**: 用户界面更简洁
4. **更安全**: 角色分配逻辑由服务端控制
5. **易于维护**: 角色变更只需要修改后端

## 🔐 安全考虑

1. ✅ 只有管理员可以指定版主（通过 `[Authorize]` 和角色检查）
2. ✅ 服务端验证城市是否存在
3. ✅ 服务端验证用户是否已经是版主
4. ✅ 使用 Dapr 服务间调用保证安全性
5. ✅ 角色分配通过 UserService 统一管理

## 📦 相关文件

### 后端
- `/go-noma/src/Services/CityService/CityService/API/Controllers/CitiesController.cs`
- `/go-noma/src/Services/UserService/UserService/Database/migrations/003_add_moderator_role.sql`

### 文档
- `/go-noma/CITY_MODERATOR_ROLE_AUTO_ASSIGN.md` (本文件)

## 🎯 下一步

- [ ] 更新前端代码，移除角色选择相关逻辑
- [ ] 更新 API 文档
- [ ] 添加单元测试
- [ ] 考虑添加"移除版主"时是否需要自动移除 moderator 角色

## 📅 更新时间

2025年11月16日

## 👤 作者

GitHub Copilot
