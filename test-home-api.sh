#!/bin/bash

# 测试 HomeController API

GATEWAY_URL="http://localhost:5000"
API_BASE="$GATEWAY_URL/api/home"

echo "======================================"
echo "测试 Gateway HomeController (BFF)"
echo "======================================"
echo ""

# 1. 健康检查
echo "1️⃣ 测试健康检查..."
echo "GET $API_BASE/health"
response=$(curl -s -w "\n%{http_code}" "$API_BASE/health")
http_code=$(echo "$response" | tail -n1)
body=$(echo "$response" | head -n-1)

if [ "$http_code" = "200" ]; then
    echo "✅ 健康检查成功"
    echo "$body" | jq '.' 2>/dev/null || echo "$body"
else
    echo "❌ 健康检查失败 (HTTP $http_code)"
    echo "$body"
fi
echo ""

# 2. 获取首页聚合数据（默认参数）
echo "2️⃣ 测试首页聚合接口（默认参数）..."
echo "GET $API_BASE/feed"
response=$(curl -s -w "\n%{http_code}" "$API_BASE/feed")
http_code=$(echo "$response" | tail -n1)
body=$(echo "$response" | head -n-1)

if [ "$http_code" = "200" ]; then
    echo "✅ 首页数据加载成功"
    echo "$body" | jq '.' 2>/dev/null || echo "$body"
else
    echo "⚠️ 首页数据加载失败 (HTTP $http_code)"
    echo "$body" | jq '.' 2>/dev/null || echo "$body"
fi
echo ""

# 3. 获取首页聚合数据（自定义参数）
echo "3️⃣ 测试首页聚合接口（限制5个城市，10个活动）..."
echo "GET $API_BASE/feed?cityLimit=5&meetupLimit=10"
response=$(curl -s -w "\n%{http_code}" "$API_BASE/feed?cityLimit=5&meetupLimit=10")
http_code=$(echo "$response" | tail -n1)
body=$(echo "$response" | head -n-1)

if [ "$http_code" = "200" ]; then
    echo "✅ 首页数据加载成功（自定义参数）"
    echo "$body" | jq '.' 2>/dev/null || echo "$body"
else
    echo "⚠️ 首页数据加载失败 (HTTP $http_code)"
    echo "$body" | jq '.' 2>/dev/null || echo "$body"
fi
echo ""

# 4. 测试容错机制（后端服务不可用时）
echo "4️⃣ 测试容错机制..."
echo "说明：即使后端服务不可用，Gateway 应该返回空列表而不是错误"
echo "观察上面的响应，如果 cities 或 meetups 为空数组，说明容错机制生效 ✅"
echo ""

echo "======================================"
echo "测试完成！"
echo "======================================"
echo ""
echo "📝 预期结果："
echo "  • 健康检查返回 200 OK"
echo "  • 首页聚合接口返回统一的 ApiResponse 格式"
echo "  • success: true/false"
echo "  • data: { cities: [], meetups: [], timestamp, hasMoreCities, hasMoreMeetups }"
echo "  • 如果后端服务未启动，应返回空数组（容错机制）"
echo ""
