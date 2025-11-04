-- =====================================================
-- 修复 city_pros_cons 表的 RLS 策略
-- 允许后端服务使用 Service Role Key 插入数据
-- =====================================================

-- 删除旧的 INSERT 策略
DROP POLICY IF EXISTS "Authenticated users can insert pros and cons" ON city_pros_cons;

-- 创建新的 INSERT 策略:允许已认证用户或服务角色插入
CREATE POLICY "Authenticated users and service role can insert pros and cons"
    ON city_pros_cons FOR INSERT
    WITH CHECK (
        auth.role() = 'authenticated' OR 
        auth.role() = 'service_role'
    );

-- 验证策略
SELECT 
    schemaname,
    tablename,
    policyname,
    permissive,
    roles,
    cmd,
    qual,
    with_check
FROM pg_policies 
WHERE tablename = 'city_pros_cons'
ORDER BY policyname;
