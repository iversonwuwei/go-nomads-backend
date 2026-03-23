ALTER TABLE public.user_preferences
    ADD COLUMN IF NOT EXISTS privacy_policy_accepted_version TEXT NOT NULL DEFAULT '',
    ADD COLUMN IF NOT EXISTS terms_of_service_accepted_version TEXT NOT NULL DEFAULT '';