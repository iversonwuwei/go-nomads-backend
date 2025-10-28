-- 添加事件参与者测试数据
-- 确保这些事件和用户已存在

-- 为 Bangkok 活动添加参与者
INSERT INTO event_participants (id, event_id, user_id, status, registered_at)
VALUES 
  (gen_random_uuid(), '00000000-0000-0000-0000-000000000001', '9d789131-e560-47cf-9ff1-b05f9c345207', 'registered', NOW() - INTERVAL '2 hours'),
  (gen_random_uuid(), '00000000-0000-0000-0000-000000000001', 'user_002', 'registered', NOW() - INTERVAL '5 hours'),
  (gen_random_uuid(), '00000000-0000-0000-0000-000000000001', 'user_003', 'registered', NOW() - INTERVAL '1 day'),
  (gen_random_uuid(), '00000000-0000-0000-0000-000000000001', 'user_004', 'registered', NOW() - INTERVAL '3 days')
ON CONFLICT (event_id, user_id) DO NOTHING;

-- 为 Chiang Mai 活动添加参与者  
INSERT INTO event_participants (id, event_id, user_id, status, registered_at)
VALUES 
  (gen_random_uuid(), '00000000-0000-0000-0000-000000000002', '9d789131-e560-47cf-9ff1-b05f9c345207', 'registered', NOW() - INTERVAL '1 hour'),
  (gen_random_uuid(), '00000000-0000-0000-0000-000000000002', 'user_005', 'registered', NOW() - INTERVAL '6 hours'),
  (gen_random_uuid(), '00000000-0000-0000-0000-000000000002', 'user_006', 'registered', NOW() - INTERVAL '2 days')
ON CONFLICT (event_id, user_id) DO NOTHING;

-- 为 Bali 活动添加参与者
INSERT INTO event_participants (id, event_id, user_id, status, registered_at)
VALUES 
  (gen_random_uuid(), '00000000-0000-0000-0000-000000000003', 'user_007', 'registered', NOW() - INTERVAL '30 minutes'),
  (gen_random_uuid(), '00000000-0000-0000-0000-000000000003', 'user_008', 'registered', NOW() - INTERVAL '4 hours')
ON CONFLICT (event_id, user_id) DO NOTHING;

-- 为 Lisbon 活动添加参与者
INSERT INTO event_participants (id, event_id, user_id, status, registered_at)
VALUES 
  (gen_random_uuid(), '00000000-0000-0000-0000-000000000004', '9d789131-e560-47cf-9ff1-b05f9c345207', 'registered', NOW() - INTERVAL '45 minutes'),
  (gen_random_uuid(), '00000000-0000-0000-0000-000000000004', 'user_009', 'registered', NOW() - INTERVAL '7 hours'),
  (gen_random_uuid(), '00000000-0000-0000-0000-000000000004', 'user_010', 'registered', NOW() - INTERVAL '1 day')
ON CONFLICT (event_id, user_id) DO NOTHING;

-- 验证插入结果
SELECT 
  ep.id,
  e.title as event_title,
  ep.user_id,
  ep.status,
  ep.registered_at
FROM event_participants ep
JOIN events e ON ep.event_id = e.id
ORDER BY e.title, ep.registered_at DESC;
