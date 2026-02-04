-- 直接插入测试旅行历史数据
-- 用户 ID: bffcd353-d6ea-48ea-899d-967bd958cdbe (walden.wuwei@163.com)
-- Date: 2025-12-23

-- 首先检查是否存在该用户
-- SELECT id, email FROM users WHERE id = 'bffcd353-d6ea-48ea-899d-967bd958cdbe';

-- 直接插入测试数据（不依赖 cities 表）
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
VALUES 
-- 东京之旅
(
    gen_random_uuid(),
    'bffcd353-d6ea-48ea-899d-967bd958cdbe',
    'Tokyo',
    'Japan',
    'JP',
    35.6762,
    139.6503,
    NOW() - INTERVAL '10 days',
    NOW() - INTERVAL '5 days',
    true,
    '很棒的旅行体验！东京是一座充满活力的城市。',
    4.5,
    NULL,
    NOW(),
    NOW()
),
-- 新加坡之旅
(
    gen_random_uuid(),
    'bffcd353-d6ea-48ea-899d-967bd958cdbe',
    'Singapore',
    'Singapore',
    'SG',
    1.3521,
    103.8198,
    NOW() - INTERVAL '20 days',
    NOW() - INTERVAL '15 days',
    true,
    '新加坡的美食太棒了！',
    4.8,
    NULL,
    NOW(),
    NOW()
),
-- 曼谷之旅
(
    gen_random_uuid(),
    'bffcd353-d6ea-48ea-899d-967bd958cdbe',
    'Bangkok',
    'Thailand',
    'TH',
    13.7563,
    100.5018,
    NOW() - INTERVAL '30 days',
    NOW() - INTERVAL '25 days',
    true,
    '曼谷的寺庙很壮观',
    4.2,
    NULL,
    NOW(),
    NOW()
);

-- 验证插入结果
SELECT id, city, country, country_code, arrival_time, departure_time, is_confirmed 
FROM travel_history 
WHERE user_id = 'bffcd353-d6ea-48ea-899d-967bd958cdbe'
ORDER BY arrival_time DESC;
