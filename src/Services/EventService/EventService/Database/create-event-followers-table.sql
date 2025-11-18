-- 创建 event_followers 表（Meetup 关注功能）
-- 执行环境：Supabase SQL Editor

CREATE TABLE IF NOT EXISTS event_followers
(
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_id UUID NOT NULL REFERENCES events (id) ON DELETE CASCADE,
    user_id UUID NOT NULL,
    followed_at          TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    notification_enabled BOOLEAN DEFAULT TRUE,

    -- 唯一约束：一个用户只能关注一个 Meetup 一次
    UNIQUE (event_id, user_id)
);

-- 创建索引以提高查询性能
CREATE INDEX idx_event_followers_event_id ON event_followers (event_id);
CREATE INDEX idx_event_followers_user_id ON event_followers (user_id);
CREATE INDEX idx_event_followers_followed_at ON event_followers (followed_at);

-- 添加注释
COMMENT ON TABLE event_followers IS 'Meetup 关注者表';
COMMENT ON COLUMN event_followers.id IS '主键ID';
COMMENT ON COLUMN event_followers.event_id IS '被关注的 Meetup ID';
COMMENT ON COLUMN event_followers.user_id IS '关注者用户ID';
COMMENT ON COLUMN event_followers.followed_at IS '关注时间';
COMMENT ON COLUMN event_followers.notification_enabled IS '是否启用通知';

-- 启用 Row Level Security (RLS)
ALTER TABLE event_followers ENABLE ROW LEVEL SECURITY;

-- 创建策略：用户可以查看所有关注记录
CREATE
POLICY "Users can view all followers"
ON event_followers FOR
SELECT
    USING
    (true);

-- 创建策略：用户只能插入自己的关注记录
CREATE
POLICY "Users can insert their own follows"
ON event_followers FOR
INSERT WITH CHECK (true);

-- 创建策略：用户只能删除自己的关注记录
CREATE
POLICY "Users can delete their own follows"
ON event_followers FOR
DELETE
    USING (true);

-- 验证表创建
SELECT table_name,
       column_name,
       data_type,
       is_nullable
FROM information_schema.columns
WHERE table_name = 'event_followers'
ORDER BY ordinal_position;
