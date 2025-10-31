-- =====================================================
-- 删除外键约束 - 允许独立存储用户内容
-- 原因: 用户数据存储在 auth.users，不需要在 public.users 中
-- =====================================================

-- 删除 user_city_expenses 的外键约束
ALTER TABLE user_city_expenses 
DROP CONSTRAINT IF EXISTS fk_user_city_expenses_user;

ALTER TABLE user_city_expenses 
DROP CONSTRAINT IF EXISTS user_city_expenses_user_id_fkey;

-- 删除 user_city_photos 的外键约束
ALTER TABLE user_city_photos 
DROP CONSTRAINT IF EXISTS fk_user_city_photos_user;

ALTER TABLE user_city_photos 
DROP CONSTRAINT IF EXISTS user_city_photos_user_id_fkey;

-- 删除 user_city_reviews 的外键约束
ALTER TABLE user_city_reviews 
DROP CONSTRAINT IF EXISTS fk_user_city_reviews_user;

ALTER TABLE user_city_reviews 
DROP CONSTRAINT IF EXISTS user_city_reviews_user_id_fkey;

-- 验证
DO $$
BEGIN
    RAISE NOTICE '✅ Foreign key constraints removed';
    RAISE NOTICE 'User content can now be stored independently';
END $$;
