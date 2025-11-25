-- 快速修复：删除 category 字段的 CHECK 约束
-- 执行此脚本后，category 字段可以存储任意字符串（包括 UUID）

ALTER TABLE events DROP CONSTRAINT IF EXISTS events_category_check;

-- 验证
SELECT 'Constraint dropped successfully' as status;
