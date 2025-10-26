#!/bin/bash

# 执行 SQL 脚本来禁用 RLS
# 使用 Supabase connection string

PGPASSWORD="bwTyaM1eJ1TRIZI3" psql \
  -h db.lcfbajrocmjlqndkrsao.supabase.co \
  -p 6543 \
  -U postgres.lcfbajrocmjlqndkrsao \
  -d postgres \
  -c "ALTER TABLE public.coworking_spaces DISABLE ROW LEVEL SECURITY;"

echo "✅ RLS 已禁用,可以进行开发测试"
echo "⚠️  生产环境请重新启用 RLS 并配置正确的策略"
