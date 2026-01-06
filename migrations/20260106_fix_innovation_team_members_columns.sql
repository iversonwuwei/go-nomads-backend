-- ============================================================
-- 修复 innovation_team_members 表缺失的列
-- 目的：恢复被误删的 name 和 avatar_url 列
-- 日期：2026-01-06
-- ============================================================

-- 添加 name 列（如果不存在）
ALTER TABLE innovation_team_members 
ADD COLUMN IF NOT EXISTS name VARCHAR(100);

-- 添加 avatar_url 列（如果不存在）
ALTER TABLE innovation_team_members 
ADD COLUMN IF NOT EXISTS avatar_url TEXT;

-- 添加注释
COMMENT ON COLUMN innovation_team_members.name IS '成员姓名';
COMMENT ON COLUMN innovation_team_members.avatar_url IS '成员头像URL';

-- 验证
DO $$
DECLARE
    has_name BOOLEAN;
    has_avatar BOOLEAN;
BEGIN
    SELECT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'innovation_team_members' AND column_name = 'name'
    ) INTO has_name;
    
    SELECT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_name = 'innovation_team_members' AND column_name = 'avatar_url'
    ) INTO has_avatar;
    
    RAISE NOTICE '========== 修复结果 ==========';
    RAISE NOTICE 'innovation_team_members.name 列存在: %', has_name;
    RAISE NOTICE 'innovation_team_members.avatar_url 列存在: %', has_avatar;
END $$;
