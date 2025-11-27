-- ============================================
-- 添加城市图片字段迁移
-- 日期: 2025-11-27
-- 描述: 为 cities 表添加竖屏和横屏图片字段
-- ============================================

-- 添加竖屏封面图 URL 字段 (720x1280)
ALTER TABLE public.cities
ADD COLUMN IF NOT EXISTS portrait_image_url TEXT;

-- 添加横屏图片 URL 列表字段 (1280x720)，使用 TEXT[] 数组存储
ALTER TABLE public.cities
ADD COLUMN IF NOT EXISTS landscape_image_urls TEXT[];

-- 添加字段注释
COMMENT ON COLUMN public.cities.portrait_image_url IS '竖屏封面图 URL (720x1280)，由 AI 生成并存储在 Supabase Storage';
COMMENT ON COLUMN public.cities.landscape_image_urls IS '横屏图片 URL 列表 (1280x720)，JSON 数组格式，由 AI 生成并存储在 Supabase Storage';

-- 创建索引以加速图片查询（可选）
CREATE INDEX IF NOT EXISTS idx_cities_portrait_image ON public.cities(portrait_image_url) WHERE portrait_image_url IS NOT NULL;

-- 回滚脚本 (如需回滚，请执行以下语句):
-- ALTER TABLE public.cities DROP COLUMN IF EXISTS portrait_image_url;
-- ALTER TABLE public.cities DROP COLUMN IF EXISTS landscape_image_urls;
-- DROP INDEX IF EXISTS idx_cities_portrait_image;
