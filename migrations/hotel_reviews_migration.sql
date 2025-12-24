-- =====================================================
-- 酒店评论表数据库迁移脚本
-- 创建时间: 2024-12-XX
-- 描述: 创建酒店评论表
-- =====================================================

-- 1. 创建酒店评论表
CREATE TABLE IF NOT EXISTS hotel_reviews (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    hotel_id UUID NOT NULL,
    user_id UUID NOT NULL,
    user_name VARCHAR(100) NOT NULL DEFAULT '匿名用户',
    rating INTEGER NOT NULL CHECK (rating >= 1 AND rating <= 5),
    title VARCHAR(200),
    content TEXT NOT NULL,
    visit_date TIMESTAMP WITH TIME ZONE,
    photo_urls JSONB DEFAULT '[]'::jsonb,
    is_verified BOOLEAN NOT NULL DEFAULT false,
    helpful_count INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE
);

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_hotel_reviews_hotel_id ON hotel_reviews(hotel_id);
CREATE INDEX IF NOT EXISTS idx_hotel_reviews_user_id ON hotel_reviews(user_id);
CREATE INDEX IF NOT EXISTS idx_hotel_reviews_rating ON hotel_reviews(rating);
CREATE INDEX IF NOT EXISTS idx_hotel_reviews_created_at ON hotel_reviews(created_at DESC);
CREATE INDEX IF NOT EXISTS idx_hotel_reviews_helpful_count ON hotel_reviews(helpful_count DESC);

-- 注意：允许用户对同一酒店发表多条评论，不再限制唯一性
-- 如需恢复限制，取消下面注释：
-- CREATE UNIQUE INDEX IF NOT EXISTS idx_hotel_reviews_unique_user_hotel 
-- ON hotel_reviews(hotel_id, user_id);

-- 2. 添加外键约束（如果 hotels 表存在）
-- ALTER TABLE hotel_reviews 
--     ADD CONSTRAINT fk_hotel_reviews_hotel 
--     FOREIGN KEY (hotel_id) REFERENCES hotels(id) ON DELETE CASCADE;

-- 3. 创建函数：更新酒店评论数量
CREATE OR REPLACE FUNCTION update_hotel_review_count()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        UPDATE hotels SET review_count = review_count + 1 WHERE id = NEW.hotel_id;
    ELSIF TG_OP = 'DELETE' THEN
        UPDATE hotels SET review_count = GREATEST(review_count - 1, 0) WHERE id = OLD.hotel_id;
    END IF;
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

-- 6. 创建触发器：自动更新酒店评论数量
DROP TRIGGER IF EXISTS trigger_hotel_review_count ON hotel_reviews;
CREATE TRIGGER trigger_hotel_review_count
    AFTER INSERT OR DELETE ON hotel_reviews
    FOR EACH ROW
    EXECUTE FUNCTION update_hotel_review_count();

-- 7. 创建函数：更新酒店平均评分
CREATE OR REPLACE FUNCTION update_hotel_average_rating()
RETURNS TRIGGER AS $$
DECLARE
    avg_rating NUMERIC;
BEGIN
    SELECT COALESCE(AVG(rating), 0) INTO avg_rating 
    FROM hotel_reviews 
    WHERE hotel_id = COALESCE(NEW.hotel_id, OLD.hotel_id);
    
    UPDATE hotels 
    SET rating = ROUND(avg_rating, 1) 
    WHERE id = COALESCE(NEW.hotel_id, OLD.hotel_id);
    
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

-- 8. 创建触发器：自动更新酒店平均评分
DROP TRIGGER IF EXISTS trigger_hotel_average_rating ON hotel_reviews;
CREATE TRIGGER trigger_hotel_average_rating
    AFTER INSERT OR UPDATE OR DELETE ON hotel_reviews
    FOR EACH ROW
    EXECUTE FUNCTION update_hotel_average_rating();

-- =====================================================
-- 迁移完成
-- =====================================================
