-- 创建版主申请表
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

-- 创建触发器：自动更新 updated_at
CREATE OR REPLACE FUNCTION update_moderator_application_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trigger_update_moderator_application_updated_at ON moderator_applications;
CREATE TRIGGER trigger_update_moderator_application_updated_at
    BEFORE UPDATE ON moderator_applications
    FOR EACH ROW
    EXECUTE FUNCTION update_moderator_application_updated_at();

-- 创建统计视图
CREATE OR REPLACE VIEW moderator_application_statistics AS
SELECT 
    COUNT(*) as total,
    COUNT(*) FILTER (WHERE status = 'pending') as pending,
    COUNT(*) FILTER (WHERE status = 'approved') as approved,
    COUNT(*) FILTER (WHERE status = 'rejected') as rejected
FROM moderator_applications;

-- 添加注释
COMMENT ON TABLE moderator_applications IS '版主申请记录表';
COMMENT ON COLUMN moderator_applications.user_id IS '申请用户ID';
COMMENT ON COLUMN moderator_applications.city_id IS '申请的城市ID';
COMMENT ON COLUMN moderator_applications.reason IS '申请原因';
COMMENT ON COLUMN moderator_applications.status IS '申请状态: pending-待审核, approved-已批准, rejected-已拒绝';
COMMENT ON COLUMN moderator_applications.processed_by IS '处理该申请的管理员ID';
COMMENT ON COLUMN moderator_applications.processed_at IS '处理时间';
COMMENT ON COLUMN moderator_applications.rejection_reason IS '拒绝原因（如果被拒绝）';

-- 验证表已创建
SELECT 
    table_name, 
    column_name, 
    data_type 
FROM information_schema.columns 
WHERE table_name = 'moderator_applications' 
ORDER BY ordinal_position;
