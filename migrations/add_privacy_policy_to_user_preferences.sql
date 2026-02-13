-- 添加隐私政策同意字段到用户偏好设置表
-- Migration: add_privacy_policy_to_user_preferences
-- Date: 2026-02-13

-- 添加隐私政策是否已同意字段
ALTER TABLE user_preferences 
ADD COLUMN IF NOT EXISTS privacy_policy_accepted BOOLEAN NOT NULL DEFAULT FALSE;

-- 添加隐私政策同意时间字段
ALTER TABLE user_preferences 
ADD COLUMN IF NOT EXISTS privacy_policy_accepted_at TIMESTAMPTZ NULL;

COMMENT ON COLUMN user_preferences.privacy_policy_accepted IS '用户是否已同意隐私政策';
COMMENT ON COLUMN user_preferences.privacy_policy_accepted_at IS '用户同意隐私政策的时间';
