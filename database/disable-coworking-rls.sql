-- 临时禁用 coworking_spaces 表的 RLS (仅用于开发测试)
ALTER TABLE public.coworking_spaces DISABLE ROW LEVEL SECURITY;

-- 或者,如果要保持 RLS 启用,添加允许所有操作的策略:
-- DROP POLICY IF EXISTS "Allow all operations for development" ON public.coworking_spaces;
-- CREATE POLICY "Allow all operations for development" 
-- ON public.coworking_spaces 
-- FOR ALL 
-- USING (true) 
-- WITH CHECK (true);
