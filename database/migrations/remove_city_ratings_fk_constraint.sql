-- 移除 city_ratings 表的外键约束
-- 原因：评分系统应该独立于认证系统，用户ID可能来自不同来源

-- 1. 移除 user_id 外键约束
ALTER TABLE city_ratings 
DROP CONSTRAINT IF EXISTS city_ratings_user_id_fkey;

-- 2. 移除 city_rating_categories 的 created_by 外键约束（可选）
ALTER TABLE city_rating_categories 
DROP CONSTRAINT IF EXISTS city_rating_categories_created_by_fkey;

-- 3. 添加注释说明
COMMENT ON COLUMN city_ratings.user_id IS '用户ID，不依赖外键约束以支持多种认证源';
COMMENT ON COLUMN city_rating_categories.created_by IS '创建者ID，不依赖外键约束以支持多种认证源';

-- 验证约束已移除
SELECT
    conname AS constraint_name,
    contype AS constraint_type,
    conrelid::regclass AS table_name
FROM pg_constraint
WHERE conrelid IN ('city_ratings'::regclass, 'city_rating_categories'::regclass)
    AND contype = 'f';  -- 'f' = foreign key
