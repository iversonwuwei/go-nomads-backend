#!/bin/bash

# 用户城市内容表迁移执行脚本
# 使用 Supabase REST API 执行 SQL

echo "🚀 开始执行数据库迁移..."

# Supabase 配置
PROJECT_REF="lcfbajrocmjlqndkrsao"
SERVICE_ROLE_KEY="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImxjZmJhanJvY21qbHFuZGtyc2FvIiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTcyOTQ5MzI0MywiZXhwIjoyMDQ1MDY5MjQzfQ.bGDiCTOiL9mC7Y5AUo2mwlc8pDILPO0o-JVpFhf-xzo"

# SQL 文件路径
SQL_FILE="./database/migrations/create_user_city_content_tables.sql"

echo "📁 读取 SQL 文件: $SQL_FILE"

# 读取 SQL 文件
SQL_CONTENT=$(cat "$SQL_FILE")

# 使用 Supabase 管理 API 执行 SQL
# 注意: Supabase 提供了一个特殊的端点用于执行 SQL
echo "📤 发送 SQL 到 Supabase..."

# 创建临时文件存储 SQL
TEMP_SQL=$(mktemp)
echo "$SQL_CONTENT" > "$TEMP_SQL"

# 使用 curl 执行 SQL (通过 Supabase 的 SQL editor API)
response=$(curl -s -w "\n%{http_code}" \
  -X POST \
  "https://${PROJECT_REF}.supabase.co/rest/v1/rpc/exec_sql" \
  -H "apikey: ${SERVICE_ROLE_KEY}" \
  -H "Authorization: Bearer ${SERVICE_ROLE_KEY}" \
  -H "Content-Type: application/json" \
  -H "Prefer: return=minimal" \
  --data-binary @- <<EOF
{
  "query": $(jq -Rs . < "$TEMP_SQL")
}
EOF
)

# 清理临时文件
rm "$TEMP_SQL"

# 提取 HTTP 状态码
http_code=$(echo "$response" | tail -n1)
body=$(echo "$response" | sed '$d')

echo ""
echo "📊 响应状态码: $http_code"

if [ "$http_code" -eq 200 ] || [ "$http_code" -eq 201 ] || [ "$http_code" -eq 204 ]; then
    echo "✅ 迁移成功!"
    echo ""
    echo "验证创建的表:"
    echo "请在 Supabase SQL Editor 中运行以下查询:"
    echo ""
    echo "SELECT table_name FROM information_schema.tables"
    echo "WHERE table_schema = 'public' AND table_name LIKE 'user_city_%';"
else
    echo "❌ 迁移失败!"
    echo "响应内容:"
    echo "$body"
    echo ""
    echo "⚠️  请手动在 Supabase SQL Editor 中执行迁移:"
    echo "1. 访问: https://supabase.com/dashboard/project/${PROJECT_REF}/sql/new"
    echo "2. 复制文件内容: $SQL_FILE"
    echo "3. 粘贴到 SQL Editor"
    echo "4. 点击 'Run' 按钮"
fi

echo ""
echo "🔗 Supabase SQL Editor: https://supabase.com/dashboard/project/${PROJECT_REF}/sql/new"
