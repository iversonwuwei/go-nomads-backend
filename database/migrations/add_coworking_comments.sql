-- =============================================================
-- Add coworking comments table
-- Supports text comments and image attachments
-- =============================================================

CREATE TABLE IF NOT EXISTS public.coworking_comments (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    coworking_id UUID NOT NULL REFERENCES public.coworking_spaces(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    content TEXT NOT NULL,
    rating INTEGER NOT NULL DEFAULT 0 CHECK (rating >= 0 AND rating <= 5),
    images TEXT[],
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE,
    is_active BOOLEAN DEFAULT TRUE
);

CREATE INDEX IF NOT EXISTS idx_coworking_comments_coworking_id
    ON public.coworking_comments(coworking_id);

CREATE INDEX IF NOT EXISTS idx_coworking_comments_user_id
    ON public.coworking_comments(user_id);

CREATE INDEX IF NOT EXISTS idx_coworking_comments_created_at
    ON public.coworking_comments(created_at DESC);

-- No RLS required for comments
ALTER TABLE public.coworking_comments DISABLE ROW LEVEL SECURITY;

COMMENT ON TABLE public.coworking_comments IS 'Coworking space comments with text and image support';
