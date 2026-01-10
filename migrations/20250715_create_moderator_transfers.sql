-- =====================================================
-- 版主转让表迁移脚本
-- 创建时间: 2025-07-15
-- 描述: 创建 moderator_transfers 表，用于存储版主转让请求记录
-- =====================================================

-- 1. 创建版主转让表
CREATE TABLE IF NOT EXISTS moderator_transfers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    city_id UUID NOT NULL REFERENCES cities(id) ON DELETE CASCADE,
    from_user_id UUID NOT NULL,         -- 发起转让的版主用户ID
    to_user_id UUID NOT NULL,           -- 接收转让的目标用户ID
    status VARCHAR(20) NOT NULL DEFAULT 'pending', -- pending, accepted, rejected, cancelled, expired
    message TEXT,                       -- 转让说明/消息
    response_message TEXT,              -- 接收方的回复消息
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    responded_at TIMESTAMP WITH TIME ZONE, -- 响应时间
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT (NOW() + INTERVAL '7 days'), -- 过期时间（默认7天）
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- 2. 创建索引
-- 按城市ID查询
CREATE INDEX IF NOT EXISTS idx_moderator_transfers_city_id ON moderator_transfers(city_id);

-- 按发起人查询（获取发起的转让请求）
CREATE INDEX IF NOT EXISTS idx_moderator_transfers_from_user_id ON moderator_transfers(from_user_id);

-- 按接收人查询（获取收到的转让请求）
CREATE INDEX IF NOT EXISTS idx_moderator_transfers_to_user_id ON moderator_transfers(to_user_id);

-- 按状态筛选
CREATE INDEX IF NOT EXISTS idx_moderator_transfers_status ON moderator_transfers(status);

-- 组合索引：接收人 + 状态（用于获取待处理的转让请求）
CREATE INDEX IF NOT EXISTS idx_moderator_transfers_to_user_status ON moderator_transfers(to_user_id, status);

-- 组合索引：城市 + 接收人 + 状态（用于检查是否已有待处理的转让请求）
CREATE INDEX IF NOT EXISTS idx_moderator_transfers_city_to_user_status ON moderator_transfers(city_id, to_user_id, status);

-- 组合索引：状态 + 过期时间（用于处理过期转让请求）
CREATE INDEX IF NOT EXISTS idx_moderator_transfers_status_expires ON moderator_transfers(status, expires_at);

-- 3. 添加注释
COMMENT ON TABLE moderator_transfers IS '版主转让表，存储版主将权限转让给其他用户的请求记录';
COMMENT ON COLUMN moderator_transfers.id IS '转让请求唯一标识';
COMMENT ON COLUMN moderator_transfers.city_id IS '关联的城市ID';
COMMENT ON COLUMN moderator_transfers.from_user_id IS '发起转让的版主用户ID';
COMMENT ON COLUMN moderator_transfers.to_user_id IS '接收转让的目标用户ID';
COMMENT ON COLUMN moderator_transfers.status IS '转让状态: pending-待处理, accepted-已接受, rejected-已拒绝, cancelled-已取消, expired-已过期';
COMMENT ON COLUMN moderator_transfers.message IS '转让说明/消息';
COMMENT ON COLUMN moderator_transfers.response_message IS '接收方的回复消息';
COMMENT ON COLUMN moderator_transfers.created_at IS '转让请求创建时间';
COMMENT ON COLUMN moderator_transfers.responded_at IS '响应时间';
COMMENT ON COLUMN moderator_transfers.expires_at IS '转让请求过期时间（默认7天）';
COMMENT ON COLUMN moderator_transfers.updated_at IS '最后更新时间';

-- 4. 创建触发器函数：自动更新 updated_at
CREATE OR REPLACE FUNCTION update_moderator_transfers_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- 5. 创建触发器
DROP TRIGGER IF EXISTS trigger_update_moderator_transfers_updated_at ON moderator_transfers;
CREATE TRIGGER trigger_update_moderator_transfers_updated_at
    BEFORE UPDATE ON moderator_transfers
    FOR EACH ROW
    EXECUTE FUNCTION update_moderator_transfers_updated_at();
