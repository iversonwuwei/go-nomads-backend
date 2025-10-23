# 天气功能快速开始指南

## 🚀 3 步完成天气功能集成

### 步骤 1: 获取 API Key (2分钟)

1. 访问 [OpenWeatherMap](https://openweathermap.org/api)
2. 点击 "Sign Up" 注册免费账号
3. 登录后访问 [API Keys](https://home.openweathermap.org/api_keys)
4. 复制你的 API Key

### 步骤 2: 配置 API Key (1分钟)

编辑文件 `src/Services/CityService/CityService/appsettings.Development.json`:

```json
{
  "Weather": {
    "ApiKey": "粘贴你的 API Key 到这里"
  }
}
```

### 步骤 3: 重启服务 (1分钟)

```bash
# 重新构建
docker-compose build city-service

# 重启服务
docker-compose restart city-service

# 等待 5 秒
sleep 5

# 测试
./test-city-weather.sh
```

## ✅ 验证成功

运行测试脚本后，你应该看到：

```bash
✅ City Service 运行正常
✅ 成功获取城市列表
✅ 城市包含天气信息

天气详情：
{
  "temperature": 22.5,
  "feelsLike": 21.8,
  "weather": "Clouds",
  "weatherDescription": "局部多云",
  "weatherIcon": "02d",
  ...
}
```

## 🎉 完成！

现在你的 CityService 已经集成了天气功能：

- 🌡️ 实时温度数据
- ☁️ 天气状况和描述
- 💨 风速和风向
- 🌅 日出日落时间
- 📊 更多气象数据...

### 前端调用示例

```bash
# Gateway BFF 接口
curl http://localhost:5000/api/home/feed | jq '.data.cities[0].weather'

# City Service 直接接口
curl http://localhost:8002/api/cities | jq '.[0].weather'
```

## 📚 更多文档

- `WEATHER_IMPLEMENTATION_SUMMARY.md` - 完整实现总结
- `WEATHER_API_DOCUMENTATION.md` - API 详细文档
- `WEATHER_API_SETUP.md` - 详细配置指南

## ❓ 遇到问题？

### 天气数据为 null？

检查：
1. API Key 是否正确粘贴（没有空格或引号）
2. 服务是否已重启
3. 查看日志：`docker logs city-service | grep -i weather`

### API 调用失败？

确认：
1. API Key 已激活（新注册需要等待几分钟）
2. 网络可以访问 api.openweathermap.org
3. 没有超出免费额度（60次/分钟）

---

**现在就试试吧！只需 4 分钟！** 🚀
