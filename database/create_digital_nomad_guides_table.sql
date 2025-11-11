-- ====================================
-- 数字游民指南表
-- ====================================

-- 删除旧表(如果存在)
DROP TABLE IF EXISTS digital_nomad_guides CASCADE;

-- 创建表
CREATE TABLE digital_nomad_guides (
    id TEXT PRIMARY KEY,
    city_id TEXT NOT NULL,
    city_name TEXT NOT NULL,
    overview TEXT NOT NULL,
    visa_info JSONB NOT NULL DEFAULT '{}'::jsonb,
    best_areas JSONB NOT NULL DEFAULT '[]'::jsonb,
    workspace_recommendations JSONB NOT NULL DEFAULT '[]'::jsonb,
    tips JSONB NOT NULL DEFAULT '[]'::jsonb,
    essential_info JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 创建索引
CREATE INDEX idx_digital_nomad_guides_city_id 
    ON digital_nomad_guides(city_id);

CREATE INDEX idx_digital_nomad_guides_updated_at 
    ON digital_nomad_guides(updated_at DESC);

-- 添加唯一约束(每个城市只有一个指南)
CREATE UNIQUE INDEX idx_digital_nomad_guides_city_id_unique 
    ON digital_nomad_guides(city_id);

-- 添加注释
COMMENT ON TABLE digital_nomad_guides IS '数字游民城市指南';
COMMENT ON COLUMN digital_nomad_guides.id IS '指南ID(主键)';
COMMENT ON COLUMN digital_nomad_guides.city_id IS '城市ID';
COMMENT ON COLUMN digital_nomad_guides.city_name IS '城市名称';
COMMENT ON COLUMN digital_nomad_guides.overview IS '概览';
COMMENT ON COLUMN digital_nomad_guides.visa_info IS '签证信息(JSON)';
COMMENT ON COLUMN digital_nomad_guides.best_areas IS '最佳区域列表(JSON数组)';
COMMENT ON COLUMN digital_nomad_guides.workspace_recommendations IS '工作空间推荐(JSON数组)';
COMMENT ON COLUMN digital_nomad_guides.tips IS '实用建议(JSON数组)';
COMMENT ON COLUMN digital_nomad_guides.essential_info IS '重要信息(JSON对象)';
COMMENT ON COLUMN digital_nomad_guides.created_at IS '创建时间';
COMMENT ON COLUMN digital_nomad_guides.updated_at IS '更新时间';

-- ⚠️ RLS策略暂时禁用,等测试通过后再启用
-- ALTER TABLE digital_nomad_guides ENABLE ROW LEVEL SECURITY;
-- CREATE POLICY "Allow public read access" ON digital_nomad_guides FOR SELECT USING (true);
-- CREATE POLICY "Allow service write access" ON digital_nomad_guides FOR ALL USING (auth.role() = 'service_role');

-- ====================================
-- JSONB Schema 示例:
-- ====================================

-- visa_info:
-- {
--   "type": "Tourist Visa",
--   "duration": 90,
--   "requirements": "Valid passport...",
--   "cost": 50.0,
--   "process": "Apply online..."
-- }

-- best_areas:
-- [
--   {
--     "name": "Downtown",
--     "description": "City center...",
--     "entertainmentScore": 4.5,
--     "entertainmentDescription": "Great nightlife...",
--     "tourismScore": 4.8,
--     "tourismDescription": "Many attractions...",
--     "economyScore": 3.0,
--     "economyDescription": "Moderate cost...",
--     "cultureScore": 4.2,
--     "cultureDescription": "Rich history..."
--   }
-- ]

-- workspace_recommendations:
-- [
--   "Cafe A - Fast WiFi",
--   "Coworking Space B - $15/day"
-- ]

-- tips:
-- [
--   "Learn basic language",
--   "Use public transport"
-- ]

-- essential_info:
-- {
--   "currency": "USD",
--   "language": "English",
--   "timezone": "UTC-5"
-- }
