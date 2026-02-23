-- ====================================
-- 更新 notifications 表类型约束
-- 添加举报通知类型支持: user_report, city_report
-- 日期: 2025-06-15
-- ====================================

-- 删除旧的类型约束
ALTER TABLE public.notifications 
DROP CONSTRAINT IF EXISTS notifications_type_check;

-- 添加新的类型约束，包含举报通知类型
ALTER TABLE public.notifications
ADD CONSTRAINT notifications_type_check CHECK (
    type IN (
        -- 版主申请相关
        'moderator_application',
        'moderator_approved', 
        'moderator_rejected',
        -- 版主转让相关
        'moderator_transfer',
        'moderator_transfer_result',
        -- 活动邀请相关
        'event_invitation',
        'event_invitation_response',
        -- 举报相关
        'user_report',
        'city_report',
        -- 其他通知
        'city_update',
        'system_announcement',
        'other'
    )
);

-- 更新注释
COMMENT ON COLUMN public.notifications.type IS '通知类型: moderator_application(版主申请), moderator_approved(版主申请通过), moderator_rejected(版主申请拒绝), moderator_transfer(版主转让), moderator_transfer_result(版主转让结果), event_invitation(活动邀请), event_invitation_response(活动邀请响应), user_report(用户举报), city_report(城市举报), city_update(城市更新), system_announcement(系统公告), other(其他)';
