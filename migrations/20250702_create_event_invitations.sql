-- =====================================================
-- 活动邀请表迁移脚本
-- 创建时间: 2025-07-02
-- 描述: 创建 event_invitations 表，用于存储活动邀请记录
-- =====================================================

-- 1. 创建活动邀请表
CREATE TABLE IF NOT EXISTS event_invitations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_id UUID NOT NULL REFERENCES events(id) ON DELETE CASCADE,
    inviter_id VARCHAR(100) NOT NULL,  -- 邀请人ID
    invitee_id VARCHAR(100) NOT NULL,  -- 被邀请人ID
    status VARCHAR(20) NOT NULL DEFAULT 'pending', -- pending, accepted, rejected, expired
    message TEXT,                       -- 邀请留言
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    responded_at TIMESTAMP WITH TIME ZONE, -- 响应时间
    expires_at TIMESTAMP WITH TIME ZONE    -- 过期时间
);

-- 2. 创建索引
-- 按活动ID查询
CREATE INDEX IF NOT EXISTS idx_event_invitations_event_id ON event_invitations(event_id);

-- 按被邀请人查询（获取收到的邀请）
CREATE INDEX IF NOT EXISTS idx_event_invitations_invitee_id ON event_invitations(invitee_id);

-- 按邀请人查询（获取发出的邀请）
CREATE INDEX IF NOT EXISTS idx_event_invitations_inviter_id ON event_invitations(inviter_id);

-- 按状态筛选
CREATE INDEX IF NOT EXISTS idx_event_invitations_status ON event_invitations(status);

-- 组合索引：被邀请人 + 状态（用于获取待处理的邀请）
CREATE INDEX IF NOT EXISTS idx_event_invitations_invitee_status ON event_invitations(invitee_id, status);

-- 组合索引：活动 + 被邀请人 + 状态（用于检查是否已有邀请）
CREATE INDEX IF NOT EXISTS idx_event_invitations_event_invitee_status ON event_invitations(event_id, invitee_id, status);

-- 3. 添加注释
COMMENT ON TABLE event_invitations IS '活动邀请表，存储用户邀请其他用户参加活动的记录';
COMMENT ON COLUMN event_invitations.id IS '邀请唯一标识';
COMMENT ON COLUMN event_invitations.event_id IS '关联的活动ID';
COMMENT ON COLUMN event_invitations.inviter_id IS '发出邀请的用户ID';
COMMENT ON COLUMN event_invitations.invitee_id IS '被邀请的用户ID';
COMMENT ON COLUMN event_invitations.status IS '邀请状态: pending-待处理, accepted-已接受, rejected-已拒绝, expired-已过期';
COMMENT ON COLUMN event_invitations.message IS '邀请留言';
COMMENT ON COLUMN event_invitations.created_at IS '邀请创建时间';
COMMENT ON COLUMN event_invitations.responded_at IS '响应时间';
COMMENT ON COLUMN event_invitations.expires_at IS '邀请过期时间';
