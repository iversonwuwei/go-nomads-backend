-- ====================================
-- 附近城市表
-- ====================================

-- 删除旧表(如果存在)
DROP TABLE IF EXISTS nearby_cities CASCADE;

-- 创建表
CREATE TABLE nearby_cities (
    id TEXT PRIMARY KEY,
    source_city_id TEXT NOT NULL,
    target_city_id TEXT,
    target_city_name TEXT NOT NULL,
    country TEXT NOT NULL,
    distance_km DOUBLE PRECISION NOT NULL,
    transportation_type TEXT NOT NULL,
    travel_time_minutes INTEGER NOT NULL,
    highlights JSONB NOT NULL DEFAULT '[]'::jsonb,
    nomad_features JSONB NOT NULL DEFAULT '{}'::jsonb,
    image_url TEXT,
    overall_score DOUBLE PRECISION,
    latitude DOUBLE PRECISION,
    longitude DOUBLE PRECISION,
    is_ai_generated BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 创建索引
CREATE INDEX idx_nearby_cities_source_city_id 
    ON nearby_cities(source_city_id);

CREATE INDEX idx_nearby_cities_target_city_id 
    ON nearby_cities(target_city_id);

CREATE INDEX idx_nearby_cities_distance 
    ON nearby_cities(distance_km);

CREATE INDEX idx_nearby_cities_updated_at 
    ON nearby_cities(updated_at DESC);

-- 添加复合唯一约束(同一源城市不能有重复的目标城市)
CREATE UNIQUE INDEX idx_nearby_cities_source_target_unique 
    ON nearby_cities(source_city_id, target_city_name);

-- 添加注释
COMMENT ON TABLE nearby_cities IS '附近城市信息表，存储城市之间的相邻关系';
COMMENT ON COLUMN nearby_cities.id IS '记录ID(主键)';
COMMENT ON COLUMN nearby_cities.source_city_id IS '源城市ID';
COMMENT ON COLUMN nearby_cities.target_city_id IS '目标城市ID(如果在数据库中存在)';
COMMENT ON COLUMN nearby_cities.target_city_name IS '目标城市名称';
COMMENT ON COLUMN nearby_cities.country IS '目标城市所属国家';
COMMENT ON COLUMN nearby_cities.distance_km IS '距离(公里)';
COMMENT ON COLUMN nearby_cities.transportation_type IS '主要交通方式(train/bus/car/flight/ferry)';
COMMENT ON COLUMN nearby_cities.travel_time_minutes IS '预计旅行时间(分钟)';
COMMENT ON COLUMN nearby_cities.highlights IS '城市亮点/特色(JSON数组)';
COMMENT ON COLUMN nearby_cities.nomad_features IS '数字游民相关特色(JSON对象)';
COMMENT ON COLUMN nearby_cities.image_url IS '城市图片URL';
COMMENT ON COLUMN nearby_cities.overall_score IS '综合评分';
COMMENT ON COLUMN nearby_cities.latitude IS '目标城市纬度';
COMMENT ON COLUMN nearby_cities.longitude IS '目标城市经度';
COMMENT ON COLUMN nearby_cities.is_ai_generated IS '是否由AI生成';
COMMENT ON COLUMN nearby_cities.created_at IS '创建时间';
COMMENT ON COLUMN nearby_cities.updated_at IS '更新时间';

-- ⚠️ RLS策略暂时禁用,等测试通过后再启用
-- ALTER TABLE nearby_cities ENABLE ROW LEVEL SECURITY;
-- CREATE POLICY "Allow public read access" ON nearby_cities FOR SELECT USING (true);
-- CREATE POLICY "Allow service write access" ON nearby_cities FOR ALL USING (auth.role() = 'service_role');

-- ====================================
-- JSONB Schema 示例:
-- ====================================

-- highlights:
-- [
--   "Historic old town",
--   "Beautiful beaches",
--   "Affordable living",
--   "Great coffee culture"
-- ]

-- nomad_features:
-- {
--   "monthly_cost_usd": 1500,
--   "internet_speed_mbps": 50,
--   "coworking_spaces": 15,
--   "visa_info": "90-day visa-free for most nationalities",
--   "safety_score": 4.2,
--   "quality_of_life": "High quality of life with modern amenities"
-- }
