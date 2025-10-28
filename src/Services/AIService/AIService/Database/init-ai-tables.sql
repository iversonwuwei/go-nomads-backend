-- AI Service 数据库表结构
-- 创建时间: 2025-10-28

-- 1. 创建 AI 对话表
CREATE TABLE IF NOT EXISTS ai_conversations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    title VARCHAR(200) NOT NULL,
    user_id UUID NOT NULL,
    status VARCHAR(50) DEFAULT 'active' CHECK (status IN ('active', 'archived', 'deleted')),
    model_name VARCHAR(100) DEFAULT 'qwen-plus',
    system_prompt TEXT,
    total_messages INTEGER DEFAULT 0,
    total_tokens INTEGER DEFAULT 0,
    last_message_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
    deleted_at TIMESTAMPTZ
);

-- 2. 创建 AI 消息表
CREATE TABLE IF NOT EXISTS ai_messages (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    conversation_id UUID NOT NULL REFERENCES ai_conversations(id) ON DELETE CASCADE,
    role VARCHAR(20) NOT NULL CHECK (role IN ('user', 'assistant', 'system')),
    content TEXT NOT NULL,
    token_count INTEGER DEFAULT 0,
    model_name VARCHAR(100),
    prompt_tokens INTEGER,
    completion_tokens INTEGER,
    total_tokens INTEGER,
    response_time_ms INTEGER,
    metadata JSONB,
    error_message TEXT,
    is_error BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ,
    deleted_at TIMESTAMPTZ
);

-- 3. 创建索引以提高查询性能
-- 对话表索引
CREATE INDEX IF NOT EXISTS idx_ai_conversations_user_id ON ai_conversations(user_id);
CREATE INDEX IF NOT EXISTS idx_ai_conversations_status ON ai_conversations(status);
CREATE INDEX IF NOT EXISTS idx_ai_conversations_last_message_at ON ai_conversations(last_message_at DESC);
CREATE INDEX IF NOT EXISTS idx_ai_conversations_created_at ON ai_conversations(created_at DESC);
CREATE INDEX IF NOT EXISTS idx_ai_conversations_deleted_at ON ai_conversations(deleted_at) WHERE deleted_at IS NULL;

-- 消息表索引
CREATE INDEX IF NOT EXISTS idx_ai_messages_conversation_id ON ai_messages(conversation_id);
CREATE INDEX IF NOT EXISTS idx_ai_messages_role ON ai_messages(role);
CREATE INDEX IF NOT EXISTS idx_ai_messages_created_at ON ai_messages(created_at);
CREATE INDEX IF NOT EXISTS idx_ai_messages_deleted_at ON ai_messages(deleted_at) WHERE deleted_at IS NULL;
CREATE INDEX IF NOT EXISTS idx_ai_messages_conversation_created ON ai_messages(conversation_id, created_at);

-- 4. 创建 RLS (Row Level Security) 策略
-- 启用 RLS
ALTER TABLE ai_conversations ENABLE ROW LEVEL SECURITY;
ALTER TABLE ai_messages ENABLE ROW LEVEL SECURITY;

-- 对话表 RLS 策略
CREATE POLICY IF NOT EXISTS "Users can only access their own conversations" 
ON ai_conversations FOR ALL 
USING (user_id = auth.uid());

-- 消息表 RLS 策略 (通过对话表关联)
CREATE POLICY IF NOT EXISTS "Users can only access messages from their conversations" 
ON ai_messages FOR ALL 
USING (
    conversation_id IN (
        SELECT id FROM ai_conversations WHERE user_id = auth.uid()
    )
);

-- 5. 创建触发器函数，自动更新 updated_at 字段
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ language 'plpgsql';

-- 6. 为表创建 updated_at 触发器
CREATE TRIGGER update_ai_conversations_updated_at 
    BEFORE UPDATE ON ai_conversations 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_ai_messages_updated_at 
    BEFORE UPDATE ON ai_messages 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- 7. 创建一些有用的视图
-- 对话统计视图
CREATE OR REPLACE VIEW ai_conversation_stats AS
SELECT 
    c.id,
    c.title,
    c.user_id,
    c.status,
    c.model_name,
    c.created_at,
    c.last_message_at,
    COUNT(m.id) as message_count,
    SUM(CASE WHEN m.role = 'user' THEN 1 ELSE 0 END) as user_message_count,
    SUM(CASE WHEN m.role = 'assistant' THEN 1 ELSE 0 END) as assistant_message_count,
    SUM(m.token_count) as total_tokens_used,
    AVG(m.response_time_ms) as avg_response_time
FROM ai_conversations c
LEFT JOIN ai_messages m ON c.id = m.conversation_id AND m.deleted_at IS NULL
WHERE c.deleted_at IS NULL
GROUP BY c.id, c.title, c.user_id, c.status, c.model_name, c.created_at, c.last_message_at;

-- 用户统计视图
CREATE OR REPLACE VIEW ai_user_stats AS
SELECT 
    user_id,
    COUNT(*) as total_conversations,
    COUNT(CASE WHEN status = 'active' THEN 1 END) as active_conversations,
    COUNT(CASE WHEN status = 'archived' THEN 1 END) as archived_conversations,
    SUM(total_messages) as total_messages,
    SUM(total_tokens) as total_tokens,
    MAX(last_message_at) as last_activity_at,
    MIN(created_at) as first_conversation_at
FROM ai_conversations
WHERE deleted_at IS NULL
GROUP BY user_id;

-- 8. 插入一些示例数据 (可选，用于测试)
-- 注意：在生产环境中删除这部分
/*
INSERT INTO ai_conversations (title, user_id, system_prompt) VALUES 
('测试对话', '9d789131-e560-47cf-9ff1-b05f9c345207', '你是一个有帮助的AI助手，请用中文回答问题。');

INSERT INTO ai_messages (conversation_id, role, content, token_count) VALUES 
((SELECT id FROM ai_conversations WHERE title = '测试对话' LIMIT 1), 'user', '你好，请介绍一下自己', 10),
((SELECT id FROM ai_conversations WHERE title = '测试对话' LIMIT 1), 'assistant', '你好！我是一个AI助手，基于千问大模型，可以帮助您回答问题、处理任务和进行对话。有什么我可以帮助您的吗？', 35);
*/

-- 9. 授予必要的权限
-- 为匿名用户和认证用户授予权限
GRANT USAGE ON SCHEMA public TO anon, authenticated;
GRANT ALL ON ai_conversations TO anon, authenticated;
GRANT ALL ON ai_messages TO anon, authenticated;
GRANT SELECT ON ai_conversation_stats TO anon, authenticated;
GRANT SELECT ON ai_user_stats TO anon, authenticated;

-- 10. 创建存储过程（可选）
-- 清理旧的已删除记录的存储过程
CREATE OR REPLACE FUNCTION cleanup_deleted_ai_records(days_old INTEGER DEFAULT 30)
RETURNS INTEGER AS $$
DECLARE
    deleted_count INTEGER;
BEGIN
    -- 物理删除超过指定天数的软删除记录
    DELETE FROM ai_messages 
    WHERE deleted_at IS NOT NULL 
    AND deleted_at < NOW() - INTERVAL '%s days';
    
    GET DIAGNOSTICS deleted_count = ROW_COUNT;
    
    DELETE FROM ai_conversations 
    WHERE deleted_at IS NOT NULL 
    AND deleted_at < NOW() - INTERVAL '%s days';
    
    GET DIAGNOSTICS deleted_count = deleted_count + ROW_COUNT;
    
    RETURN deleted_count;
END;
$$ LANGUAGE plpgsql;

COMMENT ON TABLE ai_conversations IS 'AI 对话会话表';
COMMENT ON TABLE ai_messages IS 'AI 消息表';
COMMENT ON FUNCTION cleanup_deleted_ai_records IS '清理软删除的AI记录';

-- 完成脚本
SELECT 'AI Service 数据库表创建完成' as status;