-- Ensure each user can only verify a coworking space once
-- Step 1: remove any historical duplicates while preserving the earliest vote
WITH ranked_verifications AS (
    SELECT
        id,
        ROW_NUMBER() OVER (PARTITION BY coworking_id, user_id ORDER BY created_at ASC) AS rn
    FROM public.coworking_verifications
)
DELETE FROM public.coworking_verifications
WHERE id IN (
    SELECT id
    FROM ranked_verifications
    WHERE rn > 1
);

-- Step 2: enforce uniqueness going forward
CREATE UNIQUE INDEX IF NOT EXISTS idx_coworking_verifications_unique_vote
    ON public.coworking_verifications (coworking_id, user_id);
