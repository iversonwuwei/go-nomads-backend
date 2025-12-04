-- 创建 user_preferences 表
-- 用于存储用户的个性化偏好设置

CREATE TABLE IF NOT EXISTS public.user_preferences (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    notifications_enabled BOOLEAN NOT NULL DEFAULT true,
    travel_history_visible BOOLEAN NOT NULL DEFAULT true,
    profile_public BOOLEAN NOT NULL DEFAULT true,
    currency VARCHAR(10) NOT NULL DEFAULT 'USD',
    temperature_unit VARCHAR(20) NOT NULL DEFAULT 'Celsius',
    language VARCHAR(10) NOT NULL DEFAULT 'en',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- 确保每个用户只有一条偏好记录
    CONSTRAINT unique_user_preferences UNIQUE (user_id)
);

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_user_preferences_user_id ON public.user_preferences(user_id);

-- 添加注释
COMMENT ON TABLE public.user_preferences IS '用户偏好设置表';
COMMENT ON COLUMN public.user_preferences.notifications_enabled IS '是否启用通知';
COMMENT ON COLUMN public.user_preferences.travel_history_visible IS '旅行历史是否可见';
COMMENT ON COLUMN public.user_preferences.profile_public IS '个人资料是否公开';
COMMENT ON COLUMN public.user_preferences.currency IS '首选货币 (USD, EUR, GBP, JPY, CNY)';
COMMENT ON COLUMN public.user_preferences.temperature_unit IS '温度单位 (Celsius, Fahrenheit)';
COMMENT ON COLUMN public.user_preferences.language IS '首选语言 (en, zh)';

-- 创建更新 updated_at 的触发器函数（如果不存在）
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ language 'plpgsql';

-- 创建触发器
DROP TRIGGER IF EXISTS update_user_preferences_updated_at ON public.user_preferences;
CREATE TRIGGER update_user_preferences_updated_at
    BEFORE UPDATE ON public.user_preferences
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- 启用 RLS (Row Level Security)
ALTER TABLE public.user_preferences ENABLE ROW LEVEL SECURITY;

-- 创建 RLS 策略
-- 用户只能查看和修改自己的偏好设置
CREATE POLICY "Users can view own preferences"
    ON public.user_preferences FOR SELECT
    USING (auth.uid()::text = user_id::text);

CREATE POLICY "Users can insert own preferences"
    ON public.user_preferences FOR INSERT
    WITH CHECK (auth.uid()::text = user_id::text);

CREATE POLICY "Users can update own preferences"
    ON public.user_preferences FOR UPDATE
    USING (auth.uid()::text = user_id::text);

-- 服务角色可以访问所有记录
CREATE POLICY "Service role has full access"
    ON public.user_preferences
    USING (auth.role() = 'service_role');
