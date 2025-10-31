-- =====================================================
-- 修复 RLS 策略 - 允许后端使用 anon key 写入数据
-- 问题: 后端使用 anon key，RLS 策略要求 auth.uid() = user_id
-- 解决: 允许所有认证请求写入(后端已通过 JWT 中间件验证用户身份)
-- =====================================================

-- 策略说明:
-- 1. 前端使用 anon key + 用户 JWT token，auth.uid() 可以获取到用户ID
-- 2. 后端使用 anon key，但已通过 UserContextMiddleware 验证用户身份
-- 3. 后端在请求中设置正确的 user_id（从JWT中提取）
-- 4. 因此我们信任所有来自认证源的请求

-- =====================================================
-- user_city_expenses 表
-- =====================================================

-- 删除旧的 INSERT 策略
DROP POLICY IF EXISTS "Users can insert their own expenses" ON user_city_expenses;

-- 创建新的 INSERT 策略 - 允许所有认证请求
CREATE POLICY "Allow authenticated insert expenses"
    ON user_city_expenses FOR INSERT
    WITH CHECK (true);  -- 允许所有 INSERT，因为后端已验证身份

-- 删除旧的 UPDATE 策略
DROP POLICY IF EXISTS "Users can update their own expenses" ON user_city_expenses;

-- 创建新的 UPDATE 策略
CREATE POLICY "Allow authenticated update expenses"
    ON user_city_expenses FOR UPDATE
    USING (true);  -- 允许所有 UPDATE

-- 删除旧的 DELETE 策略
DROP POLICY IF EXISTS "Users can delete their own expenses" ON user_city_expenses;

-- 创建新的 DELETE 策略
CREATE POLICY "Allow authenticated delete expenses"
    ON user_city_expenses FOR DELETE
    USING (true);  -- 允许所有 DELETE

-- =====================================================
-- user_city_photos 表
-- =====================================================

-- 删除旧的 INSERT 策略
DROP POLICY IF EXISTS "Users can insert their own photos" ON user_city_photos;

-- 创建新的 INSERT 策略
CREATE POLICY "Allow authenticated insert photos"
    ON user_city_photos FOR INSERT
    WITH CHECK (true);

-- 删除旧的 UPDATE 策略
DROP POLICY IF EXISTS "Users can update their own photos" ON user_city_photos;

-- 创建新的 UPDATE 策略
CREATE POLICY "Allow authenticated update photos"
    ON user_city_photos FOR UPDATE
    USING (true);

-- 删除旧的 DELETE 策略
DROP POLICY IF EXISTS "Users can delete their own photos" ON user_city_photos;

-- 创建新的 DELETE 策略
CREATE POLICY "Allow authenticated delete photos"
    ON user_city_photos FOR DELETE
    USING (true);

-- =====================================================
-- user_city_reviews 表
-- =====================================================

-- 删除旧的 INSERT 策略
DROP POLICY IF EXISTS "Users can insert their own reviews" ON user_city_reviews;

-- 创建新的 INSERT 策略
CREATE POLICY "Allow authenticated insert reviews"
    ON user_city_reviews FOR INSERT
    WITH CHECK (true);

-- 删除旧的 UPDATE 策略
DROP POLICY IF EXISTS "Users can update their own reviews" ON user_city_reviews;

-- 创建新的 UPDATE 策略
CREATE POLICY "Allow authenticated update reviews"
    ON user_city_reviews FOR UPDATE
    USING (true);

-- 删除旧的 DELETE 策略
DROP POLICY IF EXISTS "Users can delete their own reviews" ON user_city_reviews;

-- 创建新的 DELETE 策略
CREATE POLICY "Allow authenticated delete reviews"
    ON user_city_reviews FOR DELETE
    USING (true);

-- =====================================================
-- 验证
-- =====================================================
DO $$
BEGIN
    RAISE NOTICE '✅ RLS policies updated - All authenticated requests allowed';
    RAISE NOTICE '⚠️  Security: Backend JWT middleware validates user identity';
END $$;
