-- ============================================
-- 步骤 1: 检查 travel_history 表是否存在
-- ============================================
SELECT EXISTS (
   SELECT FROM information_schema.tables 
   WHERE table_schema = 'public'
   AND table_name = 'travel_history'
);

-- ============================================
-- 步骤 2: 如果表不存在，创建它
-- ============================================
CREATE TABLE IF NOT EXISTS travel_history (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
    city VARCHAR(100) NOT NULL,
    country VARCHAR(100) NOT NULL,
    country_code VARCHAR(2),
    latitude DOUBLE PRECISION,
    longitude DOUBLE PRECISION,
    arrival_time TIMESTAMPTZ,
    departure_time TIMESTAMPTZ,
    is_confirmed BOOLEAN DEFAULT FALSE,
    review TEXT,
    rating DECIMAL(2,1),
    city_id UUID,
    source VARCHAR(50),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_travel_history_user_id ON travel_history(user_id);
CREATE INDEX IF NOT EXISTS idx_travel_history_is_confirmed ON travel_history(is_confirmed);
CREATE INDEX IF NOT EXISTS idx_travel_history_country_code ON travel_history(country_code);

-- ============================================
-- 步骤 3: 禁用 RLS（重要！否则查询会返回空）
-- ============================================
ALTER TABLE travel_history DISABLE ROW LEVEL SECURITY;

-- ============================================
-- 步骤 4: 检查当前用户 ID
-- ============================================
-- 找到 walden.wuwei@163.com 对应的用户 ID
SELECT id, email FROM auth.users WHERE email = 'walden.wuwei@163.com';

-- ============================================
-- 步骤 5: 插入测试数据
-- 确保 user_id 是正确的用户 ID
-- ============================================
INSERT INTO travel_history (
    id, user_id, city, country, country_code, latitude, longitude,
    arrival_time, departure_time, is_confirmed, review, rating, created_at, updated_at
)
VALUES 
-- 东京之旅
(gen_random_uuid(), 'bffcd353-d6ea-48ea-899d-967bd958cdbe', 'Tokyo', 'Japan', 'JP',
 35.6762, 139.6503, NOW() - INTERVAL '10 days', NOW() - INTERVAL '5 days',
 true, '很棒的旅行体验！东京是一座充满活力的城市。', 4.5, NOW(), NOW()),
-- 新加坡之旅
(gen_random_uuid(), 'bffcd353-d6ea-48ea-899d-967bd958cdbe', 'Singapore', 'Singapore', 'SG',
 1.3521, 103.8198, NOW() - INTERVAL '20 days', NOW() - INTERVAL '15 days',
 true, '新加坡的美食太棒了！', 4.8, NOW(), NOW()),
-- 曼谷之旅
(gen_random_uuid(), 'bffcd353-d6ea-48ea-899d-967bd958cdbe', 'Bangkok', 'Thailand', 'TH',
 13.7563, 100.5018, NOW() - INTERVAL '30 days', NOW() - INTERVAL '25 days',
 true, '曼谷的寺庙很壮观', 4.2, NOW(), NOW())
ON CONFLICT (id) DO NOTHING;

-- ============================================
-- 步骤 6: 验证数据已插入
-- ============================================
SELECT id, city, country, country_code, arrival_time, is_confirmed 
FROM travel_history 
WHERE user_id = 'bffcd353-d6ea-48ea-899d-967bd958cdbe'
ORDER BY arrival_time DESC;
