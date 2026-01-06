-- 迁移：移除 coworking_reviews 表的 username 和 user_avatar 列
-- 日期：2026-01-06
-- 原因：用户信息现在通过 UserService 动态获取，不再冗余存储

-- 移除 username 列
ALTER TABLE coworking_reviews DROP COLUMN IF EXISTS username;

-- 移除 user_avatar 列
ALTER TABLE coworking_reviews DROP COLUMN IF EXISTS user_avatar;

-- 验证结果
SELECT column_name, data_type, is_nullable 
FROM information_schema.columns 
WHERE table_name = 'coworking_reviews'
ORDER BY ordinal_position;
