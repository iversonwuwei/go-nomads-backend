-- ============================================================
-- 创建举报表 (reports)
-- 用于存储用户举报记录
-- ============================================================

-- 创建举报表
CREATE TABLE IF NOT EXISTS reports (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    reporter_id TEXT NOT NULL,
    reporter_name TEXT,
    content_type TEXT NOT NULL,  -- user / message / meetup / innovationProject / chatRoom
    target_id TEXT NOT NULL,
    target_name TEXT,
    reason_id TEXT NOT NULL,     -- spam / harassment / inappropriate / fraud / violence / other
    reason_label TEXT NOT NULL,
    status TEXT NOT NULL DEFAULT 'pending',  -- pending / reviewed / resolved / dismissed
    admin_notes TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_reports_reporter_id ON reports(reporter_id);
CREATE INDEX IF NOT EXISTS idx_reports_target_id ON reports(target_id);
CREATE INDEX IF NOT EXISTS idx_reports_content_type ON reports(content_type);
CREATE INDEX IF NOT EXISTS idx_reports_status ON reports(status);
CREATE INDEX IF NOT EXISTS idx_reports_created_at ON reports(created_at DESC);

-- 添加注释
COMMENT ON TABLE reports IS '用户举报记录表';
COMMENT ON COLUMN reports.id IS '举报记录唯一标识';
COMMENT ON COLUMN reports.reporter_id IS '举报人用户ID';
COMMENT ON COLUMN reports.reporter_name IS '举报人名称（冗余字段，方便查询）';
COMMENT ON COLUMN reports.content_type IS '举报内容类型: user/message/meetup/innovationProject/chatRoom';
COMMENT ON COLUMN reports.target_id IS '被举报对象ID';
COMMENT ON COLUMN reports.target_name IS '被举报对象名称（冗余字段）';
COMMENT ON COLUMN reports.reason_id IS '举报原因标识: spam/harassment/inappropriate/fraud/violence/other';
COMMENT ON COLUMN reports.reason_label IS '举报原因描述文本';
COMMENT ON COLUMN reports.status IS '处理状态: pending(待处理)/reviewed(已审核)/resolved(已处理)/dismissed(已驳回)';
COMMENT ON COLUMN reports.admin_notes IS '管理员备注';
COMMENT ON COLUMN reports.created_at IS '创建时间';
COMMENT ON COLUMN reports.updated_at IS '更新时间';

-- RLS 策略
ALTER TABLE reports ENABLE ROW LEVEL SECURITY;

-- 允许认证用户插入自己的举报记录
CREATE POLICY "Users can insert their own reports"
    ON reports FOR INSERT
    TO authenticated
    WITH CHECK (reporter_id = auth.uid()::text);

-- 允许用户查看自己提交的举报记录
CREATE POLICY "Users can view their own reports"
    ON reports FOR SELECT
    TO authenticated
    USING (reporter_id = auth.uid()::text);

-- 服务端使用 service_role key 绕过 RLS，所以后端 API 不受影响
