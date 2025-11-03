#!/bin/bash

# 用户收藏城市 API 测试脚本
# 使用方法: ./test-favorite-api.sh YOUR_JWT_TOKEN

set -e

# 颜色输出
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# API 基础 URL
BASE_URL="http://localhost:8002/api/v1/user-favorite-cities"

# 检查参数
if [ -z "$1" ]; then
    echo -e "${RED}错误: 请提供 JWT Token${NC}"
    echo "使用方法: $0 YOUR_JWT_TOKEN"
    echo ""
    echo "获取 Token 的方式:"
    echo "1. 在 Flutter App 中登录后,从代码中打印 token"
    echo "2. 或使用 Supabase Auth API 登录获取"
    exit 1
fi

TOKEN="$1"
TEST_CITY_ID="tokyo"

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}用户收藏城市 API 测试${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""

# 函数: 打印测试标题
print_test() {
    echo -e "${YELLOW}▶ 测试: $1${NC}"
}

# 函数: 打印成功
print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

# 函数: 打印失败
print_error() {
    echo -e "${RED}✗ $1${NC}"
}

# 函数: 打印响应
print_response() {
    echo -e "${BLUE}响应:${NC}"
    echo "$1" | jq '.' 2>/dev/null || echo "$1"
    echo ""
}

# 测试 1: 检查服务健康状态
print_test "1. 检查服务健康状态"
HEALTH_RESPONSE=$(curl -s http://localhost:8002/health)
if echo "$HEALTH_RESPONSE" | grep -q "healthy"; then
    print_success "服务运行正常"
    print_response "$HEALTH_RESPONSE"
else
    print_error "服务未运行或不健康"
    exit 1
fi

# 测试 2: 检查城市是否已收藏 (初始状态)
print_test "2. 检查城市 '$TEST_CITY_ID' 是否已收藏 (初始状态)"
CHECK_RESPONSE=$(curl -s -X GET "$BASE_URL/check/$TEST_CITY_ID" \
    -H "Authorization: Bearer $TOKEN")
print_response "$CHECK_RESPONSE"

# 测试 3: 添加收藏城市
print_test "3. 添加收藏城市 '$TEST_CITY_ID'"
ADD_RESPONSE=$(curl -s -X POST "$BASE_URL" \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d "{\"cityId\": \"$TEST_CITY_ID\"}")
if echo "$ADD_RESPONSE" | grep -q "cityId"; then
    print_success "添加收藏成功"
    print_response "$ADD_RESPONSE"
elif echo "$ADD_RESPONSE" | grep -q "already"; then
    print_success "城市已在收藏列表中"
    print_response "$ADD_RESPONSE"
else
    print_error "添加收藏失败"
    print_response "$ADD_RESPONSE"
fi

# 测试 4: 再次检查城市是否已收藏 (应该返回 true)
print_test "4. 再次检查城市 '$TEST_CITY_ID' 是否已收藏"
CHECK_RESPONSE2=$(curl -s -X GET "$BASE_URL/check/$TEST_CITY_ID" \
    -H "Authorization: Bearer $TOKEN")
if echo "$CHECK_RESPONSE2" | grep -q '"isFavorited":true'; then
    print_success "收藏状态正确"
    print_response "$CHECK_RESPONSE2"
else
    print_error "收藏状态不正确"
    print_response "$CHECK_RESPONSE2"
fi

# 测试 5: 获取所有收藏城市 ID
print_test "5. 获取所有收藏城市 ID"
IDS_RESPONSE=$(curl -s -X GET "$BASE_URL/ids" \
    -H "Authorization: Bearer $TOKEN")
if echo "$IDS_RESPONSE" | grep -q "$TEST_CITY_ID"; then
    print_success "成功获取收藏城市 ID 列表"
    print_response "$IDS_RESPONSE"
else
    print_error "收藏城市 ID 列表中未找到测试城市"
    print_response "$IDS_RESPONSE"
fi

# 测试 6: 获取收藏城市列表 (分页)
print_test "6. 获取收藏城市列表 (分页)"
LIST_RESPONSE=$(curl -s -X GET "$BASE_URL?page=1&pageSize=10" \
    -H "Authorization: Bearer $TOKEN")
if echo "$LIST_RESPONSE" | grep -q "items"; then
    print_success "成功获取收藏城市列表"
    print_response "$LIST_RESPONSE"
else
    print_error "获取收藏城市列表失败"
    print_response "$LIST_RESPONSE"
fi

# 测试 7: 取消收藏城市
print_test "7. 取消收藏城市 '$TEST_CITY_ID'"
DELETE_RESPONSE=$(curl -s -w "\nHTTP_STATUS:%{http_code}" -X DELETE "$BASE_URL/$TEST_CITY_ID" \
    -H "Authorization: Bearer $TOKEN")
HTTP_STATUS=$(echo "$DELETE_RESPONSE" | grep "HTTP_STATUS" | cut -d':' -f2)
if [ "$HTTP_STATUS" = "204" ] || [ "$HTTP_STATUS" = "200" ]; then
    print_success "取消收藏成功 (HTTP $HTTP_STATUS)"
else
    print_error "取消收藏失败 (HTTP $HTTP_STATUS)"
fi
echo ""

# 测试 8: 最后检查城市是否已收藏 (应该返回 false)
print_test "8. 最后检查城市 '$TEST_CITY_ID' 是否已收藏"
CHECK_RESPONSE3=$(curl -s -X GET "$BASE_URL/check/$TEST_CITY_ID" \
    -H "Authorization: Bearer $TOKEN")
if echo "$CHECK_RESPONSE3" | grep -q '"isFavorited":false'; then
    print_success "收藏状态正确 (已取消)"
    print_response "$CHECK_RESPONSE3"
else
    print_error "收藏状态不正确"
    print_response "$CHECK_RESPONSE3"
fi

# 测试完成
echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}所有测试完成!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo "下一步:"
echo "1. 在 Flutter App 中测试 UI 交互"
echo "2. 检查 Supabase 数据库中的数据"
echo "3. 测试不同用户之间的数据隔离"
echo ""
