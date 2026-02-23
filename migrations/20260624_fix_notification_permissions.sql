-- ====================================
-- 撤销 20260615_remove_rls_and_grants.sql 造成的权限损坏
-- 日期: 2026-02-24
-- 问题: 之前的迁移 REVOKE ALL 破坏了 service_role 的默认权限，
--       导致后端无法操作 notifications 表和 RPC 函数
-- 修复: 恢复 Supabase 默认权限即可，角色控制在服务层完成
-- ====================================

-- 恢复 notifications 表的默认权限
GRANT ALL ON public.notifications TO postgres, service_role;

-- 恢复 RPC 函数的默认权限
GRANT EXECUTE ON FUNCTION public.get_admin_user_ids() TO postgres, service_role;
GRANT EXECUTE ON FUNCTION public.get_city_moderator_user_ids(text) TO postgres, service_role;

-- 恢复视图的默认权限
GRANT ALL ON public.unread_notifications_count TO postgres, service_role;
