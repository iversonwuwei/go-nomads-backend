-- ====================================
-- 迁移脚本：为 digital_nomad_guides 和 nearby_cities 添加 user_id 字段
-- 使数据从公共共享变为用户专属
-- ====================================

-- 1. digital_nomad_guides 添加 user_id 列
ALTER TABLE digital_nomad_guides 
    ADD COLUMN IF NOT EXISTS user_id TEXT NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001';

-- 移除旧的 city_id 唯一约束（每个城市只有一个指南）
DROP INDEX IF EXISTS idx_digital_nomad_guides_city_id_unique;

-- 创建新的复合唯一约束（每个用户每个城市只有一个指南）
CREATE UNIQUE INDEX IF NOT EXISTS idx_digital_nomad_guides_user_city_unique 
    ON digital_nomad_guides(user_id, city_id);

-- 创建 user_id 索引
CREATE INDEX IF NOT EXISTS idx_digital_nomad_guides_user_id 
    ON digital_nomad_guides(user_id);

-- 添加注释
COMMENT ON COLUMN digital_nomad_guides.user_id IS '用户ID，指南归属的用户';

-- 2. nearby_cities 添加 user_id 列
ALTER TABLE nearby_cities 
    ADD COLUMN IF NOT EXISTS user_id TEXT NOT NULL DEFAULT '00000000-0000-0000-0000-000000000001';

-- 移除旧的复合唯一约束（同一源城市不能有重复的目标城市）
DROP INDEX IF EXISTS idx_nearby_cities_source_target_unique;

-- 创建新的复合唯一约束（同一用户、同一源城市不能有重复的目标城市）
CREATE UNIQUE INDEX IF NOT EXISTS idx_nearby_cities_user_source_target_unique 
    ON nearby_cities(user_id, source_city_id, target_city_name);

-- 创建 user_id 索引
CREATE INDEX IF NOT EXISTS idx_nearby_cities_user_id 
    ON nearby_cities(user_id);

-- 添加注释
COMMENT ON COLUMN nearby_cities.user_id IS '用户ID，附近城市数据归属的用户';

-- ====================================
-- 验证迁移结果
-- ====================================
-- SELECT column_name, data_type, is_nullable, column_default 
-- FROM information_schema.columns 
-- WHERE table_name IN ('digital_nomad_guides', 'nearby_cities') 
--   AND column_name = 'user_id';
