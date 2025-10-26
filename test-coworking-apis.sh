#!/bin/bash

# Test Country and City APIs for Add Coworking Page

echo "🧪 测试 Add Coworking Page 相关 API"
echo "===================================="
echo ""

# API Base URL
API_BASE="http://localhost:5000"

# 获取认证 token
echo "📡 测试 1: 登录获取 Token"
LOGIN_RESPONSE=$(curl -s -X POST $API_BASE/api/v1/users/login \
  -H "Content-Type: application/json" \
  -d '{"email":"walden.wuwei@gmail.com","password":"walden123456"}')

echo "$LOGIN_RESPONSE" | jq '.'

# 提取 token
TOKEN=$(echo "$LOGIN_RESPONSE" | jq -r '.data.token')

if [ -z "$TOKEN" ] || [ "$TOKEN" = "null" ]; then
    echo "❌ 登录失败，无法获取 Token"
    exit 1
fi

echo ""
echo "✅ Token 获取成功"
echo ""

# 测试获取国家列表
echo "===================================="
echo "📡 测试 2: 获取国家列表"
COUNTRIES_RESPONSE=$(curl -s -X GET $API_BASE/api/v1/cities/countries \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json")

echo "$COUNTRIES_RESPONSE" | jq '.'
echo ""

# 提取第一个国家的 ID (中国)
CHINA_ID=$(echo "$COUNTRIES_RESPONSE" | jq -r '.data[0].id')

if [ -z "$CHINA_ID" ] || [ "$CHINA_ID" = "null" ]; then
    echo "❌ 未能获取国家 ID"
    exit 1
fi

echo "✅ 国家列表获取成功"
echo "   第一个国家 ID: $CHINA_ID"
echo ""

# 测试根据国家 ID 获取城市列表
echo "===================================="
echo "📡 测试 3: 根据国家 ID 获取城市列表"
echo "   国家 ID: $CHINA_ID"
CITIES_RESPONSE=$(curl -s -X GET "$API_BASE/api/v1/cities/by-country/$CHINA_ID" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json")

echo "$CITIES_RESPONSE" | jq '.'
echo ""

# 统计城市数量
CITY_COUNT=$(echo "$CITIES_RESPONSE" | jq '.data | length')

echo "✅ 城市列表获取成功"
echo "   城市数量: $CITY_COUNT"
echo ""

# 显示前 5 个城市
echo "===================================="
echo "📋 前 5 个城市:"
echo "$CITIES_RESPONSE" | jq -r '.data[0:5] | .[] | "   - \(.name) (ID: \(.id))"'
echo ""

echo "===================================="
echo "✅ 所有测试完成"
