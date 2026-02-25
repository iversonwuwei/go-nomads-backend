-- ============================================================
-- 微信支付字段扩展迁移
-- 为 orders 和 payment_transactions 表添加微信支付相关字段
-- ============================================================

-- 1. orders 表：添加 payment_method 和微信支付字段
ALTER TABLE public.orders
    ADD COLUMN IF NOT EXISTS payment_method VARCHAR(20) NOT NULL DEFAULT 'paypal',
    ADD COLUMN IF NOT EXISTS wechat_prepay_id VARCHAR(100) NULL,
    ADD COLUMN IF NOT EXISTS wechat_transaction_id VARCHAR(100) NULL;

-- 2. 添加索引
CREATE INDEX IF NOT EXISTS idx_orders_payment_method ON public.orders(payment_method);
CREATE INDEX IF NOT EXISTS idx_orders_wechat_transaction_id ON public.orders(wechat_transaction_id);

-- 3. payment_transactions 表：添加微信交易 ID
ALTER TABLE public.payment_transactions
    ADD COLUMN IF NOT EXISTS wechat_transaction_id VARCHAR(100) NULL;

CREATE INDEX IF NOT EXISTS idx_transactions_wechat_transaction_id ON public.payment_transactions(wechat_transaction_id);

-- 4. 注释
COMMENT ON COLUMN public.orders.payment_method IS '支付方式: paypal, wechat';
COMMENT ON COLUMN public.orders.wechat_prepay_id IS '微信支付预支付 ID';
COMMENT ON COLUMN public.orders.wechat_transaction_id IS '微信支付交易号';
COMMENT ON COLUMN public.payment_transactions.wechat_transaction_id IS '微信支付交易号';
