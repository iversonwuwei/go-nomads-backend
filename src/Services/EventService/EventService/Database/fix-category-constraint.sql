-- 修复 events 表 category 字段约束
-- 问题：category 字段有 CHECK 约束，不允许存储 UUID

-- 1. 查看当前约束
SELECT conname, pg_get_constraintdef(oid) as constraint_def
FROM pg_constraint 
WHERE conrelid = 'events'::regclass 
  AND conname LIKE '%category%';

-- 2. 删除旧的 CHECK 约束（如果存在）
ALTER TABLE events DROP CONSTRAINT IF EXISTS events_category_check;

-- 3. 可选：添加新约束（确保 category 要么是 NULL，要么是有效的 UUID）
-- 如果需要严格验证 UUID 格式：
ALTER TABLE events 
ADD CONSTRAINT events_category_uuid_check 
CHECK (
  category IS NULL 
  OR category ~ '^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$'
);

-- 4. 验证修改
SELECT conname, pg_get_constraintdef(oid) as constraint_def
FROM pg_constraint 
WHERE conrelid = 'events'::regclass;
