-- 测试 AI 表创建脚本的语法
-- 这是一个简化版本用于验证语法

-- 测试表创建
CREATE TABLE IF NOT EXISTS test_ai_conversations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    title VARCHAR(200) NOT NULL,
    user_id UUID NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- 测试 RLS 策略语法
ALTER TABLE test_ai_conversations ENABLE ROW LEVEL SECURITY;

-- 测试 DROP 和 CREATE POLICY 语法
DROP POLICY IF EXISTS "test_policy" ON test_ai_conversations;
CREATE POLICY "test_policy" 
ON test_ai_conversations FOR ALL 
USING (user_id = auth.uid());

-- 测试存储过程语法
CREATE OR REPLACE FUNCTION test_cleanup(days_old INTEGER DEFAULT 30)
RETURNS INTEGER AS $$
DECLARE
    deleted_count INTEGER;
BEGIN
    DELETE FROM test_ai_conversations 
    WHERE created_at < NOW() - (days_old || ' days')::INTERVAL;
    
    GET DIAGNOSTICS deleted_count = ROW_COUNT;
    RETURN deleted_count;
END;
$$ LANGUAGE plpgsql;

-- 清理测试
DROP FUNCTION IF EXISTS test_cleanup;
DROP TABLE IF EXISTS test_ai_conversations;

SELECT 'Syntax test completed' as result;