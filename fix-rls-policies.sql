-- 修复 Supabase RLS 策略以允许 anon 和 authenticated 角色访问
-- 这是为了让后端服务能够使用 anon key 进行数据库操作

-- ====================================
-- Users 表策略
-- ====================================

-- 删除旧的 service_role 策略(如果存在)
DROP POLICY IF EXISTS "Enable insert for service role" ON users;
DROP POLICY IF EXISTS "Enable select for service role" ON users;
DROP POLICY IF EXISTS "Enable update for service role" ON users;
DROP POLICY IF EXISTS "Enable delete for service role" ON users;

-- 为 anon 和 authenticated 角色添加完整的 CRUD 权限
CREATE POLICY "Enable insert for anon" ON users
    FOR INSERT TO anon, authenticated
    WITH CHECK (true);

CREATE POLICY "Enable select for anon" ON users
    FOR SELECT TO anon, authenticated
    USING (true);

CREATE POLICY "Enable update for anon" ON users
    FOR UPDATE TO anon, authenticated
    USING (true);

CREATE POLICY "Enable delete for anon" ON users
    FOR DELETE TO anon, authenticated
    USING (true);

-- ====================================
-- Roles 表策略
-- ====================================

-- 确保 RLS 已启用
ALTER TABLE roles ENABLE ROW LEVEL SECURITY;

-- 删除旧策略(如果存在)
DROP POLICY IF EXISTS "service_role_select_roles" ON roles;
DROP POLICY IF EXISTS "service_role_insert_roles" ON roles;

-- 为 roles 表添加查询权限(所有人都可以查询角色)
CREATE POLICY "Enable select for all" ON roles
    FOR SELECT TO anon, authenticated, service_role
    USING (true);

-- 只允许 service_role 插入/更新/删除角色
CREATE POLICY "Enable insert for service_role" ON roles
    FOR INSERT TO service_role
    WITH CHECK (true);

CREATE POLICY "Enable update for service_role" ON roles
    FOR UPDATE TO service_role
    USING (true);

CREATE POLICY "Enable delete for service_role" ON roles
    FOR DELETE TO service_role
    USING (true);

-- ====================================
-- 验证策略
-- ====================================

-- 查看 users 表的策略
SELECT schemaname, tablename, policyname, roles, cmd
FROM pg_policies
WHERE tablename = 'users';

-- 查看 roles 表的策略
SELECT schemaname, tablename, policyname, roles, cmd
FROM pg_policies
WHERE tablename = 'roles';
