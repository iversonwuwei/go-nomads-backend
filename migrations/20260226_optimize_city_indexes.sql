-- ============================================================================
-- City 相关表索引优化迁移
-- 日期: 2026-02-26
-- 说明: 为 cities 及关联表添加复合/部分索引，匹配实际查询模式；
--       创建 RPC 函数替代全表加载的 C# 计算；
--       清理冗余的单列索引
-- 约束: Supabase SQL Editor 自动包装隐式事务，
--       故使用 CREATE INDEX (非 CONCURRENTLY) 包裹在 BEGIN/COMMIT 中
-- ============================================================================

BEGIN;

-- ===================== 启用 pg_trgm 扩展（如果尚未启用）=====================
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- ===================== cities 表索引 =====================

-- 1. 城市列表主查询：is_active + is_deleted 基础过滤 + overall_score 排序
--    覆盖: GetAllAsync, SearchAsync, GetRecommendedAsync, GetPopularAsync
CREATE INDEX IF NOT EXISTS idx_cities_list_main
  ON cities (overall_score DESC)
  WHERE is_active = true AND is_deleted = false;

-- 2. 按国家ID查询 + 评分排序（高频操作）
--    覆盖: GetByCountryIdAsync, GetByCountryIdsAsync, GetByContinentAsync
CREATE INDEX IF NOT EXISTS idx_cities_by_country_id
  ON cities (country_id, overall_score DESC)
  WHERE is_active = true AND is_deleted = false;

-- 3. 按区域查询 + 评分排序
--    覆盖: GetByRegionAsync, GetCountByRegionAsync
CREATE INDEX IF NOT EXISTS idx_cities_by_region
  ON cities (region, overall_score DESC)
  WHERE is_active = true AND is_deleted = false;

-- 4. 推荐城市双排序
--    覆盖: GetRecommendedAsync, GetPopularAsync
CREATE INDEX IF NOT EXISTS idx_cities_recommended
  ON cities (overall_score DESC, community_score DESC)
  WHERE is_active = true AND is_deleted = false;

-- 5. 按国家名模糊搜索
--    覆盖: GetByCountryAsync
CREATE INDEX IF NOT EXISTS idx_cities_country_name_trgm
  ON cities USING GIN (country gin_trgm_ops);

-- ===================== city_moderators 复合索引 =====================

-- 6. 查询活跃版主（覆盖 GetByCityIdAsync with activeOnly=true）
CREATE INDEX IF NOT EXISTS idx_city_moderators_city_active
  ON city_moderators (city_id, is_active, created_at ASC);

-- ===================== city_pros_cons 复合索引 =====================

-- 7. 按城市查询优缺点（带软删除过滤和时间排序）
CREATE INDEX IF NOT EXISTS idx_city_pros_cons_city_active
  ON city_pros_cons (city_id, created_at DESC)
  WHERE is_deleted = false;

-- ===================== user_city_reviews 复合索引（分页查询优化）=====================

-- 8. 评论按城市+时间分页（GetByCityIdPagedAsync）
CREATE INDEX IF NOT EXISTS idx_user_city_reviews_city_paged
  ON user_city_reviews (city_id, created_at DESC);

-- ===================== user_city_photos 复合索引 =====================

-- 9. 照片按城市+时间分页
CREATE INDEX IF NOT EXISTS idx_user_city_photos_city_paged
  ON user_city_photos (city_id, created_at DESC);

-- ===================== user_favorite_cities 复合索引 =====================

-- 10. 用户收藏列表+时间排序
CREATE INDEX IF NOT EXISTS idx_user_favorite_cities_user_paged
  ON user_favorite_cities (user_id, created_at DESC);

-- ===================== nearby_cities 复合索引 =====================

-- 11. 按来源城市查附近城市+按距离排序
CREATE INDEX IF NOT EXISTS idx_nearby_cities_source_dist
  ON nearby_cities (source_city_id, distance_km ASC);

-- ===================== weather_cache 优化 =====================

-- 12. 天气缓存查询（按 city_id + expired_at 排序，查询时 WHERE expired_at > now() 可利用该索引）
CREATE INDEX IF NOT EXISTS idx_weather_cache_city_expired
  ON weather_cache (city_id, expired_at DESC);

-- ============================================================================
-- RPC 函数：find_nearest_city — 利用 PostGIS 替代 C# 内存计算
-- ============================================================================
CREATE OR REPLACE FUNCTION find_nearest_city(
  p_lat DOUBLE PRECISION,
  p_lng DOUBLE PRECISION,
  p_max_distance_km DOUBLE PRECISION DEFAULT 50.0
)
RETURNS TABLE (
  city_id UUID,
  city_name VARCHAR,
  distance_km DOUBLE PRECISION
)
LANGUAGE sql STABLE
AS $$
  SELECT
    c.id AS city_id,
    c.name AS city_name,
    ST_Distance(
      c.location::geography,
      ST_SetSRID(ST_MakePoint(p_lng, p_lat), 4326)::geography
    ) / 1000.0 AS distance_km
  FROM cities c
  WHERE c.is_active = true
    AND c.is_deleted = false
    AND c.location IS NOT NULL
    AND ST_DWithin(
      c.location::geography,
      ST_SetSRID(ST_MakePoint(p_lng, p_lat), 4326)::geography,
      p_max_distance_km * 1000  -- ST_DWithin 使用米为单位
    )
  ORDER BY distance_km ASC
  LIMIT 1;
$$;

-- ============================================================================
-- RPC 函数：search_cities_by_name — 利用 trgm 索引替代全表加载
-- ============================================================================
CREATE OR REPLACE FUNCTION search_cities_by_name(
  p_name TEXT,
  p_country_code TEXT DEFAULT NULL
)
RETURNS SETOF cities
LANGUAGE sql STABLE
AS $$
  SELECT *
  FROM cities
  WHERE is_active = true
    AND is_deleted = false
    AND (
      name ILIKE '%' || p_name || '%'
      OR name_en ILIKE '%' || p_name || '%'
    )
    AND (
      p_country_code IS NULL
      OR country = p_country_code
    )
  ORDER BY overall_score DESC NULLS LAST
  LIMIT 50;
$$;

-- ============================================================================
-- RPC 函数：get_city_review_avg_rating — 替代全量加载后内存计算
-- ============================================================================
CREATE OR REPLACE FUNCTION get_city_review_avg_rating(p_city_id TEXT)
RETURNS NUMERIC
LANGUAGE sql STABLE
AS $$
  SELECT ROUND(AVG(rating)::numeric, 2)
  FROM user_city_reviews
  WHERE city_id = p_city_id;
$$;

-- ============================================================================
-- RPC 函数：get_city_favorite_count — 替代 Select("id") + 内存 Count
-- ============================================================================
CREATE OR REPLACE FUNCTION get_city_favorite_count(p_city_id TEXT)
RETURNS INTEGER
LANGUAGE sql STABLE
AS $$
  SELECT COUNT(*)::INTEGER
  FROM user_favorite_cities
  WHERE city_id = p_city_id;
$$;

-- ============================================================================
-- RPC 函数：get_user_favorite_cities_count — 替代 Select("id") + 内存 Count
-- ============================================================================
CREATE OR REPLACE FUNCTION get_user_favorite_cities_count(p_user_id UUID)
RETURNS INTEGER
LANGUAGE sql STABLE
AS $$
  SELECT COUNT(*)::INTEGER
  FROM user_favorite_cities
  WHERE user_id = p_user_id;
$$;

-- ============================================================================
-- RPC 函数：get_city_total_count — 替代全表加载 + 内存 Count
-- ============================================================================
CREATE OR REPLACE FUNCTION get_city_total_count()
RETURNS INTEGER
LANGUAGE sql STABLE
AS $$
  SELECT COUNT(*)::INTEGER
  FROM cities
  WHERE is_active = true AND is_deleted = false;
$$;

-- ============================================================================
-- 清理冗余的单列索引（已被复合索引覆盖）
-- ============================================================================

-- idx_cities_active_score 被 idx_cities_list_main 覆盖（相同条件，相同排序）
DROP INDEX IF EXISTS idx_cities_active_score;

-- idx_cities_is_active 被所有部分索引的 WHERE 子句覆盖
DROP INDEX IF EXISTS idx_cities_is_active;

-- idx_city_moderators_is_active 被 idx_city_moderators_city_active 覆盖
DROP INDEX IF EXISTS idx_city_moderators_is_active;

-- idx_city_moderators_city_id 被 idx_city_moderators_city_active 覆盖（前导列）
DROP INDEX IF EXISTS idx_city_moderators_city_id;

-- idx_nearby_cities_source_city_id 被 idx_nearby_cities_source_dist 覆盖（前导列）
DROP INDEX IF EXISTS idx_nearby_cities_source_city_id;

-- idx_user_city_reviews_city_id 被 idx_user_city_reviews_city_paged 覆盖（前导列）
DROP INDEX IF EXISTS idx_user_city_reviews_city_id;

-- idx_user_city_photos_city_id 被 idx_user_city_photos_city_paged 覆盖（前导列）
DROP INDEX IF EXISTS idx_user_city_photos_city_id;

-- idx_user_favorite_cities_user_id 被 idx_user_favorite_cities_user_paged 覆盖（前导列）
DROP INDEX IF EXISTS idx_user_favorite_cities_user_id;

-- ============================================================================
-- 统计信息刷新
-- ============================================================================
ANALYZE cities;
ANALYZE city_moderators;
ANALYZE city_pros_cons;
ANALYZE user_city_reviews;
ANALYZE user_city_photos;
ANALYZE user_favorite_cities;
ANALYZE nearby_cities;
ANALYZE weather_cache;

COMMIT;
