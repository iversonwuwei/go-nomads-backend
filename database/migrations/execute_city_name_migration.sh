#!/bin/bash

# ============================================================
# 城市名称英文化迁移脚本
# ============================================================

set -e  # 遇到错误立即退出

echo "============================================================"
echo "  城市名称英文化迁移"
echo "============================================================"
echo ""

# Supabase连接信息
DB_HOST="db.lcfbajrocmjlqndkrsao.supabase.co"
DB_PORT="6543"
DB_NAME="postgres"
DB_USER="postgres.lcfbajrocmjlqndkrsao"
DB_PASSWORD="bwTyaM1eJ1TRIZI3"

# 构建连接字符串
CONNECTION_STRING="postgresql://${DB_USER}:${DB_PASSWORD}@${DB_HOST}:${DB_PORT}/${DB_NAME}?sslmode=require"

echo "📊 步骤 1: 查看当前城市名称（迁移前）"
echo "------------------------------------------------------------"
psql "$CONNECTION_STRING" -c "SELECT name, country FROM cities WHERE country = 'China' LIMIT 5;"

echo ""
read -p "⚠️  确认要将所有中文城市名称转换为英文吗？(yes/no): " confirm

if [ "$confirm" != "yes" ]; then
    echo "❌ 操作已取消"
    exit 0
fi

echo ""
echo "🔄 步骤 2: 执行名称转换"
echo "------------------------------------------------------------"

# 执行迁移脚本
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MIGRATION_FILE="${SCRIPT_DIR}/convert_city_names_to_english.sql"

if [ ! -f "$MIGRATION_FILE" ]; then
    echo "❌ 错误：找不到迁移文件 $MIGRATION_FILE"
    exit 1
fi

psql "$CONNECTION_STRING" -f "$MIGRATION_FILE"

echo ""
echo "✅ 步骤 3: 验证转换结果"
echo "------------------------------------------------------------"
psql "$CONNECTION_STRING" -c "SELECT name, country, COUNT(*) as count FROM cities WHERE country = 'China' GROUP BY name, country ORDER BY name LIMIT 10;"

echo ""
echo "============================================================"
echo "  ✅ 迁移完成！"
echo "============================================================"
echo ""
echo "📝 后续步骤："
echo "  1. 清理Redis缓存（如果有）"
echo "  2. 重启CityService服务"
echo "  3. 更新前端代码以使用国际化文件"
echo "  4. 测试天气数据获取是否正常"
echo ""
