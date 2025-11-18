-- =============================================================
-- Disable RLS for coworking_verifications table
-- This allows the service to insert records without user context
-- =============================================================

ALTER TABLE public.coworking_verifications DISABLE ROW LEVEL SECURITY;

-- Drop existing RLS policies
DROP POLICY IF EXISTS "Users manage own coworking verifications" ON public.coworking_verifications;

COMMENT ON TABLE public.coworking_verifications IS 'Coworking verification votes - RLS disabled for service-level operations';
