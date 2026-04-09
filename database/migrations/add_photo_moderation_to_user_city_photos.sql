-- 为 user_city_photos 表添加审核字段

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'user_city_photos'
          AND column_name = 'moderation_status'
    ) THEN
        ALTER TABLE user_city_photos
            ADD COLUMN moderation_status VARCHAR(20) NOT NULL DEFAULT 'pending';
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'user_city_photos'
          AND column_name = 'moderation_reason'
    ) THEN
        ALTER TABLE user_city_photos
            ADD COLUMN moderation_reason TEXT;
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'user_city_photos'
          AND column_name = 'reviewed_at'
    ) THEN
        ALTER TABLE user_city_photos
            ADD COLUMN reviewed_at TIMESTAMP WITH TIME ZONE;
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'user_city_photos'
          AND column_name = 'reviewed_by'
    ) THEN
        ALTER TABLE user_city_photos
            ADD COLUMN reviewed_by UUID;
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.table_constraints
        WHERE table_name = 'user_city_photos'
          AND constraint_name = 'chk_user_city_photos_moderation_status'
    ) THEN
        ALTER TABLE user_city_photos
            ADD CONSTRAINT chk_user_city_photos_moderation_status
            CHECK (moderation_status IN ('pending', 'approved', 'rejected'));
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS idx_user_city_photos_moderation_status ON user_city_photos(moderation_status);

COMMENT ON COLUMN user_city_photos.moderation_status IS '图片审核状态: pending/approved/rejected';
COMMENT ON COLUMN user_city_photos.moderation_reason IS '审核备注或拒绝原因';
COMMENT ON COLUMN user_city_photos.reviewed_at IS '审核时间';
COMMENT ON COLUMN user_city_photos.reviewed_by IS '审核管理员ID';