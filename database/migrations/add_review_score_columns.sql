-- =====================================================
-- 为 user_city_reviews 表添加详细评分字段
-- 添加时间: 2025-10-31
-- =====================================================

-- 添加详细评分列(如果不存在)
DO $$ 
BEGIN
    -- Internet Quality Score
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'user_city_reviews' 
        AND column_name = 'internet_quality_score'
    ) THEN
        ALTER TABLE user_city_reviews 
        ADD COLUMN internet_quality_score INTEGER CHECK (internet_quality_score >= 1 AND internet_quality_score <= 5);
    END IF;

    -- Safety Score
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'user_city_reviews' 
        AND column_name = 'safety_score'
    ) THEN
        ALTER TABLE user_city_reviews 
        ADD COLUMN safety_score INTEGER CHECK (safety_score >= 1 AND safety_score <= 5);
    END IF;

    -- Cost Score
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'user_city_reviews' 
        AND column_name = 'cost_score'
    ) THEN
        ALTER TABLE user_city_reviews 
        ADD COLUMN cost_score INTEGER CHECK (cost_score >= 1 AND cost_score <= 5);
    END IF;

    -- Community Score
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'user_city_reviews' 
        AND column_name = 'community_score'
    ) THEN
        ALTER TABLE user_city_reviews 
        ADD COLUMN community_score INTEGER CHECK (community_score >= 1 AND community_score <= 5);
    END IF;

    -- Weather Score
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'user_city_reviews' 
        AND column_name = 'weather_score'
    ) THEN
        ALTER TABLE user_city_reviews 
        ADD COLUMN weather_score INTEGER CHECK (weather_score >= 1 AND weather_score <= 5);
    END IF;

    -- Review Text (备用文本字段)
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'user_city_reviews' 
        AND column_name = 'review_text'
    ) THEN
        ALTER TABLE user_city_reviews 
        ADD COLUMN review_text TEXT;
    END IF;
END $$;

-- 添加索引以优化查询
CREATE INDEX IF NOT EXISTS idx_user_city_reviews_internet_score 
    ON user_city_reviews(internet_quality_score) 
    WHERE internet_quality_score IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_user_city_reviews_safety_score 
    ON user_city_reviews(safety_score) 
    WHERE safety_score IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_user_city_reviews_cost_score 
    ON user_city_reviews(cost_score) 
    WHERE cost_score IS NOT NULL;

-- 添加注释
COMMENT ON COLUMN user_city_reviews.internet_quality_score IS '互联网质量评分 (1-5)';
COMMENT ON COLUMN user_city_reviews.safety_score IS '安全评分 (1-5)';
COMMENT ON COLUMN user_city_reviews.cost_score IS '费用评分 (1-5)';
COMMENT ON COLUMN user_city_reviews.community_score IS '社区评分 (1-5)';
COMMENT ON COLUMN user_city_reviews.weather_score IS '天气评分 (1-5)';
COMMENT ON COLUMN user_city_reviews.review_text IS '额外评论文本(可选)';

-- 验证查询
SELECT 
    column_name, 
    data_type, 
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_name = 'user_city_reviews'
ORDER BY ordinal_position;
