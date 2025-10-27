-- 修复 Coworking 表中的城市ID映射
-- 问题: coworking 表中的 city_id 与 cities 表中的真实 ID 不匹配
-- 解决: 将测试数据的 city_id 更新为真实城市的 ID

-- 1. 查看当前 coworking 数据的 city_id 分布
SELECT 
    COALESCE(city_id::text, 'NULL') as city_id,
    COUNT(*) as count,
    STRING_AGG(name, ', ') as coworking_names
FROM coworkings
GROUP BY city_id;

-- 2. 查看cities表中的前10个城市ID和名称
SELECT id, name, country, region
FROM cities
WHERE country = 'China'
ORDER BY created_at
LIMIT 10;

-- 3. 更新coworking数据的city_id
-- 将"北京创新共享办公空间"关联到北京市
UPDATE coworkings
SET city_id = (SELECT id FROM cities WHERE name = '北京市' AND country = 'China' LIMIT 1)
WHERE name LIKE '%北京%' AND (city_id IS NULL OR city_id::text = '8503bc5a-bfe9-4fcf-87ea-85586bb3653f');

-- 将"上海创意共享办公空间"关联到上海市
UPDATE coworkings
SET city_id = (SELECT id FROM cities WHERE name = '上海市' AND country = 'China' LIMIT 1)
WHERE name LIKE '%上海%' AND city_id IS NULL;

-- 将其他测试数据关联到杭州市(如果没有更合适的城市)
UPDATE coworkings
SET city_id = (SELECT id FROM cities WHERE name = '杭州市' AND country = 'China' LIMIT 1)
WHERE city_id::text = '8503bc5a-bfe9-4fcf-87ea-85586bb3653f' 
  AND name NOT LIKE '%北京%' 
  AND name NOT LIKE '%上海%';

-- 4. 验证修复结果
SELECT 
    c.id,
    c.name as coworking_name,
    c.city_id,
    cities.name as city_name,
    cities.country
FROM coworkings c
LEFT JOIN cities ON c.city_id = cities.id
ORDER BY c.created_at DESC;

-- 5. 显示每个城市的 coworking 数量
SELECT 
    cities.id as city_id,
    cities.name as city_name,
    cities.country,
    COUNT(c.id) as coworking_count
FROM cities
LEFT JOIN coworkings c ON cities.id = c.city_id
WHERE cities.country = 'China'
GROUP BY cities.id, cities.name, cities.country
HAVING COUNT(c.id) > 0
ORDER BY coworking_count DESC;
