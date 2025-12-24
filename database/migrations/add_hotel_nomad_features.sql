-- ============================================================
-- 酒店数字游民友好功能字段扩展
-- 用于支持添加酒店页面的新字段
-- ============================================================

-- 添加数字游民相关字段到hotels表
ALTER TABLE public.hotels 
ADD COLUMN IF NOT EXISTS wifi_speed INTEGER,                    -- WiFi速度 (Mbps)
ADD COLUMN IF NOT EXISTS has_wifi BOOLEAN DEFAULT false,        -- 是否有WiFi
ADD COLUMN IF NOT EXISTS has_work_desk BOOLEAN DEFAULT false,   -- 是否有工作台
ADD COLUMN IF NOT EXISTS has_coworking_space BOOLEAN DEFAULT false, -- 是否有共享办公空间
ADD COLUMN IF NOT EXISTS has_air_conditioning BOOLEAN DEFAULT false, -- 是否有空调
ADD COLUMN IF NOT EXISTS has_kitchen BOOLEAN DEFAULT false,     -- 是否有厨房
ADD COLUMN IF NOT EXISTS has_laundry BOOLEAN DEFAULT false,     -- 是否有洗衣设施
ADD COLUMN IF NOT EXISTS has_parking BOOLEAN DEFAULT false,     -- 是否有停车场
ADD COLUMN IF NOT EXISTS has_pool BOOLEAN DEFAULT false,        -- 是否有游泳池
ADD COLUMN IF NOT EXISTS has_gym BOOLEAN DEFAULT false,         -- 是否有健身房
ADD COLUMN IF NOT EXISTS has_24h_reception BOOLEAN DEFAULT false, -- 是否有24小时前台
ADD COLUMN IF NOT EXISTS has_long_stay_discount BOOLEAN DEFAULT false, -- 是否有长住折扣
ADD COLUMN IF NOT EXISTS is_pet_friendly BOOLEAN DEFAULT false, -- 是否宠物友好
ADD COLUMN IF NOT EXISTS long_stay_discount_percent DECIMAL(5,2), -- 长住折扣百分比
ADD COLUMN IF NOT EXISTS city_name VARCHAR(200),                -- 城市名称（冗余字段，便于查询）
ADD COLUMN IF NOT EXISTS country VARCHAR(200);                  -- 国家名称

-- 添加索引以优化查询
CREATE INDEX IF NOT EXISTS idx_hotels_has_wifi ON public.hotels(has_wifi) WHERE has_wifi = true;
CREATE INDEX IF NOT EXISTS idx_hotels_has_coworking ON public.hotels(has_coworking_space) WHERE has_coworking_space = true;
CREATE INDEX IF NOT EXISTS idx_hotels_wifi_speed ON public.hotels(wifi_speed DESC) WHERE wifi_speed IS NOT NULL;

-- 添加注释
COMMENT ON COLUMN public.hotels.wifi_speed IS 'WiFi速度（Mbps）';
COMMENT ON COLUMN public.hotels.has_wifi IS '是否提供WiFi';
COMMENT ON COLUMN public.hotels.has_work_desk IS '是否有工作台/书桌';
COMMENT ON COLUMN public.hotels.has_coworking_space IS '是否有共享办公空间';
COMMENT ON COLUMN public.hotels.has_air_conditioning IS '是否有空调';
COMMENT ON COLUMN public.hotels.has_kitchen IS '是否有厨房设施';
COMMENT ON COLUMN public.hotels.has_laundry IS '是否有洗衣设施';
COMMENT ON COLUMN public.hotels.has_parking IS '是否有停车场';
COMMENT ON COLUMN public.hotels.has_pool IS '是否有游泳池';
COMMENT ON COLUMN public.hotels.has_gym IS '是否有健身房';
COMMENT ON COLUMN public.hotels.has_24h_reception IS '是否有24小时前台服务';
COMMENT ON COLUMN public.hotels.has_long_stay_discount IS '是否提供长住折扣';
COMMENT ON COLUMN public.hotels.is_pet_friendly IS '是否允许携带宠物';
COMMENT ON COLUMN public.hotels.long_stay_discount_percent IS '长住折扣百分比';
COMMENT ON COLUMN public.hotels.city_name IS '城市名称（冗余字段）';
COMMENT ON COLUMN public.hotels.country IS '国家名称';

-- 删除RLS策略（不需要在服务端验证，由Gateway和应用层处理）
DROP POLICY IF EXISTS "Authenticated users can create hotels" ON public.hotels;
DROP POLICY IF EXISTS "Creators can update own hotels" ON public.hotels;
