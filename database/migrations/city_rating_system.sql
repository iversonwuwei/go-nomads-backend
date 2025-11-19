-- City Rating System Tables
-- 城市评分系统表结构

-- 1. 评分项表 (Rating Categories)
CREATE TABLE IF NOT EXISTS city_rating_categories (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    name_en VARCHAR(100),
    description TEXT,
    icon VARCHAR(50), -- 图标名称
    is_default BOOLEAN DEFAULT false, -- 是否是默认评分项
    created_by UUID REFERENCES auth.users(id),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    is_active BOOLEAN DEFAULT true,
    display_order INTEGER DEFAULT 0 -- 显示顺序
);

-- 2. 用户评分表 (User Ratings)
CREATE TABLE IF NOT EXISTS city_ratings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    city_id UUID NOT NULL,
    user_id UUID NOT NULL REFERENCES auth.users(id),
    category_id UUID NOT NULL REFERENCES city_rating_categories(id) ON DELETE CASCADE,
    rating INTEGER NOT NULL CHECK (rating >= 0 AND rating <= 5),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(city_id, user_id, category_id) -- 每个用户对每个城市的每个评分项只能评一次
);

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_city_rating_categories_active ON city_rating_categories(is_active);
CREATE INDEX IF NOT EXISTS idx_city_rating_categories_order ON city_rating_categories(display_order);
CREATE INDEX IF NOT EXISTS idx_city_ratings_city_id ON city_ratings(city_id);
CREATE INDEX IF NOT EXISTS idx_city_ratings_user_id ON city_ratings(user_id);
CREATE INDEX IF NOT EXISTS idx_city_ratings_category_id ON city_ratings(category_id);
CREATE INDEX IF NOT EXISTS idx_city_ratings_city_category ON city_ratings(city_id, category_id);

-- 插入默认评分项
INSERT INTO city_rating_categories (name, name_en, description, icon, is_default, display_order) VALUES
('生活成本', 'Cost of Living', '城市的整体生活成本', 'attach_money', true, 1),
('气候舒适度', 'Climate', '城市的气候和天气舒适度', 'wb_sunny', true, 2),
('交通便利度', 'Transportation', '公共交通和出行便利程度', 'directions_bus', true, 3),
('美食', 'Food', '餐饮选择和美食质量', 'restaurant', true, 4),
('安全', 'Safety', '城市治安和安全水平', 'security', true, 5),
('互联网', 'Internet', '网络速度和稳定性', 'wifi', true, 6),
('娱乐活动', 'Entertainment', '娱乐和休闲活动丰富度', 'local_activity', true, 7),
('医疗', 'Healthcare', '医疗设施和服务质量', 'local_hospital', true, 8),
('友好度', 'Friendliness', '当地人友好程度', 'people', true, 9),
('英语普及度', 'English Level', '英语使用和沟通便利度', 'language', true, 10)
ON CONFLICT DO NOTHING;

-- 创建视图：城市评分统计
CREATE OR REPLACE VIEW city_rating_statistics AS
SELECT 
    cr.city_id,
    crc.id as category_id,
    crc.name as category_name,
    crc.name_en as category_name_en,
    crc.icon,
    crc.display_order,
    COUNT(cr.id) as rating_count,
    ROUND(AVG(cr.rating)::numeric, 1) as average_rating
FROM city_rating_categories crc
LEFT JOIN city_ratings cr ON crc.id = cr.category_id
WHERE crc.is_active = true
GROUP BY cr.city_id, crc.id, crc.name, crc.name_en, crc.icon, crc.display_order
ORDER BY crc.display_order;

COMMENT ON TABLE city_rating_categories IS '城市评分项表';
COMMENT ON TABLE city_ratings IS '用户城市评分表';
COMMENT ON VIEW city_rating_statistics IS '城市评分统计视图';
