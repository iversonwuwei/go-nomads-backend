-- ============================================
-- 会员系统迁移脚本
-- 创建日期: 2025-12-03
-- 描述: 创建会员计划表、用户会员表，支持会员等级、自动续费、AI使用量统计等功能
-- ============================================

-- ============================================
-- 1. 会员计划配置表 (membership_plans)
-- ============================================

CREATE TABLE IF NOT EXISTS public.membership_plans (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    level INTEGER NOT NULL UNIQUE,  -- 0=Free, 1=Basic, 2=Pro, 3=Premium
    name VARCHAR(50) NOT NULL,
    description TEXT,
    price_yearly DECIMAL(10,2) NOT NULL DEFAULT 0,
    price_monthly DECIMAL(10,2) NOT NULL DEFAULT 0,
    currency VARCHAR(10) DEFAULT 'USD',
    icon VARCHAR(10),
    color VARCHAR(10),  -- 十六进制颜色
    features JSONB DEFAULT '[]',
    ai_usage_limit INTEGER DEFAULT 0,  -- -1 表示无限制
    can_use_ai BOOLEAN DEFAULT false,
    can_apply_moderator BOOLEAN DEFAULT false,
    moderator_deposit DECIMAL(10,2) DEFAULT 0,
    is_active BOOLEAN DEFAULT true,
    sort_order INTEGER DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

COMMENT ON TABLE public.membership_plans IS '会员计划配置表';
COMMENT ON COLUMN public.membership_plans.level IS '会员等级: 0=Free, 1=Basic, 2=Pro, 3=Premium';
COMMENT ON COLUMN public.membership_plans.ai_usage_limit IS 'AI使用次数限制，-1表示无限制';
COMMENT ON COLUMN public.membership_plans.features IS '功能列表 JSON 数组';

-- 索引
CREATE INDEX IF NOT EXISTS idx_membership_plans_level ON public.membership_plans(level);
CREATE INDEX IF NOT EXISTS idx_membership_plans_is_active ON public.membership_plans(is_active);

-- ============================================
-- 2. 用户会员信息表 (memberships)
-- ============================================

-- 会员表
CREATE TABLE IF NOT EXISTS public.memberships (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id UUID NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    level INTEGER NOT NULL DEFAULT 0,  -- 0=Free, 1=Basic, 2=Pro, 3=Premium
    start_date TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    expiry_date TIMESTAMP WITH TIME ZONE,
    auto_renew BOOLEAN DEFAULT false,
    ai_usage_this_month INTEGER DEFAULT 0,
    ai_usage_reset_date TIMESTAMP WITH TIME ZONE,
    moderator_deposit DECIMAL(10,2),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(user_id)  -- 每个用户只能有一条会员记录
);

-- 会员等级说明注释
COMMENT ON TABLE public.memberships IS '用户会员信息表';
COMMENT ON COLUMN public.memberships.level IS '会员等级: 0=Free, 1=Basic, 2=Pro, 3=Premium';
COMMENT ON COLUMN public.memberships.auto_renew IS '是否开启自动续费';
COMMENT ON COLUMN public.memberships.ai_usage_this_month IS '本月AI使用次数';
COMMENT ON COLUMN public.memberships.ai_usage_reset_date IS 'AI使用次数重置日期（每月1号）';
COMMENT ON COLUMN public.memberships.moderator_deposit IS '版主保证金';

-- 索引
CREATE INDEX IF NOT EXISTS idx_memberships_user_id ON public.memberships(user_id);
CREATE INDEX IF NOT EXISTS idx_memberships_level ON public.memberships(level);
CREATE INDEX IF NOT EXISTS idx_memberships_expiry_date ON public.memberships(expiry_date);
CREATE INDEX IF NOT EXISTS idx_memberships_auto_renew ON public.memberships(auto_renew) WHERE auto_renew = true;

-- 启用 RLS
ALTER TABLE public.memberships ENABLE ROW LEVEL SECURITY;

-- RLS 策略（先删除再创建，避免重复执行报错）
DROP POLICY IF EXISTS "Users can view own membership" ON public.memberships;
DROP POLICY IF EXISTS "Service can manage all memberships" ON public.memberships;

-- 用户可以查看自己的会员信息
CREATE POLICY "Users can view own membership" ON public.memberships 
    FOR SELECT USING (auth.uid()::text = user_id::text);

-- 后端服务可以管理所有会员记录（通过 service_role key）
CREATE POLICY "Service can manage all memberships" ON public.memberships 
    FOR ALL USING (true);

-- 自动更新 updated_at 触发器
CREATE OR REPLACE TRIGGER update_memberships_updated_at
    BEFORE UPDATE ON public.memberships
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- ============================================
-- 验证脚本
-- ============================================
-- 检查表是否创建成功
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'memberships' AND table_schema = 'public') THEN
        RAISE NOTICE '✅ memberships 表创建成功';
    ELSE
        RAISE EXCEPTION '❌ memberships 表创建失败';
    END IF;
    
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'membership_plans' AND table_schema = 'public') THEN
        RAISE NOTICE '✅ membership_plans 表创建成功';
    ELSE
        RAISE EXCEPTION '❌ membership_plans 表创建失败';
    END IF;
END $$;

-- ============================================
-- 4. 初始化会员计划数据
-- ============================================

INSERT INTO public.membership_plans (level, name, description, price_yearly, price_monthly, icon, color, features, ai_usage_limit, can_use_ai, can_apply_moderator, moderator_deposit, sort_order)
VALUES 
    (0, 'Free', 'Basic access to the platform', 0, 0, '🆓', '#6B7280', 
     '["Browse cities and reviews", "View coworking spaces", "Basic city search", "Limited AI travel plans (3/month)"]'::jsonb, 
     3, false, false, 0, 0),
    
    (1, 'Basic', 'Essential features for digital nomads', 49, 4.08, '⭐', '#3B82F6', 
     '["Everything in Free", "AI travel plan generation (20/month)", "AI digital nomad guides", "Save favorite cities", "Create meetups", "Join city chats"]'::jsonb, 
     20, true, false, 0, 1),
    
    (2, 'Pro', 'Advanced features for serious travelers', 99, 8.25, '💎', '#8B5CF6', 
     '["Everything in Basic", "Unlimited AI travel plans (100/month)", "Priority AI generation", "Apply to become a moderator", "Advanced city analytics", "Export travel plans"]'::jsonb, 
     100, true, true, 50, 2),
    
    (3, 'Premium', 'Full access to all features', 149, 12.42, '👑', '#FF4458', 
     '["Everything in Pro", "Unlimited AI usage", "Early access to new features", "Priority support", "Custom travel recommendations", "API access", "No ads"]'::jsonb, 
     -1, true, true, 30, 3)
ON CONFLICT (level) DO UPDATE SET
    name = EXCLUDED.name,
    description = EXCLUDED.description,
    price_yearly = EXCLUDED.price_yearly,
    price_monthly = EXCLUDED.price_monthly,
    icon = EXCLUDED.icon,
    color = EXCLUDED.color,
    features = EXCLUDED.features,
    ai_usage_limit = EXCLUDED.ai_usage_limit,
    can_use_ai = EXCLUDED.can_use_ai,
    can_apply_moderator = EXCLUDED.can_apply_moderator,
    moderator_deposit = EXCLUDED.moderator_deposit,
    sort_order = EXCLUDED.sort_order,
    updated_at = CURRENT_TIMESTAMP;

-- 验证初始化数据
DO $$
DECLARE
    plan_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO plan_count FROM public.membership_plans;
    IF plan_count = 4 THEN
        RAISE NOTICE '✅ 会员计划数据初始化成功: % 条记录', plan_count;
    ELSE
        RAISE WARNING '⚠️ 会员计划数据不完整: % 条记录', plan_count;
    END IF;
END $$;
