-- =====================================================
-- 城市 Pros & Cons 表创建脚本
-- 用于存储用户分享的城市优点和挑战
-- =====================================================

-- 创建城市 Pros & Cons 表
CREATE TABLE IF NOT EXISTS city_pros_cons (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    city_id VARCHAR(100) NOT NULL,
    text TEXT NOT NULL CHECK (LENGTH(text) > 0 AND LENGTH(text) <= 500),
    is_pro BOOLEAN NOT NULL, -- true = 优点, false = 挑战/缺点
    upvotes INTEGER NOT NULL DEFAULT 0 CHECK (upvotes >= 0),
    downvotes INTEGER NOT NULL DEFAULT 0 CHECK (downvotes >= 0),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    
    CONSTRAINT fk_city_pros_cons_user FOREIGN KEY (user_id) 
        REFERENCES auth.users(id) ON DELETE CASCADE
);

-- 索引优化
CREATE INDEX idx_city_pros_cons_user_id ON city_pros_cons(user_id);
CREATE INDEX idx_city_pros_cons_city_id ON city_pros_cons(city_id);
CREATE INDEX idx_city_pros_cons_city_type ON city_pros_cons(city_id, is_pro);
CREATE INDEX idx_city_pros_cons_created_at ON city_pros_cons(created_at DESC);
CREATE INDEX idx_city_pros_cons_upvotes ON city_pros_cons(upvotes DESC);

-- RLS 策略
ALTER TABLE city_pros_cons ENABLE ROW LEVEL SECURITY;

-- 所有人都可以查看 Pros & Cons（公开）
CREATE POLICY "Anyone can view pros and cons"
    ON city_pros_cons FOR SELECT
    USING (true);

-- 已认证用户可以插入 Pros & Cons
CREATE POLICY "Authenticated users can insert pros and cons"
    ON city_pros_cons FOR INSERT
    WITH CHECK (auth.uid() = user_id);

-- 用户只能更新自己的 Pros & Cons
CREATE POLICY "Users can update their own pros and cons"
    ON city_pros_cons FOR UPDATE
    USING (auth.uid() = user_id);

-- 用户只能删除自己的 Pros & Cons
CREATE POLICY "Users can delete their own pros and cons"
    ON city_pros_cons FOR DELETE
    USING (auth.uid() = user_id);

-- 自动更新 updated_at 触发器
CREATE OR REPLACE FUNCTION update_city_pros_cons_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_update_city_pros_cons_updated_at
    BEFORE UPDATE ON city_pros_cons
    FOR EACH ROW
    EXECUTE FUNCTION update_city_pros_cons_updated_at();

-- 注释
COMMENT ON TABLE city_pros_cons IS '用户分享的城市优点和挑战';
COMMENT ON COLUMN city_pros_cons.is_pro IS 'true = 优点, false = 挑战/缺点';
COMMENT ON COLUMN city_pros_cons.upvotes IS '点赞数';
COMMENT ON COLUMN city_pros_cons.downvotes IS '点踩数';
