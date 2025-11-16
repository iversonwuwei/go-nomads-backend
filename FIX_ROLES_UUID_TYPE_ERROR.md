# 修复 roles 和 users 表字段类型不匹配错误 - 快速指南

## 问题

执行外键约束时遇到错误：

```
ERROR: 42804: foreign key constraint "users_role_id_fkey" cannot be implemented
DETAIL: Key columns "role_id" and "id" are of incompatible types: uuid and character varying.
```

## 原因

- `users` 表的 `role_id` 字段是 **UUID** 类型
- `roles` 表的 `id` 字段是 **VARCHAR(50)** 类型
- 两者类型不匹配，无法创建外键约束

## 解决方案

在 Supabase Dashboard → SQL Editor 中执行以下完整脚本：

```sql
-- 修复 roles 和 users 表字段类型不匹配问题并添加 moderator 角色
-- Date: 2025-11-16

-- Step 1: 删除外键约束（如果存在）
ALTER TABLE public.users DROP CONSTRAINT IF EXISTS fk_users_role_id;
ALTER TABLE public.users DROP CONSTRAINT IF EXISTS users_role_id_fkey;

-- Step 2: 修改 roles 表的 id 字段类型为 VARCHAR(50)
ALTER TABLE public.roles ALTER COLUMN id TYPE VARCHAR(50);

-- Step 3: 插入基础角色（确保存在）
INSERT INTO public.roles (id, name, description) VALUES
    ('role_user', 'user', '普通用户角色'),
    ('role_admin', 'admin', '管理员角色')
ON CONFLICT (id) DO UPDATE 
SET name = EXCLUDED.name, 
    description = EXCLUDED.description;

-- Step 4: 修改 users 表的 role_id 字段类型为 VARCHAR(50)
ALTER TABLE public.users ALTER COLUMN role_id TYPE VARCHAR(50);

-- Step 5: 设置默认值
ALTER TABLE public.users ALTER COLUMN role_id SET DEFAULT 'role_user';

-- Step 6: 更新现有用户的 role_id
-- 将任何非标准格式的 role_id 更新为 'role_user'
UPDATE public.users 
SET role_id = 'role_user'
WHERE role_id IS NOT NULL 
  AND role_id NOT IN ('role_user', 'role_admin', 'role_moderator');

-- 为 NULL 的 role_id 设置默认值
UPDATE public.users 
SET role_id = 'role_user'
WHERE role_id IS NULL;

-- Step 7: 重新创建外键约束
ALTER TABLE public.users
ADD CONSTRAINT fk_users_role_id 
FOREIGN KEY (role_id) 
REFERENCES public.roles(id)
ON DELETE SET NULL;

-- Step 8: 插入 moderator 角色
INSERT INTO public.roles (id, name, description) VALUES
    ('role_moderator', 'moderator', '城市版主角色 - 可以管理特定城市的内容')
ON CONFLICT (id) DO UPDATE 
SET name = EXCLUDED.name, 
    description = EXCLUDED.description;

-- Step 9: 验证结果
SELECT * FROM public.roles ORDER BY name;
SELECT role_id, COUNT(*) as user_count 
FROM public.users 
GROUP BY role_id 
ORDER BY user_count DESC;
```

## 验证

执行后应该看到：

1. **字段类型验证**：
   - `users.role_id` 类型: `character varying (50)`
   - `roles.id` 类型: `character varying (50)`

2. **角色列表**：

   | id | name | description |
   |---|---|---|
   | role_admin | admin | 管理员角色 |
   | role_moderator | moderator | 城市版主角色 - 可以管理特定城市的内容 |
   | role_user | user | 普通用户角色 |

3. **用户角色分布**：
   - 所有用户的 `role_id` 应该都是有效的角色 ID（role_user, role_admin, role_moderator）

## 后续测试

```bash
# 测试获取 moderator 角色
curl http://localhost:5001/api/v1/roles/by-name/moderator

# 预期返回
{
  "success": true,
  "data": {
    "id": "role_moderator",
    "name": "moderator",
    "description": "城市版主角色 - 可以管理特定城市的内容"
  }
}
```

## 相关文件

- 修复脚本：`/go-noma/src/Services/UserService/UserService/Database/migrations/003_fix_and_add_moderator_role.sql`
- 完整文档：`/go-noma/CITY_MODERATOR_ROLE_AUTO_ASSIGN.md`
