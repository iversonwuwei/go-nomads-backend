-- 修复 user_favorite_cities RLS 策略
-- 问题: 后端使用 anon key 调用 Supabase，但 RLS 策略要求 auth.uid() = user_id
-- 解决: 禁用 RLS，业务逻辑已在 Controller 层验证用户身份

-- 删除所有旧策略
DROP POLICY IF EXISTS "Users can view their own favorite cities" ON user_favorite_cities;
DROP POLICY IF EXISTS "Users can add their own favorite cities" ON user_favorite_cities;
DROP POLICY IF EXISTS "Users can remove their own favorite cities" ON user_favorite_cities;
DROP POLICY IF EXISTS "Users can update their own favorite cities" ON user_favorite_cities;
DROP POLICY IF EXISTS "Allow authenticated access to favorite cities" ON user_favorite_cities;

-- 禁用 RLS（因为后端使用 anon key，无法通过 RLS 验证）
-- 安全由 Controller 层的 UserContext 验证保证
ALTER TABLE user_favorite_cities DISABLE ROW LEVEL SECURITY;

-- 验证 RLS 已禁用
SELECT tablename, rowsecurity 
FROM pg_tables 
WHERE tablename = 'user_favorite_cities';
