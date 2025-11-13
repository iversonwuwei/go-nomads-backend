#!/bin/bash

# ====================================
# 执行通知系统数据库迁移
# ====================================

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# 数据库配置（从环境变量或配置文件读取）
DB_HOST="${SUPABASE_DB_HOST:-db.lcfbajrocmjlqndkrsao.supabase.co}"
DB_PORT="${SUPABASE_DB_PORT:-5432}"
DB_NAME="${SUPABASE_DB_NAME:-postgres}"
DB_USER="${SUPABASE_DB_USER:-postgres}"
DB_PASSWORD="${SUPABASE_DB_PASSWORD}"

# 迁移文件路径
MIGRATION_FILE="./database/migrations/create-notifications-table.sql"

echo -e "${YELLOW}=====================================${NC}"
echo -e "${YELLOW}通知系统数据库迁移${NC}"
echo -e "${YELLOW}=====================================${NC}"

# 检查迁移文件是否存在
if [ ! -f "$MIGRATION_FILE" ]; then
    echo -e "${RED}❌ 错误: 迁移文件不存在: $MIGRATION_FILE${NC}"
    exit 1
fi

# 检查数据库密码是否设置
if [ -z "$DB_PASSWORD" ]; then
    echo -e "${YELLOW}请输入数据库密码:${NC}"
    read -s DB_PASSWORD
fi

# 执行迁移
echo -e "${GREEN}🚀 开始执行数据库迁移...${NC}"
echo -e "${YELLOW}数据库: $DB_HOST:$DB_PORT/$DB_NAME${NC}"

PGPASSWORD=$DB_PASSWORD psql \
    -h "$DB_HOST" \
    -p "$DB_PORT" \
    -U "$DB_USER" \
    -d "$DB_NAME" \
    -f "$MIGRATION_FILE"

# 检查执行结果
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ 数据库迁移成功完成！${NC}"
    echo -e "${GREEN}=====================================${NC}"
    echo -e "${GREEN}创建的对象:${NC}"
    echo -e "  - 表: public.notifications"
    echo -e "  - 索引: 6个索引"
    echo -e "  - RPC函数: get_admin_user_ids()"
    echo -e "  - 触发器: trigger_set_notification_read_at"
    echo -e "  - 视图: unread_notifications_count"
    echo -e "  - RLS策略: 4个策略"
    echo -e "${GREEN}=====================================${NC}"
else
    echo -e "${RED}❌ 数据库迁移失败${NC}"
    exit 1
fi

# 验证表创建
echo -e "${YELLOW}🔍 验证表创建...${NC}"
PGPASSWORD=$DB_PASSWORD psql \
    -h "$DB_HOST" \
    -p "$DB_PORT" \
    -U "$DB_USER" \
    -d "$DB_NAME" \
    -c "\d public.notifications"

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ 表结构验证成功${NC}"
else
    echo -e "${RED}❌ 表结构验证失败${NC}"
    exit 1
fi

echo -e "${GREEN}=====================================${NC}"
echo -e "${GREEN}迁移完成！${NC}"
echo -e "${GREEN}=====================================${NC}"
