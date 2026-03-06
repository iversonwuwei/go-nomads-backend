-- ============================================================
-- Add persistent moderation fields for city photos
-- and create admin audit events table
-- Date: 2026-03-06
-- ============================================================

-- 1) user_city_photos: moderation columns
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'user_city_photos' AND column_name = 'moderation_status'
    ) THEN
        ALTER TABLE user_city_photos ADD COLUMN moderation_status VARCHAR(20);
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'user_city_photos' AND column_name = 'moderated_by'
    ) THEN
        ALTER TABLE user_city_photos ADD COLUMN moderated_by VARCHAR(100);
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'user_city_photos' AND column_name = 'moderation_note'
    ) THEN
        ALTER TABLE user_city_photos ADD COLUMN moderation_note TEXT;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'user_city_photos' AND column_name = 'moderated_at'
    ) THEN
        ALTER TABLE user_city_photos ADD COLUMN moderated_at TIMESTAMPTZ;
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS idx_user_city_photos_moderation_status
    ON user_city_photos(moderation_status);

CREATE INDEX IF NOT EXISTS idx_user_city_photos_moderated_at
    ON user_city_photos(moderated_at DESC);

-- 2) admin_audit_events table
CREATE TABLE IF NOT EXISTS admin_audit_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    scope VARCHAR(100) NOT NULL DEFAULT 'global',
    entity_id TEXT NOT NULL DEFAULT '',
    action VARCHAR(100) NOT NULL,
    note TEXT NOT NULL,
    metadata JSONB NOT NULL DEFAULT '{}'::jsonb,
    happened_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_admin_audit_events_scope_happened_at
    ON admin_audit_events(scope, happened_at DESC);

CREATE INDEX IF NOT EXISTS idx_admin_audit_events_action
    ON admin_audit_events(action);

COMMENT ON TABLE admin_audit_events IS 'Admin audit trail events';
COMMENT ON COLUMN admin_audit_events.scope IS 'Business scope: reports/city-photos/...';
COMMENT ON COLUMN admin_audit_events.entity_id IS 'Target entity id in the scope';
COMMENT ON COLUMN admin_audit_events.action IS 'Action name';
COMMENT ON COLUMN admin_audit_events.note IS 'Human readable note';
COMMENT ON COLUMN admin_audit_events.metadata IS 'Extra structured data';

ALTER TABLE admin_audit_events ENABLE ROW LEVEL SECURITY;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_policies
        WHERE schemaname = 'public'
          AND tablename = 'admin_audit_events'
          AND policyname = 'Service role full access on admin_audit_events'
    ) THEN
        CREATE POLICY "Service role full access on admin_audit_events"
            ON admin_audit_events
            FOR ALL
            TO service_role
            USING (true)
            WITH CHECK (true);
    END IF;
END $$;
