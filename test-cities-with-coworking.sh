#!/bin/bash

# 测试新的城市列表接口(含 Coworking 数量)
# 专门为 coworking_home 页面设计

echo "========================================="
echo "测试 CityService - 城市列表(含Coworking数量)"
echo "========================================="
echo ""

# 测试 Gateway 代理接口
echo "1. 测试 Gateway 接口: GET /api/v1/home/cities-with-coworking"
echo "-----------------------------------------"
curl -X GET "http://localhost:5000/api/v1/home/cities-with-coworking?page=1&pageSize=10" \
  -H "Content-Type: application/json" \
  -w "\nHTTP Status: %{http_code}\n" \
  | jq '.'

echo ""
echo ""

# 测试 CityService 直接接口
echo "2. 测试 CityService 直接接口: GET /api/v1/cities/with-coworking-count"
echo "-----------------------------------------"
curl -X GET "http://localhost:8002/api/v1/cities/with-coworking-count?page=1&pageSize=10" \
  -H "Content-Type: application/json" \
  -w "\nHTTP Status: %{http_code}\n" \
  | jq '.'

echo ""
echo ""

# 测试第二页数据
echo "3. 测试分页 - 第2页"
echo "-----------------------------------------"
curl -X GET "http://localhost:5000/api/v1/home/cities-with-coworking?page=2&pageSize=5" \
  -H "Content-Type: application/json" \
  -w "\nHTTP Status: %{http_code}\n" \
  | jq '.'

echo ""
echo "========================================="
echo "测试完成"
echo "========================================="
