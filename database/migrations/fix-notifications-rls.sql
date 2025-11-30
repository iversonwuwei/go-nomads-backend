-- ====================================
-- 修复 notifications 表的 RLS 策略
-- ====================================
-- 问题: 服务端使用 service_role 插入通知时被 RLS 策略阻止
-- 日期: 2025-11-30
-- ====================================

-- 方案1: 完全禁用 RLS（最简单的解决方案，适用于后端服务直接管理权限的场景）
ALTER TABLE public.notifications DISABLE ROW LEVEL SECURITY;

-- 确保表的权限正确
GRANT ALL ON public.notifications TO service_role;
GRANT ALL ON public.notifications TO authenticated;
GRANT ALL ON public.notifications TO anon;

-- ====================================
-- 完成
-- ====================================
