-- 用户统计数据表
-- 用于存储用户的游牧生活统计信息

CREATE TABLE IF NOT EXISTS user_stats (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    countries_visited INTEGER DEFAULT 0,
    cities_lived INTEGER DEFAULT 0,
    days_nomading INTEGER DEFAULT 0,
    meetups_attended INTEGER DEFAULT 0,
    trips_completed INTEGER DEFAULT 0,
    reviews_written INTEGER DEFAULT 0,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    CONSTRAINT user_stats_user_id_unique UNIQUE (user_id)
);

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_user_stats_user_id ON user_stats(user_id);

-- 添加注释
COMMENT ON TABLE user_stats IS '用户统计数据表 - 记录用户的游牧生活统计信息';
COMMENT ON COLUMN user_stats.countries_visited IS '访问过的国家数量';
COMMENT ON COLUMN user_stats.cities_lived IS '居住过的城市数量';
COMMENT ON COLUMN user_stats.days_nomading IS '游牧天数';
COMMENT ON COLUMN user_stats.meetups_attended IS '参加的聚会数量';
COMMENT ON COLUMN user_stats.trips_completed IS '完成的旅行数量';
COMMENT ON COLUMN user_stats.reviews_written IS '撰写的评论数量';
