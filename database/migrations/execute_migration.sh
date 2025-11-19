#!/bin/bash

# 城市评分系统 - 数据库迁移执行脚本
# 使用方法：
# 1. 设置环境变量 SUPABASE_DB_URL
# 2. 运行: ./execute_migration.sh

echo "🚀 开始执行城市评分系统数据库迁移..."
echo ""

if [ -z "$SUPABASE_DB_URL" ]; then
    echo "❌ 错误: 请先设置环境变量 SUPABASE_DB_URL"
    echo ""
    echo "示例:"
    echo 'export SUPABASE_DB_URL="postgresql://postgres:[PASSWORD]@[HOST]:[PORT]/postgres"'
    echo ""
    exit 1
fi

echo "📝 执行 SQL 文件: city_rating_system.sql"
psql "$SUPABASE_DB_URL" -f city_rating_system.sql

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ 迁移成功！"
    echo ""
    echo "🔍 验证表创建："
    psql "$SUPABASE_DB_URL" -c "SELECT table_name FROM information_schema.tables WHERE table_name IN ('city_rating_categories', 'city_ratings');"
    echo ""
    echo "📊 检查默认评分项："
    psql "$SUPABASE_DB_URL" -c "SELECT COUNT(*) as category_count FROM city_rating_categories;"
else
    echo ""
    echo "❌ 迁移失败，请检查错误信息"
    exit 1
fi
