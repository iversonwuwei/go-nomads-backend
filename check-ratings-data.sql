-- 检查数据库中的评分和花费数据

-- 1. 检查 city_ratings 表
SELECT COUNT(*) as total_ratings, 
       COUNT(DISTINCT city_id) as cities_with_ratings,
       COUNT(DISTINCT user_id) as unique_users
FROM city_ratings;

-- 2. 检查每个城市的评分数量
SELECT c.name, c.id, COUNT(cr.id) as rating_count
FROM cities c
LEFT JOIN city_ratings cr ON c.id = cr.city_id
GROUP BY c.id, c.name
HAVING COUNT(cr.id) > 0
ORDER BY rating_count DESC
LIMIT 10;

-- 3. 检查 user_city_expenses 表
SELECT COUNT(*) as total_expenses,
       COUNT(DISTINCT city_id) as cities_with_expenses
FROM user_city_expenses;

-- 4. 检查每个城市的平均花费
SELECT c.name, c.id, 
       COUNT(e.id) as expense_count,
       AVG(e.total) as avg_cost
FROM cities c
LEFT JOIN user_city_expenses e ON c.id::text = e.city_id
GROUP BY c.id, c.name
HAVING COUNT(e.id) > 0
ORDER BY avg_cost DESC
LIMIT 10;
