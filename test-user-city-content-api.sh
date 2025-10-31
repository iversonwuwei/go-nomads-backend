#!/bin/bash

# 用户城市内容 API 测试脚本
# 测试 CityService 的用户内容端点

BASE_URL="http://localhost:8002"
CITY_ID="bangkok-thailand"

# 颜色输出
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "🧪 用户城市内容 API 测试"
echo "================================"
echo ""

# 需要先获取 JWT Token (这里使用假的 token 测试)
# 在实际环境中，需要先登录获取真实 token
TOKEN="your_jwt_token_here"

echo "📍 测试城市: $CITY_ID"
echo "🔗 API 基础URL: $BASE_URL"
echo ""

# 测试 1: 获取城市照片列表 (不需要认证)
echo -e "${YELLOW}测试 1: 获取城市照片列表${NC}"
echo "GET /api/cities/$CITY_ID/user-content/photos"
curl -s -w "\nHTTP Status: %{http_code}\n" \
  "$BASE_URL/api/cities/$CITY_ID/user-content/photos?onlyMine=false"
echo ""
echo "---"
echo ""

# 测试 2: 获取城市费用列表 (不需要认证)
echo -e "${YELLOW}测试 2: 获取城市费用列表${NC}"
echo "GET /api/cities/$CITY_ID/user-content/expenses"
curl -s -w "\nHTTP Status: %{http_code}\n" \
  "$BASE_URL/api/cities/$CITY_ID/user-content/expenses?onlyMine=false"
echo ""
echo "---"
echo ""

# 测试 3: 获取城市评论列表 (公开接口)
echo -e "${YELLOW}测试 3: 获取城市评论列表${NC}"
echo "GET /api/cities/$CITY_ID/user-content/reviews"
curl -s -w "\nHTTP Status: %{http_code}\n" \
  "$BASE_URL/api/cities/$CITY_ID/user-content/reviews"
echo ""
echo "---"
echo ""

# 测试 4: 获取城市内容统计 (公开接口)
echo -e "${YELLOW}测试 4: 获取城市内容统计${NC}"
echo "GET /api/cities/$CITY_ID/user-content/stats"
curl -s -w "\nHTTP Status: %{http_code}\n" \
  "$BASE_URL/api/cities/$CITY_ID/user-content/stats"
echo ""
echo "---"
echo ""

# 测试 5: 添加照片 (需要认证 - 会返回 401)
echo -e "${YELLOW}测试 5: 添加照片 (需要认证)${NC}"
echo "POST /api/cities/$CITY_ID/user-content/photos"
curl -s -w "\nHTTP Status: %{http_code}\n" \
  -X POST \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "cityId": "'$CITY_ID'",
    "imageUrl": "https://example.com/photo.jpg",
    "caption": "Test photo",
    "location": "Test location"
  }' \
  "$BASE_URL/api/cities/$CITY_ID/user-content/photos"
echo ""
echo "---"
echo ""

echo -e "${GREEN}✅ 测试完成！${NC}"
echo ""
echo "预期结果:"
echo "  - 测试 1-4: HTTP 200 (成功返回空数组或空对象)"
echo "  - 测试 5: HTTP 401 (未授权 - 需要登录)"
echo ""
echo "如果看到 HTTP 200，说明 API 端点工作正常！"
echo "如果看到 HTTP 401，说明认证机制正常工作！"
