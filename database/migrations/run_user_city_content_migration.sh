#!/bin/bash

# 用户城市内容表迁移脚本
# 使用 Supabase PostgreSQL 数据库

# Supabase 连接信息
HOST="db.lcfbajrocmjlqndkrsao.supabase.co"
PORT="6543"
DATABASE="postgres"
USER="postgres.lcfbajrocmjlqndkrsao"
PASSWORD="bwTyaM1eJ1TRIZI3"

# SQL 文件路径
SQL_FILE="$(dirname "$0")/create_user_city_content_tables.sql"

echo "🚀 开始执行用户城市内容表迁移..."
echo "   数据库: $HOST:$PORT/$DATABASE"
echo "   SQL文件: $SQL_FILE"
echo ""

# 设置 PGPASSWORD 环境变量以避免密码提示
export PGPASSWORD="$PASSWORD"

# 执行 SQL 迁移
psql -h "$HOST" -p "$PORT" -U "$USER" -d "$DATABASE" -f "$SQL_FILE"

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ 迁移成功完成!"
    echo ""
    echo "已创建的表："
    echo "  - user_city_photos (用户城市照片)"
    echo "  - user_city_expenses (用户城市费用)"
    echo "  - user_city_reviews (用户城市评论)"
    echo "  - user_city_content_stats (统计视图)"
    echo ""
    echo "已配置："
    echo "  - RLS 行级安全策略"
    echo "  - 性能优化索引"
    echo "  - 外键约束"
else
    echo ""
    echo "❌ 迁移失败，请检查错误信息"
    exit 1
fi

# 清除密码环境变量
unset PGPASSWORD
