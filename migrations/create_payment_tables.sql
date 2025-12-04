-- 创建支付相关表
-- 用于存储订单和支付交易记录

-- ============================================================
-- 1. 订单表 (orders)
-- ============================================================
CREATE TABLE IF NOT EXISTS public.orders (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_number VARCHAR(50) NOT NULL UNIQUE,
    user_id UUID NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    
    -- 订单类型: membership_upgrade, membership_renew, moderator_deposit
    order_type VARCHAR(50) NOT NULL,
    
    -- 订单状态: pending, processing, completed, failed, refunded, cancelled
    status VARCHAR(20) NOT NULL DEFAULT 'pending',
    
    -- 金额信息
    amount DECIMAL(10, 2) NOT NULL,
    currency VARCHAR(3) NOT NULL DEFAULT 'USD',
    
    -- 会员相关信息
    membership_level INT NULL,
    duration_days INT NULL,
    
    -- PayPal 订单信息
    paypal_order_id VARCHAR(100) NULL,
    paypal_capture_id VARCHAR(100) NULL,
    paypal_payer_id VARCHAR(100) NULL,
    paypal_payer_email VARCHAR(255) NULL,
    
    -- 错误信息
    error_message TEXT NULL,
    
    -- 元数据 (JSON)
    metadata JSONB NULL,
    
    -- 时间戳
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    completed_at TIMESTAMPTZ NULL,
    expired_at TIMESTAMPTZ NULL
);

-- 索引
CREATE INDEX IF NOT EXISTS idx_orders_user_id ON public.orders(user_id);
CREATE INDEX IF NOT EXISTS idx_orders_status ON public.orders(status);
CREATE INDEX IF NOT EXISTS idx_orders_order_number ON public.orders(order_number);
CREATE INDEX IF NOT EXISTS idx_orders_paypal_order_id ON public.orders(paypal_order_id);
CREATE INDEX IF NOT EXISTS idx_orders_created_at ON public.orders(created_at DESC);

-- ============================================================
-- 2. 支付交易记录表 (payment_transactions)
-- ============================================================
CREATE TABLE IF NOT EXISTS public.payment_transactions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id UUID NOT NULL REFERENCES public.orders(id) ON DELETE CASCADE,
    
    -- 交易类型: payment, refund, chargeback
    transaction_type VARCHAR(20) NOT NULL DEFAULT 'payment',
    
    -- 交易状态: pending, completed, failed
    status VARCHAR(20) NOT NULL DEFAULT 'pending',
    
    -- 金额信息
    amount DECIMAL(10, 2) NOT NULL,
    currency VARCHAR(3) NOT NULL DEFAULT 'USD',
    
    -- PayPal 交易信息
    paypal_transaction_id VARCHAR(100) NULL,
    paypal_capture_id VARCHAR(100) NULL,
    
    -- 支付方式
    payment_method VARCHAR(50) NOT NULL DEFAULT 'paypal',
    
    -- 原始响应 (JSON)
    raw_response JSONB NULL,
    
    -- 错误信息
    error_code VARCHAR(50) NULL,
    error_message TEXT NULL,
    
    -- 时间戳
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- 索引
CREATE INDEX IF NOT EXISTS idx_payment_transactions_order_id ON public.payment_transactions(order_id);
CREATE INDEX IF NOT EXISTS idx_payment_transactions_status ON public.payment_transactions(status);
CREATE INDEX IF NOT EXISTS idx_payment_transactions_paypal_transaction_id ON public.payment_transactions(paypal_transaction_id);

-- ============================================================
-- 3. 更新触发器
-- ============================================================
DROP TRIGGER IF EXISTS update_orders_updated_at ON public.orders;
CREATE TRIGGER update_orders_updated_at
    BEFORE UPDATE ON public.orders
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

DROP TRIGGER IF EXISTS update_payment_transactions_updated_at ON public.payment_transactions;
CREATE TRIGGER update_payment_transactions_updated_at
    BEFORE UPDATE ON public.payment_transactions
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- ============================================================
-- 4. RLS 策略
-- ============================================================
ALTER TABLE public.orders ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.payment_transactions ENABLE ROW LEVEL SECURITY;

-- 用户只能查看自己的订单
CREATE POLICY "Users can view own orders"
    ON public.orders FOR SELECT
    USING (auth.uid()::text = user_id::text);

-- 服务角色可以访问所有记录
CREATE POLICY "Service role has full access to orders"
    ON public.orders
    USING (auth.role() = 'service_role');

CREATE POLICY "Service role has full access to transactions"
    ON public.payment_transactions
    USING (auth.role() = 'service_role');

-- ============================================================
-- 5. 注释
-- ============================================================
COMMENT ON TABLE public.orders IS '订单表 - 存储用户的支付订单';
COMMENT ON TABLE public.payment_transactions IS '支付交易记录表 - 存储每笔支付交易的详细信息';

COMMENT ON COLUMN public.orders.order_type IS '订单类型: membership_upgrade(会员升级), membership_renew(会员续费), moderator_deposit(版主保证金)';
COMMENT ON COLUMN public.orders.status IS '订单状态: pending(待支付), processing(处理中), completed(已完成), failed(失败), refunded(已退款), cancelled(已取消)';
COMMENT ON COLUMN public.orders.paypal_order_id IS 'PayPal 订单ID';
COMMENT ON COLUMN public.orders.paypal_capture_id IS 'PayPal 支付确认ID';
