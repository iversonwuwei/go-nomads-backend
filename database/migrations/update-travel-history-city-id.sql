-- ============================================================
-- 批量更新 travel_history 表中缺失的 city_id
-- 基于 city 名称和坐标匹配 cities 表
-- ============================================================

-- 1. 首先查看有多少记录需要更新
SELECT 
    COUNT(*) as total,
    COUNT(city_id) as with_city_id,
    COUNT(*) - COUNT(city_id) as without_city_id
FROM travel_history;

-- 2. 查看需要匹配的城市列表
SELECT DISTINCT 
    city, 
    country, 
    latitude, 
    longitude
FROM travel_history 
WHERE city_id IS NULL;

-- 3. 查看 cities 表中对应的城市（用于验证匹配）
SELECT 
    th.city as travel_city,
    th.country as travel_country,
    c.id as matched_city_id,
    c.name as city_name,
    c.name_en as city_name_en,
    c.country as city_country,
    -- 计算距离（简化版）
    SQRT(POWER(th.latitude - c.latitude, 2) + POWER(th.longitude - c.longitude, 2)) as approx_distance
FROM (
    SELECT DISTINCT city, country, latitude, longitude
    FROM travel_history 
    WHERE city_id IS NULL
) th
LEFT JOIN cities c ON (
    -- 英文名精确匹配
    LOWER(c.name_en) = LOWER(th.city)
    OR 
    -- 中文名精确匹配
    c.name = th.city
    OR
    -- 国家匹配 + 坐标相近（约50km范围）
    (LOWER(c.country) = LOWER(th.country) 
     AND ABS(c.latitude - th.latitude) < 0.5 
     AND ABS(c.longitude - th.longitude) < 0.5)
)
WHERE c.id IS NOT NULL;

-- ============================================================
-- 4. 执行更新（按名称精确匹配）
-- ============================================================

-- 4.1 按英文名精确匹配
UPDATE travel_history th
SET city_id = c.id::text, updated_at = NOW()
FROM cities c
WHERE th.city_id IS NULL
  AND LOWER(c.name_en) = LOWER(th.city)
  AND c.is_active = true;

-- 4.2 按中文名精确匹配
UPDATE travel_history th
SET city_id = c.id::text, updated_at = NOW()
FROM cities c
WHERE th.city_id IS NULL
  AND c.name = th.city
  AND c.is_active = true;

-- 4.3 按国家+名称模糊匹配
UPDATE travel_history th
SET city_id = c.id::text, updated_at = NOW()
FROM cities c
WHERE th.city_id IS NULL
  AND LOWER(c.country) = LOWER(th.country)
  AND (LOWER(c.name_en) LIKE '%' || LOWER(th.city) || '%'
       OR LOWER(th.city) LIKE '%' || LOWER(c.name_en) || '%')
  AND c.is_active = true;

-- 4.4 按坐标最近匹配（50km范围内）
-- 使用 DISTINCT ON 确保每个 travel_history 只匹配一个最近的城市
WITH nearest_cities AS (
    SELECT DISTINCT ON (th.id)
        th.id as travel_history_id,
        c.id::text as city_id,
        -- Haversine 简化计算
        SQRT(POWER(th.latitude - c.latitude, 2) + POWER(th.longitude - c.longitude, 2)) as distance
    FROM travel_history th
    CROSS JOIN cities c
    WHERE th.city_id IS NULL
      AND c.latitude IS NOT NULL 
      AND c.longitude IS NOT NULL
      AND c.is_active = true
      -- 约50km范围（纬度1度 ≈ 111km）
      AND ABS(c.latitude - th.latitude) < 0.5
      AND ABS(c.longitude - th.longitude) < 0.5
    ORDER BY th.id, distance
)
UPDATE travel_history th
SET city_id = nc.city_id, updated_at = NOW()
FROM nearest_cities nc
WHERE th.id = nc.travel_history_id
  AND th.city_id IS NULL;

-- ============================================================
-- 5. 验证更新结果
-- ============================================================
SELECT 
    th.id,
    th.city,
    th.country,
    th.city_id,
    c.name as matched_city_name,
    c.name_en as matched_city_name_en
FROM travel_history th
LEFT JOIN cities c ON th.city_id = c.id::text
ORDER BY th.created_at DESC;

-- 6. 最终统计
SELECT 
    COUNT(*) as total,
    COUNT(city_id) as with_city_id,
    COUNT(*) - COUNT(city_id) as still_without_city_id
FROM travel_history;
