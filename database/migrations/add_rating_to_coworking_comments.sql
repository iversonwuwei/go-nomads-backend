-- =============================================================
-- Add rating column to coworking_comments table
-- 为 coworking_comments 表添加评分字段
-- =============================================================

-- 添加 rating 列，默认值为 0，范围 0-5
ALTER TABLE public.coworking_comments 
ADD COLUMN IF NOT EXISTS rating INTEGER NOT NULL DEFAULT 0 
CHECK (rating >= 0 AND rating <= 5);

-- 为现有记录设置默认评分（如果需要）
UPDATE public.coworking_comments 
SET rating = 0 
WHERE rating IS NULL;

COMMENT ON COLUMN public.coworking_comments.rating IS '评分 (0-5 星)';
