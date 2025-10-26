-- 为 coworking_spaces 表添加写入策略
-- 允许服务账号或认证用户创建和管理 coworking spaces

-- 1. 删除现有的只读策略
DROP POLICY IF EXISTS "Public read access" ON public.coworking_spaces;

-- 2. 添加新的策略

-- 公开读取 (只显示激活的记录)
CREATE POLICY "Public can view active coworking spaces" 
ON public.coworking_spaces 
FOR SELECT 
USING (is_active = true);

-- 认证用户可以创建 coworking spaces
CREATE POLICY "Authenticated users can create coworking spaces" 
ON public.coworking_spaces 
FOR INSERT 
WITH CHECK (true);

-- 用户可以更新自己创建的 coworking spaces
CREATE POLICY "Users can update own coworking spaces" 
ON public.coworking_spaces 
FOR UPDATE 
USING (auth.uid()::text = created_by::text OR created_by IS NULL);

-- 用户可以删除自己创建的 coworking spaces
CREATE POLICY "Users can delete own coworking spaces" 
ON public.coworking_spaces 
FOR DELETE 
USING (auth.uid()::text = created_by::text OR created_by IS NULL);

-- 3. 为服务账号添加绕过 RLS 的策略
-- 如果使用 service_role key，RLS 会自动绕过
-- 但如果需要为特定角色设置，可以创建角色并授权

-- 4. 验证策略
SELECT 
    schemaname,
    tablename,
    policyname,
    permissive,
    roles,
    cmd,
    qual,
    with_check
FROM pg_policies
WHERE tablename = 'coworking_spaces';
