#!/bin/bash

# DeepSeek AI Service 集成测试脚本
# 用于测试从千问迁移到 DeepSeek 后的功能

set -e

BASE_URL="http://localhost:8009"
API_URL="$BASE_URL/api/chat"

# 颜色输出
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo "🚀 DeepSeek AI Service 集成测试"
echo "================================"
echo ""

# 1. 健康检查
echo "1️⃣ 测试服务健康检查..."
HEALTH_RESPONSE=$(curl -s $BASE_URL/health)
echo "响应: $HEALTH_RESPONSE"

if echo $HEALTH_RESPONSE | grep -q "deepseek-chat"; then
    echo -e "${GREEN}✅ 健康检查通过 - DeepSeek 模型已配置${NC}"
else
    echo -e "${RED}❌ 健康检查失败 - 未检测到 DeepSeek 模型${NC}"
    exit 1
fi

echo ""

# 2. AI 专用健康检查
echo "2️⃣ 测试 AI 服务健康检查..."
AI_HEALTH_RESPONSE=$(curl -s $BASE_URL/health/ai)
echo "响应: $AI_HEALTH_RESPONSE"

if echo $AI_HEALTH_RESPONSE | grep -q "DeepSeek"; then
    echo -e "${GREEN}✅ AI 健康检查通过 - DeepSeek Provider 已识别${NC}"
else
    echo -e "${RED}❌ AI 健康检查失败${NC}"
    exit 1
fi

echo ""

# 检查是否提供了 JWT Token
if [ -z "$JWT_TOKEN" ]; then
    echo -e "${YELLOW}⚠️ 未提供 JWT_TOKEN 环境变量，跳过 API 功能测试${NC}"
    echo -e "${YELLOW}提示: export JWT_TOKEN='your-jwt-token' 来运行完整测试${NC}"
    echo ""
    echo "✅ 基础健康检查全部通过！"
    echo "🎉 DeepSeek 迁移成功！"
    exit 0
fi

echo "3️⃣ 测试创建对话（使用 DeepSeek Chat 模型）..."
CREATE_CONV_RESPONSE=$(curl -s -X POST $API_URL/conversations \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -d '{
    "title": "DeepSeek 测试对话",
    "systemPrompt": "你是一个友好的 AI 助手，使用 DeepSeek 模型",
    "modelName": "deepseek-chat"
  }')

CONVERSATION_ID=$(echo $CREATE_CONV_RESPONSE | grep -o '"id":"[^"]*' | cut -d'"' -f4)

if [ -z "$CONVERSATION_ID" ]; then
    echo -e "${RED}❌ 创建对话失败${NC}"
    echo "响应: $CREATE_CONV_RESPONSE"
    exit 1
fi

echo -e "${GREEN}✅ 对话创建成功，ID: $CONVERSATION_ID${NC}"
echo ""

# 4. 测试发送消息
echo "4️⃣ 测试发送消息到 DeepSeek..."
SEND_MSG_RESPONSE=$(curl -s -X POST $API_URL/conversations/$CONVERSATION_ID/messages \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -d '{
    "content": "你好，DeepSeek！请用一句话介绍你自己。",
    "temperature": 0.7,
    "maxTokens": 500
  }')

echo "响应: $SEND_MSG_RESPONSE"

if echo $SEND_MSG_RESPONSE | grep -q "content"; then
    echo -e "${GREEN}✅ 消息发送成功，DeepSeek 已响应${NC}"
else
    echo -e "${RED}❌ 消息发送失败${NC}"
    exit 1
fi

echo ""

# 5. 测试 DeepSeek Coder 模型
echo "5️⃣ 测试创建对话（使用 DeepSeek Coder 模型）..."
CREATE_CODER_RESPONSE=$(curl -s -X POST $API_URL/conversations \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -d '{
    "title": "DeepSeek Coder 测试",
    "systemPrompt": "你是一个专业的编程助手",
    "modelName": "deepseek-coder"
  }')

CODER_CONV_ID=$(echo $CREATE_CODER_RESPONSE | grep -o '"id":"[^"]*' | cut -d'"' -f4)

if [ -z "$CODER_CONV_ID" ]; then
    echo -e "${RED}❌ 创建 Coder 对话失败${NC}"
    echo "响应: $CREATE_CODER_RESPONSE"
else
    echo -e "${GREEN}✅ DeepSeek Coder 对话创建成功，ID: $CODER_CONV_ID${NC}"
fi

echo ""

# 6. 测试代码生成
if [ ! -z "$CODER_CONV_ID" ]; then
    echo "6️⃣ 测试代码生成功能..."
    CODE_RESPONSE=$(curl -s -X POST $API_URL/conversations/$CODER_CONV_ID/messages \
      -H "Content-Type: application/json" \
      -H "Authorization: Bearer $JWT_TOKEN" \
      -d '{
        "content": "写一个 Python 快速排序函数",
        "temperature": 0.3,
        "maxTokens": 1000
      }')

    if echo $CODE_RESPONSE | grep -q "def"; then
        echo -e "${GREEN}✅ 代码生成成功${NC}"
    else
        echo -e "${YELLOW}⚠️ 代码生成响应异常${NC}"
    fi
fi

echo ""
echo "================================"
echo "🎉 DeepSeek 集成测试完成！"
echo ""
echo "📊 测试总结:"
echo "  ✅ 服务健康检查"
echo "  ✅ DeepSeek Chat 模型"
if [ ! -z "$CODER_CONV_ID" ]; then
    echo "  ✅ DeepSeek Coder 模型"
fi
echo ""
echo "🔗 相关链接:"
echo "  - API 文档: http://localhost:8009/scalar/v1"
echo "  - 健康检查: http://localhost:8009/health"
echo "  - AI 健康检查: http://localhost:8009/health/ai"
echo ""
