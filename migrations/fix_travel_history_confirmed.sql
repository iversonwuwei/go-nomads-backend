-- ============================================
-- 检查并修复 travel_history 数据
-- ============================================

-- 1. 检查当前数据的 is_confirmed 值
SELECT id, city, country, is_confirmed, arrival_time 
FROM travel_history 
WHERE user_id = 'bffcd353-d6ea-48ea-899d-967bd958cdbe';

-- 2. 如果 is_confirmed 是 false 或 NULL，更新为 true
UPDATE travel_history 
SET is_confirmed = true,
    updated_at = NOW()
WHERE user_id = 'bffcd353-d6ea-48ea-899d-967bd958cdbe'
  AND (is_confirmed IS NULL OR is_confirmed = false);

-- 3. 验证更新结果
SELECT id, city, country, is_confirmed, arrival_time 
FROM travel_history 
WHERE user_id = 'bffcd353-d6ea-48ea-899d-967bd958cdbe';
