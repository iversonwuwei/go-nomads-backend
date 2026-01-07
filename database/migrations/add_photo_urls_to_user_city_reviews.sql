-- ============================================================
-- 给 user_city_reviews 表添加 photo_urls 字段
-- 用于存储评论关联的照片 URL
-- ============================================================

-- 添加 photo_urls 字段（TEXT 数组，最多 5 张照片）
ALTER TABLE user_city_reviews 
ADD COLUMN IF NOT EXISTS photo_urls TEXT[] DEFAULT '{}';

-- 添加注释
COMMENT ON COLUMN user_city_reviews.photo_urls IS '评论关联的照片 URL 数组（最多 5 张）';

-- 验证字段已添加
SELECT column_name, data_type, column_default 
FROM information_schema.columns 
WHERE table_name = 'user_city_reviews' AND column_name = 'photo_urls';
