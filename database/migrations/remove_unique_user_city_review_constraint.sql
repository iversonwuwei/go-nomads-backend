-- 移除 user_city_reviews 表的 unique constraint
-- 允许同一个用户对同一个城市多次评论

-- 删除唯一性约束
ALTER TABLE user_city_reviews 
DROP CONSTRAINT IF EXISTS unique_user_city_review;

-- 添加注释说明变更原因
COMMENT ON TABLE user_city_reviews IS '用户城市评论表 - 允许同一用户对同一城市多次评论';
