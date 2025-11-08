-- GeoNames 城市数据表
-- 用于存储从 GeoNames.org 导入的完整城市信息

CREATE TABLE IF NOT EXISTS geonames_cities (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    
    -- GeoNames 基础信息
    geoname_id BIGINT NOT NULL UNIQUE,
    name VARCHAR(200) NOT NULL,
    ascii_name VARCHAR(200),
    alternate_names JSONB,
    
    -- 地理坐标
    latitude DOUBLE PRECISION,
    longitude DOUBLE PRECISION,
    
    -- 特征分类
    feature_class VARCHAR(10),
    feature_code VARCHAR(10),
    
    -- 国家信息
    country_code VARCHAR(2) NOT NULL,
    country_name VARCHAR(200),
    
    -- 行政区划
    admin1_code VARCHAR(20),
    admin1_name VARCHAR(200),
    admin2_code VARCHAR(80),
    admin2_name VARCHAR(200),
    admin3_code VARCHAR(20),
    admin4_code VARCHAR(20),
    
    -- 人口和海拔
    population BIGINT,
    elevation INTEGER,
    dem INTEGER,
    
    -- 时区
    timezone VARCHAR(100),
    
    -- GeoNames 元数据
    modification_date TIMESTAMP,
    
    -- 同步状态
    synced_to_cities BOOLEAN DEFAULT false,
    city_id UUID REFERENCES cities(id),
    
    -- 时间戳
    imported_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    -- 备注
    notes TEXT
);

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_geonames_cities_geoname_id ON geonames_cities(geoname_id);
CREATE INDEX IF NOT EXISTS idx_geonames_cities_name ON geonames_cities(name);
CREATE INDEX IF NOT EXISTS idx_geonames_cities_country_code ON geonames_cities(country_code);
CREATE INDEX IF NOT EXISTS idx_geonames_cities_population ON geonames_cities(population DESC);
CREATE INDEX IF NOT EXISTS idx_geonames_cities_feature_code ON geonames_cities(feature_code);
CREATE INDEX IF NOT EXISTS idx_geonames_cities_synced ON geonames_cities(synced_to_cities);
CREATE INDEX IF NOT EXISTS idx_geonames_cities_city_id ON geonames_cities(city_id);

-- 创建更新时间触发器
CREATE OR REPLACE FUNCTION update_geonames_cities_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_update_geonames_cities_updated_at
    BEFORE UPDATE ON geonames_cities
    FOR EACH ROW
    EXECUTE FUNCTION update_geonames_cities_updated_at();

-- 添加注释
COMMENT ON TABLE geonames_cities IS '从 GeoNames.org 导入的完整城市数据';
COMMENT ON COLUMN geonames_cities.geoname_id IS 'GeoNames 唯一标识符';
COMMENT ON COLUMN geonames_cities.feature_class IS 'P=城市, A=行政区, H=水体等';
COMMENT ON COLUMN geonames_cities.feature_code IS 'PPLA=一级行政区首府, PPLC=首都, PPL=居住地';
COMMENT ON COLUMN geonames_cities.admin1_name IS '一级行政区名称(省/州)';
COMMENT ON COLUMN geonames_cities.admin2_name IS '二级行政区名称(市/县)';
COMMENT ON COLUMN geonames_cities.synced_to_cities IS '是否已同步到 cities 表';
COMMENT ON COLUMN geonames_cities.city_id IS '对应的 cities 表记录ID';
