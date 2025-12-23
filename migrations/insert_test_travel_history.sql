-- 插入测试旅行历史数据
-- 从 cities 表中获取一个城市 ID
-- Date: 2025-12-23

-- 插入一条测试数据（以东京为例）
INSERT INTO travel_history (
    id,
    user_id,
    city,
    country,
    country_code,
    latitude,
    longitude,
    arrival_time,
    departure_time,
    is_confirmed,
    review,
    rating,
    city_id,
    created_at,
    updated_at
)
SELECT 
    gen_random_uuid(),
    'bffcd353-d6ea-48ea-899d-967bd958cdbe',  -- 替换为实际的用户 ID
    c.name,
    c.country,
    'JP',  -- 日本的国家代码
    c.latitude,
    c.longitude,
    NOW() - INTERVAL '7 days',  -- 7天前到达
    NOW() - INTERVAL '2 days',  -- 2天前离开
    true,  -- 已确认
    '很棒的旅行体验！东京是一座充满活力的城市。',
    4.5,
    c.id,
    NOW(),
    NOW()
FROM cities c
WHERE c.name ILIKE '%Tokyo%' OR c.name_en ILIKE '%Tokyo%'
LIMIT 1;

-- 如果上面没有找到东京，插入一条使用任意城市的测试数据
INSERT INTO travel_history (
    id,
    user_id,
    city,
    country,
    country_code,
    latitude,
    longitude,
    arrival_time,
    departure_time,
    is_confirmed,
    review,
    rating,
    city_id,
    created_at,
    updated_at
)
SELECT 
    gen_random_uuid(),
    'bffcd353-d6ea-48ea-899d-967bd958cdbe',  -- 替换为实际的用户 ID
    c.name,
    c.country,
    CASE 
        WHEN c.country ILIKE '%China%' THEN 'CN'
        WHEN c.country ILIKE '%Japan%' THEN 'JP'
        WHEN c.country ILIKE '%Thailand%' THEN 'TH'
        WHEN c.country ILIKE '%Vietnam%' THEN 'VN'
        ELSE NULL
    END,
    c.latitude,
    c.longitude,
    NOW() - INTERVAL '14 days',
    NOW() - INTERVAL '10 days',
    true,
    '测试旅行历史记录',
    4.0,
    c.id,
    NOW(),
    NOW()
FROM cities c
WHERE NOT EXISTS (
    SELECT 1 FROM travel_history th 
    WHERE th.user_id = 'bffcd353-d6ea-48ea-899d-967bd958cdbe'
)
LIMIT 1;

-- 查看插入的数据
SELECT 
    th.id,
    th.city,
    th.country,
    th.country_code,
    th.arrival_time,
    th.departure_time,
    th.is_confirmed,
    th.rating,
    th.city_id
FROM travel_history th
WHERE th.user_id = 'bffcd353-d6ea-48ea-899d-967bd958cdbe'
ORDER BY th.created_at DESC;
