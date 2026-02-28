-- ============================================
-- 会员表新增 billing_cycle 列
-- 创建日期: 2025-07-10
-- 描述: 为 memberships 表添加 billing_cycle 字段，区分月付/年付
--       0 = Monthly（月付）, 1 = Yearly（年付，默认）
-- ============================================

-- 1. 添加 billing_cycle 列
ALTER TABLE public.memberships
    ADD COLUMN IF NOT EXISTS billing_cycle INTEGER NOT NULL DEFAULT 1;

-- 2. 注释
COMMENT ON COLUMN public.memberships.billing_cycle IS '计费周期: 0=Monthly（月付）, 1=Yearly（年付）';

-- 3. 索引（方便按计费周期筛选）
CREATE INDEX IF NOT EXISTS idx_memberships_billing_cycle ON public.memberships(billing_cycle);

-- 4. 验证
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'memberships'
          AND column_name = 'billing_cycle'
    ) THEN
        RAISE NOTICE '✅ billing_cycle 列添加成功';
    ELSE
        RAISE EXCEPTION '❌ billing_cycle 列添加失败';
    END IF;
END $$;
