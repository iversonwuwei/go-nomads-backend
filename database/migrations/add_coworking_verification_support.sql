-- =============================================================
-- Coworking verification support
-- 1. Adds verification_status column to coworking_spaces
-- 2. Creates coworking_verifications table for community votes
-- 3. Configures RLS policies so users can manage their own votes
-- =============================================================

ALTER TABLE public.coworking_spaces
    ADD COLUMN IF NOT EXISTS verification_status TEXT NOT NULL DEFAULT 'unverified';

DO
$$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.constraint_column_usage
        WHERE table_name = 'coworking_spaces'
          AND constraint_name = 'coworking_spaces_verification_status_check'
    ) THEN
        ALTER TABLE public.coworking_spaces
            ADD CONSTRAINT coworking_spaces_verification_status_check
            CHECK (verification_status IN ('verified', 'unverified'));
    END IF;
END;
$$;

CREATE TABLE IF NOT EXISTS public.coworking_verifications (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    coworking_id UUID NOT NULL REFERENCES public.coworking_spaces(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_coworking_verifications_unique
    ON public.coworking_verifications(coworking_id, user_id);

CREATE INDEX IF NOT EXISTS idx_coworking_verifications_user_id
    ON public.coworking_verifications(user_id);

ALTER TABLE public.coworking_verifications ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "Users manage own coworking verifications" ON public.coworking_verifications;
CREATE POLICY "Users manage own coworking verifications"
    ON public.coworking_verifications
    FOR ALL
    USING (auth.role() = 'service_role' OR auth.uid()::text = user_id::text)
    WITH CHECK (auth.role() = 'service_role' OR auth.uid()::text = user_id::text);
