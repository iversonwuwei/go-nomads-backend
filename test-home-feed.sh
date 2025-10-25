#!/bin/bash

# 测试 Home Feed API
# 确保 Gateway 服务在 localhost:5000 运行

echo "🧪 测试 Home Feed API"
echo "=================================="

BASE_URL="http://localhost:5000/api/v1"

echo ""
echo "📡 测试 1: 获取首页聚合数据 (默认参数)"
curl -s -X GET "${BASE_URL}/home/feed" | jq '.'

echo ""
echo "=================================="
echo "📡 测试 2: 获取首页聚合数据 (自定义限制)"
curl -s -X GET "${BASE_URL}/home/feed?cityLimit=5&meetupLimit=10" | jq '.'

echo ""
echo "=================================="
echo "📡 测试 3: 检查响应结构"
curl -s -X GET "${BASE_URL}/home/feed" | jq '{
  success: .success,
  message: .message,
  cityCount: (.data.cities | length),
  meetupCount: (.data.meetups | length),
  hasMoreCities: .data.hasMoreCities,
  hasMoreMeetups: .data.hasMoreMeetups,
  timestamp: .data.timestamp
}'

echo ""
echo "=================================="
echo "✅ 测试完成"
