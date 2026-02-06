-- ============================================
-- 修复 is_deleted 字段的 NULL 值问题
-- 创建日期: 2026-02-05
-- 描述: 将所有表的 is_deleted 字段设为 NOT NULL，默认值 false
-- ============================================

-- ============================================
-- 1. innovations 表
-- ============================================
-- 更新 NULL 值为 false
UPDATE public.innovations SET is_deleted = false WHERE is_deleted IS NULL;

-- 修改列为 NOT NULL，默认 false
ALTER TABLE public.innovations 
ALTER COLUMN is_deleted SET DEFAULT false,
ALTER COLUMN is_deleted SET NOT NULL;

-- ============================================
-- 2. coworking_spaces 表
-- ============================================
UPDATE public.coworking_spaces SET is_deleted = false WHERE is_deleted IS NULL;

ALTER TABLE public.coworking_spaces 
ALTER COLUMN is_deleted SET DEFAULT false,
ALTER COLUMN is_deleted SET NOT NULL;

-- ============================================
-- 3. cities 表
-- ============================================
UPDATE public.cities SET is_deleted = false WHERE is_deleted IS NULL;

ALTER TABLE public.cities 
ALTER COLUMN is_deleted SET DEFAULT false,
ALTER COLUMN is_deleted SET NOT NULL;

-- ============================================
-- 4. events 表 (如果存在)
-- ============================================
DO $$
BEGIN
    IF EXISTS (
        SELECT FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'events' 
        AND column_name = 'is_deleted'
    ) THEN
        UPDATE public.events SET is_deleted = false WHERE is_deleted IS NULL;
        
        ALTER TABLE public.events 
        ALTER COLUMN is_deleted SET DEFAULT false,
        ALTER COLUMN is_deleted SET NOT NULL;
    END IF;
END $$;

-- ============================================
-- 5. innovation_comments 表 (如果存在)
-- ============================================
DO $$
BEGIN
    IF EXISTS (
        SELECT FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'innovation_comments' 
        AND column_name = 'is_deleted'
    ) THEN
        UPDATE public.innovation_comments SET is_deleted = false WHERE is_deleted IS NULL;
        
        ALTER TABLE public.innovation_comments 
        ALTER COLUMN is_deleted SET DEFAULT false,
        ALTER COLUMN is_deleted SET NOT NULL;
    END IF;
END $$;

-- ============================================
-- 验证修复结果
-- ============================================
SELECT 'innovations' as table_name, COUNT(*) as null_count 
FROM public.innovations WHERE is_deleted IS NULL
UNION ALL
SELECT 'coworking_spaces', COUNT(*) 
FROM public.coworking_spaces WHERE is_deleted IS NULL
UNION ALL
SELECT 'cities', COUNT(*) 
FROM public.cities WHERE is_deleted IS NULL;
