# 天气 API 配置指南

## 获取 OpenWeatherMap API Key

### 1. 注册账号
访问 [OpenWeatherMap](https://openweathermap.org/api) 注册免费账号。

### 2. 获取 API Key
- 登录后访问 [API Keys](https://home.openweathermap.org/api_keys)
- 复制你的 API Key

### 3. 配置 API Key

#### 方式一：直接修改配置文件（开发环境）

编辑 `src/Services/CityService/CityService/appsettings.Development.json`:

```json
{
  "Weather": {
    "ApiKey": "your_actual_api_key_here"
  }
}
```

#### 方式二：使用环境变量（生产环境推荐）

```bash
export Weather__ApiKey="your_actual_api_key_here"
```

或在 Docker Compose 中：

```yaml
services:
  city-service:
    environment:
      - Weather__ApiKey=your_actual_api_key_here
```

#### 方式三：使用 Kubernetes Secret

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: weather-api-secret
type: Opaque
stringData:
  api-key: your_actual_api_key_here
```

```yaml
apiVersion: v1
kind: Deployment
spec:
  template:
    spec:
      containers:
      - name: city-service
        env:
        - name: Weather__ApiKey
          valueFrom:
            secretKeyRef:
              name: weather-api-secret
              key: api-key
```

## 免费额度

OpenWeatherMap 免费计划：
- **60 次调用/分钟**
- **1,000,000 次调用/月**
- 3 小时预报数据
- 当前天气数据
- 5 天预报（每 3 小时）

这对于大多数应用来说已经足够！

## 其他天气 API 服务（可选）

如果需要更多额度或功能，可以考虑：

### WeatherAPI
- **免费**: 1,000,000 次/月
- **URL**: https://www.weatherapi.com/
- **优势**: 更大的免费额度

### Visual Crossing
- **免费**: 1,000 次/天
- **URL**: https://www.visualcrossing.com/
- **优势**: 历史天气数据

### Tomorrow.io
- **免费**: 500 次/天，25 次/小时
- **URL**: https://www.tomorrow.io/
- **优势**: 高精度预报

## 测试 API

配置完成后，可以测试 API 是否正常工作：

```bash
# 测试 OpenWeatherMap API（替换 YOUR_API_KEY）
curl "https://api.openweathermap.org/data/2.5/weather?q=Tokyo&appid=YOUR_API_KEY&units=metric&lang=zh_cn"
```

预期响应：
```json
{
  "coord": { "lon": 139.6917, "lat": 35.6895 },
  "weather": [
    {
      "id": 800,
      "main": "Clear",
      "description": "晴朗",
      "icon": "01d"
    }
  ],
  "main": {
    "temp": 22.5,
    "feels_like": 21.8,
    "temp_min": 20.0,
    "temp_max": 25.0,
    "pressure": 1013,
    "humidity": 65
  },
  "wind": {
    "speed": 3.5,
    "deg": 180
  },
  ...
}
```

## 缓存策略

为了避免超出免费额度，CityService 实现了以下缓存策略：

- **缓存时间**: 10 分钟（可配置）
- **缓存键**: `weather_{cityName}` 或 `weather_coord_{lat}_{lon}`
- **缓存位置**: 内存缓存（MemoryCache）

### 调整缓存时间

编辑配置文件：

```json
{
  "Weather": {
    "CacheDuration": "00:15:00"  // 15 分钟
  }
}
```

## 监控 API 使用量

在 OpenWeatherMap 控制台可以查看：
- 每日/每月调用次数
- 实时调用统计
- API 响应时间

建议设置使用量警报，避免超出免费额度。

## 故障排查

### 问题 1: 401 Unauthorized
**原因**: API Key 无效或未配置

**解决**:
1. 检查 API Key 是否正确
2. 确认 API Key 已激活（新注册需要等待几分钟）
3. 检查环境变量或配置文件

### 问题 2: 429 Too Many Requests
**原因**: 超出免费额度限制

**解决**:
1. 增加缓存时间
2. 减少请求频率
3. 升级到付费计划

### 问题 3: 天气数据为 null
**原因**: 城市名称不正确或 API 调用失败

**解决**:
1. 检查城市名称拼写
2. 使用经纬度代替城市名称（更精确）
3. 查看日志了解详细错误

## 安全建议

⚠️ **不要将 API Key 提交到 Git**

添加到 `.gitignore`:
```
appsettings.Development.json
appsettings.Production.json
*.local.json
```

使用环境变量或密钥管理服务（如 Azure Key Vault, AWS Secrets Manager）存储 API Key。

---

**配置完成后，重启 CityService 即可使用天气功能！**
