-- =====================================================
-- 修复 RLS 策略 - 允许 service_role 绕过 RLS
-- 问题: 后端使用 service_role key，但 RLS 策略只允许 authenticated 用户
-- 解决: 修改 INSERT 策略，允许 service_role 或者验证用户身份
-- =====================================================

-- 1. 删除旧的 INSERT 策略
DROP POLICY IF EXISTS "Users can insert their own expenses" ON user_city_expenses;
DROP POLICY IF EXISTS "Users can insert their own photos" ON user_city_photos;
DROP POLICY IF EXISTS "Users can insert their own reviews" ON user_city_reviews;

-- 2. 创建新的 INSERT 策略 - 允许 service_role 或用户插入自己的数据
CREATE POLICY "Allow insert own expenses or via service_role"
    ON user_city_expenses FOR INSERT
    WITH CHECK (
        auth.uid() = user_id  -- 用户只能插入自己的数据
        OR 
        auth.jwt() ->> 'role' = 'service_role'  -- 或者使用 service_role
    );

CREATE POLICY "Allow insert own photos or via service_role"
    ON user_city_photos FOR INSERT
    WITH CHECK (
        auth.uid() = user_id 
        OR 
        auth.jwt() ->> 'role' = 'service_role'
    );

CREATE POLICY "Allow insert own reviews or via service_role"
    ON user_city_reviews FOR INSERT
    WITH CHECK (
        auth.uid() = user_id 
        OR 
        auth.jwt() ->> 'role' = 'service_role'
    );

-- 3. 同样修改 UPDATE 策略
DROP POLICY IF EXISTS "Users can update their own expenses" ON user_city_expenses;
DROP POLICY IF EXISTS "Users can update their own photos" ON user_city_photos;
DROP POLICY IF EXISTS "Users can update their own reviews" ON user_city_reviews;

CREATE POLICY "Allow update own expenses or via service_role"
    ON user_city_expenses FOR UPDATE
    USING (
        auth.uid() = user_id 
        OR 
        auth.jwt() ->> 'role' = 'service_role'
    );

CREATE POLICY "Allow update own photos or via service_role"
    ON user_city_photos FOR UPDATE
    USING (
        auth.uid() = user_id 
        OR 
        auth.jwt() ->> 'role' = 'service_role'
    );

CREATE POLICY "Allow update own reviews or via service_role"
    ON user_city_reviews FOR UPDATE
    USING (
        auth.uid() = user_id 
        OR 
        auth.jwt() ->> 'role' = 'service_role'
    );

-- 4. 同样修改 DELETE 策略
DROP POLICY IF EXISTS "Users can delete their own expenses" ON user_city_expenses;
DROP POLICY IF EXISTS "Users can delete their own photos" ON user_city_photos;
DROP POLICY IF EXISTS "Users can delete their own reviews" ON user_city_reviews;

CREATE POLICY "Allow delete own expenses or via service_role"
    ON user_city_expenses FOR DELETE
    USING (
        auth.uid() = user_id 
        OR 
        auth.jwt() ->> 'role' = 'service_role'
    );

CREATE POLICY "Allow delete own photos or via service_role"
    ON user_city_photos FOR DELETE
    USING (
        auth.uid() = user_id 
        OR 
        auth.jwt() ->> 'role' = 'service_role'
    );

CREATE POLICY "Allow delete own reviews or via service_role"
    ON user_city_reviews FOR DELETE
    USING (
        auth.uid() = user_id 
        OR 
        auth.jwt() ->> 'role' = 'service_role'
    );

-- 5. 验证策略已创建
DO $$
BEGIN
    RAISE NOTICE '✅ RLS policies updated to allow service_role access';
END $$;
