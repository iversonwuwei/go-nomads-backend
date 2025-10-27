#!/bin/bash

echo "==================================="
echo "测试 Coworking 批量统计 API"
echo "==================================="

# 获取城市列表
echo ""
echo "📋 步骤 1: 获取城市列表..."
CITIES_RESPONSE=$(curl -s http://localhost:5001/api/v1/cities?pageSize=10)
echo "$CITIES_RESPONSE" | jq '.'

# 提取城市 ID
CITY_IDS=$(echo "$CITIES_RESPONSE" | jq -r '.data.items[].id' | head -5 | paste -sd "," -)
echo ""
echo "✅ 提取前5个城市 ID: $CITY_IDS"

# 调用批量统计 API
echo ""
echo "📊 步骤 2: 批量获取城市 Coworking 数量..."
BATCH_RESPONSE=$(curl -s "http://localhost:5001/api/v1/coworking/count-by-cities?cityIds=$CITY_IDS")
echo "$BATCH_RESPONSE" | jq '.'

# 解析结果
echo ""
echo "✅ 步骤 3: 解析统计结果..."
echo "$BATCH_RESPONSE" | jq -r '.data | to_entries[] | "\(.key): \(.value) 个 Coworking 空间"'

# 验证性能: 获取更多城市
echo ""
echo "==================================="
echo "性能测试: 批量获取 50 个城市"
echo "==================================="

CITY_IDS_50=$(echo "$CITIES_RESPONSE" | jq -r '.data.items[].id' | paste -sd "," -)
echo "📊 测试 50 个城市批量查询..."

start_time=$(date +%s%3N)
BATCH_50=$(curl -s "http://localhost:5001/api/v1/coworking/count-by-cities?cityIds=$CITY_IDS_50")
end_time=$(date +%s%3N)

duration=$((end_time - start_time))
echo "✅ 批量查询耗时: ${duration}ms"

count=$(echo "$BATCH_50" | jq '.data | length')
echo "✅ 返回 $count 个城市的统计数据"

echo ""
echo "==================================="
echo "测试完成!"
echo "==================================="
