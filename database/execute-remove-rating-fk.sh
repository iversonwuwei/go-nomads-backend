#!/bin/bash

# Supabase 数据库连接信息
DB_HOST="db.lcfbajrocmjlqndkrsao.supabase.co"
DB_PORT="6543"
DB_NAME="postgres"
DB_USER="postgres"
DB_PASSWORD="bwTyaM1eJ1TRIZI3"

# SQL 脚本路径
SQL_SCRIPT="/Users/walden/Workspaces/WaldenProjects/go-noma/database/migrations/remove_city_ratings_fk_constraint.sql"

echo "🔧 正在移除 city_ratings 外键约束..."

# 使用 psql 连接 Supabase 并执行 SQL
PGPASSWORD="$DB_PASSWORD" psql \
  -h "$DB_HOST" \
  -p "$DB_PORT" \
  -U "$DB_USER" \
  -d "$DB_NAME" \
  -f "$SQL_SCRIPT"

if [ $? -eq 0 ]; then
    echo "✅ 外键约束已成功移除"
    echo "📋 验证结果已显示（应该没有外键约束）"
else
    echo "❌ 执行失败，请检查错误信息"
    exit 1
fi
