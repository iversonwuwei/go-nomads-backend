ALTER TABLE public.user_preferences
    ADD COLUMN IF NOT EXISTS privacy_policy_accepted BOOLEAN NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS privacy_policy_accepted_at TIMESTAMPTZ NULL,
    ADD COLUMN IF NOT EXISTS terms_of_service_accepted BOOLEAN NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS terms_of_service_accepted_at TIMESTAMPTZ NULL;