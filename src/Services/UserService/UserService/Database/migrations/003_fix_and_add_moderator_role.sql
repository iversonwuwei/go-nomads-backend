-- 修复 roles 和 users 表字段类型不匹配问题并添加 moderator 角色
-- Date: 2025-11-16
-- 
-- 问题: roles.id 和 users.role_id 字段类型不一致，导致无法创建外键
-- 解决: 统一将两个字段都修改为 VARCHAR(50) 类型

-- ============================================
-- Step 1: 删除外键约束（如果存在）
-- ============================================

ALTER TABLE public.users DROP CONSTRAINT IF EXISTS fk_users_role_id;
ALTER TABLE public.users DROP CONSTRAINT IF EXISTS users_role_id_fkey;

-- ============================================
-- Step 2: 修改 roles 表的 id 字段类型为 VARCHAR(50)
-- ============================================

-- 修改 roles.id 字段类型
-- 如果 id 中有 UUID 格式的数据，会自动转换为字符串
ALTER TABLE public.roles ALTER COLUMN id TYPE VARCHAR(50);

-- ============================================
-- Step 3: 插入基础角色（确保这些角色存在）
-- ============================================

-- 插入或更新默认角色
INSERT INTO public.roles (id, name, description) VALUES
    ('role_user', 'user', '普通用户角色'),
    ('role_admin', 'admin', '管理员角色')
ON CONFLICT (id) DO UPDATE 
SET name = EXCLUDED.name, 
    description = EXCLUDED.description,
    updated_at = CURRENT_TIMESTAMP;

-- ============================================
-- Step 4: 修改 users 表的 role_id 字段类型为 VARCHAR(50)
-- ============================================

-- 修改 users.role_id 字段类型
ALTER TABLE public.users ALTER COLUMN role_id TYPE VARCHAR(50);

-- 设置默认值
ALTER TABLE public.users ALTER COLUMN role_id SET DEFAULT 'role_user';

-- ============================================
-- Step 5: 更新现有用户的 role_id（如果需要）
-- ============================================

-- 将任何非标准格式的 role_id 更新为 'role_user'
UPDATE public.users 
SET role_id = 'role_user'
WHERE role_id IS NOT NULL 
  AND role_id NOT IN ('role_user', 'role_admin', 'role_moderator');

-- 为 NULL 的 role_id 设置默认值
UPDATE public.users 
SET role_id = 'role_user'
WHERE role_id IS NULL;

-- ============================================
-- Step 6: 重新创建外键约束
-- ============================================

ALTER TABLE public.users
ADD CONSTRAINT fk_users_role_id 
FOREIGN KEY (role_id) 
REFERENCES public.roles(id)
ON DELETE SET NULL;

-- ============================================
-- Step 7: 插入 moderator 角色
-- ============================================

INSERT INTO public.roles (id, name, description) VALUES
    ('role_moderator', 'moderator', '城市版主角色 - 可以管理特定城市的内容')
ON CONFLICT (id) DO UPDATE 
SET name = EXCLUDED.name, 
    description = EXCLUDED.description,
    updated_at = CURRENT_TIMESTAMP;

-- ============================================
-- Step 8: 验证结果
-- ============================================

-- 验证 users 表的 role_id 字段类型
SELECT 
    column_name, 
    data_type, 
    character_maximum_length
FROM information_schema.columns
WHERE table_schema = 'public' 
  AND table_name = 'users' 
  AND column_name = 'role_id';

-- 验证 roles 表的 id 字段类型
SELECT 
    column_name, 
    data_type, 
    character_maximum_length
FROM information_schema.columns
WHERE table_schema = 'public' 
  AND table_name = 'roles' 
  AND column_name = 'id';

-- 验证所有角色
SELECT * FROM public.roles ORDER BY name;

-- 验证用户的 role_id 分布
SELECT role_id, COUNT(*) as user_count 
FROM public.users 
GROUP BY role_id 
ORDER BY user_count DESC;

-- 预期结果:
-- 1. users.role_id 和 roles.id 都应该是 character varying (VARCHAR)
-- 2. character_maximum_length 都应该是 50
-- 3. 应该有三个角色: admin, moderator, user
-- 4. 所有用户的 role_id 都应该是有效的角色 ID
