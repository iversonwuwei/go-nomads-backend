-- 创建 AI 生成的旅行计划表
-- 用于持久化存储 AI 生成的旅行计划数据

CREATE TABLE IF NOT EXISTS ai_travel_plans (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    city_id TEXT NOT NULL,
    city_name TEXT NOT NULL,
    city_image TEXT,
    duration INTEGER NOT NULL DEFAULT 7,
    budget_level TEXT NOT NULL DEFAULT 'medium' CHECK (budget_level IN ('low', 'medium', 'high')),
    travel_style TEXT NOT NULL DEFAULT 'culture' CHECK (travel_style IN ('adventure', 'relaxation', 'culture', 'nightlife')),
    interests TEXT[],
    departure_location TEXT,
    departure_date TIMESTAMPTZ,
    plan_data JSONB NOT NULL DEFAULT '{}'::jsonb,
    status TEXT NOT NULL DEFAULT 'draft' CHECK (status IN ('draft', 'published', 'archived')),
    is_public BOOLEAN NOT NULL DEFAULT false,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_ai_travel_plans_user_id ON ai_travel_plans(user_id);
CREATE INDEX IF NOT EXISTS idx_ai_travel_plans_city_id ON ai_travel_plans(city_id);
CREATE INDEX IF NOT EXISTS idx_ai_travel_plans_created_at ON ai_travel_plans(created_at DESC);
CREATE INDEX IF NOT EXISTS idx_ai_travel_plans_public ON ai_travel_plans(is_public) WHERE is_public = true;

-- 创建 updated_at 自动更新触发器
CREATE OR REPLACE FUNCTION update_ai_travel_plans_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trigger_ai_travel_plans_updated_at ON ai_travel_plans;
CREATE TRIGGER trigger_ai_travel_plans_updated_at
    BEFORE UPDATE ON ai_travel_plans
    FOR EACH ROW
    EXECUTE FUNCTION update_ai_travel_plans_updated_at();

-- 添加注释
COMMENT ON TABLE ai_travel_plans IS 'AI 生成的旅行计划存储表';
COMMENT ON COLUMN ai_travel_plans.id IS '计划 ID';
COMMENT ON COLUMN ai_travel_plans.user_id IS '用户 ID - 关联请求生成计划的用户';
COMMENT ON COLUMN ai_travel_plans.city_id IS '目标城市 ID';
COMMENT ON COLUMN ai_travel_plans.city_name IS '目标城市名称';
COMMENT ON COLUMN ai_travel_plans.city_image IS '城市图片 URL';
COMMENT ON COLUMN ai_travel_plans.duration IS '旅行天数';
COMMENT ON COLUMN ai_travel_plans.budget_level IS '预算等级: low, medium, high';
COMMENT ON COLUMN ai_travel_plans.travel_style IS '旅行风格: adventure, relaxation, culture, nightlife';
COMMENT ON COLUMN ai_travel_plans.interests IS '兴趣标签数组';
COMMENT ON COLUMN ai_travel_plans.departure_location IS '出发地';
COMMENT ON COLUMN ai_travel_plans.departure_date IS '出发日期';
COMMENT ON COLUMN ai_travel_plans.plan_data IS '完整的旅行计划数据 (JSON 格式)';
COMMENT ON COLUMN ai_travel_plans.status IS '计划状态: draft, published, archived';
COMMENT ON COLUMN ai_travel_plans.is_public IS '是否公开可见';
COMMENT ON COLUMN ai_travel_plans.created_at IS '创建时间';
COMMENT ON COLUMN ai_travel_plans.updated_at IS '更新时间';
