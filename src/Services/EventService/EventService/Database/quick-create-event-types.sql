-- =============================================
-- 快速创建聚会类型表和预设数据
-- 可直接在 Supabase SQL Editor 中执行
-- =============================================

-- 1. 创建表
CREATE TABLE IF NOT EXISTS event_types (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    en_name VARCHAR(100) NOT NULL,
    description TEXT,
    icon VARCHAR(50),
    sort_order INT DEFAULT 0,
    is_active BOOLEAN DEFAULT TRUE,
    is_system BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- 2. 创建索引
CREATE INDEX IF NOT EXISTS idx_event_types_is_active ON event_types(is_active);
CREATE INDEX IF NOT EXISTS idx_event_types_sort_order ON event_types(sort_order);
CREATE UNIQUE INDEX IF NOT EXISTS idx_event_types_name ON event_types(name) WHERE is_active = TRUE;
CREATE UNIQUE INDEX IF NOT EXISTS idx_event_types_en_name ON event_types(en_name) WHERE is_active = TRUE;

-- 3. 插入预设数据（20种类型）
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
('瑜伽冥想', 'Yoga & Meditation', '瑜伽和冥想练习', TRUE, 20)
ON CONFLICT DO NOTHING;

-- 4. 启用 RLS
ALTER TABLE event_types ENABLE ROW LEVEL SECURITY;

-- 5. 创建 RLS 策略
DROP POLICY IF EXISTS "Anyone can view active event types" ON event_types;
CREATE POLICY "Anyone can view active event types"
    ON event_types FOR SELECT
    USING (is_active = TRUE);

DROP POLICY IF EXISTS "Authenticated users can view all event types" ON event_types;
CREATE POLICY "Authenticated users can view all event types"
    ON event_types FOR SELECT
    TO authenticated
    USING (TRUE);

-- 6. 创建更新触发器
CREATE OR REPLACE FUNCTION update_event_types_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trigger_update_event_types_updated_at ON event_types;
CREATE TRIGGER trigger_update_event_types_updated_at
    BEFORE UPDATE ON event_types
    FOR EACH ROW
    EXECUTE FUNCTION update_event_types_updated_at();

-- 完成！
SELECT '✅ 聚会类型表创建成功！已插入 20 个预设类型。' as status;
SELECT COUNT(*) as total_types FROM event_types WHERE is_active = TRUE;
