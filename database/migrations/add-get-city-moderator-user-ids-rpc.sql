-- ============================================================
-- RPC 函数：根据城市 ID 获取该城市活跃版主的用户 ID 列表
-- 用于举报城市内容时通知版主
-- ============================================================

-- 先删除旧版本（如果存在）
DROP FUNCTION IF EXISTS public.get_city_moderator_user_ids(UUID) CASCADE;
DROP FUNCTION IF EXISTS public.get_city_moderator_user_ids(TEXT) CASCADE;

-- 创建 RPC 函数
CREATE OR REPLACE FUNCTION public.get_city_moderator_user_ids(p_city_id TEXT)
RETURNS SETOF TEXT
LANGUAGE sql
STABLE
SECURITY DEFINER
AS $$
SELECT cm.user_id::text
FROM public.city_moderators cm
WHERE
    cm.city_id = p_city_id::uuid
    AND cm.is_active = true;
$$;

COMMENT ON FUNCTION public.get_city_moderator_user_ids(TEXT)
    IS '根据城市 ID 获取该城市所有活跃版主的用户 ID 列表';

-- 授权
GRANT EXECUTE ON FUNCTION public.get_city_moderator_user_ids(TEXT) TO service_role;
