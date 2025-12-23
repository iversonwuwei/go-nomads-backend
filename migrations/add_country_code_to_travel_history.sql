-- 添加国家代码字段到旅行历史表
-- Migration: add_country_code_to_travel_history
-- Date: 2025-12-23
-- 用途: 支持 Nomads Stats 统计功能，使用 ISO 3166-1 alpha-2 国家代码

-- 添加 country_code 列 (ISO 3166-1 alpha-2 格式，如 CN, US, JP)
ALTER TABLE travel_history 
ADD COLUMN IF NOT EXISTS country_code VARCHAR(2);

-- 创建索引以支持按国家统计
CREATE INDEX IF NOT EXISTS idx_travel_history_country_code ON travel_history(country_code);

-- 添加注释
COMMENT ON COLUMN travel_history.country_code IS '国家代码 (ISO 3166-1 alpha-2)，用于统计';

-- 更新已有数据的 country_code（基于国家名称的常见映射）
-- 注意：这是一个基础映射，可能需要手动补充
UPDATE travel_history
SET country_code = CASE
    WHEN LOWER(country) LIKE '%china%' OR country = '中国' THEN 'CN'
    WHEN LOWER(country) LIKE '%united states%' OR LOWER(country) = 'usa' OR country = '美国' THEN 'US'
    WHEN LOWER(country) LIKE '%japan%' OR country = '日本' THEN 'JP'
    WHEN LOWER(country) LIKE '%korea%' OR country = '韩国' THEN 'KR'
    WHEN LOWER(country) LIKE '%thailand%' OR country = '泰国' THEN 'TH'
    WHEN LOWER(country) LIKE '%vietnam%' OR country = '越南' THEN 'VN'
    WHEN LOWER(country) LIKE '%singapore%' OR country = '新加坡' THEN 'SG'
    WHEN LOWER(country) LIKE '%malaysia%' OR country = '马来西亚' THEN 'MY'
    WHEN LOWER(country) LIKE '%indonesia%' OR country = '印度尼西亚' OR country = '印尼' THEN 'ID'
    WHEN LOWER(country) LIKE '%philippines%' OR country = '菲律宾' THEN 'PH'
    WHEN LOWER(country) LIKE '%australia%' OR country = '澳大利亚' THEN 'AU'
    WHEN LOWER(country) LIKE '%new zealand%' OR country = '新西兰' THEN 'NZ'
    WHEN LOWER(country) LIKE '%united kingdom%' OR LOWER(country) = 'uk' OR country = '英国' THEN 'GB'
    WHEN LOWER(country) LIKE '%germany%' OR country = '德国' THEN 'DE'
    WHEN LOWER(country) LIKE '%france%' OR country = '法国' THEN 'FR'
    WHEN LOWER(country) LIKE '%italy%' OR country = '意大利' THEN 'IT'
    WHEN LOWER(country) LIKE '%spain%' OR country = '西班牙' THEN 'ES'
    WHEN LOWER(country) LIKE '%portugal%' OR country = '葡萄牙' THEN 'PT'
    WHEN LOWER(country) LIKE '%netherlands%' OR country = '荷兰' THEN 'NL'
    WHEN LOWER(country) LIKE '%canada%' OR country = '加拿大' THEN 'CA'
    WHEN LOWER(country) LIKE '%mexico%' OR country = '墨西哥' THEN 'MX'
    WHEN LOWER(country) LIKE '%brazil%' OR country = '巴西' THEN 'BR'
    WHEN LOWER(country) LIKE '%india%' OR country = '印度' THEN 'IN'
    WHEN LOWER(country) LIKE '%russia%' OR country = '俄罗斯' THEN 'RU'
    WHEN LOWER(country) LIKE '%turkey%' OR country = '土耳其' THEN 'TR'
    WHEN LOWER(country) LIKE '%egypt%' OR country = '埃及' THEN 'EG'
    WHEN LOWER(country) LIKE '%south africa%' OR country = '南非' THEN 'ZA'
    WHEN LOWER(country) LIKE '%united arab emirates%' OR LOWER(country) = 'uae' OR country = '阿联酋' THEN 'AE'
    ELSE NULL
END
WHERE country_code IS NULL;
