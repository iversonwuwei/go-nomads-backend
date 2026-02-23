-- ====================================
-- 修复 get_admin_user_ids 和 get_city_moderator_user_ids RPC 函数
-- 问题: SETOF TEXT 返回格式为 [{"func_name": "value"}]，
--       无法直接被 Supabase .NET 客户端反序列化为 List<string>
-- 修复: 改为返回 json 类型，使用 json_agg 返回纯字符串数组 ["uuid1", "uuid2"]
-- 日期: 2025-06-15
-- ====================================

-- ====================================
-- 修复 get_admin_user_ids
-- ====================================
DROP FUNCTION IF EXISTS public.get_admin_user_ids() CASCADE;

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

COMMENT ON FUNCTION public.get_admin_user_ids() IS '获取所有管理员用户的ID列表（返回 JSON 数组）';

-- ====================================
-- 修复 get_city_moderator_user_ids
-- ====================================
DROP FUNCTION IF EXISTS public.get_city_moderator_user_ids(text) CASCADE;
DROP FUNCTION IF EXISTS public.get_city_moderator_user_ids(uuid) CASCADE;

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

COMMENT ON FUNCTION public.get_city_moderator_user_ids(text) IS '获取指定城市的活跃版主用户ID列表（返回 JSON 数组）';
