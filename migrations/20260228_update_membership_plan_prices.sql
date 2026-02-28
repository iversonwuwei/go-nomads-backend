-- ============================================
-- 更新会员计划价格（2026-02-28）
-- 调整为对标主流 AI 服务的 CNY 定价
-- 支持月付和年付两种计费周期
-- ============================================

-- Basic: ¥38/月, ¥298/年
-- Pro:   ¥68/月, ¥598/年
-- Premium: ¥128/月, ¥998/年

UPDATE public.membership_plans
SET
    price_yearly = 298,
    price_monthly = 38,
    currency = 'CNY',
    updated_at = CURRENT_TIMESTAMP
WHERE level = 1;

UPDATE public.membership_plans
SET
    price_yearly = 598,
    price_monthly = 68,
    currency = 'CNY',
    updated_at = CURRENT_TIMESTAMP
WHERE level = 2;

UPDATE public.membership_plans
SET
    price_yearly = 998,
    price_monthly = 128,
    currency = 'CNY',
    updated_at = CURRENT_TIMESTAMP
WHERE level = 3;

-- Free 计划保持不变
UPDATE public.membership_plans
SET
    currency = 'CNY',
    updated_at = CURRENT_TIMESTAMP
WHERE level = 0;

-- 验证更新结果
DO $$
DECLARE
    rec RECORD;
BEGIN
    FOR rec IN SELECT level, name, price_monthly, price_yearly, currency FROM public.membership_plans ORDER BY level
    LOOP
        RAISE NOTICE '✅ Level %: % - ¥%/月, ¥%/年 (%)', rec.level, rec.name, rec.price_monthly, rec.price_yearly, rec.currency;
    END LOOP;
END $$;
