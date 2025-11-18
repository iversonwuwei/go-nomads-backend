# 角色管理 API 测试指南

## 📋 概述

已为 UserService 添加完整的角色管理功能:

- ✅ 创建独立的 `roles` 表
- ✅ 添加默认角色: `user` 和 `admin`
- ✅ 在 `users` 表中添加 `role_id` 外键
- ✅ 实现完整的 CRUD API

## 🗄️ 数据库迁移

### 步骤 1: 在 Supabase 执行迁移

在 Supabase Dashboard → SQL Editor 中执行:

```sql
-- 创建角色表
CREATE TABLE IF NOT EXISTS public.roles (
    id VARCHAR(50) PRIMARY KEY,
    name VARCHAR(50) NOT NULL UNIQUE,
    description TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 插入默认角色
INSERT INTO public.roles (id, name, description) VALUES
    ('role_user', 'user', '普通用户角色'),
    ('role_admin', 'admin', '管理员角色')
ON CONFLICT (name) DO NOTHING;

-- 添加 role_id 字段到 users 表
ALTER TABLE public.users 
ADD COLUMN IF NOT EXISTS role_id VARCHAR(50) DEFAULT 'role_user';

-- 为现有用户设置 role_id
UPDATE public.users 
SET role_id = CASE 
    WHEN role = 'admin' THEN 'role_admin'
    ELSE 'role_user'
END
WHERE role_id IS NULL;

-- 添加外键约束
ALTER TABLE public.users
ADD CONSTRAINT fk_users_role_id 
FOREIGN KEY (role_id) 
REFERENCES public.roles(id)
ON DELETE SET NULL;

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_users_role_id ON public.users(role_id);

-- 启用 RLS
ALTER TABLE public.roles ENABLE ROW LEVEL SECURITY;

-- 角色表读取策略
CREATE POLICY "Allow read access to roles" ON public.roles
    FOR SELECT
    USING (true);
```

### 步骤 2: 验证迁移

```sql
-- 查看角色表
SELECT * FROM public.roles;

-- 查看用户表结构
\d public.users

-- 检查外键约束
SELECT 
    tc.constraint_name, 
    tc.table_name, 
    kcu.column_name, 
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name 
FROM 
    information_schema.table_constraints AS tc 
    JOIN information_schema.key_column_usage AS kcu
      ON tc.constraint_name = kcu.constraint_name
    JOIN information_schema.constraint_column_usage AS ccu
      ON ccu.constraint_name = tc.constraint_name
WHERE tc.table_name='users' AND tc.constraint_type='FOREIGN KEY';
```

## 🧪 API 测试

### 1. 获取所有角色

```bash
curl http://localhost:5001/api/roles
```

**预期响应:**

```json
{
  "success": true,
  "message": "Roles retrieved successfully",
  "data": [
    {
      "id": "role_user",
      "name": "user",
      "description": "普通用户角色",
      "createdAt": "2024-10-21T...",
      "updatedAt": "2024-10-21T..."
    },
    {
      "id": "role_admin",
      "name": "admin",
      "description": "管理员角色",
      "createdAt": "2024-10-21T...",
      "updatedAt": "2024-10-21T..."
    }
  ]
}
```

### 2. 根据ID获取角色

```bash
curl http://localhost:5001/api/roles/role_admin
```

### 3. 创建新角色

```bash
curl -X POST http://localhost:5001/api/roles \
  -H "Content-Type: application/json" \
  -d '{
    "name": "moderator",
    "description": "内容审核员角色"
  }'
```

**预期响应:**

```json
{
  "success": true,
  "message": "Role created successfully",
  "data": {
    "id": "role_moderator",
    "name": "moderator",
    "description": "内容审核员角色",
    "createdAt": "2024-10-21T...",
    "updatedAt": "2024-10-21T..."
  }
}
```

### 4. 更新角色

```bash
curl -X PUT http://localhost:5001/api/roles/role_moderator \
  -H "Content-Type: application/json" \
  -d '{
    "description": "高级内容审核员角色"
  }'
```

### 5. 删除角色

```bash
curl -X DELETE http://localhost:5001/api/roles/role_moderator
```

**注意:** 无法删除默认角色 (`role_user`, `role_admin`)

### 6. 创建用户 (使用 role_id)

```bash
curl -X POST http://localhost:5001/api/users \
  -H "Content-Type: application/json" \
  -d '{
    "name": "管理员",
    "email": "admin@example.com",
    "password": "admin123",
    "phone": "13800138000"
  }'
```

用户会自动获得默认的 `role_user` 角色 (通过 `role_id` 字段)

## 📁 创建的文件

### 模型 (Shared)

- `src/Shared/Shared/Models/Role.cs` - 角色模型

### Repository

- `src/Services/UserService/UserService/Repositories/IRoleRepository.cs` - 角色仓储接口
- `src/Services/UserService/UserService/Repositories/RoleRepository.cs` - 角色仓储实现

### Controller

- `src/Services/UserService/UserService/Controllers/RolesController.cs` - 角色管理 API

### Database

- `src/Services/UserService/UserService/Database/migrations/002_create_roles_table.sql` - 角色表迁移脚本
- `src/Services/UserService/UserService/Database/schema.sql` - 已更新完整架构

## 🔍 数据结构

### roles 表

```sql
Column       | Type                        | Default
-------------|-----------------------------|------------------
id           | VARCHAR(50)                 | PRIMARY KEY
name         | VARCHAR(50)                 | NOT NULL UNIQUE
description  | TEXT                        | NULL
created_at   | TIMESTAMP WITH TIME ZONE    | CURRENT_TIMESTAMP
updated_at   | TIMESTAMP WITH TIME ZONE    | CURRENT_TIMESTAMP
```

### users 表 (新增字段)

```sql
Column       | Type                        | Default
-------------|-----------------------------|------------------
role_id      | VARCHAR(50)                 | 'role_user'
             | FOREIGN KEY → roles(id)     |
```

## 🔐 角色常量

在代码中可以使用:

```csharp
// 使用角色 ID 常量
Role.RoleIds.User    // "role_user"
Role.RoleIds.Admin   // "role_admin"

// 使用角色名称常量
Role.RoleNames.User  // "user"
Role.RoleNames.Admin // "admin"
```

## ⚠️ 重要提醒

1. **外键约束**: `users.role_id` 必须引用 `roles.id` 中存在的值
2. **默认角色**: 新用户默认获得 `role_user` 角色
3. **不可删除**: 系统不允许删除 `role_user` 和 `role_admin`
4. **迁移顺序**: 必须先执行 `001_add_password_and_role.sql`，再执行 `002_create_roles_table.sql`
5. **向后兼容**: 保留了 `users.role` 字段以保持向后兼容，但推荐使用 `role_id`

## 📝 下一步建议

1. **更新 UserServiceImpl**: 在创建用户时根据需求设置 `role_id`
2. **添加授权**: 在 Controller 上添加 `[Authorize(Roles = "admin")]` 保护管理员操作
3. **角色验证**: 在业务逻辑中验证用户角色权限
4. **API 文档**: 更新 Swagger/Scalar 文档说明角色管理 API

## 🧹 清理 (可选)

如果确认 `role_id` 工作正常，可以删除旧的 `role` 字段:

```sql
-- 确认所有用户都有有效的 role_id
SELECT COUNT(*) FROM users WHERE role_id IS NULL;

-- 如果返回 0，可以安全删除旧字段
ALTER TABLE public.users DROP COLUMN IF EXISTS role;
```
