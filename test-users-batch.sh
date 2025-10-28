#!/bin/bash

# 测试批量获取用户 API
# POST /api/v1/users/batch

echo "========================================="
echo "测试批量获取用户 API"
echo "========================================="

# 配置
BASE_URL="http://localhost:5000/api/v1"

echo ""
echo "1. 测试批量获取用户（带示例用户ID）"
curl -X POST "$BASE_URL/users/batch" \
  -H "Content-Type: application/json" \
  -d '{
    "userIds": [
      "user_001",
      "user_002",
      "user_003"
    ]
  }' \
  -w "\n\nHTTP Status: %{http_code}\n" \
  | jq '.'

echo ""
echo "2. 测试空列表"
curl -X POST "$BASE_URL/users/batch" \
  -H "Content-Type: application/json" \
  -d '{
    "userIds": []
  }' \
  -w "\n\nHTTP Status: %{http_code}\n" \
  | jq '.'

echo ""
echo "3. 测试超过100个用户ID的限制"
# 生成101个用户ID
USER_IDS=$(seq -f '"user_%03g"' 1 101 | tr '\n' ',' | sed 's/,$//')
curl -X POST "$BASE_URL/users/batch" \
  -H "Content-Type: application/json" \
  -d "{
    \"userIds\": [$USER_IDS]
  }" \
  -w "\n\nHTTP Status: %{http_code}\n" \
  | jq '.'

echo ""
echo "========================================="
echo "测试完成"
echo "========================================="
