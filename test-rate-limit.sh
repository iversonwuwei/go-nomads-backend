#!/bin/bash

echo "======================================"
echo "测试 1: 登录接口限流（5次/分钟）"
echo "======================================"
echo "连续发送 7 次登录请求..."
for i in {1..7}; do
  response=$(curl -s -w "\nHTTP_CODE:%{http_code}" -X POST http://localhost:5000/api/users/login \
    -H "Content-Type: application/json" \
    -d '{"email":"test@example.com","password":"test123"}')
  
  http_code=$(echo "$response" | grep "HTTP_CODE:" | cut -d: -f2)
  echo "请求 $i: HTTP $http_code"
  
  if [ "$http_code" == "429" ]; then
    echo "  ✅ 触发限流！"
  fi
done

echo ""
echo "======================================"
echo "测试 2: API 接口限流（100次/分钟）"
echo "======================================"
echo "连续发送 105 次 API 请求..."
success_count=0
rate_limited_count=0

for i in {1..105}; do
  http_code=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:5000/api/users)
  if [ "$http_code" == "429" ]; then
    ((rate_limited_count++))
  else
    ((success_count++))
  fi
  
  # 每 10 次显示一次进度
  if [ $((i % 10)) -eq 0 ]; then
    echo "已发送 $i 次请求 (成功: $success_count, 限流: $rate_limited_count)"
  fi
done

echo ""
echo "总结:"
echo "  成功请求: $success_count"
echo "  限流拒绝: $rate_limited_count"
if [ $rate_limited_count -gt 0 ]; then
  echo "  ✅ API 限流正常工作！"
fi

echo ""
echo "======================================"
echo "测试 3: 健康检查（无限流）"
echo "======================================"
for i in {1..3}; do
  http_code=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:5000/health)
  echo "健康检查 $i: HTTP $http_code (应为 200)"
done
