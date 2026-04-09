BEGIN;

CREATE TABLE IF NOT EXISTS public.app_system_settings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    section TEXT NOT NULL,
    setting_key TEXT NOT NULL,
    label TEXT NOT NULL,
    description TEXT NULL,
    value_type TEXT NOT NULL DEFAULT 'string',
    value TEXT NOT NULL DEFAULT '',
    default_value TEXT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    is_secret BOOLEAN NOT NULL DEFAULT false,
    sort_order INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    created_by UUID NULL,
    updated_by UUID NULL,
    is_deleted BOOLEAN NOT NULL DEFAULT false,
    deleted_at TIMESTAMPTZ NULL,
    deleted_by UUID NULL
);

CREATE UNIQUE INDEX IF NOT EXISTS idx_app_system_settings_section_key
    ON public.app_system_settings (section, setting_key)
    WHERE is_deleted = false;

CREATE INDEX IF NOT EXISTS idx_app_system_settings_section_sort
    ON public.app_system_settings (section, sort_order)
    WHERE is_deleted = false;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name = 'app_config_snapshots'
    ) THEN
        ALTER TABLE public.app_config_snapshots
            ADD COLUMN IF NOT EXISTS system_settings JSONB NOT NULL DEFAULT '{}'::jsonb;
    END IF;
END $$;

INSERT INTO public.app_system_settings (
    section,
    setting_key,
    label,
    description,
    value_type,
    value,
    default_value,
    is_active,
    is_secret,
    sort_order
) VALUES
    ('general', 'platform_name', '平台名称', '后台与管理文案默认展示名称', 'string', 'Go Nomads Admin', 'Go Nomads Admin', true, false, 10),
    ('moderation', 'report_threshold', '举报阈值', '达到阈值后进入高优先级人工审核', 'number', '5', '5', true, false, 20),
    ('ai', 'default_model', '默认模型', 'AI 对话默认模型标识', 'string', 'gpt-4o-mini', 'gpt-4o-mini', true, false, 30),
    ('notification', 'retention_days', '通知保留天数', '历史通知自动清理保留天数', 'number', '90', '90', true, false, 40),
    ('maintenance', 'maintenance_mode', '维护模式', '开启后用于前台维护展示与运维协同', 'boolean', 'false', 'false', true, false, 50)
ON CONFLICT DO NOTHING;

COMMIT;