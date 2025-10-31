-- =====================================================
-- 用户城市内容表创建脚本
-- 包含：照片、费用、评论三个独立表
-- =====================================================

-- 1. 用户城市照片表
CREATE TABLE IF NOT EXISTS user_city_photos (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    city_id VARCHAR(100) NOT NULL,
    image_url TEXT NOT NULL,
    caption TEXT,
    location TEXT,
    taken_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    
    CONSTRAINT fk_user_city_photos_user FOREIGN KEY (user_id) 
        REFERENCES auth.users(id) ON DELETE CASCADE
);

-- 索引优化
CREATE INDEX idx_user_city_photos_user_id ON user_city_photos(user_id);
CREATE INDEX idx_user_city_photos_city_id ON user_city_photos(city_id);
CREATE INDEX idx_user_city_photos_user_city ON user_city_photos(user_id, city_id);
CREATE INDEX idx_user_city_photos_created_at ON user_city_photos(created_at DESC);

-- RLS 策略
ALTER TABLE user_city_photos ENABLE ROW LEVEL SECURITY;

-- 用户只能查看自己的照片
CREATE POLICY "Users can view their own photos"
    ON user_city_photos FOR SELECT
    USING (auth.uid() = user_id);

-- 用户只能插入自己的照片
CREATE POLICY "Users can insert their own photos"
    ON user_city_photos FOR INSERT
    WITH CHECK (auth.uid() = user_id);

-- 用户只能更新自己的照片
CREATE POLICY "Users can update their own photos"
    ON user_city_photos FOR UPDATE
    USING (auth.uid() = user_id);

-- 用户只能删除自己的照片
CREATE POLICY "Users can delete their own photos"
    ON user_city_photos FOR DELETE
    USING (auth.uid() = user_id);


-- 2. 用户城市费用表
CREATE TABLE IF NOT EXISTS user_city_expenses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    city_id VARCHAR(100) NOT NULL,
    category VARCHAR(50) NOT NULL CHECK (category IN ('food', 'transport', 'accommodation', 'activity', 'shopping', 'other')),
    amount DECIMAL(10, 2) NOT NULL CHECK (amount >= 0),
    currency VARCHAR(10) NOT NULL DEFAULT 'USD',
    description TEXT,
    date TIMESTAMP WITH TIME ZONE NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    
    CONSTRAINT fk_user_city_expenses_user FOREIGN KEY (user_id) 
        REFERENCES auth.users(id) ON DELETE CASCADE
);

-- 索引优化
CREATE INDEX idx_user_city_expenses_user_id ON user_city_expenses(user_id);
CREATE INDEX idx_user_city_expenses_city_id ON user_city_expenses(city_id);
CREATE INDEX idx_user_city_expenses_user_city ON user_city_expenses(user_id, city_id);
CREATE INDEX idx_user_city_expenses_category ON user_city_expenses(category);
CREATE INDEX idx_user_city_expenses_date ON user_city_expenses(date DESC);

-- RLS 策略
ALTER TABLE user_city_expenses ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Users can view their own expenses"
    ON user_city_expenses FOR SELECT
    USING (auth.uid() = user_id);

CREATE POLICY "Users can insert their own expenses"
    ON user_city_expenses FOR INSERT
    WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can update their own expenses"
    ON user_city_expenses FOR UPDATE
    USING (auth.uid() = user_id);

CREATE POLICY "Users can delete their own expenses"
    ON user_city_expenses FOR DELETE
    USING (auth.uid() = user_id);


-- 3. 用户城市评论表
CREATE TABLE IF NOT EXISTS user_city_reviews (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    city_id VARCHAR(100) NOT NULL,
    rating INTEGER NOT NULL CHECK (rating >= 1 AND rating <= 5),
    title VARCHAR(200) NOT NULL,
    content TEXT NOT NULL,
    visit_date TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    
    CONSTRAINT fk_user_city_reviews_user FOREIGN KEY (user_id) 
        REFERENCES auth.users(id) ON DELETE CASCADE,
    -- 每个用户每个城市只能有一条评论
    CONSTRAINT unique_user_city_review UNIQUE (user_id, city_id)
);

-- 索引优化
CREATE INDEX idx_user_city_reviews_user_id ON user_city_reviews(user_id);
CREATE INDEX idx_user_city_reviews_city_id ON user_city_reviews(city_id);
CREATE INDEX idx_user_city_reviews_rating ON user_city_reviews(rating);
CREATE INDEX idx_user_city_reviews_created_at ON user_city_reviews(created_at DESC);

-- RLS 策略
ALTER TABLE user_city_reviews ENABLE ROW LEVEL SECURITY;

-- 所有人都可以查看评论（公开）
CREATE POLICY "Anyone can view reviews"
    ON user_city_reviews FOR SELECT
    USING (true);

-- 用户只能插入自己的评论
CREATE POLICY "Users can insert their own reviews"
    ON user_city_reviews FOR INSERT
    WITH CHECK (auth.uid() = user_id);

-- 用户只能更新自己的评论
CREATE POLICY "Users can update their own reviews"
    ON user_city_reviews FOR UPDATE
    USING (auth.uid() = user_id);

-- 用户只能删除自己的评论
CREATE POLICY "Users can delete their own reviews"
    ON user_city_reviews FOR DELETE
    USING (auth.uid() = user_id);

-- 自动更新 updated_at 触发器
CREATE OR REPLACE FUNCTION update_user_city_reviews_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_update_user_city_reviews_updated_at
    BEFORE UPDATE ON user_city_reviews
    FOR EACH ROW
    EXECUTE FUNCTION update_user_city_reviews_updated_at();


-- 4. 创建统计视图（可选）
CREATE OR REPLACE VIEW user_city_content_stats AS
SELECT 
    city_id,
    COUNT(DISTINCT p.user_id) as photo_contributors,
    COUNT(DISTINCT e.user_id) as expense_contributors,
    COUNT(DISTINCT r.user_id) as review_contributors,
    COUNT(p.id) as total_photos,
    COUNT(e.id) as total_expenses,
    COUNT(r.id) as total_reviews,
    COALESCE(AVG(r.rating), 0) as average_rating
FROM (SELECT DISTINCT city_id FROM user_city_photos 
      UNION SELECT DISTINCT city_id FROM user_city_expenses
      UNION SELECT DISTINCT city_id FROM user_city_reviews) cities
LEFT JOIN user_city_photos p ON cities.city_id = p.city_id
LEFT JOIN user_city_expenses e ON cities.city_id = e.city_id
LEFT JOIN user_city_reviews r ON cities.city_id = r.city_id
GROUP BY city_id;

-- 注释
COMMENT ON TABLE user_city_photos IS '用户上传的城市照片';
COMMENT ON TABLE user_city_expenses IS '用户记录的城市费用';
COMMENT ON TABLE user_city_reviews IS '用户发表的城市评论';
COMMENT ON VIEW user_city_content_stats IS '城市用户内容统计视图';
