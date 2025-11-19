-- 测试数据脚本 - 用于测试城市评分功能
-- 注意：需要先执行 city_rating_system.sql 创建表结构

-- 1. 先查看现有的城市ID和用户ID
-- SELECT id, name FROM cities LIMIT 5;
-- SELECT id, email FROM auth.users LIMIT 5;

-- 2. 插入测试评分数据
-- 请替换下面的 city_id 和 user_id 为你数据库中实际存在的值

-- 示例：为某个城市添加测试评分
-- 假设城市ID为: '123e4567-e89b-12d3-a456-426614174000'
-- 假设用户ID为: '987fcdeb-51a2-43d7-9876-543210fedcba'

/*
INSERT INTO city_ratings (city_id, user_id, category_id, rating) 
SELECT 
    '123e4567-e89b-12d3-a456-426614174000'::uuid, -- 替换为真实城市ID
    '987fcdeb-51a2-43d7-9876-543210fedcba'::uuid, -- 替换为真实用户ID
    id,
    FLOOR(RANDOM() * 5 + 1)::integer -- 随机1-5分
FROM city_rating_categories
WHERE is_default = true
ON CONFLICT (city_id, user_id, category_id) DO NOTHING;
*/

-- 3. 查询评分统计
-- 替换为你的城市ID查看结果
/*
SELECT 
    crc.name as category_name,
    COUNT(cr.id) as rating_count,
    ROUND(AVG(cr.rating)::numeric, 1) as average_rating
FROM city_rating_categories crc
LEFT JOIN city_ratings cr ON crc.id = cr.category_id 
    AND cr.city_id = '123e4567-e89b-12d3-a456-426614174000'::uuid
WHERE crc.is_active = true
GROUP BY crc.id, crc.name, crc.display_order
ORDER BY crc.display_order;
*/

-- 4. 查看城市总得分
/*
SELECT 
    ROUND(AVG(avg_rating), 1) as overall_score
FROM (
    SELECT 
        crc.id,
        AVG(cr.rating) as avg_rating
    FROM city_rating_categories crc
    LEFT JOIN city_ratings cr ON crc.id = cr.category_id 
        AND cr.city_id = '123e4567-e89b-12d3-a456-426614174000'::uuid
    WHERE crc.is_active = true
    GROUP BY crc.id
    HAVING COUNT(cr.id) > 0
) subquery;
*/
