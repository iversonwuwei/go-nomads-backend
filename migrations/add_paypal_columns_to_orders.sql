-- 增量迁移脚本：为现有 orders 表添加缺失的列
-- 如果 orders 表已存在但缺少某些列，使用此脚本

-- 检查并添加 order_number 列
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'orders' 
        AND column_name = 'order_number'
    ) THEN
        ALTER TABLE public.orders ADD COLUMN order_number VARCHAR(50) NULL;
        -- 为现有记录生成订单号
        UPDATE public.orders SET order_number = 'ORD' || to_char(created_at, 'YYYYMMDDHH24MISS') || floor(random() * 9000 + 1000)::text WHERE order_number IS NULL;
        ALTER TABLE public.orders ALTER COLUMN order_number SET NOT NULL;
        CREATE UNIQUE INDEX IF NOT EXISTS idx_orders_order_number ON public.orders(order_number);
        RAISE NOTICE 'Added column order_number';
    END IF;
END $$;

-- 检查并添加 order_type 列
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'orders' 
        AND column_name = 'order_type'
    ) THEN
        ALTER TABLE public.orders ADD COLUMN order_type VARCHAR(50) NOT NULL DEFAULT 'membership_upgrade';
        RAISE NOTICE 'Added column order_type';
    END IF;
END $$;

-- 检查并添加 status 列
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'orders' 
        AND column_name = 'status'
    ) THEN
        ALTER TABLE public.orders ADD COLUMN status VARCHAR(20) NOT NULL DEFAULT 'pending';
        RAISE NOTICE 'Added column status';
    END IF;
END $$;

-- 检查并添加 amount 列
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'orders' 
        AND column_name = 'amount'
    ) THEN
        ALTER TABLE public.orders ADD COLUMN amount DECIMAL(10, 2) NOT NULL DEFAULT 0;
        RAISE NOTICE 'Added column amount';
    END IF;
END $$;

-- 检查并添加 currency 列
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'orders' 
        AND column_name = 'currency'
    ) THEN
        ALTER TABLE public.orders ADD COLUMN currency VARCHAR(3) NOT NULL DEFAULT 'USD';
        RAISE NOTICE 'Added column currency';
    END IF;
END $$;

-- 检查并添加 membership_level 列
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'orders' 
        AND column_name = 'membership_level'
    ) THEN
        ALTER TABLE public.orders ADD COLUMN membership_level INT NULL;
        RAISE NOTICE 'Added column membership_level';
    END IF;
END $$;

-- 检查并添加 duration_days 列
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'orders' 
        AND column_name = 'duration_days'
    ) THEN
        ALTER TABLE public.orders ADD COLUMN duration_days INT NULL;
        RAISE NOTICE 'Added column duration_days';
    END IF;
END $$;

-- 检查并添加 paypal_order_id 列
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'orders' 
        AND column_name = 'paypal_order_id'
    ) THEN
        ALTER TABLE public.orders ADD COLUMN paypal_order_id VARCHAR(100) NULL;
        RAISE NOTICE 'Added column paypal_order_id';
    END IF;
END $$;

-- 检查并添加 paypal_capture_id 列
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'orders' 
        AND column_name = 'paypal_capture_id'
    ) THEN
        ALTER TABLE public.orders ADD COLUMN paypal_capture_id VARCHAR(100) NULL;
        RAISE NOTICE 'Added column paypal_capture_id';
    END IF;
END $$;

-- 检查并添加 paypal_payer_id 列
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'orders' 
        AND column_name = 'paypal_payer_id'
    ) THEN
        ALTER TABLE public.orders ADD COLUMN paypal_payer_id VARCHAR(100) NULL;
        RAISE NOTICE 'Added column paypal_payer_id';
    END IF;
END $$;

-- 检查并添加 paypal_payer_email 列
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'orders' 
        AND column_name = 'paypal_payer_email'
    ) THEN
        ALTER TABLE public.orders ADD COLUMN paypal_payer_email VARCHAR(255) NULL;
        RAISE NOTICE 'Added column paypal_payer_email';
    END IF;
END $$;

-- 检查并添加 error_message 列
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'orders' 
        AND column_name = 'error_message'
    ) THEN
        ALTER TABLE public.orders ADD COLUMN error_message TEXT NULL;
        RAISE NOTICE 'Added column error_message';
    END IF;
END $$;

-- 检查并添加 metadata 列
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'orders' 
        AND column_name = 'metadata'
    ) THEN
        ALTER TABLE public.orders ADD COLUMN metadata JSONB NULL;
        RAISE NOTICE 'Added column metadata';
    END IF;
END $$;

-- 检查并添加 expired_at 列
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'orders' 
        AND column_name = 'expired_at'
    ) THEN
        ALTER TABLE public.orders ADD COLUMN expired_at TIMESTAMPTZ NULL;
        RAISE NOTICE 'Added column expired_at';
    END IF;
END $$;

-- 检查并添加 completed_at 列
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'orders' 
        AND column_name = 'completed_at'
    ) THEN
        ALTER TABLE public.orders ADD COLUMN completed_at TIMESTAMPTZ NULL;
        RAISE NOTICE 'Added column completed_at';
    END IF;
END $$;

-- 检查并添加 created_at 列
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'orders' 
        AND column_name = 'created_at'
    ) THEN
        ALTER TABLE public.orders ADD COLUMN created_at TIMESTAMPTZ NOT NULL DEFAULT NOW();
        RAISE NOTICE 'Added column created_at';
    END IF;
END $$;

-- 检查并添加 updated_at 列
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'orders' 
        AND column_name = 'updated_at'
    ) THEN
        ALTER TABLE public.orders ADD COLUMN updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW();
        RAISE NOTICE 'Added column updated_at';
    END IF;
END $$;

-- 创建索引 (如果不存在)
CREATE INDEX IF NOT EXISTS idx_orders_user_id ON public.orders(user_id);
CREATE INDEX IF NOT EXISTS idx_orders_status ON public.orders(status);
CREATE INDEX IF NOT EXISTS idx_orders_paypal_order_id ON public.orders(paypal_order_id);
CREATE INDEX IF NOT EXISTS idx_orders_created_at ON public.orders(created_at DESC);

-- 完成
SELECT 'Migration completed. Added all missing columns to orders table.' as result;
