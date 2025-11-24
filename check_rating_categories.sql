-- 检查评分项数据
SELECT 
    id,
    name,
    name_en,
    icon,
    is_default,
    is_active,
    display_order,
    created_at
FROM city_rating_categories
WHERE is_active = true
ORDER BY display_order;

-- 统计评分项数量
SELECT 
    COUNT(*) as total_count,
    COUNT(CASE WHEN is_default THEN 1 END) as default_count,
    COUNT(CASE WHEN is_active THEN 1 END) as active_count
FROM city_rating_categories;
