-- 地理数据三级结构：国家 -> 省份 -> 城市
-- 创建时间: 2025-10-23

-- 1. 创建国家表
CREATE TABLE IF NOT EXISTS countries (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    name_zh VARCHAR(100) NOT NULL,
    code VARCHAR(2) NOT NULL UNIQUE, -- ISO 3166-1 alpha-2
    code_alpha3 VARCHAR(3), -- ISO 3166-1 alpha-3
    continent VARCHAR(50),
    flag_url VARCHAR(500),
    calling_code VARCHAR(20),
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE
);

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_countries_code ON countries(code);
CREATE INDEX IF NOT EXISTS idx_countries_continent ON countries(continent);
CREATE INDEX IF NOT EXISTS idx_countries_is_active ON countries(is_active);

-- 2. 创建省份表
CREATE TABLE IF NOT EXISTS provinces (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    country_id UUID NOT NULL REFERENCES countries(id) ON DELETE CASCADE,
    code VARCHAR(10),
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE,
    UNIQUE(country_id, name)
);

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_provinces_country_id ON provinces(country_id);
CREATE INDEX IF NOT EXISTS idx_provinces_code ON provinces(code);
CREATE INDEX IF NOT EXISTS idx_provinces_is_active ON provinces(is_active);

-- 3. 修改城市表，添加外键关联
-- 注意：如果 cities 表已存在，使用 ALTER TABLE
ALTER TABLE cities ADD COLUMN IF NOT EXISTS country_id UUID REFERENCES countries(id) ON DELETE SET NULL;
ALTER TABLE cities ADD COLUMN IF NOT EXISTS province_id UUID REFERENCES provinces(id) ON DELETE SET NULL;

-- 创建新索引
CREATE INDEX IF NOT EXISTS idx_cities_country_id ON cities(country_id);
CREATE INDEX IF NOT EXISTS idx_cities_province_id ON cities(province_id);

-- 4. 添加注释
COMMENT ON TABLE countries IS '国家表';
COMMENT ON TABLE provinces IS '省份/州表';
COMMENT ON COLUMN cities.country_id IS '国家外键';
COMMENT ON COLUMN cities.province_id IS '省份外键';

-- 5. 插入中国数据
INSERT INTO countries (id, name, name_zh, code, code_alpha3, continent, is_active)
VALUES ('00000000-0000-0000-0000-000000000001', 'China', '中国', 'CN', 'CHN', 'Asia', true)
ON CONFLICT (code) DO NOTHING;

-- 查询结果
SELECT 
    'countries' as table_name, 
    COUNT(*) as row_count 
FROM countries
UNION ALL
SELECT 
    'provinces' as table_name, 
    COUNT(*) as row_count 
FROM provinces
UNION ALL
SELECT 
    'cities' as table_name, 
    COUNT(*) as row_count 
FROM cities;
