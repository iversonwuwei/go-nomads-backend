-- 添加 moderator 角色
-- 用于城市版主功能
-- Date: 2025-11-16

-- 方案 1: 如果 roles 表的 id 字段是 VARCHAR 类型（正确的）
-- 直接插入即可
INSERT INTO public.roles (id, name, description)
VALUES ('role_moderator', 'moderator', '城市版主角色 - 可以管理特定城市的内容')
ON CONFLICT
    (name)
    DO NOTHING;

-- 如果上面的语句报错 "invalid input syntax for type uuid"
-- 说明 roles 表的 id 字段被错误地设置为 UUID 类型
-- 请按照下面的步骤修复：

/*
-- 方案 2: 修复 roles 表的 id 字段类型（如果需要）

-- Step 1: 删除外键约束
ALTER TABLE public.users DROP CONSTRAINT IF EXISTS fk_users_role_id;

-- Step 2: 修改 roles 表的 id 字段类型
ALTER TABLE public.roles ALTER COLUMN id TYPE VARCHAR(50);

-- Step 3: 重新创建外键约束
ALTER TABLE public.users
ADD CONSTRAINT fk_users_role_id 
FOREIGN KEY (role_id) 
REFERENCES public.roles(id)
ON DELETE SET NULL;

-- Step 4: 插入 moderator 角色
INSERT INTO public.roles (id, name, description) VALUES
    ('role_moderator', 'moderator', '城市版主角色 - 可以管理特定城市的内容')
ON CONFLICT (name) DO NOTHING;
*/

-- 验证角色已创建
SELECT *
FROM public.roles
WHERE name = 'moderator';
