#!/bin/bash

# UserService Database Migration Script
# 用于执行数据库迁移到 Supabase

set -e

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}==================================================${NC}"
echo -e "${GREEN}  UserService Database Migration Tool${NC}"
echo -e "${GREEN}==================================================${NC}"
echo ""

# 检查是否设置了数据库连接字符串
if [ -z "$SUPABASE_DB_URL" ]; then
    echo -e "${YELLOW}环境变量 SUPABASE_DB_URL 未设置${NC}"
    echo ""
    echo "请设置数据库连接字符串:"
    echo -e "${YELLOW}export SUPABASE_DB_URL='postgresql://postgres:[YOUR-PASSWORD]@db.lcfbajrocmjlqndkrsao.supabase.co:5432/postgres'${NC}"
    echo ""
    echo "或者直接提供连接字符串:"
    read -p "数据库连接字符串: " SUPABASE_DB_URL
    
    if [ -z "$SUPABASE_DB_URL" ]; then
        echo -e "${RED}错误: 数据库连接字符串不能为空${NC}"
        exit 1
    fi
fi

# 获取脚本所在目录
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
MIGRATIONS_DIR="$SCRIPT_DIR"

echo -e "${GREEN}迁移文件目录: ${MIGRATIONS_DIR}${NC}"
echo ""

# 检查 psql 是否安装
if ! command -v psql &> /dev/null; then
    echo -e "${RED}错误: psql 未安装${NC}"
    echo ""
    echo "请安装 PostgreSQL 客户端:"
    echo "  macOS: brew install postgresql"
    echo "  Ubuntu: sudo apt-get install postgresql-client"
    echo ""
    echo -e "${YELLOW}或者使用 Supabase Dashboard 手动执行迁移${NC}"
    echo "请访问: https://app.supabase.com"
    exit 1
fi

# 列出所有迁移文件
echo -e "${GREEN}可用的迁移文件:${NC}"
migration_files=($(ls -1 ${MIGRATIONS_DIR}/*.sql 2>/dev/null | sort))

if [ ${#migration_files[@]} -eq 0 ]; then
    echo -e "${RED}错误: 未找到迁移文件${NC}"
    exit 1
fi

for i in "${!migration_files[@]}"; do
    filename=$(basename "${migration_files[$i]}")
    echo "  [$i] $filename"
done
echo ""

# 询问执行哪个迁移
read -p "请选择要执行的迁移 (输入编号,或按回车执行全部): " choice

if [ -z "$choice" ]; then
    # 执行所有迁移
    echo -e "${GREEN}执行所有迁移...${NC}"
    for file in "${migration_files[@]}"; do
        filename=$(basename "$file")
        echo ""
        echo -e "${YELLOW}执行: $filename${NC}"
        if psql "$SUPABASE_DB_URL" -f "$file"; then
            echo -e "${GREEN}✓ $filename 执行成功${NC}"
        else
            echo -e "${RED}✗ $filename 执行失败${NC}"
            exit 1
        fi
    done
else
    # 执行选定的迁移
    if [ "$choice" -ge 0 ] && [ "$choice" -lt "${#migration_files[@]}" ]; then
        file="${migration_files[$choice]}"
        filename=$(basename "$file")
        echo ""
        echo -e "${YELLOW}执行: $filename${NC}"
        if psql "$SUPABASE_DB_URL" -f "$file"; then
            echo -e "${GREEN}✓ $filename 执行成功${NC}"
        else
            echo -e "${RED}✗ $filename 执行失败${NC}"
            exit 1
        fi
    else
        echo -e "${RED}错误: 无效的选择${NC}"
        exit 1
    fi
fi

echo ""
echo -e "${GREEN}==================================================${NC}"
echo -e "${GREEN}  迁移完成!${NC}"
echo -e "${GREEN}==================================================${NC}"
echo ""
echo "后续步骤:"
echo "  1. 验证数据库结构: psql \$SUPABASE_DB_URL -c '\\d users'"
echo "  2. 重启 UserService 服务"
echo "  3. 测试 API 端点"
echo ""
