BEGIN;

CREATE TABLE IF NOT EXISTS public.app_static_texts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    text_key TEXT NOT NULL,
    locale TEXT NOT NULL DEFAULT 'zh-CN',
    text_value TEXT NOT NULL DEFAULT '',
    category TEXT NOT NULL DEFAULT '',
    description TEXT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    version INTEGER NOT NULL DEFAULT 1,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by UUID NULL,
    updated_by UUID NULL,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ NULL,
    deleted_by UUID NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_app_static_texts_key_locale
    ON public.app_static_texts (text_key, locale)
    WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS idx_app_static_texts_category
    ON public.app_static_texts (category, locale)
    WHERE is_deleted = false;

CREATE TABLE IF NOT EXISTS public.app_option_groups (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    group_code TEXT NOT NULL,
    group_name TEXT NOT NULL,
    group_name_en TEXT NULL,
    description TEXT NULL,
    is_system BOOLEAN NOT NULL DEFAULT false,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by UUID NULL,
    updated_by UUID NULL,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ NULL,
    deleted_by UUID NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_app_option_groups_code
    ON public.app_option_groups (group_code)
    WHERE is_deleted = false;

CREATE TABLE IF NOT EXISTS public.app_option_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    group_id UUID NOT NULL REFERENCES public.app_option_groups (id) ON DELETE CASCADE,
    option_code TEXT NOT NULL,
    option_value TEXT NOT NULL,
    option_value_en TEXT NULL,
    icon TEXT NULL,
    color TEXT NULL,
    sort_order INTEGER NOT NULL DEFAULT 0,
    is_active BOOLEAN NOT NULL DEFAULT true,
    metadata TEXT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by UUID NULL,
    updated_by UUID NULL,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ NULL,
    deleted_by UUID NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_app_option_items_group_code
    ON public.app_option_items (group_id, option_code)
    WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS idx_app_option_items_group_sort
    ON public.app_option_items (group_id, sort_order)
    WHERE is_deleted = false;

CREATE TABLE IF NOT EXISTS public.app_config_snapshots (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    version INTEGER NOT NULL,
    static_texts JSONB NOT NULL DEFAULT '{}'::jsonb,
    option_groups JSONB NOT NULL DEFAULT '{}'::jsonb,
    system_settings JSONB NOT NULL DEFAULT '{}'::jsonb,
    is_published BOOLEAN NOT NULL DEFAULT false,
    published_by UUID NULL,
    published_at TIMESTAMPTZ NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by UUID NULL,
    updated_by UUID NULL,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ NULL,
    deleted_by UUID NULL
);

ALTER TABLE public.app_config_snapshots
    ADD COLUMN IF NOT EXISTS static_texts JSONB NOT NULL DEFAULT '{}'::jsonb,
    ADD COLUMN IF NOT EXISTS option_groups JSONB NOT NULL DEFAULT '{}'::jsonb,
    ADD COLUMN IF NOT EXISTS system_settings JSONB NOT NULL DEFAULT '{}'::jsonb,
    ADD COLUMN IF NOT EXISTS is_published BOOLEAN NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS published_by UUID NULL,
    ADD COLUMN IF NOT EXISTS published_at TIMESTAMPTZ NULL,
    ADD COLUMN IF NOT EXISTS created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    ADD COLUMN IF NOT EXISTS updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    ADD COLUMN IF NOT EXISTS created_by UUID NULL,
    ADD COLUMN IF NOT EXISTS updated_by UUID NULL,
    ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS deleted_at TIMESTAMPTZ NULL,
    ADD COLUMN IF NOT EXISTS deleted_by UUID NULL;

CREATE UNIQUE INDEX IF NOT EXISTS idx_app_config_snapshots_version
    ON public.app_config_snapshots (version)
    WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS idx_app_config_snapshots_published
    ON public.app_config_snapshots (is_published, version DESC)
    WHERE is_deleted = false;

NOTIFY pgrst, 'reload schema';

COMMIT;