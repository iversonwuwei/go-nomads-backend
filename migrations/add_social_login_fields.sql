-- 添加社交登录字段到 users 表
-- 执行前请先备份数据库

-- 添加社交登录提供商字段
ALTER TABLE users ADD COLUMN IF NOT EXISTS social_provider VARCHAR(50);

-- 添加社交登录用户唯一标识字段（OpenID）
ALTER TABLE users ADD COLUMN IF NOT EXISTS social_open_id VARCHAR(255);

-- 添加索引以支持按社交登录信息查询
CREATE INDEX IF NOT EXISTS idx_users_social_login ON users (social_provider, social_open_id) WHERE social_provider IS NOT NULL;

-- 添加注释
COMMENT ON COLUMN users.social_provider IS '社交登录提供商（wechat, qq, douyin, apple, google）';
COMMENT ON COLUMN users.social_open_id IS '社交登录平台用户唯一标识（OpenID）';
