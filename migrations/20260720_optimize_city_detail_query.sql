-- ============================================================================
-- 城市详情查询优化迁移
-- 日期: 2026-07-20
-- 说明: 
--   1. 创建 get_city_by_id RPC 函数 — 单次查询返回完整城市数据（含 landscape_image_urls）
--   2. 创建 search_cities_v2 RPC 函数 — 多字段模糊搜索（name + name_en + country）
--   3. 添加 name_en 的 pg_trgm GIN 索引，优化英文搜索
--   4. 创建 get_city_with_ratings RPC — 单次查询城市 + 评论统计
-- ============================================================================

BEGIN;

-- ============================================================================
-- RPC 函数：get_city_by_id — 通过 ID 获取城市完整数据
-- 用途：替代 Postgrest ORM 查询 + 单独 HTTP 获取 landscape_image_urls 的双重调用
-- 返回：单个 JSON 对象（利用 Supabase RPC 直接执行）
-- ============================================================================
CREATE OR REPLACE FUNCTION get_city_by_id(p_city_id UUID)
RETURNS SETOF cities
LANGUAGE sql STABLE
AS $$
  SELECT *
  FROM cities
  WHERE id = p_city_id
    AND is_deleted = false
  LIMIT 1;
$$;

-- ============================================================================
-- RPC 函数：search_cities_v2 — 多字段模糊搜索 + pg_trgm 相似度排序
-- 用途：替代当前仅搜索 name 字段的 ILIKE 查询
-- 支持：中文名、英文名、国家名同时搜索
-- ============================================================================
CREATE OR REPLACE FUNCTION search_cities_v2(
  p_query TEXT,
  p_country TEXT DEFAULT NULL,
  p_region TEXT DEFAULT NULL,
  p_min_score NUMERIC DEFAULT NULL,
  p_page_number INT DEFAULT 1,
  p_page_size INT DEFAULT 20
)
RETURNS SETOF cities
LANGUAGE sql STABLE
AS $$
  SELECT c.*
  FROM cities c
  WHERE c.is_active = true
    AND c.is_deleted = false
    AND (
      p_query IS NULL
      OR p_query = ''
      OR c.name ILIKE '%' || p_query || '%'
      OR c.name_en ILIKE '%' || p_query || '%'
      OR c.country ILIKE '%' || p_query || '%'
    )
    AND (
      p_country IS NULL
      OR c.country ILIKE '%' || p_country || '%'
    )
    AND (
      p_region IS NULL
      OR c.region = p_region
    )
    AND (
      p_min_score IS NULL
      OR c.overall_score >= p_min_score
    )
  ORDER BY
    -- 当有搜索词时，按相似度排序（精确匹配优先），否则按评分排序
    CASE
      WHEN p_query IS NOT NULL AND p_query != '' THEN
        GREATEST(
          similarity(c.name, p_query),
          similarity(COALESCE(c.name_en, ''), p_query)
        )
      ELSE 0
    END DESC,
    c.overall_score DESC NULLS LAST
  LIMIT p_page_size
  OFFSET (p_page_number - 1) * p_page_size;
$$;

-- ============================================================================
-- RPC 函数：get_city_with_review_stats — 城市详情 + 评论统计一次查询
-- 用途：减少 GetCityByIdAsync 中对 CacheService 的额外 HTTP 调用
-- ============================================================================
CREATE OR REPLACE FUNCTION get_city_with_review_stats(p_city_id UUID)
RETURNS TABLE (
  city_id UUID,
  city_name TEXT,
  review_count BIGINT,
  avg_rating NUMERIC,
  avg_cost NUMERIC
)
LANGUAGE sql STABLE
AS $$
  SELECT
    c.id AS city_id,
    c.name AS city_name,
    COALESCE(r.review_count, 0) AS review_count,
    COALESCE(r.avg_rating, 0) AS avg_rating,
    COALESCE(e.avg_cost, 0) AS avg_cost
  FROM cities c
  LEFT JOIN LATERAL (
    SELECT
      COUNT(DISTINCT user_id) AS review_count,
      ROUND(AVG(rating)::numeric, 2) AS avg_rating
    FROM user_city_reviews
    WHERE city_id = c.id::text
  ) r ON true
  LEFT JOIN LATERAL (
    SELECT
      ROUND(AVG(amount)::numeric, 2) AS avg_cost
    FROM user_city_expenses
    WHERE city_id = c.id::text
  ) e ON true
  WHERE c.id = p_city_id
    AND c.is_deleted = false;
$$;

-- ============================================================================
-- 索引：name_en 模糊搜索索引（GIN + pg_trgm）
-- ============================================================================
CREATE INDEX IF NOT EXISTS idx_cities_name_en_trgm
  ON cities USING GIN (name_en gin_trgm_ops);

-- ============================================================================
-- 索引：name 模糊搜索索引（GIN + pg_trgm）— 如不存在则创建
-- ============================================================================
CREATE INDEX IF NOT EXISTS idx_cities_name_trgm
  ON cities USING GIN (name gin_trgm_ops);

-- ============================================================================
-- 索引：城市 ID 查找索引（含 is_deleted 过滤条件）
-- ============================================================================
CREATE INDEX IF NOT EXISTS idx_cities_by_id_not_deleted
  ON cities (id)
  WHERE is_deleted = false;

-- ============================================================================
-- 统计信息刷新
-- ============================================================================
ANALYZE cities;
ANALYZE user_city_reviews;
ANALYZE user_city_expenses;

COMMIT;
