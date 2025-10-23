#!/bin/bash

# 测试 City Service 天气功能

echo "======================================"
echo "测试 City Service 天气集成"
echo "======================================"
echo ""

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# 配置
CITY_SERVICE_URL="http://localhost:8002"
API_BASE="$CITY_SERVICE_URL/api/cities"

# 检查 jq 是否安装
if ! command -v jq &> /dev/null; then
    echo -e "${YELLOW}⚠️  jq 未安装，响应将不会格式化${NC}"
    echo ""
fi

# 1. 健康检查
echo "1️⃣ 测试 City Service 健康检查..."
response=$(curl -s -w "\n%{http_code}" "$CITY_SERVICE_URL/health")
http_code=$(echo "$response" | tail -n1)
body=$(echo "$response" | sed '$d')

if [ "$http_code" = "200" ]; then
    echo -e "${GREEN}✅ City Service 运行正常${NC}"
    echo "$body" | jq '.' 2>/dev/null || echo "$body"
else
    echo -e "${RED}❌ City Service 未运行 (HTTP $http_code)${NC}"
    echo "$body"
    exit 1
fi
echo ""

# 2. 获取城市列表
echo "2️⃣ 测试获取城市列表（应包含天气数据）..."
response=$(curl -s -w "\n%{http_code}" "$API_BASE?pageNumber=1&pageSize=3")
http_code=$(echo "$response" | tail -n1)
body=$(echo "$response" | sed '$d')

if [ "$http_code" = "200" ]; then
    echo -e "${GREEN}✅ 成功获取城市列表${NC}"
    
    # 检查是否有城市数据
    city_count=$(echo "$body" | jq 'length' 2>/dev/null || echo "0")
    echo -e "城市数量: ${YELLOW}$city_count${NC}"
    
    if [ "$city_count" -gt "0" ]; then
        echo ""
        echo "第一个城市的数据："
        echo "$body" | jq '.[0]' 2>/dev/null || echo "$body" | head -n 20
        
        # 检查是否有天气数据
        has_weather=$(echo "$body" | jq '.[0].weather != null' 2>/dev/null)
        if [ "$has_weather" = "true" ]; then
            echo ""
            echo -e "${GREEN}✅ 城市包含天气信息${NC}"
            echo ""
            echo "天气详情："
            echo "$body" | jq '.[0].weather' 2>/dev/null
        else
            echo ""
            echo -e "${YELLOW}⚠️  城市不包含天气信息（可能是 API Key 未配置）${NC}"
        fi
    else
        echo -e "${YELLOW}⚠️  数据库中没有城市数据${NC}"
    fi
else
    echo -e "${RED}❌ 获取城市列表失败 (HTTP $http_code)${NC}"
    echo "$body"
fi
echo ""

# 3. 测试天气数据字段
echo "3️⃣ 检查天气数据字段完整性..."
weather_data=$(echo "$body" | jq '.[0].weather // {}' 2>/dev/null)

if [ "$weather_data" != "{}" ] && [ "$weather_data" != "null" ]; then
    echo "检查必需字段："
    
    fields=("temperature" "feelsLike" "weather" "weatherDescription" "weatherIcon" "humidity" "windSpeed" "windDirection" "pressure" "visibility" "cloudiness" "sunrise" "sunset" "updatedAt" "timestamp")
    
    for field in "${fields[@]}"; do
        value=$(echo "$weather_data" | jq -r ".$field // \"null\"" 2>/dev/null)
        if [ "$value" != "null" ]; then
            echo -e "  ${GREEN}✓${NC} $field: $value"
        else
            echo -e "  ${YELLOW}⚠${NC} $field: 缺失"
        fi
    done
else
    echo -e "${YELLOW}⚠️  无天气数据可检查${NC}"
    echo ""
    echo "可能的原因："
    echo "  1. OpenWeatherMap API Key 未配置"
    echo "  2. 城市没有经纬度信息"
    echo "  3. API 调用失败"
    echo ""
    echo "请检查 City Service 日志："
    echo "  docker logs city-service | grep -i weather"
fi
echo ""

# 4. 配置检查
echo "4️⃣ 检查天气 API 配置..."
echo ""
echo "请确认以下配置已正确设置："
echo ""
echo "文件: src/Services/CityService/CityService/appsettings.Development.json"
echo ""
cat << 'EOF'
{
  "Weather": {
    "Provider": "OpenWeatherMap",
    "ApiKey": "YOUR_ACTUAL_API_KEY_HERE",  👈 需要替换
    "BaseUrl": "https://api.openweathermap.org/data/2.5",
    "CacheDuration": "00:10:00",
    "Language": "zh_cn"
  }
}
EOF
echo ""

# 5. 提供帮助信息
echo "======================================"
echo "测试完成！"
echo "======================================"
echo ""

if [ "$has_weather" = "true" ]; then
    echo -e "${GREEN}🎉 天气功能集成成功！${NC}"
    echo ""
    echo "天气数据已成功集成到城市信息中。"
    echo "Gateway 可以直接使用这些数据展示天气信息。"
else
    echo -e "${YELLOW}⚠️  天气功能需要配置${NC}"
    echo ""
    echo "📝 配置步骤："
    echo ""
    echo "1. 获取 OpenWeatherMap API Key"
    echo "   访问: https://openweathermap.org/api"
    echo "   注册免费账号并获取 API Key"
    echo ""
    echo "2. 更新配置文件"
    echo "   编辑: src/Services/CityService/CityService/appsettings.Development.json"
    echo "   替换: \"ApiKey\": \"YOUR_ACTUAL_API_KEY_HERE\""
    echo ""
    echo "3. 重启 City Service"
    echo "   docker restart city-service"
    echo ""
    echo "详细文档: WEATHER_API_SETUP.md"
fi
echo ""
