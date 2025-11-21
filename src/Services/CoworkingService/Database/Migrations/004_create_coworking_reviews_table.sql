-- Migration: Create coworking_reviews table
-- Description: 为 Coworking 空间添加评论功能
-- Author: AI Assistant
-- Date: 2024

-- ============================================================
-- 1. 创建 coworking_reviews 表
-- ============================================================
CREATE TABLE IF NOT EXISTS public.coworking_reviews (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    coworking_id UUID NOT NULL REFERENCES public.coworking_spaces(id) ON DELETE CASCADE,
    user_id UUID NOT NULL,
    username VARCHAR(255) NOT NULL,
    user_avatar VARCHAR(500),
    rating DECIMAL(2, 1) NOT NULL CHECK (rating >= 1.0 AND rating <= 5.0),
    title VARCHAR(100) NOT NULL,
    content TEXT NOT NULL,
    visit_date DATE,
    photo_urls TEXT[],
    is_verified BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE
);

-- ============================================================
-- 2. 创建索引以优化查询性能
-- ============================================================

-- 按 coworking_id 和创建时间降序查询（评论列表）
CREATE INDEX idx_coworking_reviews_coworking_created 
    ON public.coworking_reviews(coworking_id, created_at DESC);

-- 按用户和 coworking 查询（查找用户是否已评论）
CREATE INDEX idx_coworking_reviews_user_coworking 
    ON public.coworking_reviews(user_id, coworking_id);

-- 按评分查询（可能用于筛选高分评论）
CREATE INDEX idx_coworking_reviews_rating 
    ON public.coworking_reviews(rating);

-- 按验证状态查询（管理员审核）
CREATE INDEX idx_coworking_reviews_verified 
    ON public.coworking_reviews(is_verified);

-- ============================================================
-- 3. 创建触发器：自动更新 updated_at 字段
-- ============================================================
CREATE OR REPLACE FUNCTION update_coworking_reviews_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_update_coworking_reviews_updated_at
    BEFORE UPDATE ON public.coworking_reviews
    FOR EACH ROW
    EXECUTE FUNCTION update_coworking_reviews_updated_at();

-- ============================================================
-- 4. 添加注释说明
-- ============================================================
COMMENT ON TABLE public.coworking_reviews IS 'Coworking 空间评论表';
COMMENT ON COLUMN public.coworking_reviews.id IS '评论唯一标识';
COMMENT ON COLUMN public.coworking_reviews.coworking_id IS 'Coworking 空间 ID';
COMMENT ON COLUMN public.coworking_reviews.user_id IS '评论用户 ID';
COMMENT ON COLUMN public.coworking_reviews.username IS '用户名';
COMMENT ON COLUMN public.coworking_reviews.user_avatar IS '用户头像 URL';
COMMENT ON COLUMN public.coworking_reviews.rating IS '评分 (1.0-5.0)';
COMMENT ON COLUMN public.coworking_reviews.title IS '评论标题';
COMMENT ON COLUMN public.coworking_reviews.content IS '评论内容';
COMMENT ON COLUMN public.coworking_reviews.visit_date IS '访问日期';
COMMENT ON COLUMN public.coworking_reviews.photo_urls IS '照片 URL 数组（最多 5 张）';
COMMENT ON COLUMN public.coworking_reviews.is_verified IS '是否已验证（管理员审核）';
COMMENT ON COLUMN public.coworking_reviews.created_at IS '创建时间';
COMMENT ON COLUMN public.coworking_reviews.updated_at IS '更新时间';

-- ============================================================
-- 5. 验证表结构
-- ============================================================
-- 查询表信息
SELECT 
    column_name, 
    data_type, 
    is_nullable, 
    column_default
FROM information_schema.columns
WHERE table_schema = 'public' 
  AND table_name = 'coworking_reviews'
ORDER BY ordinal_position;
