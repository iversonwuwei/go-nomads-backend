-- 检查用户是否存在
SELECT id, email, created_at 
FROM users 
WHERE id = '9d789131-e560-47cf-9ff1-b05f9c345207';

-- 检查 auth.users 中是否有这个用户
SELECT id, email, created_at 
FROM auth.users 
WHERE id = '9d789131-e560-47cf-9ff1-b05f9c345207';

-- 查看 users 表结构和外键
SELECT 
    tc.table_name, 
    kcu.column_name, 
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name 
FROM information_schema.table_constraints AS tc 
JOIN information_schema.key_column_usage AS kcu
  ON tc.constraint_name = kcu.constraint_name
JOIN information_schema.constraint_column_usage AS ccu
  ON ccu.constraint_name = tc.constraint_name
WHERE tc.table_name = 'user_city_expenses' 
  AND tc.constraint_type = 'FOREIGN KEY';
