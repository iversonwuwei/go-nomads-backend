#!/bin/bash

# 技能和兴趣爱好 API 测试脚本

BASE_URL="http://localhost:5001"
USER_ID="your-user-id-here"  # 替换为实际的用户 ID

echo "======================================"
echo "技能和兴趣爱好 API 测试"
echo "======================================"

# 颜色定义
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# ====================================
# 1. 技能相关测试
# ====================================

echo -e "\n${BLUE}1. 获取所有技能${NC}"
curl -X GET "$BASE_URL/api/v1/skills" \
  -H "Content-Type: application/json" | jq '.'

echo -e "\n${BLUE}2. 获取按类别分组的技能${NC}"
curl -X GET "$BASE_URL/api/v1/skills/by-category" \
  -H "Content-Type: application/json" | jq '.'

echo -e "\n${BLUE}3. 获取特定类别的技能 (Programming)${NC}"
curl -X GET "$BASE_URL/api/v1/skills/category/Programming" \
  -H "Content-Type: application/json" | jq '.'

echo -e "\n${BLUE}4. 获取单个技能详情${NC}"
curl -X GET "$BASE_URL/api/v1/skills/skill_javascript" \
  -H "Content-Type: application/json" | jq '.'

echo -e "\n${BLUE}5. 添加用户技能${NC}"
curl -X POST "$BASE_URL/api/v1/skills/users/$USER_ID" \
  -H "Content-Type: application/json" \
  -d '{
    "skillId": "skill_javascript",
    "proficiencyLevel": "advanced",
    "yearsOfExperience": 5
  }' | jq '.'

echo -e "\n${BLUE}6. 批量添加用户技能${NC}"
curl -X POST "$BASE_URL/api/v1/skills/users/$USER_ID/batch" \
  -H "Content-Type: application/json" \
  -d '[
    {
      "skillId": "skill_python",
      "proficiencyLevel": "intermediate",
      "yearsOfExperience": 3
    },
    {
      "skillId": "skill_react",
      "proficiencyLevel": "expert",
      "yearsOfExperience": 7
    }
  ]' | jq '.'

echo -e "\n${BLUE}7. 获取用户的所有技能${NC}"
curl -X GET "$BASE_URL/api/v1/skills/users/$USER_ID" \
  -H "Content-Type: application/json" | jq '.'

echo -e "\n${BLUE}8. 更新用户技能${NC}"
curl -X PUT "$BASE_URL/api/v1/skills/users/$USER_ID/skill_javascript" \
  -H "Content-Type: application/json" \
  -d '{
    "proficiencyLevel": "expert",
    "yearsOfExperience": 6
  }' | jq '.'

echo -e "\n${BLUE}9. 删除用户技能${NC}"
curl -X DELETE "$BASE_URL/api/v1/skills/users/$USER_ID/skill_javascript" \
  -H "Content-Type: application/json" | jq '.'

# ====================================
# 2. 兴趣相关测试
# ====================================

echo -e "\n${GREEN}======================================"
echo "兴趣爱好 API 测试"
echo -e "======================================${NC}"

echo -e "\n${BLUE}10. 获取所有兴趣${NC}"
curl -X GET "$BASE_URL/api/v1/interests" \
  -H "Content-Type: application/json" | jq '.'

echo -e "\n${BLUE}11. 获取按类别分组的兴趣${NC}"
curl -X GET "$BASE_URL/api/v1/interests/by-category" \
  -H "Content-Type: application/json" | jq '.'

echo -e "\n${BLUE}12. 获取特定类别的兴趣 (Travel)${NC}"
curl -X GET "$BASE_URL/api/v1/interests/category/Travel" \
  -H "Content-Type: application/json" | jq '.'

echo -e "\n${BLUE}13. 获取单个兴趣详情${NC}"
curl -X GET "$BASE_URL/api/v1/interests/interest_hiking" \
  -H "Content-Type: application/json" | jq '.'

echo -e "\n${BLUE}14. 添加用户兴趣${NC}"
curl -X POST "$BASE_URL/api/v1/interests/users/$USER_ID" \
  -H "Content-Type: application/json" \
  -d '{
    "interestId": "interest_hiking",
    "intensityLevel": "passionate"
  }' | jq '.'

echo -e "\n${BLUE}15. 批量添加用户兴趣${NC}"
curl -X POST "$BASE_URL/api/v1/interests/users/$USER_ID/batch" \
  -H "Content-Type: application/json" \
  -d '[
    {
      "interestId": "interest_backpacking",
      "intensityLevel": "moderate"
    },
    {
      "interestId": "interest_photography",
      "intensityLevel": "passionate"
    }
  ]' | jq '.'

echo -e "\n${BLUE}16. 获取用户的所有兴趣${NC}"
curl -X GET "$BASE_URL/api/v1/interests/users/$USER_ID" \
  -H "Content-Type: application/json" | jq '.'

echo -e "\n${BLUE}17. 更新用户兴趣${NC}"
curl -X PUT "$BASE_URL/api/v1/interests/users/$USER_ID/interest_hiking" \
  -H "Content-Type: application/json" \
  -d '{
    "intensityLevel": "moderate"
  }' | jq '.'

echo -e "\n${BLUE}18. 删除用户兴趣${NC}"
curl -X DELETE "$BASE_URL/api/v1/interests/users/$USER_ID/interest_hiking" \
  -H "Content-Type: application/json" | jq '.'

# ====================================
# 3. 带认证的当前用户测试（需要 JWT Token）
# ====================================

echo -e "\n${GREEN}======================================"
echo "当前用户 API 测试（需要认证）"
echo -e "======================================${NC}"

# 设置 JWT Token（需要先登录获取）
JWT_TOKEN="your-jwt-token-here"

echo -e "\n${BLUE}19. 获取当前用户的技能${NC}"
curl -X GET "$BASE_URL/api/v1/skills/me" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $JWT_TOKEN" | jq '.'

echo -e "\n${BLUE}20. 添加当前用户技能${NC}"
curl -X POST "$BASE_URL/api/v1/skills/me" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -d '{
    "skillId": "skill_flutter",
    "proficiencyLevel": "advanced",
    "yearsOfExperience": 4
  }' | jq '.'

echo -e "\n${BLUE}21. 获取当前用户的兴趣${NC}"
curl -X GET "$BASE_URL/api/v1/interests/me" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $JWT_TOKEN" | jq '.'

echo -e "\n${BLUE}22. 添加当前用户兴趣${NC}"
curl -X POST "$BASE_URL/api/v1/interests/me" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -d '{
    "interestId": "interest_coworking",
    "intensityLevel": "passionate"
  }' | jq '.'

echo -e "\n${GREEN}======================================"
echo "测试完成!"
echo -e "======================================${NC}"
