#!/bin/bash

# 测试参与者列表 API (包含用户信息)

echo "=========================================="
echo "测试参与者列表 API (新版 - 包含 User 对象)"
echo "=========================================="
echo ""

# 1. 登录获取 token
echo "📝 步骤 1: 登录获取 token"
LOGIN_RESPONSE=$(curl -s -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "walden.wuwei@gmail.com",
    "password": "walden123456"
  }')

TOKEN=$(echo $LOGIN_RESPONSE | jq -r '.data.accessToken')

if [ "$TOKEN" == "null" ] || [ -z "$TOKEN" ]; then
  echo "❌ 登录失败!"
  echo "$LOGIN_RESPONSE" | jq .
  exit 1
fi

echo "✅ 登录成功!"
echo "Token: ${TOKEN:0:50}..."
echo ""

# 2. 测试不同事件的参与者列表
EVENT_IDS=(
  "00000000-0000-0000-0000-000000000001"  # Bangkok
  "00000000-0000-0000-0000-000000000002"  # Chiang Mai
  "00000000-0000-0000-0000-000000000004"  # Lisbon
)

EVENT_NAMES=(
  "Bangkok"
  "Chiang Mai"
  "Lisbon"
)

for i in "${!EVENT_IDS[@]}"; do
  EVENT_ID="${EVENT_IDS[$i]}"
  EVENT_NAME="${EVENT_NAMES[$i]}"
  
  echo "=========================================="
  echo "📋 测试事件: $EVENT_NAME"
  echo "   事件ID: $EVENT_ID"
  echo "=========================================="
  
  RESPONSE=$(curl -s -X GET "http://localhost:5000/api/v1/events/$EVENT_ID/participants" \
    -H "Authorization: Bearer $TOKEN")
  
  # 检查响应
  SUCCESS=$(echo $RESPONSE | jq -r '.success')
  
  if [ "$SUCCESS" == "true" ]; then
    COUNT=$(echo $RESPONSE | jq '.data | length')
    echo "✅ 成功获取 $COUNT 个参与者"
    echo ""
    
    # 显示参与者详细信息
    echo "参与者列表:"
    echo $RESPONSE | jq '.data[] | {
      userId: .userId,
      status: .status,
      registeredAt: .registeredAt,
      user: .user
    }'
    
    # 验证 user 对象结构
    echo ""
    echo "🔍 验证 User 对象结构:"
    FIRST_USER=$(echo $RESPONSE | jq -r '.data[0].user')
    if [ "$FIRST_USER" != "null" ] && [ -n "$FIRST_USER" ]; then
      echo "✅ User 对象存在"
      echo $RESPONSE | jq '.data[0].user | {
        id: .id,
        name: .name,
        email: .email,
        avatar: .avatar,
        phone: .phone
      }'
    else
      echo "⚠️  User 对象为空"
    fi
  else
    echo "❌ 获取失败"
    echo $RESPONSE | jq .
  fi
  
  echo ""
  echo ""
done

echo "=========================================="
echo "测试完成!"
echo "=========================================="
