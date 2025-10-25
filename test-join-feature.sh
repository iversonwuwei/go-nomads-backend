#!/bin/bash

echo "=========================================="
echo "测试 Meetup 加入功能持久化"
echo "=========================================="
echo ""

# 1. 登录获取 token
echo "1️⃣  登录获取 token..."
LOGIN_RESPONSE=$(curl -s -X POST http://localhost:5000/api/v1/users/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "walden.wuwei@gmail.com",
    "password": "walden123456"
  }')

echo "登录响应: $LOGIN_RESPONSE"
echo ""

TOKEN=$(echo $LOGIN_RESPONSE | jq -r '.data.token // empty')
USER_ID=$(echo $LOGIN_RESPONSE | jq -r '.data.userId // empty')

if [ -z "$TOKEN" ] || [ "$TOKEN" == "null" ]; then
  echo "❌ 登录失败，无法获取 token"
  exit 1
fi

echo "✅ 登录成功!"
echo "Token: ${TOKEN:0:50}..."
echo "User ID: $USER_ID"
echo ""

# 2. 获取活动列表（应该显示 isParticipant 状态）
echo "2️⃣  获取活动列表..."
EVENTS_RESPONSE=$(curl -s http://localhost:5000/api/v1/events?status=upcoming \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-User-Id: $USER_ID")

echo "活动列表响应:"
echo $EVENTS_RESPONSE | jq '.data.items[] | {id, title, isParticipant, currentParticipants}'
echo ""

# 获取第一个活动的 ID
EVENT_ID=$(echo $EVENTS_RESPONSE | jq -r '.data.items[0].id // empty')

if [ -z "$EVENT_ID" ] || [ "$EVENT_ID" == "null" ]; then
  echo "❌ 没有可用的活动"
  exit 1
fi

echo "选择的活动 ID: $EVENT_ID"
echo ""

# 3. 加入活动
echo "3️⃣  加入活动 $EVENT_ID ..."
JOIN_RESPONSE=$(curl -s -X POST "http://localhost:5000/api/v1/events/$EVENT_ID/join" \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-User-Id: $USER_ID" \
  -H "Content-Type: application/json" \
  -d '{}')

echo "加入活动响应:"
echo $JOIN_RESPONSE | jq '.'
echo ""

# 4. 再次获取活动列表，验证 isParticipant 是否为 true
echo "4️⃣  再次获取活动列表，验证参与状态..."
EVENTS_RESPONSE_AFTER=$(curl -s http://localhost:5000/api/v1/events?status=upcoming \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-User-Id: $USER_ID")

echo "更新后的活动列表:"
echo $EVENTS_RESPONSE_AFTER | jq '.data.items[] | {id, title, isParticipant, currentParticipants}'
echo ""

# 检查目标活动的 isParticipant 状态
IS_PARTICIPANT=$(echo $EVENTS_RESPONSE_AFTER | jq -r ".data.items[] | select(.id == \"$EVENT_ID\") | .isParticipant")

if [ "$IS_PARTICIPANT" == "true" ]; then
  echo "✅ 成功! 活动 $EVENT_ID 的 isParticipant = true"
else
  echo "❌ 失败! 活动 $EVENT_ID 的 isParticipant = $IS_PARTICIPANT"
fi

echo ""
echo "=========================================="
echo "测试完成"
echo "=========================================="
