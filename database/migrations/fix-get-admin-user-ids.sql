-- ====================================
-- 修复 get_admin_user_ids 函数
-- ====================================
-- 问题: 原函数查询 auth.users 表, 但实际用户数据在 public.users 表
-- 日期: 2025-11-30
-- ====================================

-- 删除旧函数
DROP FUNCTION IF EXISTS public.get_admin_user_ids() CASCADE;

-- 创建新函数：从 public.users 和 public.roles 表中查询管理员
CREATE OR REPLACE FUNCTION public.get_admin_user_ids()
RETURNS SETOF TEXT
LANGUAGE sql
STABLE
SECURITY DEFINER
AS $$
    SELECT u.id::text
    FROM public.users u
    INNER JOIN public.roles r ON u.role_id = r.id
    WHERE r.name = 'admin' OR r.name = 'administrator';
$$;

COMMENT ON FUNCTION public.get_admin_user_ids() IS '获取所有管理员用户的ID列表（从 public.users 表查询）';

-- 授权
GRANT EXECUTE ON FUNCTION public.get_admin_user_ids() TO service_role;
GRANT EXECUTE ON FUNCTION public.get_admin_user_ids() TO authenticated;

-- ====================================
-- 完成
-- ====================================
