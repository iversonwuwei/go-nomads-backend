-- 旅行历史表
-- 用于存储用户的旅行历史记录（包括自动检测和手动添加的）

CREATE TABLE IF NOT EXISTS travel_history (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    city VARCHAR(100) NOT NULL,
    country VARCHAR(100) NOT NULL,
    latitude DOUBLE PRECISION,
    longitude DOUBLE PRECISION,
    arrival_time TIMESTAMPTZ NOT NULL,
    departure_time TIMESTAMPTZ,
    is_confirmed BOOLEAN DEFAULT FALSE,
    review TEXT,
    rating DOUBLE PRECISION CHECK (rating IS NULL OR (rating >= 1 AND rating <= 5)),
    photos JSONB,
    city_id VARCHAR(36),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_travel_history_user_id ON travel_history(user_id);
CREATE INDEX IF NOT EXISTS idx_travel_history_arrival_time ON travel_history(arrival_time);
CREATE INDEX IF NOT EXISTS idx_travel_history_is_confirmed ON travel_history(is_confirmed);
CREATE INDEX IF NOT EXISTS idx_travel_history_city_country ON travel_history(city, country);

-- 添加注释
COMMENT ON TABLE travel_history IS '旅行历史表 - 记录用户的旅行历史';
COMMENT ON COLUMN travel_history.user_id IS '关联的用户ID';
COMMENT ON COLUMN travel_history.city IS '城市名称';
COMMENT ON COLUMN travel_history.country IS '国家名称';
COMMENT ON COLUMN travel_history.latitude IS '纬度';
COMMENT ON COLUMN travel_history.longitude IS '经度';
COMMENT ON COLUMN travel_history.arrival_time IS '到达时间';
COMMENT ON COLUMN travel_history.departure_time IS '离开时间';
COMMENT ON COLUMN travel_history.is_confirmed IS '是否已确认（自动检测的需要用户确认）';
COMMENT ON COLUMN travel_history.review IS '旅行评价/回顾';
COMMENT ON COLUMN travel_history.rating IS '评分（1-5星）';
COMMENT ON COLUMN travel_history.photos IS '照片URL列表（JSON数组）';
COMMENT ON COLUMN travel_history.city_id IS '关联的城市ID（用于链接到城市详情）';

-- RLS (Row Level Security) 策略
ALTER TABLE travel_history ENABLE ROW LEVEL SECURITY;

-- 允许用户查看自己的旅行历史
CREATE POLICY travel_history_select_own ON travel_history
    FOR SELECT
    USING (auth.uid() = user_id);

-- 允许后端服务访问所有旅行历史（通过 service_role 密钥）
CREATE POLICY travel_history_select_service ON travel_history
    FOR SELECT
    USING (auth.role() = 'service_role');

-- 允许用户插入自己的旅行历史
CREATE POLICY travel_history_insert_own ON travel_history
    FOR INSERT
    WITH CHECK (auth.uid() = user_id);

-- 允许后端服务插入旅行历史
CREATE POLICY travel_history_insert_service ON travel_history
    FOR INSERT
    WITH CHECK (auth.role() = 'service_role');

-- 允许用户更新自己的旅行历史
CREATE POLICY travel_history_update_own ON travel_history
    FOR UPDATE
    USING (auth.uid() = user_id);

-- 允许后端服务更新旅行历史
CREATE POLICY travel_history_update_service ON travel_history
    FOR UPDATE
    USING (auth.role() = 'service_role');

-- 允许用户删除自己的旅行历史
CREATE POLICY travel_history_delete_own ON travel_history
    FOR DELETE
    USING (auth.uid() = user_id);

-- 允许后端服务删除旅行历史
CREATE POLICY travel_history_delete_service ON travel_history
    FOR DELETE
    USING (auth.role() = 'service_role');

-- 触发器：自动更新 updated_at
CREATE OR REPLACE FUNCTION update_travel_history_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER travel_history_updated_at_trigger
    BEFORE UPDATE ON travel_history
    FOR EACH ROW
    EXECUTE FUNCTION update_travel_history_updated_at();
