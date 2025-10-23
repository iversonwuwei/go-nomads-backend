# 城市天气信息 API 响应示例

## API 端点
```
GET /api/home/feed
```

## 响应结构

### 完整响应示例

```json
{
  "success": true,
  "message": "首页数据加载成功",
  "data": {
    "cities": [
      {
        "id": "tokyo-japan",
        "name": "Tokyo",
        "country": "Japan",
        "imageUrl": "https://example.com/tokyo.jpg",
        "description": "东京是日本的首都和最大城市",
        "meetupCount": 156,
        "weather": {
          "temperature": 22.5,
          "feelsLike": 21.8,
          "tempMin": 20.0,
          "tempMax": 25.0,
          "weather": "Clouds",
          "weatherDescription": "局部多云",
          "weatherIcon": "02d",
          "humidity": 65,
          "windSpeed": 3.5,
          "windDirection": 180,
          "windDirectionDescription": "南风",
          "windGust": 5.2,
          "pressure": 1013,
          "seaLevelPressure": 1013,
          "visibility": 10000,
          "cloudiness": 40,
          "rain1h": null,
          "rain3h": null,
          "snow1h": null,
          "snow3h": null,
          "sunrise": "2025-10-23T05:30:00Z",
          "sunset": "2025-10-23T17:45:00Z",
          "timezoneOffset": 32400,
          "uvIndex": 6.5,
          "airQualityIndex": 45,
          "dataSource": "OpenWeatherMap",
          "updatedAt": "2025-10-23T08:00:00Z",
          "timestamp": "2025-10-23T08:00:00Z"
        }
      },
      {
        "id": "london-uk",
        "name": "London",
        "country": "United Kingdom",
        "imageUrl": "https://example.com/london.jpg",
        "description": "伦敦是英国的首都",
        "meetupCount": 203,
        "weather": {
          "temperature": 15.2,
          "feelsLike": 14.5,
          "tempMin": 13.0,
          "tempMax": 17.0,
          "weather": "Rain",
          "weatherDescription": "小雨",
          "weatherIcon": "10d",
          "humidity": 80,
          "windSpeed": 5.2,
          "windDirection": 270,
          "windDirectionDescription": "西风",
          "windGust": 8.5,
          "pressure": 1008,
          "seaLevelPressure": 1008,
          "visibility": 8000,
          "cloudiness": 75,
          "rain1h": 2.5,
          "rain3h": 5.0,
          "snow1h": null,
          "snow3h": null,
          "sunrise": "2025-10-23T06:45:00Z",
          "sunset": "2025-10-23T18:30:00Z",
          "timezoneOffset": 0,
          "uvIndex": 2.0,
          "airQualityIndex": 35,
          "dataSource": "OpenWeatherMap",
          "updatedAt": "2025-10-23T08:00:00Z",
          "timestamp": "2025-10-23T08:00:00Z"
        }
      },
      {
        "id": "beijing-china",
        "name": "Beijing",
        "country": "China",
        "imageUrl": "https://example.com/beijing.jpg",
        "description": "北京是中国的首都",
        "meetupCount": 89,
        "weather": {
          "temperature": 18.0,
          "feelsLike": 17.2,
          "tempMin": 15.0,
          "tempMax": 20.0,
          "weather": "Clear",
          "weatherDescription": "晴朗",
          "weatherIcon": "01d",
          "humidity": 45,
          "windSpeed": 2.5,
          "windDirection": 90,
          "windDirectionDescription": "东风",
          "windGust": null,
          "pressure": 1015,
          "seaLevelPressure": 1015,
          "visibility": 10000,
          "cloudiness": 0,
          "rain1h": null,
          "rain3h": null,
          "snow1h": null,
          "snow3h": null,
          "sunrise": "2025-10-23T06:00:00Z",
          "sunset": "2025-10-23T17:30:00Z",
          "timezoneOffset": 28800,
          "uvIndex": 7.5,
          "airQualityIndex": 85,
          "dataSource": "OpenWeatherMap",
          "updatedAt": "2025-10-23T08:00:00Z",
          "timestamp": "2025-10-23T08:00:00Z"
        }
      }
    ],
    "meetups": [ /* ... */ ],
    "timestamp": "2025-10-23T08:00:00Z",
    "hasMoreCities": true,
    "hasMoreMeetups": true
  },
  "errors": []
}
```

## 天气字段说明

### 核心字段（必填）

| 字段 | 类型 | 说明 | 示例 |
|------|------|------|------|
| `temperature` | decimal | 当前温度（摄氏度） | `22.5` |
| `feelsLike` | decimal | 体感温度（摄氏度） | `21.8` |
| `weather` | string | 天气状况代码 | `Clear`, `Clouds`, `Rain`, `Snow` |
| `weatherDescription` | string | 天气详细描述 | `晴朗`, `局部多云`, `小雨` |
| `weatherIcon` | string | 天气图标代码 | `01d`, `02n`, `10d` |
| `humidity` | int | 湿度百分比 (0-100) | `65` |
| `windSpeed` | decimal | 风速（米/秒） | `3.5` |
| `windDirection` | int | 风向（度数，0-360） | `180` (南风) |
| `pressure` | int | 气压（百帕） | `1013` |
| `visibility` | int | 能见度（米） | `10000` |
| `cloudiness` | int | 云量百分比 (0-100) | `40` |
| `sunrise` | DateTime | 日出时间（UTC） | `2025-10-23T05:30:00Z` |
| `sunset` | DateTime | 日落时间（UTC） | `2025-10-23T17:45:00Z` |
| `updatedAt` | DateTime | 数据更新时间（UTC） | `2025-10-23T08:00:00Z` |
| `timestamp` | DateTime | 数据时间戳（UTC） | `2025-10-23T08:00:00Z` |

### 扩展字段（可选）

| 字段 | 类型 | 说明 | 示例 |
|------|------|------|------|
| `tempMin` | decimal? | 最低温度 | `20.0` |
| `tempMax` | decimal? | 最高温度 | `25.0` |
| `windDirectionDescription` | string? | 风向描述 | `南风`, `东北风` |
| `windGust` | decimal? | 阵风速度（米/秒） | `5.2` |
| `seaLevelPressure` | int? | 海平面气压（百帕） | `1013` |
| `groundLevelPressure` | int? | 地面气压（百帕） | `1010` |
| `rain1h` | decimal? | 过去1小时降雨量（毫米） | `2.5` |
| `rain3h` | decimal? | 过去3小时降雨量（毫米） | `5.0` |
| `snow1h` | decimal? | 过去1小时降雪量（毫米） | `0.5` |
| `snow3h` | decimal? | 过去3小时降雪量（毫米） | `1.2` |
| `timezoneOffset` | int? | 时区偏移（秒） | `32400` (UTC+9) |
| `uvIndex` | decimal? | UV 指数 | `6.5` |
| `airQualityIndex` | int? | 空气质量指数 | `45` (优良) |
| `dataSource` | string? | 数据来源 | `OpenWeatherMap` |

## 天气图标代码参考

### 日间（d = day）
- `01d` - 晴朗 ☀️
- `02d` - 少云 🌤️
- `03d` - 多云 ☁️
- `04d` - 阴天 ☁️☁️
- `09d` - 阵雨 🌧️
- `10d` - 雨 🌦️
- `11d` - 雷暴 ⛈️
- `13d` - 雪 ❄️
- `50d` - 雾 🌫️

### 夜间（n = night）
- `01n` - 晴朗 🌙
- `02n` - 少云 ☁️🌙
- `03n` - 多云 ☁️
- `04n` - 阴天 ☁️☁️
- `09n` - 阵雨 🌧️
- `10n` - 雨 🌧️
- `11n` - 雷暴 ⛈️
- `13n` - 雪 ❄️
- `50n` - 雾 🌫️

## 天气状况代码

| 代码 | 说明 | 中文 |
|------|------|------|
| `Clear` | Clear sky | 晴朗 |
| `Clouds` | Cloudy | 多云 |
| `Rain` | Rain | 雨 |
| `Drizzle` | Drizzle | 毛毛雨 |
| `Thunderstorm` | Thunderstorm | 雷暴 |
| `Snow` | Snow | 雪 |
| `Mist` | Mist | 薄雾 |
| `Smoke` | Smoke | 烟雾 |
| `Haze` | Haze | 霾 |
| `Dust` | Dust | 尘 |
| `Fog` | Fog | 雾 |
| `Sand` | Sand | 沙尘 |
| `Ash` | Volcanic ash | 火山灰 |
| `Squall` | Squall | 飑 |
| `Tornado` | Tornado | 龙卷风 |

## 风向参考

| 度数范围 | 风向 | 英文 |
|---------|------|------|
| 0° | 北风 | North |
| 45° | 东北风 | Northeast |
| 90° | 东风 | East |
| 135° | 东南风 | Southeast |
| 180° | 南风 | South |
| 225° | 西南风 | Southwest |
| 270° | 西风 | West |
| 315° | 西北风 | Northwest |
| 360° | 北风 | North |

## 空气质量指数 (AQI) 参考

| AQI 范围 | 等级 | 健康影响 |
|---------|------|----------|
| 0-50 | 优 | 空气质量令人满意，基本无空气污染 |
| 51-100 | 良 | 空气质量可接受，但某些污染物可能对极少数异常敏感人群健康有较弱影响 |
| 101-150 | 轻度污染 | 易感人群症状有轻度加剧，健康人群出现刺激症状 |
| 151-200 | 中度污染 | 进一步加剧易感人群症状，可能对健康人群心脏、呼吸系统有影响 |
| 201-300 | 重度污染 | 心脏病和肺病患者症状显著加剧，运动耐受力降低 |
| 300+ | 严重污染 | 健康人群运动耐受力降低，有明显强烈症状 |

## UV 指数参考

| UV 指数 | 等级 | 防护建议 |
|---------|------|----------|
| 0-2 | 低 | 无需特殊防护 |
| 3-5 | 中等 | 需要防晒 |
| 6-7 | 高 | 必须防晒 |
| 8-10 | 很高 | 额外防护措施 |
| 11+ | 极高 | 避免外出 |

## 前端使用示例

### React 组件示例

```tsx
interface CityCardProps {
  city: CityDto;
}

function CityCard({ city }: CityCardProps) {
  const weather = city.weather;
  
  if (!weather) {
    return <div>天气数据加载中...</div>;
  }

  return (
    <div className="city-card">
      <h2>{city.name}, {city.country}</h2>
      <img src={city.imageUrl} alt={city.name} />
      
      <div className="weather-section">
        <div className="temperature">
          <span className="temp-value">{Math.round(weather.temperature)}°C</span>
          <span className="feels-like">体感 {Math.round(weather.feelsLike)}°C</span>
        </div>
        
        <div className="weather-icon">
          <img 
            src={`https://openweathermap.org/img/wn/${weather.weatherIcon}@2x.png`}
            alt={weather.weatherDescription}
          />
          <span>{weather.weatherDescription}</span>
        </div>
        
        <div className="weather-details">
          <div>💧 湿度: {weather.humidity}%</div>
          <div>💨 风速: {weather.windSpeed} m/s {weather.windDirectionDescription}</div>
          <div>👁️ 能见度: {(weather.visibility / 1000).toFixed(1)} km</div>
          <div>☁️ 云量: {weather.cloudiness}%</div>
          {weather.uvIndex && <div>☀️ UV: {weather.uvIndex}</div>}
          {weather.airQualityIndex && (
            <div>🌫️ AQI: {weather.airQualityIndex}</div>
          )}
        </div>
        
        <div className="sun-times">
          <div>🌅 日出: {new Date(weather.sunrise).toLocaleTimeString()}</div>
          <div>🌇 日落: {new Date(weather.sunset).toLocaleTimeString()}</div>
        </div>
      </div>
      
      <div className="meetup-count">
        📅 {city.meetupCount} 个活动
      </div>
    </div>
  );
}
```

### 温度单位转换工具

```typescript
// 摄氏度转华氏度
function celsiusToFahrenheit(celsius: number): number {
  return (celsius * 9/5) + 32;
}

// 风速单位转换
function mpsToKmh(mps: number): number {
  return mps * 3.6;
}

function mpsToMph(mps: number): number {
  return mps * 2.237;
}

// 风向角度转文字描述
function getWindDirection(degrees: number): string {
  const directions = ['北', '东北', '东', '东南', '南', '西南', '西', '西北'];
  const index = Math.round(((degrees % 360) / 45)) % 8;
  return directions[index] + '风';
}

// 获取空气质量等级
function getAQILevel(aqi: number): { level: string; color: string } {
  if (aqi <= 50) return { level: '优', color: 'green' };
  if (aqi <= 100) return { level: '良', color: 'yellow' };
  if (aqi <= 150) return { level: '轻度污染', color: 'orange' };
  if (aqi <= 200) return { level: '中度污染', color: 'red' };
  if (aqi <= 300) return { level: '重度污染', color: 'purple' };
  return { level: '严重污染', color: 'maroon' };
}
```

## 后端集成建议

### 1. 天气数据来源

推荐使用以下天气 API 服务：

- **OpenWeatherMap** - 免费额度 60 次/分钟
- **WeatherAPI** - 免费额度 100万次/月
- **Visual Crossing** - 免费额度 1000次/天
- **Tomorrow.io** - 高精度天气预报

### 2. 数据缓存策略

```csharp
// 建议缓存时间: 10-30 分钟
[ResponseCache(Duration = 600)] // 10 分钟
public async Task<ActionResult<ApiResponse<List<CityDto>>>> GetCities()
{
    // 实现逻辑
}
```

### 3. 数据更新频率

- **实时天气**: 每 10-15 分钟更新
- **天气预报**: 每 1-3 小时更新
- **日出日落**: 每天更新一次
- **空气质量**: 每 30-60 分钟更新

---

**创建时间**: 2025-10-23  
**版本**: 1.0
