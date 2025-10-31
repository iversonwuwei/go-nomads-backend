-- =====================================================
-- 为 user_city_expenses 和 user_city_photos 表添加 updated_at 字段
-- 添加时间: 2025-10-31
-- =====================================================

-- 1. 为 user_city_expenses 表添加 updated_at 字段
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'user_city_expenses' 
        AND column_name = 'updated_at'
    ) THEN
        ALTER TABLE user_city_expenses 
        ADD COLUMN updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW();
        
        -- 将现有记录的 updated_at 设置为 created_at
        UPDATE user_city_expenses 
        SET updated_at = created_at 
        WHERE updated_at IS NULL;
    END IF;
END $$;

-- 2. 为 user_city_photos 表添加 updated_at 字段
DO $$ 
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'user_city_photos' 
        AND column_name = 'updated_at'
    ) THEN
        ALTER TABLE user_city_photos 
        ADD COLUMN updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW();
        
        -- 将现有记录的 updated_at 设置为 created_at
        UPDATE user_city_photos 
        SET updated_at = created_at 
        WHERE updated_at IS NULL;
    END IF;
END $$;

-- 3. 创建自动更新 updated_at 的触发器函数（如果不存在）
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- 4. 为 user_city_expenses 表添加触发器
DROP TRIGGER IF EXISTS trigger_update_user_city_expenses_updated_at ON user_city_expenses;
CREATE TRIGGER trigger_update_user_city_expenses_updated_at
    BEFORE UPDATE ON user_city_expenses
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- 5. 为 user_city_photos 表添加触发器
DROP TRIGGER IF EXISTS trigger_update_user_city_photos_updated_at ON user_city_photos;
CREATE TRIGGER trigger_update_user_city_photos_updated_at
    BEFORE UPDATE ON user_city_photos
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- 6. 添加注释
COMMENT ON COLUMN user_city_expenses.updated_at IS '费用记录最后更新时间';
COMMENT ON COLUMN user_city_photos.updated_at IS '照片记录最后更新时间';

-- 7. 验证字段已添加
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'user_city_expenses' 
        AND column_name = 'updated_at'
    ) THEN
        RAISE EXCEPTION 'Failed to add updated_at column to user_city_expenses';
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'user_city_photos' 
        AND column_name = 'updated_at'
    ) THEN
        RAISE EXCEPTION 'Failed to add updated_at column to user_city_photos';
    END IF;
    
    RAISE NOTICE '✅ Successfully added updated_at columns to user_city_expenses and user_city_photos';
END $$;
