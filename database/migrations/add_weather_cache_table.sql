-- Weather Cache Table
-- 天气数据缓存表 - 用于减少第三方 API 调用和提升性能

-- 创建天气缓存表
CREATE TABLE IF NOT EXISTS weather_cache (
    city_id UUID PRIMARY KEY,
    city_name VARCHAR(100) NOT NULL,
    country_code VARCHAR(10),
    
    -- 天气基础信息
    temperature DECIMAL(5, 2) NOT NULL, -- 温度（摄氏度）
    feels_like DECIMAL(5, 2), -- 体感温度
    weather_condition VARCHAR(50) NOT NULL, -- 天气状况（如 Clear, Clouds, Rain）
    description TEXT, -- 天气详细描述（如 晴朗, 多云）
    icon_code VARCHAR(10), -- OpenWeatherMap 图标代码
    
    -- 详细气象数据
    humidity INTEGER, -- 湿度（%）
    pressure INTEGER, -- 气压（hPa）
    wind_speed DECIMAL(5, 2), -- 风速（m/s）
    wind_direction INTEGER, -- 风向（度数）
    clouds INTEGER, -- 云量（%）
    visibility INTEGER, -- 可见度（米）
    
    -- 日出日落时间
    sunrise TIMESTAMP WITH TIME ZONE,
    sunset TIMESTAMP WITH TIME ZONE,
    
    -- 缓存元数据
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    expired_at TIMESTAMP WITH TIME ZONE NOT NULL,
    api_source VARCHAR(50) DEFAULT 'openweathermap', -- API 来源
    
    -- 外键约束（如果 cities 表存在）
    CONSTRAINT fk_weather_cache_city FOREIGN KEY (city_id) 
        REFERENCES cities(id) ON DELETE CASCADE
);

-- 创建索引优化查询性能
CREATE INDEX IF NOT EXISTS idx_weather_cache_expired_at 
    ON weather_cache(expired_at);

CREATE INDEX IF NOT EXISTS idx_weather_cache_city_name 
    ON weather_cache(city_name);

CREATE INDEX IF NOT EXISTS idx_weather_cache_updated_at 
    ON weather_cache(updated_at DESC);

-- 创建复合索引用于查询有效缓存
CREATE INDEX IF NOT EXISTS idx_weather_cache_valid 
    ON weather_cache(city_id, expired_at) 
    WHERE expired_at > CURRENT_TIMESTAMP;

-- 添加注释
COMMENT ON TABLE weather_cache IS '天气数据缓存表，用于减少对 OpenWeatherMap API 的调用频率';
COMMENT ON COLUMN weather_cache.city_id IS '城市ID（主键）';
COMMENT ON COLUMN weather_cache.temperature IS '当前温度（摄氏度）';
COMMENT ON COLUMN weather_cache.weather_condition IS '天气状况代码';
COMMENT ON COLUMN weather_cache.expired_at IS '缓存过期时间，建议设置为 1-2 小时后';
COMMENT ON COLUMN weather_cache.updated_at IS '数据最后更新时间';

-- 创建自动更新 updated_at 的触发器
CREATE OR REPLACE FUNCTION update_weather_cache_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_update_weather_cache_updated_at
    BEFORE UPDATE ON weather_cache
    FOR EACH ROW
    EXECUTE FUNCTION update_weather_cache_updated_at();

-- 创建清理过期缓存的函数
CREATE OR REPLACE FUNCTION clean_expired_weather_cache()
RETURNS INTEGER AS $$
DECLARE
    deleted_count INTEGER;
BEGIN
    DELETE FROM weather_cache 
    WHERE expired_at < CURRENT_TIMESTAMP - INTERVAL '1 day';
    
    GET DIAGNOSTICS deleted_count = ROW_COUNT;
    
    RETURN deleted_count;
END;
$$ LANGUAGE plpgsql;

COMMENT ON FUNCTION clean_expired_weather_cache() IS '清理过期超过1天的天气缓存数据';

-- 可以设置定时任务定期清理（需要 pg_cron 扩展）
-- SELECT cron.schedule('clean-weather-cache', '0 2 * * *', 'SELECT clean_expired_weather_cache();');
