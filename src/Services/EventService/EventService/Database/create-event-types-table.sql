-- 创建 event_types 表（聚会类型）
CREATE TABLE IF NOT EXISTS event_types (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL, -- 中文名称
    en_name VARCHAR(100) NOT NULL, -- 英文名称
    description TEXT, -- 描述
    icon VARCHAR(50), -- 图标名称（可选）
    sort_order INT DEFAULT 0, -- 排序顺序
    is_active BOOLEAN DEFAULT TRUE, -- 是否启用
    is_system BOOLEAN DEFAULT FALSE, -- 是否系统预设（系统预设不可删除）
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_event_types_is_active ON event_types(is_active);
CREATE INDEX IF NOT EXISTS idx_event_types_sort_order ON event_types(sort_order);

-- 添加唯一约束
CREATE UNIQUE INDEX IF NOT EXISTS idx_event_types_name ON event_types(name) WHERE is_active = TRUE;
CREATE UNIQUE INDEX IF NOT EXISTS idx_event_types_en_name ON event_types(en_name) WHERE is_active = TRUE;

-- 添加注释
COMMENT ON TABLE event_types IS '聚会类型表';
COMMENT ON COLUMN event_types.name IS '中文名称';
COMMENT ON COLUMN event_types.en_name IS '英文名称';
COMMENT ON COLUMN event_types.description IS '类型描述';
COMMENT ON COLUMN event_types.icon IS '图标名称';
COMMENT ON COLUMN event_types.sort_order IS '排序顺序（越小越靠前）';
COMMENT ON COLUMN event_types.is_active IS '是否启用';
COMMENT ON COLUMN event_types.is_system IS '是否系统预设类型';

-- 插入预设的聚会类型
INSERT INTO event_types (name, en_name, description, is_system, sort_order) VALUES
('社交网络', 'Networking', '商务社交和职业发展', TRUE, 1),
('工作坊', 'Workshop', '技能学习和实践活动', TRUE, 2),
('社交聚会', 'Social Gathering', '轻松休闲的社交活动', TRUE, 3),
('运动健身', 'Sports & Fitness', '体育运动和健身活动', TRUE, 4),
('美食饮品', 'Food & Drinks', '美食品鉴和聚餐活动', TRUE, 5),
('共享办公', 'Coworking Session', '共同办公和协作', TRUE, 6),
('语言交换', 'Language Exchange', '语言学习和文化交流', TRUE, 7),
('文化活动', 'Cultural Event', '文化艺术和展览活动', TRUE, 8),
('技术聚会', 'Tech Meetup', '技术分享和交流', TRUE, 9),
('旅行规划', 'Travel Planning', '旅行计划和经验分享', TRUE, 10),
('读书会', 'Book Club', '阅读分享和讨论', TRUE, 11),
('游戏之夜', 'Gaming Night', '桌游、电竞等游戏活动', TRUE, 12),
('摄影漫步', 'Photography Walk', '摄影爱好者外拍活动', TRUE, 13),
('徒步户外', 'Hiking & Outdoor', '户外徒步和探险', TRUE, 14),
('音乐艺术', 'Music & Arts', '音乐演出和艺术活动', TRUE, 15),
('商务午餐', 'Business Lunch', '商务交流午餐会', TRUE, 16),
('职业发展', 'Career Development', '职业规划和发展活动', TRUE, 17),
('志愿活动', 'Volunteer Activity', '公益和志愿服务', TRUE, 18),
('电影之夜', 'Movie Night', '电影观赏和讨论', TRUE, 19),
('瑜伽冥想', 'Yoga & Meditation', '瑜伽和冥想练习', TRUE, 20);

-- 启用 RLS
ALTER TABLE event_types ENABLE ROW LEVEL SECURITY;

-- 创建 RLS 策略：所有人都可以查看启用的类型
CREATE POLICY "Anyone can view active event types"
    ON event_types FOR SELECT
    USING (is_active = TRUE);

-- 创建 RLS 策略：只有认证用户可以查看所有类型（包括禁用的）
CREATE POLICY "Authenticated users can view all event types"
    ON event_types FOR SELECT
    TO authenticated
    USING (TRUE);

-- 创建 RLS 策略：只有管理员可以插入、更新、删除
CREATE POLICY "Admins can manage event types"
    ON event_types FOR ALL
    TO authenticated
    USING (
        EXISTS (
            SELECT 1 FROM user_roles
            WHERE user_id = auth.uid()
            AND role = 'admin'
        )
    );

-- 创建更新时间触发器
CREATE OR REPLACE FUNCTION update_event_types_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_update_event_types_updated_at
    BEFORE UPDATE ON event_types
    FOR EACH ROW
    EXECUTE FUNCTION update_event_types_updated_at();
