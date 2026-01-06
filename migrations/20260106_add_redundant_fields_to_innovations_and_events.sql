-- ============================================================
-- 方案4：为 innovations 和 events 表添加冗余字段
-- 目的：消除对 UserService 和 CityService 的运行时依赖
-- 日期：2026-01-06
-- ============================================================

-- ============================================================
-- Part 1: innovations 表
-- ============================================================

-- 1.1 添加冗余字段
ALTER TABLE innovations 
ADD COLUMN IF NOT EXISTS creator_name VARCHAR(255),
ADD COLUMN IF NOT EXISTS creator_avatar VARCHAR(500);

-- 1.2 添加注释
COMMENT ON COLUMN innovations.creator_name IS '冗余字段：创建者名称，来源于 users.name';
COMMENT ON COLUMN innovations.creator_avatar IS '冗余字段：创建者头像，来源于 users.avatar';

-- 1.3 从现有数据填充
UPDATE innovations i
SET 
    creator_name = u.name,
    creator_avatar = u.avatar
FROM users u
WHERE i.creator_id = u.id
  AND i.creator_id IS NOT NULL
  AND (i.creator_name IS NULL OR i.creator_name = '');

-- 1.4 创建索引
CREATE INDEX IF NOT EXISTS idx_innovations_creator_name ON innovations(creator_name);

-- ============================================================
-- Part 2: events 表
-- ============================================================

-- 2.1 添加冗余字段
ALTER TABLE events 
ADD COLUMN IF NOT EXISTS organizer_name VARCHAR(255),
ADD COLUMN IF NOT EXISTS organizer_avatar VARCHAR(500),
ADD COLUMN IF NOT EXISTS city_name VARCHAR(255),
ADD COLUMN IF NOT EXISTS city_name_en VARCHAR(255),
ADD COLUMN IF NOT EXISTS city_country VARCHAR(100);

-- 2.2 添加注释
COMMENT ON COLUMN events.organizer_name IS '冗余字段：组织者名称，来源于 users.name';
COMMENT ON COLUMN events.organizer_avatar IS '冗余字段：组织者头像，来源于 users.avatar';
COMMENT ON COLUMN events.city_name IS '冗余字段：城市名称（中文），来源于 cities.name';
COMMENT ON COLUMN events.city_name_en IS '冗余字段：城市名称（英文），来源于 cities.name_en';
COMMENT ON COLUMN events.city_country IS '冗余字段：城市所属国家，来源于 cities.country';

-- 2.3 从现有数据填充
UPDATE events e
SET 
    organizer_name = u.name,
    organizer_avatar = u.avatar
FROM users u
WHERE e.organizer_id = u.id
  AND e.organizer_id IS NOT NULL
  AND (e.organizer_name IS NULL OR e.organizer_name = '');

UPDATE events e
SET 
    city_name = c.name,
    city_name_en = c.name_en,
    city_country = c.country
FROM cities c
WHERE e.city_id = c.id
  AND e.city_id IS NOT NULL
  AND (e.city_name IS NULL OR e.city_name = '');

-- 2.4 创建索引
CREATE INDEX IF NOT EXISTS idx_events_organizer_name ON events(organizer_name);
CREATE INDEX IF NOT EXISTS idx_events_city_name ON events(city_name);

-- ============================================================
-- Part 3: innovation_comments 表（评论者信息）
-- ============================================================

-- 3.1 添加冗余字段
ALTER TABLE innovation_comments 
ADD COLUMN IF NOT EXISTS user_name VARCHAR(255),
ADD COLUMN IF NOT EXISTS user_avatar VARCHAR(500);

-- 3.2 添加注释
COMMENT ON COLUMN innovation_comments.user_name IS '冗余字段：评论者名称，来源于 users.name';
COMMENT ON COLUMN innovation_comments.user_avatar IS '冗余字段：评论者头像，来源于 users.avatar';

-- 3.3 从现有数据填充
UPDATE innovation_comments ic
SET 
    user_name = u.name,
    user_avatar = u.avatar
FROM users u
WHERE ic.user_id = u.id
  AND ic.user_id IS NOT NULL
  AND (ic.user_name IS NULL OR ic.user_name = '');

-- ============================================================
-- Part 4: 验证迁移结果
-- ============================================================

DO $$
DECLARE
    innovations_total INTEGER;
    innovations_filled INTEGER;
    events_total INTEGER;
    events_organizer_filled INTEGER;
    events_city_filled INTEGER;
    comments_total INTEGER;
    comments_filled INTEGER;
BEGIN
    -- innovations 统计
    SELECT COUNT(*) INTO innovations_total FROM innovations WHERE is_deleted = false;
    SELECT COUNT(*) INTO innovations_filled FROM innovations WHERE creator_name IS NOT NULL AND is_deleted = false;
    
    -- events 统计
    SELECT COUNT(*) INTO events_total FROM events WHERE is_deleted = false;
    SELECT COUNT(*) INTO events_organizer_filled FROM events WHERE organizer_name IS NOT NULL AND is_deleted = false;
    SELECT COUNT(*) INTO events_city_filled FROM events WHERE city_name IS NOT NULL AND is_deleted = false;
    
    -- comments 统计
    SELECT COUNT(*) INTO comments_total FROM innovation_comments;
    SELECT COUNT(*) INTO comments_filled FROM innovation_comments WHERE user_name IS NOT NULL;
    
    RAISE NOTICE '========== 迁移完成统计 ==========';
    RAISE NOTICE 'innovations 表:';
    RAISE NOTICE '  总记录数: %', innovations_total;
    RAISE NOTICE '  已填充创建者信息: %', innovations_filled;
    RAISE NOTICE '';
    RAISE NOTICE 'events 表:';
    RAISE NOTICE '  总记录数: %', events_total;
    RAISE NOTICE '  已填充组织者信息: %', events_organizer_filled;
    RAISE NOTICE '  已填充城市信息: %', events_city_filled;
    RAISE NOTICE '';
    RAISE NOTICE 'innovation_comments 表:';
    RAISE NOTICE '  总记录数: %', comments_total;
    RAISE NOTICE '  已填充用户信息: %', comments_filled;
END $$;
