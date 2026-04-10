#!/bin/bash

# 使用 psql 执行数据库迁移
echo "🚀 使用 psql 执行数据库迁移..."

# Supabase 数据库连接信息
# 直连 db.<project-ref>.supabase.co 时，用户名必须使用 postgres
DB_HOST="${SUPABASE_DB_HOST:-db.lcfbajrocmjlqndkrsao.supabase.co}"
DB_PORT="${SUPABASE_DB_PORT:-6543}"
DB_NAME="${SUPABASE_DB_NAME:-postgres}"
DB_USER="${SUPABASE_DB_USER:-postgres}"
DB_PASSWORD="${SUPABASE_DB_PASSWORD:-}"

# SQL 文件路径
SQL_FILE="./database/migrations/create_user_city_content_tables.sql"

echo "📁 SQL 文件: $SQL_FILE"
echo "🔗 数据库: $DB_HOST:$DB_PORT/$DB_NAME"
echo ""

if [ -z "$DB_PASSWORD" ]; then
  echo "❌ 未设置 SUPABASE_DB_PASSWORD 环境变量!"
  echo ""
  echo "请先设置数据库密码，例如:"
  echo 'export SUPABASE_DB_PASSWORD="<your-password>"'
  exit 1
fi

# 检查 psql 是否安装
if ! command -v psql &> /dev/null; then
    echo "❌ psql 未安装!"
    echo ""
    echo "macOS 安装方法:"
    echo "brew install postgresql"
    exit 1
fi

echo "✅ psql 已安装"
echo ""
echo "📤 执行迁移..."

# 使用 psql 执行 SQL 文件
PGPASSWORD="$DB_PASSWORD" psql \
  -h "$DB_HOST" \
  -p "$DB_PORT" \
  -U "$DB_USER" \
  -d "$DB_NAME" \
  -f "$SQL_FILE" \
  -v ON_ERROR_STOP=1

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ 迁移成功!"
    echo ""
    echo "📊 验证创建的表..."
    
    # 验证表
    PGPASSWORD="$DB_PASSWORD" psql \
      -h "$DB_HOST" \
      -p "$DB_PORT" \
      -U "$DB_USER" \
      -d "$DB_NAME" \
      -c "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_name LIKE 'user_city_%' ORDER BY table_name;"
    
    echo ""
    echo "🎉 数据库迁移完成!"
else
    echo ""
    echo "❌ 迁移失败!"
    echo ""
    echo "⚠️  请手动在 Supabase SQL Editor 中执行迁移:"
    echo "1. 访问: https://supabase.com/dashboard/project/lcfbajrocmjlqndkrsao/sql/new"
    echo "2. 复制文件内容: $SQL_FILE"
    echo "3. 粘贴到 SQL Editor"
    echo "4. 点击 'Run' 按钮"
fi
