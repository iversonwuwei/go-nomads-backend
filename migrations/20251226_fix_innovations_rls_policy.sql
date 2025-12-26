-- ============================================
-- 禁用 innovations 和 innovation_likes 表的 RLS 策略
-- 解决 "new row violates row-level security policy" 错误
-- ============================================

-- ========== innovations 表 ==========
-- 禁用 RLS
ALTER TABLE public.innovations DISABLE ROW LEVEL SECURITY;

-- 删除现有的 RLS 策略（如果存在）
DROP POLICY IF EXISTS "Public read access" ON public.innovations;
DROP POLICY IF EXISTS "Users can create innovations" ON public.innovations;
DROP POLICY IF EXISTS "Users can update own innovations" ON public.innovations;
DROP POLICY IF EXISTS "Users can delete own innovations" ON public.innovations;

-- ========== innovation_likes 表 ==========
-- 禁用 RLS
ALTER TABLE public.innovation_likes DISABLE ROW LEVEL SECURITY;

-- 删除现有的 RLS 策略（如果存在）
DROP POLICY IF EXISTS "Public read access" ON public.innovation_likes;
DROP POLICY IF EXISTS "Users can like" ON public.innovation_likes;
DROP POLICY IF EXISTS "Users can unlike" ON public.innovation_likes;
DROP POLICY IF EXISTS "Users can manage own likes" ON public.innovation_likes;
DROP POLICY IF EXISTS "innovation_likes_insert_policy" ON public.innovation_likes;
DROP POLICY IF EXISTS "innovation_likes_delete_policy" ON public.innovation_likes;
DROP POLICY IF EXISTS "innovation_likes_select_policy" ON public.innovation_likes;

-- 完成：现在 innovations 和 innovation_likes 表都没有任何行级安全限制
