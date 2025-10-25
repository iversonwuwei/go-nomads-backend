-- ============================================
-- 添加活动关注者表 (event_followers)
-- 创建日期: 2025-10-25
-- 说明: 用于"关注活动"功能，区别于 event_participants(参与活动)
-- ============================================

-- 活动关注者表
CREATE TABLE IF NOT EXISTS public.event_followers (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    event_id UUID NOT NULL REFERENCES public.events(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    followed_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    notification_enabled BOOLEAN DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(event_id, user_id)
);

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_event_followers_event_id ON public.event_followers(event_id);
CREATE INDEX IF NOT EXISTS idx_event_followers_user_id ON public.event_followers(user_id);
CREATE INDEX IF NOT EXISTS idx_event_followers_followed_at ON public.event_followers(followed_at);

-- 添加注释
COMMENT ON TABLE public.event_followers IS '活动关注者表 - 记录用户关注的活动(不同于参与活动)';
COMMENT ON COLUMN public.event_followers.event_id IS '活动ID';
COMMENT ON COLUMN public.event_followers.user_id IS '用户ID';
COMMENT ON COLUMN public.event_followers.followed_at IS '关注时间';
COMMENT ON COLUMN public.event_followers.notification_enabled IS '是否启用通知';

-- 验证表创建
SELECT 
    'event_followers 表创建成功' as status,
    COUNT(*) as column_count
FROM information_schema.columns
WHERE table_schema = 'public' 
  AND table_name = 'event_followers';
