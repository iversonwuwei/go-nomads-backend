-- ============================================================
-- 修复举报表 RLS 策略
-- 后端服务使用 anon key 连接 Supabase，auth.uid() 不可用
-- 因此需要禁用 RLS，由后端 API 层控制权限
-- ============================================================

-- 删除现有 RLS 策略
DROP POLICY IF EXISTS "Users can insert their own reports" ON reports;
DROP POLICY IF EXISTS "Users can view their own reports" ON reports;

-- 禁用 RLS（后端已通过 JWT 中间件验证用户身份）
ALTER TABLE reports DISABLE ROW LEVEL SECURITY;

-- 授权
GRANT ALL ON public.reports TO service_role;
GRANT ALL ON public.reports TO authenticated;
GRANT ALL ON public.reports TO anon;
