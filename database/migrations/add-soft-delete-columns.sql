-- ============================================
-- 添加逻辑删除字段到主要业务表
-- 创建日期: 2025-01-XX
-- 描述: 为 cities, coworking_spaces, innovations, events 表添加 is_deleted, deleted_at, deleted_by 字段
-- ============================================

-- ============================================
-- 1. cities 表
-- ============================================
-- 注意: cities 表已有 is_active 字段用于禁用，这里添加 is_deleted 用于逻辑删除
ALTER TABLE public.cities 
ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN DEFAULT false,
ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMP WITH TIME ZONE,
ADD COLUMN IF NOT EXISTS deleted_by UUID REFERENCES public.users(id);

-- 创建索引加速查询
CREATE INDEX IF NOT EXISTS idx_cities_is_deleted ON public.cities(is_deleted);

-- ============================================
-- 2. coworking_spaces 表
-- ============================================
ALTER TABLE public.coworking_spaces 
ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN DEFAULT false,
ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMP WITH TIME ZONE,
ADD COLUMN IF NOT EXISTS deleted_by UUID REFERENCES public.users(id);

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_coworking_is_deleted ON public.coworking_spaces(is_deleted);

-- ============================================
-- 3. innovations 表
-- ============================================
ALTER TABLE public.innovations 
ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN DEFAULT false,
ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMP WITH TIME ZONE,
ADD COLUMN IF NOT EXISTS deleted_by UUID REFERENCES public.users(id);

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_innovations_is_deleted ON public.innovations(is_deleted);

-- ============================================
-- 4. events 表 (如果存在)
-- ============================================
DO $$
BEGIN
    IF EXISTS (SELECT FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'events') THEN
        ALTER TABLE public.events 
        ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN DEFAULT false;
        ALTER TABLE public.events 
        ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMP WITH TIME ZONE;
        ALTER TABLE public.events 
        ADD COLUMN IF NOT EXISTS deleted_by UUID REFERENCES public.users(id);
        
        CREATE INDEX IF NOT EXISTS idx_events_is_deleted ON public.events(is_deleted);
    END IF;
END $$;

-- ============================================
-- 5. 更新查询视图和策略 (可选)
-- ============================================
-- 注意: 如果有 RLS 策略，需要更新它们以排除已删除的记录
-- 例如:
-- DROP POLICY IF EXISTS "Public read access" ON public.innovations;
-- CREATE POLICY "Public read access" ON public.innovations 
--     FOR SELECT USING (is_public = true AND is_deleted = false);

-- ============================================
-- 6. 注释
-- ============================================
COMMENT ON COLUMN public.cities.is_deleted IS '逻辑删除标记';
COMMENT ON COLUMN public.cities.deleted_at IS '删除时间';
COMMENT ON COLUMN public.cities.deleted_by IS '删除操作执行者ID';

COMMENT ON COLUMN public.coworking_spaces.is_deleted IS '逻辑删除标记';
COMMENT ON COLUMN public.coworking_spaces.deleted_at IS '删除时间';
COMMENT ON COLUMN public.coworking_spaces.deleted_by IS '删除操作执行者ID';

COMMENT ON COLUMN public.innovations.is_deleted IS '逻辑删除标记';
COMMENT ON COLUMN public.innovations.deleted_at IS '删除时间';
COMMENT ON COLUMN public.innovations.deleted_by IS '删除操作执行者ID';
