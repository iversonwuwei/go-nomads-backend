-- 扩展 innovations 表，添加更多创业项目详情字段
-- Migration: 20241225_extend_innovations_table.sql

-- 添加新字段到 innovations 表
ALTER TABLE public.innovations
ADD COLUMN IF NOT EXISTS elevator_pitch TEXT,
ADD COLUMN IF NOT EXISTS problem TEXT,
ADD COLUMN IF NOT EXISTS solution TEXT,
ADD COLUMN IF NOT EXISTS target_audience TEXT,
ADD COLUMN IF NOT EXISTS product_type VARCHAR(100),
ADD COLUMN IF NOT EXISTS key_features TEXT,
ADD COLUMN IF NOT EXISTS competitive_advantage TEXT,
ADD COLUMN IF NOT EXISTS business_model TEXT,
ADD COLUMN IF NOT EXISTS market_opportunity TEXT,
ADD COLUMN IF NOT EXISTS ask TEXT;

-- 添加字段注释
COMMENT ON COLUMN public.innovations.elevator_pitch IS '一句话项目介绍（电梯演讲）';
COMMENT ON COLUMN public.innovations.problem IS '要解决的问题';
COMMENT ON COLUMN public.innovations.solution IS '解决方案';
COMMENT ON COLUMN public.innovations.target_audience IS '目标用户群体';
COMMENT ON COLUMN public.innovations.product_type IS '产品类型（App、SaaS、小程序等）';
COMMENT ON COLUMN public.innovations.key_features IS '核心功能特点';
COMMENT ON COLUMN public.innovations.competitive_advantage IS '竞争优势';
COMMENT ON COLUMN public.innovations.business_model IS '商业模式';
COMMENT ON COLUMN public.innovations.market_opportunity IS '市场机会';
COMMENT ON COLUMN public.innovations.ask IS '寻求的资源/合作';

-- 创建团队成员表
CREATE TABLE IF NOT EXISTS public.innovation_team_members (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    innovation_id UUID NOT NULL REFERENCES public.innovations(id) ON DELETE CASCADE,
    user_id UUID REFERENCES public.users(id) ON DELETE SET NULL,
    name VARCHAR(100) NOT NULL,
    role VARCHAR(100) NOT NULL,
    description TEXT,
    avatar_url TEXT,
    is_founder BOOLEAN DEFAULT false,
    joined_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 添加团队成员表索引
CREATE INDEX IF NOT EXISTS idx_innovation_team_members_innovation_id ON public.innovation_team_members(innovation_id);
CREATE INDEX IF NOT EXISTS idx_innovation_team_members_user_id ON public.innovation_team_members(user_id);

-- 添加团队成员表注释
COMMENT ON TABLE public.innovation_team_members IS '创新项目团队成员表';
COMMENT ON COLUMN public.innovation_team_members.innovation_id IS '关联的项目ID';
COMMENT ON COLUMN public.innovation_team_members.user_id IS '关联的用户ID（可选，可为非注册用户）';
COMMENT ON COLUMN public.innovation_team_members.name IS '成员姓名';
COMMENT ON COLUMN public.innovation_team_members.role IS '成员角色（CEO、CTO等）';
COMMENT ON COLUMN public.innovation_team_members.description IS '成员简介';
COMMENT ON COLUMN public.innovation_team_members.is_founder IS '是否为创始人';

-- 创建点赞表
CREATE TABLE IF NOT EXISTS public.innovation_likes (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    innovation_id UUID NOT NULL REFERENCES public.innovations(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(innovation_id, user_id)
);

-- 添加点赞表索引
CREATE INDEX IF NOT EXISTS idx_innovation_likes_innovation_id ON public.innovation_likes(innovation_id);
CREATE INDEX IF NOT EXISTS idx_innovation_likes_user_id ON public.innovation_likes(user_id);

-- 添加点赞表注释
COMMENT ON TABLE public.innovation_likes IS '创新项目点赞表';
COMMENT ON COLUMN public.innovation_likes.innovation_id IS '关联的项目ID';
COMMENT ON COLUMN public.innovation_likes.user_id IS '点赞用户ID';

-- 创建评论表
CREATE TABLE IF NOT EXISTS public.innovation_comments (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    innovation_id UUID NOT NULL REFERENCES public.innovations(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    content TEXT NOT NULL,
    parent_id UUID REFERENCES public.innovation_comments(id) ON DELETE CASCADE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 添加评论表索引
CREATE INDEX IF NOT EXISTS idx_innovation_comments_innovation_id ON public.innovation_comments(innovation_id);
CREATE INDEX IF NOT EXISTS idx_innovation_comments_user_id ON public.innovation_comments(user_id);
CREATE INDEX IF NOT EXISTS idx_innovation_comments_parent_id ON public.innovation_comments(parent_id);

-- 添加评论表注释
COMMENT ON TABLE public.innovation_comments IS '创新项目评论表';
COMMENT ON COLUMN public.innovation_comments.innovation_id IS '关联的项目ID';
COMMENT ON COLUMN public.innovation_comments.user_id IS '评论用户ID';
COMMENT ON COLUMN public.innovation_comments.content IS '评论内容';
COMMENT ON COLUMN public.innovation_comments.parent_id IS '父评论ID（用于回复）';

-- 启用 RLS
ALTER TABLE public.innovation_team_members ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.innovation_likes ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.innovation_comments ENABLE ROW LEVEL SECURITY;

-- 团队成员表 RLS 策略
CREATE POLICY "Anyone can view team members" ON public.innovation_team_members
    FOR SELECT USING (true);

CREATE POLICY "Creators can manage team members" ON public.innovation_team_members
    FOR ALL USING (
        EXISTS (
            SELECT 1 FROM public.innovations i
            WHERE i.id = innovation_id AND i.creator_id = auth.uid()
        )
    );

-- 点赞表 RLS 策略
CREATE POLICY "Anyone can view likes" ON public.innovation_likes
    FOR SELECT USING (true);

CREATE POLICY "Users can manage their own likes" ON public.innovation_likes
    FOR ALL USING (auth.uid() = user_id);

-- 评论表 RLS 策略
CREATE POLICY "Anyone can view comments" ON public.innovation_comments
    FOR SELECT USING (true);

CREATE POLICY "Authenticated users can create comments" ON public.innovation_comments
    FOR INSERT WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can update their own comments" ON public.innovation_comments
    FOR UPDATE USING (auth.uid() = user_id);

CREATE POLICY "Users can delete their own comments" ON public.innovation_comments
    FOR DELETE USING (auth.uid() = user_id);

-- 创建更新 updated_at 的触发器函数（如果不存在）
CREATE OR REPLACE FUNCTION public.update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- 为新表添加触发器
DROP TRIGGER IF EXISTS update_innovation_team_members_updated_at ON public.innovation_team_members;
CREATE TRIGGER update_innovation_team_members_updated_at
    BEFORE UPDATE ON public.innovation_team_members
    FOR EACH ROW EXECUTE FUNCTION public.update_updated_at_column();

DROP TRIGGER IF EXISTS update_innovation_comments_updated_at ON public.innovation_comments;
CREATE TRIGGER update_innovation_comments_updated_at
    BEFORE UPDATE ON public.innovation_comments
    FOR EACH ROW EXECUTE FUNCTION public.update_updated_at_column();
