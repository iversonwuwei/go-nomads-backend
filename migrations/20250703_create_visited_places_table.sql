-- 创建 visited_places 表
-- 用于存储用户在旅行中访问的具体地点（停留40分钟以上）

CREATE TABLE IF NOT EXISTS visited_places (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    travel_history_id UUID NOT NULL,
    user_id UUID NOT NULL,
    latitude DOUBLE PRECISION NOT NULL,
    longitude DOUBLE PRECISION NOT NULL,
    place_name VARCHAR(200),
    place_type VARCHAR(50),
    address VARCHAR(500),
    arrival_time TIMESTAMP WITH TIME ZONE NOT NULL,
    departure_time TIMESTAMP WITH TIME ZONE NOT NULL,
    duration_minutes INTEGER NOT NULL DEFAULT 0,
    photo_url TEXT,
    notes TEXT,
    is_highlight BOOLEAN NOT NULL DEFAULT FALSE,
    google_place_id VARCHAR(255),
    client_id VARCHAR(255),  -- 客户端生成的唯一标识，用于同步去重
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_visited_places_travel_history_id ON visited_places(travel_history_id);
CREATE INDEX IF NOT EXISTS idx_visited_places_user_id ON visited_places(user_id);
CREATE INDEX IF NOT EXISTS idx_visited_places_arrival_time ON visited_places(arrival_time DESC);
CREATE INDEX IF NOT EXISTS idx_visited_places_is_highlight ON visited_places(is_highlight) WHERE is_highlight = TRUE;
CREATE INDEX IF NOT EXISTS idx_visited_places_client_id ON visited_places(client_id) WHERE client_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_visited_places_location ON visited_places(latitude, longitude);

-- 创建唯一约束，防止通过 client_id 重复插入
CREATE UNIQUE INDEX IF NOT EXISTS idx_visited_places_client_id_unique 
ON visited_places(user_id, client_id) WHERE client_id IS NOT NULL;

-- 创建更新时间触发器
CREATE OR REPLACE FUNCTION update_visited_places_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trigger_visited_places_updated_at ON visited_places;
CREATE TRIGGER trigger_visited_places_updated_at
    BEFORE UPDATE ON visited_places
    FOR EACH ROW
    EXECUTE FUNCTION update_visited_places_updated_at();

-- 添加注释
COMMENT ON TABLE visited_places IS '用户访问地点表 - 记录用户在旅行中访问过的具体地点（停留40分钟以上）';
COMMENT ON COLUMN visited_places.id IS '主键 UUID';
COMMENT ON COLUMN visited_places.travel_history_id IS '关联的旅行历史 ID';
COMMENT ON COLUMN visited_places.user_id IS '用户 ID';
COMMENT ON COLUMN visited_places.latitude IS '纬度';
COMMENT ON COLUMN visited_places.longitude IS '经度';
COMMENT ON COLUMN visited_places.place_name IS '地点名称（通过逆地理编码获取）';
COMMENT ON COLUMN visited_places.place_type IS '地点类型（餐厅、咖啡馆、景点、酒店等）';
COMMENT ON COLUMN visited_places.address IS '详细地址';
COMMENT ON COLUMN visited_places.arrival_time IS '到达时间';
COMMENT ON COLUMN visited_places.departure_time IS '离开时间';
COMMENT ON COLUMN visited_places.duration_minutes IS '停留时长（分钟）';
COMMENT ON COLUMN visited_places.photo_url IS '地点照片 URL';
COMMENT ON COLUMN visited_places.notes IS '用户备注';
COMMENT ON COLUMN visited_places.is_highlight IS '是否为精选地点';
COMMENT ON COLUMN visited_places.google_place_id IS 'Google Place ID';
COMMENT ON COLUMN visited_places.client_id IS '客户端生成的唯一标识（用于同步去重）';
COMMENT ON COLUMN visited_places.created_at IS '创建时间';
COMMENT ON COLUMN visited_places.updated_at IS '更新时间';

-- 输出成功信息
DO $$
BEGIN
    RAISE NOTICE '✅ visited_places 表创建成功！';
END $$;
