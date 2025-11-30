-- ====================================
-- 重新创建通知系统数据库
-- ====================================
-- 警告：这将删除现有的 notifications 表及其所有数据！
-- 版本: 1.0.0
-- 日期: 2025-01-13

-- ====================================
-- 1. 删除旧的对象
-- ====================================
-- 删除视图
DROP VIEW IF EXISTS public.unread_notifications_count CASCADE;

-- 删除触发器
DROP TRIGGER IF EXISTS trigger_set_notification_read_at ON public.notifications CASCADE;

-- 删除触发器函数
DROP FUNCTION IF EXISTS public.set_notification_read_at() CASCADE;

-- 删除RPC函数
DROP FUNCTION IF EXISTS public.get_admin_user_ids() CASCADE;

-- 删除表
DROP TABLE IF EXISTS public.notifications CASCADE;

-- ====================================
-- 2. 创建 notifications 表
-- ====================================
CREATE TABLE public.notifications (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id TEXT NOT NULL,
    title TEXT NOT NULL,
    message TEXT NOT NULL,
    type TEXT NOT NULL CHECK (type IN ('moderator_application', 'moderator_approved', 'moderator_rejected', 'city_update', 'system_announcement', 'other')),
    related_id TEXT,
    metadata JSONB,
    is_read BOOLEAN NOT NULL DEFAULT false,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    read_at TIMESTAMP WITH TIME ZONE,
    
    -- 约束
    CONSTRAINT check_read_at CHECK (
        (is_read = false AND read_at IS NULL) OR
        (is_read = true AND read_at IS NOT NULL)
    )
);

-- ====================================
-- 3. 创建索引
-- ====================================
-- 用户ID索引（提高查询性能）
CREATE INDEX idx_notifications_user_id ON public.notifications(user_id);

-- 未读通知索引
CREATE INDEX idx_notifications_user_unread ON public.notifications(user_id, is_read) WHERE is_read = false;

-- 创建时间索引
CREATE INDEX idx_notifications_created_at ON public.notifications(created_at DESC);

-- 组合索引：用户ID + 创建时间
CREATE INDEX idx_notifications_user_created ON public.notifications(user_id, created_at DESC);

-- 类型索引
CREATE INDEX idx_notifications_type ON public.notifications(type);

-- ====================================
-- 4. 添加表注释
-- ====================================
COMMENT ON TABLE public.notifications IS '通知表 - 存储用户通知信息';
COMMENT ON COLUMN public.notifications.id IS '通知ID';
COMMENT ON COLUMN public.notifications.user_id IS '接收用户ID';
COMMENT ON COLUMN public.notifications.title IS '通知标题';
COMMENT ON COLUMN public.notifications.message IS '通知消息内容';
COMMENT ON COLUMN public.notifications.type IS '通知类型: moderator_application(版主申请), moderator_approved(版主批准), moderator_rejected(版主拒绝), city_update(城市更新), system_announcement(系统公告), other(其他)';
COMMENT ON COLUMN public.notifications.related_id IS '关联的资源ID（如城市ID、申请ID等）';
COMMENT ON COLUMN public.notifications.metadata IS '元数据（JSON格式，存储额外信息）';
COMMENT ON COLUMN public.notifications.is_read IS '是否已读';
COMMENT ON COLUMN public.notifications.created_at IS '创建时间';
COMMENT ON COLUMN public.notifications.read_at IS '阅读时间';

-- ====================================
-- 5. 创建RPC函数：获取管理员用户ID列表
-- ====================================
CREATE OR REPLACE FUNCTION public.get_admin_user_ids()
RETURNS SETOF TEXT
LANGUAGE sql
STABLE
SECURITY DEFINER
AS $$
SELECT u.id::text
FROM public.users u
    INNER JOIN public.roles r ON u.role_id = r.id
WHERE
    r.name = 'admin'
    OR r.name = 'administrator';
$$;

COMMENT ON FUNCTION public.get_admin_user_ids () IS '获取所有管理员用户的ID列表（从 public.users 表查询）';

-- ====================================
-- 6. 启用Row Level Security (RLS)
-- ====================================
ALTER TABLE public.notifications ENABLE ROW LEVEL SECURITY;

-- ====================================
-- 7. 创建RLS策略
-- ====================================

-- 用户只能查看自己的通知
CREATE POLICY "用户只能查看自己的通知"
    ON public.notifications
    FOR SELECT
    USING (user_id = auth.uid()::text);

-- 用户只能更新自己的通知
CREATE POLICY "用户只能更新自己的通知"
    ON public.notifications
    FOR UPDATE
    USING (user_id = auth.uid()::text);

-- 用户只能删除自己的通知
CREATE POLICY "用户只能删除自己的通知"
    ON public.notifications
    FOR DELETE
    USING (user_id = auth.uid()::text);

-- 服务端可以插入任何通知（使用service_role key）
CREATE POLICY "服务端可以插入通知"
    ON public.notifications
    FOR INSERT
    WITH CHECK (true);

-- ====================================
-- 8. 创建触发器：自动设置read_at时间
-- ====================================
CREATE OR REPLACE FUNCTION public.set_notification_read_at()
RETURNS TRIGGER AS $$
BEGIN
    -- 当is_read从false变为true时，自动设置read_at
    IF NEW.is_read = true AND OLD.is_read = false THEN
        NEW.read_at = NOW();
    END IF;
    
    -- 当is_read从true变为false时，清空read_at
    IF NEW.is_read = false AND OLD.is_read = true THEN
        NEW.read_at = NULL;
    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_set_notification_read_at
    BEFORE UPDATE ON public.notifications
    FOR EACH ROW
    EXECUTE FUNCTION public.set_notification_read_at();

COMMENT ON FUNCTION public.set_notification_read_at() IS '触发器函数：自动设置或清空通知的read_at时间';

-- ====================================
-- 9. 创建视图：未读通知统计
-- ====================================
CREATE OR REPLACE VIEW public.unread_notifications_count AS
SELECT 
    user_id,
    COUNT(*) as unread_count
FROM public.notifications
WHERE is_read = false
GROUP BY user_id;

COMMENT ON VIEW public.unread_notifications_count IS '用户未读通知数量统计视图';

-- ====================================
-- 10. 授权
-- ====================================
-- 允许认证用户访问notifications表
GRANT SELECT, UPDATE, DELETE ON public.notifications TO authenticated;
GRANT INSERT ON public.notifications TO service_role;

-- 允许访问视图
GRANT SELECT ON public.unread_notifications_count TO authenticated;

-- 允许执行RPC函数
GRANT EXECUTE ON FUNCTION public.get_admin_user_ids() TO service_role;

-- ====================================
-- 完成
-- ====================================
-- 重新创建完成！
-- 注意：所有旧数据已被删除
