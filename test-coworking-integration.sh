#!/bin/bash

# 测试 Coworking API 集成

BASE_URL="http://localhost:8006/api/v1/coworking"

echo "============================================================"
echo "  测试 Coworking API 集成"
echo "============================================================"
echo ""

# 1. 测试 GetAll (空数据)
echo "1️⃣ 测试 GetAll API (应该返回空列表)..."
curl -s "$BASE_URL?page=1&pageSize=20" | jq
echo ""

# 2. 测试 Create
echo "2️⃣ 测试 Create API..."
RESPONSE=$(curl -s -X POST "$BASE_URL" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "测试共享办公空间",
    "description": "这是一个测试的共享办公空间",
    "address": "北京市朝阳区建国路 88 号",
    "latitude": 39.9042,
    "longitude": 116.4074,
    "pricePerDay": 100.0,
    "amenities": ["wifi", "coffee", "meeting_room", "parking"],
    "imageUrl": "https://example.com/image.jpg",
    "phone": "010-12345678",
    "email": "test@example.com",
    "openingHours": "周一至周五: 9:00-18:00"
  }')

echo "$RESPONSE" | jq

# 提取创建的 ID
CREATED_ID=$(echo "$RESPONSE" | jq -r '.data.id')
echo ""
echo "创建的 ID: $CREATED_ID"
echo ""

# 3. 测试 GetById
echo "3️⃣ 测试 GetById API..."
curl -s "$BASE_URL/$CREATED_ID" | jq
echo ""

# 4. 测试 GetAll (应该有 1 条数据)
echo "4️⃣ 测试 GetAll API (应该返回 1 条记录)..."
curl -s "$BASE_URL?page=1&pageSize=20" | jq
echo ""

# 5. 测试 Update
echo "5️⃣ 测试 Update API..."
curl -s -X PUT "$BASE_URL/$CREATED_ID" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "更新后的共享办公空间",
    "description": "描述已更新",
    "address": "北京市朝阳区建国路 88 号",
    "latitude": 39.9042,
    "longitude": 116.4074,
    "pricePerDay": 150.0,
    "amenities": ["wifi", "coffee", "meeting_room", "parking", "24h_access"],
    "imageUrl": "https://example.com/image-updated.jpg",
    "phone": "010-87654321",
    "email": "updated@example.com",
    "openingHours": "周一至周日: 24小时营业"
  }' | jq
echo ""

# 6. 再次 GetById 验证更新
echo "6️⃣ 验证更新后的数据..."
curl -s "$BASE_URL/$CREATED_ID" | jq
echo ""

# 7. 测试 Delete
echo "7️⃣ 测试 Delete API..."
curl -s -X DELETE "$BASE_URL/$CREATED_ID" | jq
echo ""

# 8. 验证删除 (应该返回 404)
echo "8️⃣ 验证删除 (应该返回 404)..."
curl -s "$BASE_URL/$CREATED_ID" | jq
echo ""

# 9. 最终 GetAll (应该又是空列表)
echo "9️⃣ 最终 GetAll (应该返回空列表)..."
curl -s "$BASE_URL?page=1&pageSize=20" | jq
echo ""

echo "============================================================"
echo "  测试完成!"
echo "============================================================"
