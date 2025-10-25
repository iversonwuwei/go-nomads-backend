#!/bin/bash

# Supabase 数据库连接信息
DB_HOST="db.lcfbajrocmjlqndkrsao.supabase.co"
DB_PORT="6543"
DB_NAME="postgres"
DB_USER="postgres.lcfbajrocmjlqndkrsao"
DB_PASSWORD="bwTyaM1eJ1TRIZI3"

# 创建 event_followers 表的 SQL
SQL_SCRIPT="/Users/walden/Workspaces/WaldenProjects/go-noma/database/add-event-followers-table.sql"

# 使用 psql 连接 Supabase 并执行 SQL
PGPASSWORD="$DB_PASSWORD" psql \
  -h "$DB_HOST" \
  -p "$DB_PORT" \
  -U "$DB_USER" \
  -d "$DB_NAME" \
  -f "$SQL_SCRIPT"

echo "✅ event_followers 表已创建"
