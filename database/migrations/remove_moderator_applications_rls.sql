-- =========================================
-- 删除 moderator_applications 表的 RLS
-- =========================================

-- 删除所有 RLS 策略
DROP POLICY IF EXISTS "Users can view own applications" ON moderator_applications;
DROP POLICY IF EXISTS "Users can create applications" ON moderator_applications;
DROP POLICY IF EXISTS "Admins can view all applications" ON moderator_applications;
DROP POLICY IF EXISTS "Admins can update applications" ON moderator_applications;

-- 禁用 RLS
ALTER TABLE moderator_applications DISABLE ROW LEVEL SECURITY;

-- 验证 RLS 已禁用
SELECT 
    schemaname,
    tablename,
    rowsecurity as rls_enabled
FROM pg_tables 
WHERE tablename = 'moderator_applications';
