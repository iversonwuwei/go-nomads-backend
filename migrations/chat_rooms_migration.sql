-- =====================================================
-- 聊天室服务数据库迁移脚本
-- 创建时间: 2024-12-02
-- 描述: 创建聊天室、消息、成员表
-- =====================================================

-- 1. 创建聊天室表
CREATE TABLE IF NOT EXISTS chat_rooms (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    room_type VARCHAR(20) NOT NULL DEFAULT 'city', -- city, meetup, direct
    meetup_id UUID, -- 关联的 Meetup ID
    name VARCHAR(200) NOT NULL,
    description TEXT,
    city VARCHAR(100),
    country VARCHAR(100),
    image_url TEXT,
    created_by VARCHAR(100),
    is_public BOOLEAN NOT NULL DEFAULT true,
    total_members INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    is_deleted BOOLEAN NOT NULL DEFAULT false
);

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_chat_rooms_room_type ON chat_rooms(room_type);
CREATE INDEX IF NOT EXISTS idx_chat_rooms_meetup_id ON chat_rooms(meetup_id);
CREATE INDEX IF NOT EXISTS idx_chat_rooms_city_country ON chat_rooms(city, country);
CREATE INDEX IF NOT EXISTS idx_chat_rooms_is_public ON chat_rooms(is_public) WHERE is_deleted = false;

-- 2. 创建聊天室消息表
CREATE TABLE IF NOT EXISTS chat_room_messages (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    room_id VARCHAR(100) NOT NULL, -- 支持 UUID 和 meetup_ 前缀格式
    user_id VARCHAR(100) NOT NULL,
    user_name VARCHAR(100) NOT NULL,
    user_avatar TEXT,
    message TEXT NOT NULL,
    message_type VARCHAR(20) NOT NULL DEFAULT 'text', -- text, image, file, location, voice, video
    reply_to_id UUID,
    mentions_json JSONB, -- @提及的用户 ID 列表
    attachment_json JSONB, -- 附件信息
    timestamp TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMP WITH TIME ZONE
);

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_chat_room_messages_room_id ON chat_room_messages(room_id);
CREATE INDEX IF NOT EXISTS idx_chat_room_messages_user_id ON chat_room_messages(user_id);
CREATE INDEX IF NOT EXISTS idx_chat_room_messages_timestamp ON chat_room_messages(room_id, timestamp DESC);
CREATE INDEX IF NOT EXISTS idx_chat_room_messages_reply_to ON chat_room_messages(reply_to_id) WHERE reply_to_id IS NOT NULL;

-- 3. 创建聊天室成员表
CREATE TABLE IF NOT EXISTS chat_room_members (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    room_id VARCHAR(100) NOT NULL,
    user_id VARCHAR(100) NOT NULL,
    user_name VARCHAR(100) NOT NULL,
    user_avatar TEXT,
    role VARCHAR(20) NOT NULL DEFAULT 'member', -- member, admin, owner
    joined_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    last_seen_at TIMESTAMP WITH TIME ZONE,
    is_muted BOOLEAN NOT NULL DEFAULT false,
    muted_until TIMESTAMP WITH TIME ZONE,
    has_left BOOLEAN NOT NULL DEFAULT false,
    left_at TIMESTAMP WITH TIME ZONE,
    UNIQUE(room_id, user_id)
);

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_chat_room_members_room_id ON chat_room_members(room_id);
CREATE INDEX IF NOT EXISTS idx_chat_room_members_user_id ON chat_room_members(user_id);
CREATE INDEX IF NOT EXISTS idx_chat_room_members_active ON chat_room_members(room_id) WHERE has_left = false;
CREATE INDEX IF NOT EXISTS idx_chat_room_members_last_seen ON chat_room_members(room_id, last_seen_at DESC);

-- 4. 创建自动更新成员数量的触发器
CREATE OR REPLACE FUNCTION update_chat_room_member_count()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'INSERT' OR (TG_OP = 'UPDATE' AND OLD.has_left = true AND NEW.has_left = false) THEN
        -- 用户加入
        UPDATE chat_rooms 
        SET total_members = total_members + 1, updated_at = NOW()
        WHERE id::text = NEW.room_id OR ('meetup_' || meetup_id::text) = NEW.room_id;
    ELSIF TG_OP = 'UPDATE' AND OLD.has_left = false AND NEW.has_left = true THEN
        -- 用户离开
        UPDATE chat_rooms 
        SET total_members = GREATEST(0, total_members - 1), updated_at = NOW()
        WHERE id::text = NEW.room_id OR ('meetup_' || meetup_id::text) = NEW.room_id;
    END IF;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trigger_update_member_count ON chat_room_members;
CREATE TRIGGER trigger_update_member_count
    AFTER INSERT OR UPDATE OF has_left ON chat_room_members
    FOR EACH ROW
    EXECUTE FUNCTION update_chat_room_member_count();

-- 5. 创建 RLS 策略（如果需要）
-- ALTER TABLE chat_rooms ENABLE ROW LEVEL SECURITY;
-- ALTER TABLE chat_room_messages ENABLE ROW LEVEL SECURITY;
-- ALTER TABLE chat_room_members ENABLE ROW LEVEL SECURITY;

-- 6. 添加注释
COMMENT ON TABLE chat_rooms IS '聊天室表 - 存储城市聊天室、Meetup 聊天室等';
COMMENT ON TABLE chat_room_messages IS '聊天消息表 - 存储所有聊天消息';
COMMENT ON TABLE chat_room_members IS '聊天室成员表 - 记录用户加入的聊天室';

COMMENT ON COLUMN chat_rooms.room_type IS '聊天室类型：city=城市聊天室, meetup=活动聊天室, direct=私聊';
COMMENT ON COLUMN chat_room_messages.message_type IS '消息类型：text=文本, image=图片, file=文件, location=位置, voice=语音, video=视频';
COMMENT ON COLUMN chat_room_members.role IS '成员角色：member=普通成员, admin=管理员, owner=创建者';

-- 7. 插入一些示例数据（可选）
-- INSERT INTO chat_rooms (name, room_type, city, country, is_public)
-- VALUES 
--     ('Bangkok Chat', 'city', 'Bangkok', 'Thailand', true),
--     ('Chiang Mai Chat', 'city', 'Chiang Mai', 'Thailand', true),
--     ('Bali Nomads', 'city', 'Bali', 'Indonesia', true);

SELECT 'Chat room tables created successfully!' as result;
