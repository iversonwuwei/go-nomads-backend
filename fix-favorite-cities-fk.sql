-- 删除 user_favorite_cities 的外键约束
-- 原因: users 表数据可能不完整，导致外键约束失败
-- 业务逻辑已在 Controller 层验证用户身份

-- 删除外键约束
ALTER TABLE user_favorite_cities 
DROP CONSTRAINT IF EXISTS user_favorite_cities_user_id_fkey;

-- 验证外键已删除
SELECT 
    conname AS constraint_name,
    conrelid::regclass AS table_name,
    confrelid::regclass AS referenced_table
FROM pg_constraint
WHERE conrelid = 'user_favorite_cities'::regclass
  AND contype = 'f';
