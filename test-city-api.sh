#!/bin/bash

echo "测试城市列表 API - 检查 reviewCount 和 averageCost 字段"
echo "=============================================="
echo ""

# 通过 Gateway 调用 API
echo "1. 调用 API: GET http://localhost:8000/api/v1/cities?pageSize=3"
response=$(curl -s "http://localhost:8000/api/v1/cities?pageSize=3")

echo ""
echo "2. 响应数据 (格式化):"
echo "$response" | jq '.'

echo ""
echo "3. 检查第一个城市的数据:"
echo "$response" | jq '.data.items[0] | {name, reviewCount, averageCost, overallScore}'

echo ""
echo "4. 所有城市的 reviewCount 和 averageCost:"
echo "$response" | jq '.data.items[] | {name, reviewCount, averageCost}'

echo ""
echo "=============================================="
