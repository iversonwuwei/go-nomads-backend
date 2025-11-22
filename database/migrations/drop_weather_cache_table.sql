-- Drop Weather Cache Table and Related Objects
-- 删除天气缓存表及相关对象

-- 删除定时任务（如果存在）
-- DROP EXTENSION IF EXISTS pg_cron CASCADE;
-- SELECT cron.unschedule('clean-weather-cache');

-- 删除触发器（必须先删除触发器，再删除函数）
DROP TRIGGER IF EXISTS trigger_update_weather_cache_updated_at ON weather_cache;

-- 删除函数
DROP FUNCTION IF EXISTS clean_expired_weather_cache();
DROP FUNCTION IF EXISTS update_weather_cache_updated_at();

-- 删除索引
DROP INDEX IF EXISTS idx_weather_cache_valid;
DROP INDEX IF EXISTS idx_weather_cache_updated_at;
DROP INDEX IF EXISTS idx_weather_cache_city_name;
DROP INDEX IF EXISTS idx_weather_cache_expired_at;

-- 删除表
DROP TABLE IF EXISTS weather_cache CASCADE;

-- 确认删除
SELECT 'Weather cache table and all related objects have been dropped successfully' AS status;
