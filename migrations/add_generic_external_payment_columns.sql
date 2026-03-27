-- 为订单和支付交易表添加通用第三方支付字段，并从旧 PayPal 字段回填。

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public'
        AND table_name = 'orders'
        AND column_name = 'external_payment_order_id'
    ) THEN
        ALTER TABLE public.orders ADD COLUMN external_payment_order_id VARCHAR(100) NULL;
        UPDATE public.orders
        SET external_payment_order_id = paypal_order_id
        WHERE external_payment_order_id IS NULL AND paypal_order_id IS NOT NULL;
        RAISE NOTICE 'Added column external_payment_order_id';
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public'
        AND table_name = 'orders'
        AND column_name = 'external_payment_id'
    ) THEN
        ALTER TABLE public.orders ADD COLUMN external_payment_id VARCHAR(100) NULL;
        UPDATE public.orders
        SET external_payment_id = paypal_capture_id
        WHERE external_payment_id IS NULL AND paypal_capture_id IS NOT NULL;
        RAISE NOTICE 'Added column external_payment_id';
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public'
        AND table_name = 'orders'
        AND column_name = 'external_payer_id'
    ) THEN
        ALTER TABLE public.orders ADD COLUMN external_payer_id VARCHAR(100) NULL;
        UPDATE public.orders
        SET external_payer_id = paypal_payer_id
        WHERE external_payer_id IS NULL AND paypal_payer_id IS NOT NULL;
        RAISE NOTICE 'Added column external_payer_id';
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public'
        AND table_name = 'orders'
        AND column_name = 'external_payer_email'
    ) THEN
        ALTER TABLE public.orders ADD COLUMN external_payer_email VARCHAR(255) NULL;
        UPDATE public.orders
        SET external_payer_email = paypal_payer_email
        WHERE external_payer_email IS NULL AND paypal_payer_email IS NOT NULL;
        RAISE NOTICE 'Added column external_payer_email';
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public'
        AND table_name = 'payment_transactions'
        AND column_name = 'external_transaction_id'
    ) THEN
        ALTER TABLE public.payment_transactions ADD COLUMN external_transaction_id VARCHAR(100) NULL;
        UPDATE public.payment_transactions
        SET external_transaction_id = paypal_transaction_id
        WHERE external_transaction_id IS NULL AND paypal_transaction_id IS NOT NULL;
        RAISE NOTICE 'Added column external_transaction_id';
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public'
        AND table_name = 'payment_transactions'
        AND column_name = 'external_reference_id'
    ) THEN
        ALTER TABLE public.payment_transactions ADD COLUMN external_reference_id VARCHAR(100) NULL;
        UPDATE public.payment_transactions
        SET external_reference_id = paypal_capture_id
        WHERE external_reference_id IS NULL AND paypal_capture_id IS NOT NULL;
        RAISE NOTICE 'Added column external_reference_id';
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS idx_orders_external_payment_order_id ON public.orders(external_payment_order_id);
CREATE INDEX IF NOT EXISTS idx_payment_transactions_external_transaction_id ON public.payment_transactions(external_transaction_id);

COMMENT ON COLUMN public.orders.external_payment_order_id IS '第三方支付订单 ID';
COMMENT ON COLUMN public.orders.external_payment_id IS '第三方支付确认或交易 ID';
COMMENT ON COLUMN public.orders.external_payer_id IS '第三方付款方 ID';
COMMENT ON COLUMN public.orders.external_payer_email IS '第三方付款方邮箱';
COMMENT ON COLUMN public.payment_transactions.external_transaction_id IS '第三方交易 ID';
COMMENT ON COLUMN public.payment_transactions.external_reference_id IS '第三方捕获/参考 ID';

SELECT 'Migration completed. Added generic external payment columns.' AS result;