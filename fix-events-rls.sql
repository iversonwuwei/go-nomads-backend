-- 修复 Events 表的 RLS 策略
-- 允许 anon 和 authenticated 角色创建、读取、更新和删除 events

-- ====================================
-- Events 表策略
-- ====================================

-- 确保 RLS 已启用
ALTER TABLE events ENABLE ROW LEVEL SECURITY;

-- 删除旧策略(如果存在)
DROP POLICY IF EXISTS "Enable insert for anon" ON events;
DROP POLICY IF EXISTS "Enable select for anon" ON events;
DROP POLICY IF EXISTS "Enable update for anon" ON events;
DROP POLICY IF EXISTS "Enable delete for anon" ON events;

-- 为 anon 和 authenticated 角色添加完整的 CRUD 权限
CREATE POLICY "Enable insert for anon" ON events
    FOR INSERT TO anon, authenticated
    WITH CHECK (true);

CREATE POLICY "Enable select for anon" ON events
    FOR SELECT TO anon, authenticated
    USING (true);

CREATE POLICY "Enable update for anon" ON events
    FOR UPDATE TO anon, authenticated
    USING (true);

CREATE POLICY "Enable delete for anon" ON events
    FOR DELETE TO anon, authenticated
    USING (true);

-- ====================================
-- Event Participants 表策略
-- ====================================

ALTER TABLE event_participants ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "Enable insert for anon" ON event_participants;
DROP POLICY IF EXISTS "Enable select for anon" ON event_participants;
DROP POLICY IF EXISTS "Enable update for anon" ON event_participants;
DROP POLICY IF EXISTS "Enable delete for anon" ON event_participants;

CREATE POLICY "Enable insert for anon" ON event_participants
    FOR INSERT TO anon, authenticated
    WITH CHECK (true);

CREATE POLICY "Enable select for anon" ON event_participants
    FOR SELECT TO anon, authenticated
    USING (true);

CREATE POLICY "Enable update for anon" ON event_participants
    FOR UPDATE TO anon, authenticated
    USING (true);

CREATE POLICY "Enable delete for anon" ON event_participants
    FOR DELETE TO anon, authenticated
    USING (true);

-- ====================================
-- Event Followers 表策略
-- ====================================

ALTER TABLE event_followers ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "Enable insert for anon" ON event_followers;
DROP POLICY IF EXISTS "Enable select for anon" ON event_followers;
DROP POLICY IF EXISTS "Enable update for anon" ON event_followers;
DROP POLICY IF EXISTS "Enable delete for anon" ON event_followers;

CREATE POLICY "Enable insert for anon" ON event_followers
    FOR INSERT TO anon, authenticated
    WITH CHECK (true);

CREATE POLICY "Enable select for anon" ON event_followers
    FOR SELECT TO anon, authenticated
    USING (true);

CREATE POLICY "Enable update for anon" ON event_followers
    FOR UPDATE TO anon, authenticated
    USING (true);

CREATE POLICY "Enable delete for anon" ON event_followers
    FOR DELETE TO anon, authenticated
    USING (true);

-- ====================================
-- 验证策略
-- ====================================

-- 查看 events 表的策略
SELECT schemaname, tablename, policyname, roles, cmd
FROM pg_policies
WHERE tablename IN ('events', 'event_participants', 'event_followers')
ORDER BY tablename, policyname;
