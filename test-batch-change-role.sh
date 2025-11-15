#!/bin/bash

# 测试批量更改用户角色 API
# Usage: ./test-batch-change-role.sh

# 颜色定义
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

# 配置
GATEWAY_URL="${GATEWAY_URL:-http://localhost:5000}"
TOKEN="${TOKEN:-your-admin-token-here}"

echo -e "${YELLOW}测试批量更改用户角色 API${NC}"
echo "============================================"
echo ""

# 1. 首先获取角色列表
echo -e "${YELLOW}1. 获取所有角色...${NC}"
ROLES_RESPONSE=$(curl -s -X GET "$GATEWAY_URL/api/v1/roles" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json")

echo "$ROLES_RESPONSE" | python3 -m json.tool
echo ""

# 提取 admin 角色 ID（假设返回格式正确）
ADMIN_ROLE_ID=$(echo "$ROLES_RESPONSE" | python3 -c "import sys, json; data = json.load(sys.stdin); roles = data.get('data', []); admin_role = next((r for r in roles if r.get('name', '').lower() == 'admin'), None); print(admin_role['id'] if admin_role else '')" 2>/dev/null)

if [ -z "$ADMIN_ROLE_ID" ]; then
    echo -e "${RED}错误: 未找到 admin 角色${NC}"
    exit 1
fi

echo -e "${GREEN}找到 admin 角色 ID: $ADMIN_ROLE_ID${NC}"
echo ""

# 2. 获取用户列表
echo -e "${YELLOW}2. 获取用户列表...${NC}"
USERS_RESPONSE=$(curl -s -X GET "$GATEWAY_URL/api/v1/users?page=1&pageSize=5" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json")

echo "$USERS_RESPONSE" | python3 -m json.tool
echo ""

# 提取前两个用户的 ID
USER_IDS=$(echo "$USERS_RESPONSE" | python3 -c "import sys, json; data = json.load(sys.stdin); items = data.get('data', {}).get('items', []); print(','.join([item['id'] for item in items[:2]]))" 2>/dev/null)

if [ -z "$USER_IDS" ]; then
    echo -e "${RED}错误: 未找到用户${NC}"
    exit 1
fi

# 转换为 JSON 数组格式
USER_IDS_ARRAY="[\"$(echo $USER_IDS | sed 's/,/","/g')\"]"
echo -e "${GREEN}选择的用户 IDs: $USER_IDS_ARRAY${NC}"
echo ""

# 3. 批量更改用户角色为 admin
echo -e "${YELLOW}3. 批量更改用户角色为 admin...${NC}"
BATCH_RESPONSE=$(curl -s -X PATCH "$GATEWAY_URL/api/v1/users/batch/role" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"userIds\": $USER_IDS_ARRAY,
    \"roleId\": \"$ADMIN_ROLE_ID\"
  }")

echo "$BATCH_RESPONSE" | python3 -m json.tool
echo ""

# 检查响应
SUCCESS=$(echo "$BATCH_RESPONSE" | python3 -c "import sys, json; data = json.load(sys.stdin); print(data.get('success', False))" 2>/dev/null)

if [ "$SUCCESS" = "True" ]; then
    echo -e "${GREEN}✅ 批量更改用户角色成功！${NC}"
else
    echo -e "${RED}❌ 批量更改用户角色失败${NC}"
fi

echo ""
echo -e "${YELLOW}4. 验证用户角色...${NC}"
for USER_ID in $(echo $USER_IDS | tr ',' ' '); do
    USER_RESPONSE=$(curl -s -X GET "$GATEWAY_URL/api/v1/users/$USER_ID" \
      -H "Authorization: Bearer $TOKEN" \
      -H "Content-Type: application/json")
    
    USER_ROLE=$(echo "$USER_RESPONSE" | python3 -c "import sys, json; data = json.load(sys.stdin); print(data.get('data', {}).get('role', {}).get('name', 'unknown'))" 2>/dev/null)
    echo -e "  用户 $USER_ID 的角色: ${GREEN}$USER_ROLE${NC}"
done

echo ""
echo -e "${GREEN}测试完成！${NC}"
