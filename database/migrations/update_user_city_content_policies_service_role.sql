-- 移除用户城市内容相关表的 RLS
-- 可以重复执行, 确保所有策略被清理并禁用 RLS

-- 1. 用户城市照片
ALTER TABLE user_city_photos DISABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "Users can view their own photos" ON user_city_photos;
DROP POLICY IF EXISTS "Users can insert their own photos" ON user_city_photos;
DROP POLICY IF EXISTS "Users can update their own photos" ON user_city_photos;
DROP POLICY IF EXISTS "Users can delete their own photos" ON user_city_photos;

-- 2. 用户城市费用
ALTER TABLE user_city_expenses DISABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "Users can view their own expenses" ON user_city_expenses;
DROP POLICY IF EXISTS "Users can insert their own expenses" ON user_city_expenses;
DROP POLICY IF EXISTS "Users can update their own expenses" ON user_city_expenses;
DROP POLICY IF EXISTS "Users can delete their own expenses" ON user_city_expenses;

-- 3. 用户城市评论
ALTER TABLE user_city_reviews DISABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "Anyone can view reviews" ON user_city_reviews;
DROP POLICY IF EXISTS "Users can insert their own reviews" ON user_city_reviews;
DROP POLICY IF EXISTS "Users can update their own reviews" ON user_city_reviews;
DROP POLICY IF EXISTS "Users can delete their own reviews" ON user_city_reviews;
