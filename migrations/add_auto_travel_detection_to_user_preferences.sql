-- 添加自动旅行检测字段到用户偏好设置表
-- Migration: add_auto_travel_detection_to_user_preferences
-- Date: 2025-12-23

-- 添加 auto_travel_detection_enabled 列
ALTER TABLE user_preferences 
ADD COLUMN IF NOT EXISTS auto_travel_detection_enabled BOOLEAN DEFAULT FALSE;

-- 添加注释
COMMENT ON COLUMN user_preferences.auto_travel_detection_enabled IS '是否启用自动旅行检测';

-- ============================================================
-- 修复 RLS 策略问题
-- 后端服务使用 service_role key，需要确保 RLS 不阻止操作
-- ============================================================

-- 方案 A：禁用 RLS（推荐，因为后端使用 service_role key）
ALTER TABLE user_preferences DISABLE ROW LEVEL SECURITY;

-- 方案 B（可选）：如果需要保持 RLS 启用，则添加以下策略
-- 取消注释以下内容来使用方案 B

-- ALTER TABLE user_preferences ENABLE ROW LEVEL SECURITY;

-- 删除现有策略（如果存在）
-- DROP POLICY IF EXISTS "Users can view own preferences" ON user_preferences;
-- DROP POLICY IF EXISTS "Users can insert own preferences" ON user_preferences;
-- DROP POLICY IF EXISTS "Users can update own preferences" ON user_preferences;
-- DROP POLICY IF EXISTS "Service role full access" ON user_preferences;

-- 允许 service_role 完全访问（后端服务使用）
-- CREATE POLICY "Service role full access" ON user_preferences
--   FOR ALL
--   TO service_role
--   USING (true)
--   WITH CHECK (true);

-- 允许用户查看自己的偏好设置
-- CREATE POLICY "Users can view own preferences" ON user_preferences
--   FOR SELECT
--   TO authenticated
--   USING (auth.uid()::text = user_id);

-- 允许用户插入自己的偏好设置
-- CREATE POLICY "Users can insert own preferences" ON user_preferences
--   FOR INSERT
--   TO authenticated
--   WITH CHECK (auth.uid()::text = user_id);

-- 允许用户更新自己的偏好设置
-- CREATE POLICY "Users can update own preferences" ON user_preferences
--   FOR UPDATE
--   TO authenticated
--   USING (auth.uid()::text = user_id)
--   WITH CHECK (auth.uid()::text = user_id);
