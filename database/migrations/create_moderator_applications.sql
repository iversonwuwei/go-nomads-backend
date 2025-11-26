-- =========================================
-- 版主申请系统数据库表
-- =========================================

-- 1. 创建版主申请表
CREATE TABLE IF NOT EXISTS moderator_applications (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    city_id UUID NOT NULL REFERENCES cities(id) ON DELETE CASCADE,
    reason TEXT NOT NULL,
    status VARCHAR(20) DEFAULT 'pending' CHECK (status IN ('pending', 'approved', 'rejected')),
    processed_by UUID,
    processed_at TIMESTAMP WITH TIME ZONE,
    rejection_reason TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_moderator_applications_user_id ON moderator_applications(user_id);
CREATE INDEX IF NOT EXISTS idx_moderator_applications_city_id ON moderator_applications(city_id);
CREATE INDEX IF NOT EXISTS idx_moderator_applications_status ON moderator_applications(status);
CREATE INDEX IF NOT EXISTS idx_moderator_applications_created_at ON moderator_applications(created_at DESC);

-- 添加注释
COMMENT ON TABLE moderator_applications IS '版主申请记录表';
COMMENT ON COLUMN moderator_applications.user_id IS '申请用户ID';
COMMENT ON COLUMN moderator_applications.city_id IS '申请的城市ID';
COMMENT ON COLUMN moderator_applications.reason IS '申请原因';
COMMENT ON COLUMN moderator_applications.status IS '申请状态: pending-待审核, approved-已批准, rejected-已拒绝';
COMMENT ON COLUMN moderator_applications.processed_by IS '处理该申请的管理员ID';
COMMENT ON COLUMN moderator_applications.processed_at IS '处理时间';
COMMENT ON COLUMN moderator_applications.rejection_reason IS '拒绝原因（如果被拒绝）';

-- 2. 为 notifications 表添加索引（如果还没有）
CREATE INDEX IF NOT EXISTS idx_notifications_user_id ON notifications(user_id);
CREATE INDEX IF NOT EXISTS idx_notifications_type ON notifications(type);
CREATE INDEX IF NOT EXISTS idx_notifications_is_read ON notifications(is_read);
CREATE INDEX IF NOT EXISTS idx_notifications_created_at ON notifications(created_at DESC);

-- 3. 为 city_moderators 表添加索引（如果还没有）
CREATE INDEX IF NOT EXISTS idx_city_moderators_user_id ON city_moderators(user_id);
CREATE INDEX IF NOT EXISTS idx_city_moderators_city_id ON city_moderators(city_id);
CREATE INDEX IF NOT EXISTS idx_city_moderators_is_active ON city_moderators(is_active);
CREATE INDEX IF NOT EXISTS idx_city_moderators_unique_active ON city_moderators(city_id, user_id) WHERE is_active = true;

-- 4. 创建触发器：自动更新 updated_at 字段
CREATE OR REPLACE FUNCTION update_moderator_application_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_moderator_application_updated_at
    BEFORE UPDATE ON moderator_applications
    FOR EACH ROW
    EXECUTE FUNCTION update_moderator_application_updated_at();

-- 5. Row Level Security (RLS) 策略
ALTER TABLE moderator_applications ENABLE ROW LEVEL SECURITY;

-- 用户可以查看自己的申请
CREATE POLICY "Users can view their own applications"
    ON moderator_applications FOR SELECT
    USING (auth.uid() = user_id);

-- 用户可以创建申请
CREATE POLICY "Users can create applications"
    ON moderator_applications FOR INSERT
    WITH CHECK (auth.uid() = user_id);

-- 管理员可以查看所有申请
CREATE POLICY "Admins can view all applications"
    ON moderator_applications FOR SELECT
    USING (
        EXISTS (
            SELECT 1 FROM auth.users
            WHERE auth.users.id = auth.uid()
            AND auth.users.role = 'admin'
        )
    );

-- 管理员可以更新申请
CREATE POLICY "Admins can update applications"
    ON moderator_applications FOR UPDATE
    USING (
        EXISTS (
            SELECT 1 FROM auth.users
            WHERE auth.users.id = auth.uid()
            AND auth.users.role = 'admin'
        )
    );

-- 6. 创建统计视图（可选，用于快速查询统计数据）
CREATE OR REPLACE VIEW moderator_application_statistics AS
SELECT
    COUNT(*) AS total_applications,
    COUNT(*) FILTER (WHERE status = 'pending') AS pending_count,
    COUNT(*) FILTER (WHERE status = 'approved') AS approved_count,
    COUNT(*) FILTER (WHERE status = 'rejected') AS rejected_count,
    AVG(EXTRACT(EPOCH FROM (processed_at - created_at)) / 3600) FILTER (WHERE processed_at IS NOT NULL) AS avg_processing_hours
FROM moderator_applications;

-- 7. 插入测试数据（可选 - 仅用于开发环境）
-- INSERT INTO moderator_applications (user_id, city_id, reason, status)
-- VALUES 
--     ('9d789131-e560-47cf-9ff1-b05f9c345207'::UUID, 'some-city-id'::UUID, '我热爱这个城市，希望为社区做贡献', 'pending'),
--     ('another-user-id'::UUID, 'another-city-id'::UUID, '我对数字游民生活有丰富经验', 'approved');

-- 8. 授权（根据你的 Supabase 配置调整）
-- GRANT SELECT, INSERT, UPDATE ON moderator_applications TO authenticated;
-- GRANT SELECT ON moderator_application_statistics TO authenticated;

-- 完成！
-- 使用以下查询验证表结构：
-- SELECT * FROM moderator_applications LIMIT 10;
-- SELECT * FROM moderator_application_statistics;
