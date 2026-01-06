-- ============================================================
-- 方案4：为 coworking_spaces 表添加冗余字段
-- 目的：消除对 UserService 和 CityService 的运行时依赖
-- 日期：2026-01-06
-- ============================================================

-- 1. 添加冗余字段
ALTER TABLE coworking_spaces 
ADD COLUMN IF NOT EXISTS creator_name VARCHAR(255),
ADD COLUMN IF NOT EXISTS creator_avatar VARCHAR(500),
ADD COLUMN IF NOT EXISTS city_name VARCHAR(255),
ADD COLUMN IF NOT EXISTS city_name_en VARCHAR(255),
ADD COLUMN IF NOT EXISTS city_country VARCHAR(100);

-- 2. 添加注释说明这些是冗余字段
COMMENT ON COLUMN coworking_spaces.creator_name IS '冗余字段：创建者名称，来源于 users.name';
COMMENT ON COLUMN coworking_spaces.creator_avatar IS '冗余字段：创建者头像，来源于 users.avatar_url';
COMMENT ON COLUMN coworking_spaces.city_name IS '冗余字段：城市名称（中文），来源于 cities.name';
COMMENT ON COLUMN coworking_spaces.city_name_en IS '冗余字段：城市名称（英文），来源于 cities.name_en';
COMMENT ON COLUMN coworking_spaces.city_country IS '冗余字段：城市所属国家，来源于 cities.country';

-- 3. 从现有数据填充冗余字段（一次性迁移）
UPDATE coworking_spaces cs
SET 
    creator_name = u.name,
    creator_avatar = u.avatar
FROM users u
WHERE cs.created_by = u.id
  AND cs.created_by IS NOT NULL
  AND (cs.creator_name IS NULL OR cs.creator_name = '');

UPDATE coworking_spaces cs
SET 
    city_name = c.name,
    city_name_en = c.name_en,
    city_country = c.country
FROM cities c
WHERE cs.city_id = c.id
  AND cs.city_id IS NOT NULL
  AND (cs.city_name IS NULL OR cs.city_name = '');

-- 4. 创建索引优化查询（可选，如果需要按城市名搜索）
CREATE INDEX IF NOT EXISTS idx_coworking_spaces_city_name ON coworking_spaces(city_name);
CREATE INDEX IF NOT EXISTS idx_coworking_spaces_creator_name ON coworking_spaces(creator_name);

-- 5. 验证迁移结果
DO $$
DECLARE
    total_count INTEGER;
    filled_creator_count INTEGER;
    filled_city_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO total_count FROM coworking_spaces WHERE is_deleted = false;
    SELECT COUNT(*) INTO filled_creator_count FROM coworking_spaces WHERE creator_name IS NOT NULL AND is_deleted = false;
    SELECT COUNT(*) INTO filled_city_count FROM coworking_spaces WHERE city_name IS NOT NULL AND is_deleted = false;
    
    RAISE NOTICE '迁移完成统计:';
    RAISE NOTICE '  总记录数: %', total_count;
    RAISE NOTICE '  已填充创建者信息: %', filled_creator_count;
    RAISE NOTICE '  已填充城市信息: %', filled_city_count;
END $$;
