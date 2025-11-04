-- =====================================================
-- 修复外键约束 - 将 auth.users 改为 public.users
-- 因为应用使用的是 public.users 表,不是 Supabase Auth
-- =====================================================

-- ==================== city_pros_cons ====================
-- 1. 删除旧的外键约束
ALTER TABLE city_pros_cons 
    DROP CONSTRAINT IF EXISTS fk_city_pros_cons_user;

-- 2. 添加新的外键约束,指向 public.users
ALTER TABLE city_pros_cons 
    ADD CONSTRAINT fk_city_pros_cons_user 
    FOREIGN KEY (user_id) 
    REFERENCES public.users(id) 
    ON DELETE CASCADE;

-- ==================== user_city_photos ====================
ALTER TABLE user_city_photos 
    DROP CONSTRAINT IF EXISTS fk_user_city_photos_user;

ALTER TABLE user_city_photos 
    ADD CONSTRAINT fk_user_city_photos_user 
    FOREIGN KEY (user_id) 
    REFERENCES public.users(id) 
    ON DELETE CASCADE;

-- ==================== user_city_expenses ====================
ALTER TABLE user_city_expenses 
    DROP CONSTRAINT IF EXISTS fk_user_city_expenses_user;

ALTER TABLE user_city_expenses 
    ADD CONSTRAINT fk_user_city_expenses_user 
    FOREIGN KEY (user_id) 
    REFERENCES public.users(id) 
    ON DELETE CASCADE;

-- ==================== user_city_reviews ====================
ALTER TABLE user_city_reviews 
    DROP CONSTRAINT IF EXISTS fk_user_city_reviews_user;

ALTER TABLE user_city_reviews 
    ADD CONSTRAINT fk_user_city_reviews_user 
    FOREIGN KEY (user_id) 
    REFERENCES public.users(id) 
    ON DELETE CASCADE;

-- ==================== user_favorite_cities ====================
ALTER TABLE user_favorite_cities 
    DROP CONSTRAINT IF EXISTS fk_user_favorite_cities_user;

ALTER TABLE user_favorite_cities 
    ADD CONSTRAINT fk_user_favorite_cities_user 
    FOREIGN KEY (user_id) 
    REFERENCES public.users(id) 
    ON DELETE CASCADE;

-- ==================== 验证外键约束 ====================
SELECT 
    tc.table_name,
    tc.constraint_name,
    kcu.column_name,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name
FROM information_schema.table_constraints AS tc
JOIN information_schema.key_column_usage AS kcu
    ON tc.constraint_name = kcu.constraint_name
JOIN information_schema.constraint_column_usage AS ccu
    ON ccu.constraint_name = tc.constraint_name
WHERE tc.constraint_type = 'FOREIGN KEY'
    AND tc.table_name IN (
        'city_pros_cons',
        'user_city_photos',
        'user_city_expenses',
        'user_city_reviews',
        'user_favorite_cities'
    )
ORDER BY tc.table_name, tc.constraint_name;

-- ==================== 说明 ====================
-- 
-- 问题原因:
-- - 原始外键约束引用的是 auth.users (Supabase 内置认证表)
-- - 但应用使用的是 public.users (自定义用户表)
-- - 导致外键验证失败
-- 
-- 修复内容:
-- - 将所有 user_id 外键从 auth.users 改为 public.users
-- - 保持 ON DELETE CASCADE (删除用户时级联删除相关数据)
-- 
-- 影响范围:
-- - city_pros_cons
-- - user_city_photos
-- - user_city_expenses
-- - user_city_reviews
-- - user_favorite_cities
--
