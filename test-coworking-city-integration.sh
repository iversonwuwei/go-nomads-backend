#!/bin/bash

echo "======================================"
echo "测试 Coworking 城市集成"
echo "======================================"
echo ""

BASE_URL="http://localhost:5000/api/v1"

echo "1. 获取城市列表..."
curl -s -X GET "${BASE_URL}/cities?page=1&pageSize=5" | jq '.data.items[] | {id, name, country}' | head -20

echo ""
echo "2. 获取第一个城市的 Coworking 空间..."
CITY_ID=$(curl -s -X GET "${BASE_URL}/cities?page=1&pageSize=1" | jq -r '.data.items[0].id')
echo "城市 ID: ${CITY_ID}"

curl -s -X GET "${BASE_URL}/coworking?cityId=${CITY_ID}&page=1&pageSize=10" | jq '{totalCount: .data.totalCount, items: .data.items | length}'

echo ""
echo "======================================"
echo "测试完成"
echo "======================================"
