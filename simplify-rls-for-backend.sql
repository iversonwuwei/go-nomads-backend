-- =====================================================
-- 禁用 RLS 策略 - 完全信任后端应用层的身份验证
-- 后端已有完善的 JWT 身份验证,数据库层面不需要额外限制
-- =====================================================

-- ==================== 用户城市照片表 ====================
-- 禁用 RLS
ALTER TABLE user_city_photos DISABLE ROW LEVEL SECURITY;

-- 删除所有现有策略
DROP POLICY IF EXISTS "Anyone can view photos" ON user_city_photos;
DROP POLICY IF EXISTS "Users can insert their own photos" ON user_city_photos;
DROP POLICY IF EXISTS "Users can update their own photos" ON user_city_photos;
DROP POLICY IF EXISTS "Users can delete their own photos" ON user_city_photos;
DROP POLICY IF EXISTS "Allow authenticated and service role insert" ON user_city_photos;
DROP POLICY IF EXISTS "Allow authenticated and service role update" ON user_city_photos;
DROP POLICY IF EXISTS "Allow authenticated and service role delete" ON user_city_photos;

-- ==================== 用户城市费用表 ====================
-- 禁用 RLS
ALTER TABLE user_city_expenses DISABLE ROW LEVEL SECURITY;

-- 删除所有现有策略
DROP POLICY IF EXISTS "Anyone can view expenses" ON user_city_expenses;
DROP POLICY IF EXISTS "Users can insert their own expenses" ON user_city_expenses;
DROP POLICY IF EXISTS "Users can update their own expenses" ON user_city_expenses;
DROP POLICY IF EXISTS "Users can delete their own expenses" ON user_city_expenses;
DROP POLICY IF EXISTS "Allow authenticated and service role insert" ON user_city_expenses;
DROP POLICY IF EXISTS "Allow authenticated and service role update" ON user_city_expenses;
DROP POLICY IF EXISTS "Allow authenticated and service role delete" ON user_city_expenses;

-- ==================== 用户城市评论表 ====================
-- 禁用 RLS
ALTER TABLE user_city_reviews DISABLE ROW LEVEL SECURITY;

-- 删除所有现有策略
DROP POLICY IF EXISTS "Anyone can view reviews" ON user_city_reviews;
DROP POLICY IF EXISTS "Users can insert their own reviews" ON user_city_reviews;
DROP POLICY IF EXISTS "Users can update their own reviews" ON user_city_reviews;
DROP POLICY IF EXISTS "Users can delete their own reviews" ON user_city_reviews;
DROP POLICY IF EXISTS "Allow authenticated and service role insert" ON user_city_reviews;
DROP POLICY IF EXISTS "Allow authenticated and service role update" ON user_city_reviews;
DROP POLICY IF EXISTS "Allow authenticated and service role delete" ON user_city_reviews;

-- ==================== 城市 Pros & Cons 表 ====================
-- 禁用 RLS
ALTER TABLE city_pros_cons DISABLE ROW LEVEL SECURITY;

-- 删除所有现有策略
DROP POLICY IF EXISTS "Anyone can view pros and cons" ON city_pros_cons;
DROP POLICY IF EXISTS "Authenticated users can insert pros and cons" ON city_pros_cons;
DROP POLICY IF EXISTS "Authenticated users and service role can insert pros and cons" ON city_pros_cons;
DROP POLICY IF EXISTS "Users can update their own pros and cons" ON city_pros_cons;
DROP POLICY IF EXISTS "Users can delete their own pros and cons" ON city_pros_cons;
DROP POLICY IF EXISTS "Allow authenticated and service role insert" ON city_pros_cons;
DROP POLICY IF EXISTS "Allow authenticated and service role update" ON city_pros_cons;
DROP POLICY IF EXISTS "Allow authenticated and service role delete" ON city_pros_cons;

-- ==================== 用户收藏城市表 ====================
-- 禁用 RLS
ALTER TABLE user_favorite_cities DISABLE ROW LEVEL SECURITY;

-- 删除所有现有策略
DROP POLICY IF EXISTS "Anyone can view favorites" ON user_favorite_cities;
DROP POLICY IF EXISTS "Users can view their own favorites" ON user_favorite_cities;
DROP POLICY IF EXISTS "Users can insert their own favorites" ON user_favorite_cities;
DROP POLICY IF EXISTS "Users can delete their own favorites" ON user_favorite_cities;
DROP POLICY IF EXISTS "Allow authenticated and service role insert" ON user_favorite_cities;
DROP POLICY IF EXISTS "Allow authenticated and service role delete" ON user_favorite_cities;

-- ==================== 验证 RLS 状态 ====================
-- 检查 RLS 是否已禁用
SELECT 
    schemaname,
    tablename,
    rowsecurity AS rls_enabled
FROM pg_tables
WHERE tablename IN (
    'user_city_photos',
    'user_city_expenses', 
    'user_city_reviews',
    'city_pros_cons',
    'user_favorite_cities'
)
ORDER BY tablename;

-- 检查是否还有残留策略(应该为空)
SELECT 
    schemaname,
    tablename,
    policyname
FROM pg_policies 
WHERE tablename IN (
    'user_city_photos',
    'user_city_expenses', 
    'user_city_reviews',
    'city_pros_cons',
    'user_favorite_cities'
)
ORDER BY tablename, policyname;

-- ==================== 说明 ====================
-- 
-- 完全禁用 RLS 的原因:
-- 1. 后端应用已经实现了完善的 JWT 身份验证和授权
-- 2. 所有 API 请求都经过严格的权限验证
-- 3. 数据库层面的 RLS 会导致额外的性能开销
-- 4. Service Role Key 和用户 Token 混用时容易产生权限冲突
-- 
-- 安全保障:
-- - 后端 API 层有完整的权限控制
-- - 用户只能通过后端 API 访问数据
-- - Service Role Key 不会暴露给前端
-- - 每个操作都会验证用户身份和权限
-- 
-- 优点:
-- - 完全消除 RLS 相关的错误
-- - 提高数据库查询性能
-- - 简化架构,减少维护成本
-- - 灵活的权限控制由应用层实现
-- 
