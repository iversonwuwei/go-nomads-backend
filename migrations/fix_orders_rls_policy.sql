-- =============================================
-- 修复 orders 表的行级安全策略 (RLS)
-- 运行此脚本前请确保已连接到正确的 Supabase 数据库
-- =============================================

-- 1. 首先检查 RLS 是否已启用
-- SELECT relname, relrowsecurity FROM pg_class WHERE relname = 'orders';

-- 2. 禁用 RLS（如果后端使用 service_role key，这是最简单的解决方案）
-- 注意：如果使用 service_role key 连接，RLS 会被绕过
-- 但如果使用 anon key，则需要配置 RLS 策略

-- 方案 A：完全禁用 RLS（推荐用于后端服务使用 service_role key 的情况）
ALTER TABLE orders DISABLE ROW LEVEL SECURITY;

-- 方案 B：如果需要保持 RLS 启用，添加适当的策略
-- 取消下面的注释来使用

/*
-- 启用 RLS
ALTER TABLE orders ENABLE ROW LEVEL SECURITY;

-- 删除现有策略（如果存在）
DROP POLICY IF EXISTS "Users can view their own orders" ON orders;
DROP POLICY IF EXISTS "Users can create their own orders" ON orders;
DROP POLICY IF EXISTS "Users can update their own orders" ON orders;
DROP POLICY IF EXISTS "Service role can do anything" ON orders;

-- 策略1：允许服务角色执行所有操作
CREATE POLICY "Service role can do anything"
ON orders
FOR ALL
TO service_role
USING (true)
WITH CHECK (true);

-- 策略2：用户可以查看自己的订单
CREATE POLICY "Users can view their own orders"
ON orders
FOR SELECT
TO authenticated
USING (user_id = auth.uid());

-- 策略3：用户可以创建自己的订单
CREATE POLICY "Users can create their own orders"
ON orders
FOR INSERT
TO authenticated
WITH CHECK (user_id = auth.uid());

-- 策略4：用户可以更新自己的订单（仅限某些状态）
CREATE POLICY "Users can update their own orders"
ON orders
FOR UPDATE
TO authenticated
USING (user_id = auth.uid())
WITH CHECK (user_id = auth.uid());
*/

-- 3. 同样处理 payment_transactions 表
ALTER TABLE payment_transactions DISABLE ROW LEVEL SECURITY;

-- 4. 验证 RLS 状态
SELECT 
    schemaname,
    tablename,
    rowsecurity
FROM pg_tables 
WHERE tablename IN ('orders', 'payment_transactions');

-- =============================================
-- 执行说明：
-- 1. 登录 Supabase Dashboard
-- 2. 进入 SQL Editor
-- 3. 复制并执行此脚本
-- 4. 如果后端使用 service_role key，方案 A 即可
-- 5. 如果需要更细粒度的控制，使用方案 B
-- =============================================
