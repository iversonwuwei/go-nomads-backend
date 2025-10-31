-- 查看当前所有 RLS 策略
SELECT schemaname, tablename, policyname, permissive, roles, cmd, qual, with_check
FROM pg_policies
WHERE tablename IN ('user_city_expenses', 'user_city_photos', 'user_city_reviews')
ORDER BY tablename, policyname;
