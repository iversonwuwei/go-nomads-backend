-- ============================================
-- 修复 orders 表 status 字段 CHECK 约束
-- 原约束缺少 'completed' 和 'failed' 值，
-- 导致微信支付/PayPal 支付完成后更新订单状态失败
-- ============================================

-- 1. 删除旧的 status CHECK 约束
ALTER TABLE public.orders DROP CONSTRAINT IF EXISTS orders_status_check;

-- 2. 添加新的 status CHECK 约束（增加 completed、failed）
ALTER TABLE public.orders ADD CONSTRAINT orders_status_check 
    CHECK (status IN ('pending', 'processing', 'completed', 'failed', 'shipped', 'delivered', 'cancelled', 'refunded'));

-- 验证
DO $$
BEGIN
    RAISE NOTICE '✅ orders 表 status 约束已更新，现在支持: pending, processing, completed, failed, shipped, delivered, cancelled, refunded';
END $$;
