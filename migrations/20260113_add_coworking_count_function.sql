-- ============================================
-- 添加获取有 coworking 空间的城市列表函数
-- 使用数据库聚合查询，避免在应用层处理大量数据
-- 注意：这些函数对所有用户开放，无需认证
-- ============================================

-- 函数：获取有 coworking 空间的城市ID和数量
-- 返回：城市ID、coworking数量
CREATE OR REPLACE FUNCTION public.get_cities_with_coworking_count()
RETURNS TABLE (
    city_id UUID,
    coworking_count BIGINT
)
LANGUAGE sql
STABLE
SECURITY INVOKER
AS $$
    SELECT 
        cs.city_id,
        COUNT(*) as coworking_count
    FROM public.coworking_spaces cs
    WHERE cs.city_id IS NOT NULL
      AND cs.is_active = true
      AND (cs.is_deleted IS NULL OR cs.is_deleted = false)
    GROUP BY cs.city_id
    HAVING COUNT(*) > 0
    ORDER BY coworking_count DESC;
$$;

-- 添加注释
COMMENT ON FUNCTION public.get_cities_with_coworking_count() IS '获取有 coworking 空间的城市ID和数量，用于 coworking_home 页面（公开访问）';

-- 授权给所有角色（包括匿名用户）
GRANT EXECUTE ON FUNCTION public.get_cities_with_coworking_count() TO PUBLIC;

-- 函数：获取有 coworking 空间的城市详细信息（带分页）
-- 一次查询获取城市信息和 coworking 数量，避免多次调用
CREATE OR REPLACE FUNCTION public.get_cities_with_coworking_details(
    p_page INT DEFAULT 1,
    p_page_size INT DEFAULT 20
)
RETURNS TABLE (
    id UUID,
    name VARCHAR(100),
    country VARCHAR(100),
    region VARCHAR(100),
    description TEXT,
    image_url TEXT,
    overall_score DECIMAL(3,2),
    latitude DECIMAL(10,8),
    longitude DECIMAL(11,8),
    coworking_count BIGINT,
    total_count BIGINT
)
LANGUAGE sql
STABLE
SECURITY INVOKER
AS $$
    WITH coworking_stats AS (
        SELECT 
            cs.city_id,
            COUNT(*) as coworking_count
        FROM public.coworking_spaces cs
        WHERE cs.city_id IS NOT NULL
          AND cs.is_active = true
          AND (cs.is_deleted IS NULL OR cs.is_deleted = false)
        GROUP BY cs.city_id
        HAVING COUNT(*) > 0
    ),
    cities_with_coworking AS (
        SELECT 
            c.id,
            c.name,
            c.country,
            c.region,
            c.description,
            c.image_url,
            c.overall_score,
            c.latitude,
            c.longitude,
            cs.coworking_count,
            COUNT(*) OVER() as total_count
        FROM public.cities c
        INNER JOIN coworking_stats cs ON c.id = cs.city_id
        WHERE c.is_active = true
        ORDER BY c.overall_score DESC NULLS LAST
    )
    SELECT *
    FROM cities_with_coworking
    OFFSET (p_page - 1) * p_page_size
    LIMIT p_page_size;
$$;

-- 添加注释
COMMENT ON FUNCTION public.get_cities_with_coworking_details(INT, INT) IS '获取有 coworking 空间的城市详细信息（带分页），一次查询返回城市信息和 coworking 数量（公开访问）';

-- 授权给所有角色（包括匿名用户）
GRANT EXECUTE ON FUNCTION public.get_cities_with_coworking_details(INT, INT) TO PUBLIC;
GRANT EXECUTE ON FUNCTION public.get_cities_with_coworking_details(INT, INT) TO service_role;
