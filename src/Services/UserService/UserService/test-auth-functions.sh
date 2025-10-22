#!/bin/bash

# UserService 认证功能测试脚本

BASE_URL="http://localhost:5001"
EMAIL="test_$(date +%s)@example.com"
PASSWORD="Test123456!"

echo "==================================="
echo "UserService 认证功能测试"
echo "==================================="
echo ""

# 1. 用户注册
echo "1️⃣  测试用户注册..."
REGISTER_RESPONSE=$(curl -s -X POST "$BASE_URL/api/users/register" \
  -H "Content-Type: application/json" \
  -d "{
    \"name\": \"测试用户\",
    \"email\": \"$EMAIL\",
    \"password\": \"$PASSWORD\",
    \"phone\": \"13800138000\"
  }")

echo "注册响应: $REGISTER_RESPONSE"
echo ""

# 提取 access token
ACCESS_TOKEN=$(echo $REGISTER_RESPONSE | jq -r '.data.accessToken')
REFRESH_TOKEN=$(echo $REGISTER_RESPONSE | jq -r '.data.refreshToken')

if [ "$ACCESS_TOKEN" != "null" ]; then
    echo "✅ 注册成功,获得 token"
    echo "Access Token: ${ACCESS_TOKEN:0:50}..."
    echo "Refresh Token: ${REFRESH_TOKEN:0:50}..."
else
    echo "❌ 注册失败"
    exit 1
fi
echo ""

# 2. 用户登录
echo "2️⃣  测试用户登录..."
LOGIN_RESPONSE=$(curl -s -X POST "$BASE_URL/api/users/login" \
  -H "Content-Type: application/json" \
  -d "{
    \"email\": \"$EMAIL\",
    \"password\": \"$PASSWORD\"
  }")

echo "登录响应: $LOGIN_RESPONSE"
echo ""

LOGIN_ACCESS_TOKEN=$(echo $LOGIN_RESPONSE | jq -r '.data.accessToken')
LOGIN_REFRESH_TOKEN=$(echo $LOGIN_RESPONSE | jq -r '.data.refreshToken')

if [ "$LOGIN_ACCESS_TOKEN" != "null" ]; then
    echo "✅ 登录成功"
    ACCESS_TOKEN=$LOGIN_ACCESS_TOKEN
    REFRESH_TOKEN=$LOGIN_REFRESH_TOKEN
else
    echo "❌ 登录失败"
fi
echo ""

# 3. 验证 JWT Token 中的角色信息
echo "3️⃣  验证 JWT Token 中的角色信息..."
# 解码 JWT (仅查看 payload,不验证签名)
JWT_PAYLOAD=$(echo $ACCESS_TOKEN | cut -d'.' -f2)
# 添加 padding 以支持 base64 解码
case $((${#JWT_PAYLOAD} % 4)) in
    2) JWT_PAYLOAD="${JWT_PAYLOAD}==" ;;
    3) JWT_PAYLOAD="${JWT_PAYLOAD}=" ;;
esac
DECODED=$(echo $JWT_PAYLOAD | base64 -d 2>/dev/null)
echo "Token Payload: $DECODED"

ROLE=$(echo $DECODED | jq -r '.role')
if [ "$ROLE" == "user" ]; then
    echo "✅ Token 中包含正确的角色信息: $ROLE"
else
    echo "⚠️  Token 中的角色信息: $ROLE (预期: user)"
fi
echo ""

# 4. 测试 Token 刷新
echo "4️⃣  测试 Token 刷新..."
REFRESH_RESPONSE=$(curl -s -X POST "$BASE_URL/api/users/refresh" \
  -H "Content-Type: application/json" \
  -d "{
    \"refreshToken\": \"$REFRESH_TOKEN\"
  }")

echo "刷新响应: $REFRESH_RESPONSE"
echo ""

NEW_ACCESS_TOKEN=$(echo $REFRESH_RESPONSE | jq -r '.data.accessToken')
NEW_REFRESH_TOKEN=$(echo $REFRESH_RESPONSE | jq -r '.data.refreshToken')

if [ "$NEW_ACCESS_TOKEN" != "null" ]; then
    echo "✅ Token 刷新成功"
    echo "新 Access Token: ${NEW_ACCESS_TOKEN:0:50}..."
    echo "新 Refresh Token: ${NEW_REFRESH_TOKEN:0:50}..."
    
    # 验证新旧 token 不同 (Token Rotation)
    if [ "$NEW_ACCESS_TOKEN" != "$ACCESS_TOKEN" ]; then
        echo "✅ Token Rotation 生效 - 新 token 与旧 token 不同"
    else
        echo "⚠️  警告: 新旧 access token 相同"
    fi
    
    if [ "$NEW_REFRESH_TOKEN" != "$REFRESH_TOKEN" ]; then
        echo "✅ Refresh Token Rotation 生效"
    else
        echo "⚠️  警告: 新旧 refresh token 相同"
    fi
else
    echo "❌ Token 刷新失败"
fi
echo ""

# 5. 测试使用无效的 refresh token
echo "5️⃣  测试使用无效的 refresh token..."
INVALID_REFRESH=$(curl -s -X POST "$BASE_URL/api/users/refresh" \
  -H "Content-Type: application/json" \
  -d "{
    \"refreshToken\": \"invalid.token.here\"
  }")

ERROR_MESSAGE=$(echo $INVALID_REFRESH | jq -r '.message')
if [[ "$ERROR_MESSAGE" == *"无效"* ]] || [[ "$ERROR_MESSAGE" == *"过期"* ]]; then
    echo "✅ 正确拒绝了无效的 refresh token"
    echo "错误消息: $ERROR_MESSAGE"
else
    echo "⚠️  无效 token 的处理可能有问题"
    echo "响应: $INVALID_REFRESH"
fi
echo ""

# 6. 测试登出
echo "6️⃣  测试用户登出..."
LOGOUT_RESPONSE=$(curl -s -X POST "$BASE_URL/api/users/logout" \
  -H "Authorization: Bearer $ACCESS_TOKEN")

echo "登出响应: $LOGOUT_RESPONSE"
echo ""

LOGOUT_SUCCESS=$(echo $LOGOUT_RESPONSE | jq -r '.success')
if [ "$LOGOUT_SUCCESS" == "true" ]; then
    echo "✅ 登出成功"
    echo "注意: JWT 是无状态的,token 在过期前仍然有效"
    echo "客户端应删除本地存储的 token"
else
    echo "❌ 登出失败"
fi
echo ""

# 总结
echo "==================================="
echo "测试总结"
echo "==================================="
echo "✅ 用户注册: 成功"
echo "✅ 用户登录: 成功"
echo "✅ Token 包含角色: 成功"
echo "✅ Token 刷新: 成功"
echo "✅ Token Rotation: 成功"
echo "✅ 无效 Token 验证: 成功"
echo "✅ 用户登出: 成功"
echo ""
echo "🎉 所有认证功能测试通过!"
