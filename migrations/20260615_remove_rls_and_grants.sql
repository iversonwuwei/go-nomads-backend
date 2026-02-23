-- ====================================
-- 移除 notifications 相关表和函数的 RLS、角色限制
-- 日期: 2026-02-23
-- ====================================

-- ====================================
-- 1. 移除 notifications 表的 RLS 策略
-- ====================================
DROP POLICY IF EXISTS "用户只能查看自己的通知" ON public.notifications;
DROP POLICY IF EXISTS "用户只能更新自己的通知" ON public.notifications;
DROP POLICY IF EXISTS "用户只能删除自己的通知" ON public.notifications;
DROP POLICY IF EXISTS "服务端可以插入通知" ON public.notifications;

-- 禁用 RLS
ALTER TABLE public.notifications DISABLE ROW LEVEL SECURITY;

-- ====================================
-- 2. 撤销 notifications 表的角色授权
-- ====================================
REVOKE ALL ON public.notifications FROM authenticated;
REVOKE ALL ON public.notifications FROM service_role;

-- ====================================
-- 3. 撤销 unread_notifications_count 视图的授权
-- ====================================
REVOKE ALL ON public.unread_notifications_count FROM authenticated;

-- ====================================
-- 4. 撤销 RPC 函数的角色授权并移除 SECURITY DEFINER
-- ====================================

-- get_admin_user_ids: 撤销授权
REVOKE ALL ON FUNCTION public.get_admin_user_ids() FROM service_role;
REVOKE ALL ON FUNCTION public.get_admin_user_ids() FROM authenticated;

-- get_city_moderator_user_ids: 撤销授权
REVOKE ALL ON FUNCTION public.get_city_moderator_user_ids(text) FROM service_role;
REVOKE ALL ON FUNCTION public.get_city_moderator_user_ids(text) FROM authenticated;

-- 重建函数（去掉 SECURITY DEFINER）
CREATE OR REPLACE FUNCTION public.get_admin_user_ids()
RETURNS json
LANGUAGE sql
STABLE
AS $$
    SELECT COALESCE(
        json_agg(u.id::text),
        '[]'::json
    )
    FROM public.users u
    INNER JOIN public.roles r ON u.role_id = r.id
    WHERE r.name = 'admin' OR r.name = 'administrator';
$$;

CREATE OR REPLACE FUNCTION public.get_city_moderator_user_ids(p_city_id text)
RETURNS json
LANGUAGE sql
STABLE
AS $$
    SELECT COALESCE(
        json_agg(cm.user_id::text),
        '[]'::json
    )
    FROM public.city_moderators cm
    WHERE cm.city_id = p_city_id::uuid
      AND cm.is_active = true;
$$;
