# 城市天气信息功能更新

## 📋 更新概述

为 `CityDto` 添加了完整的天气信息字段，支持显示城市当前的温度、天气状况、风速、湿度等详细信息。

## 🎯 更新内容

### 1. 新增 `WeatherDto` 类

**文件**: `src/Gateway/Gateway/DTOs/WeatherDto.cs`

创建了独立的天气信息 DTO，包含以下字段分组：

#### 温度信息
- `Temperature` - 当前温度（摄氏度）
- `FeelsLike` - 体感温度
- `TempMin` - 最低温度
- `TempMax` - 最高温度

#### 天气状况
- `Weather` - 天气状况代码（Clear, Clouds, Rain 等）
- `WeatherDescription` - 详细描述（晴朗、多云、小雨等）
- `WeatherIcon` - 天气图标代码（01d, 02n 等）

#### 风力信息
- `WindSpeed` - 风速（米/秒）
- `WindDirection` - 风向（度数 0-360）
- `WindDirectionDescription` - 风向描述（北风、东南风等）
- `WindGust` - 阵风速度

#### 大气信息
- `Humidity` - 湿度百分比
- `Pressure` - 气压
- `SeaLevelPressure` - 海平面气压
- `GroundLevelPressure` - 地面气压
- `Visibility` - 能见度
- `Cloudiness` - 云量百分比

#### 降水信息
- `Rain1h` / `Rain3h` - 降雨量
- `Snow1h` / `Snow3h` - 降雪量

#### 天文信息
- `Sunrise` - 日出时间
- `Sunset` - 日落时间
- `TimezoneOffset` - 时区偏移

#### 空气质量与UV
- `UvIndex` - UV 紫外线指数
- `AirQualityIndex` - 空气质量指数

#### 元数据
- `DataSource` - 数据来源（OpenWeatherMap 等）
- `UpdatedAt` - 数据更新时间
- `Timestamp` - 数据时间戳

### 2. 更新 `CityDto` 类

**文件**: `src/Gateway/Gateway/DTOs/CityDto.cs`

添加了天气信息字段：

```csharp
/// <summary>
/// 当前天气信息
/// </summary>
public WeatherDto? Weather { get; set; }
```

## 📊 数据结构

### 完整的 CityDto 结构

```json
{
  "id": "tokyo-japan",
  "name": "Tokyo",
  "country": "Japan",
  "imageUrl": "https://example.com/tokyo.jpg",
  "description": "东京是日本的首都",
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
    "sunrise": "2025-10-23T05:30:00Z",
    "sunset": "2025-10-23T17:45:00Z",
    "timezoneOffset": 32400,
    "uvIndex": 6.5,
    "airQualityIndex": 45,
    "dataSource": "OpenWeatherMap",
    "updatedAt": "2025-10-23T08:00:00Z",
    "timestamp": "2025-10-23T08:00:00Z"
  }
}
```

## 🔧 后端实现建议

### 1. 天气 API 集成

推荐使用以下天气服务：

#### OpenWeatherMap（推荐）
```bash
# 免费额度: 60次/分钟, 1百万次/月
https://api.openweathermap.org/data/2.5/weather?q={city}&appid={API_KEY}&units=metric&lang=zh_cn
```

#### WeatherAPI
```bash
# 免费额度: 1百万次/月
https://api.weatherapi.com/v1/current.json?key={API_KEY}&q={city}&aqi=yes
```

### 2. 创建天气服务

在 `city-service` 中添加天气集成：

```csharp
// Services/WeatherService.cs
public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;

    public async Task<WeatherDto?> GetWeatherByCityAsync(string cityName)
    {
        // 1. 检查缓存（10分钟）
        var cacheKey = $"weather_{cityName}";
        if (_cache.TryGetValue(cacheKey, out WeatherDto? cachedWeather))
        {
            return cachedWeather;
        }

        // 2. 调用天气 API
        var apiKey = _configuration["Weather:ApiKey"];
        var url = $"https://api.openweathermap.org/data/2.5/weather?q={cityName}&appid={apiKey}&units=metric&lang=zh_cn";
        
        var response = await _httpClient.GetFromJsonAsync<OpenWeatherMapResponse>(url);
        
        if (response == null) return null;

        // 3. 转换为 WeatherDto
        var weather = new WeatherDto
        {
            Temperature = response.Main.Temp,
            FeelsLike = response.Main.FeelsLike,
            TempMin = response.Main.TempMin,
            TempMax = response.Main.TempMax,
            Weather = response.Weather[0].Main,
            WeatherDescription = response.Weather[0].Description,
            WeatherIcon = response.Weather[0].Icon,
            Humidity = response.Main.Humidity,
            WindSpeed = response.Wind.Speed,
            WindDirection = response.Wind.Deg,
            WindDirectionDescription = GetWindDirectionDescription(response.Wind.Deg),
            WindGust = response.Wind.Gust,
            Pressure = response.Main.Pressure,
            SeaLevelPressure = response.Main.SeaLevel,
            GroundLevelPressure = response.Main.GrndLevel,
            Visibility = response.Visibility,
            Cloudiness = response.Clouds.All,
            Sunrise = DateTimeOffset.FromUnixTimeSeconds(response.Sys.Sunrise).UtcDateTime,
            Sunset = DateTimeOffset.FromUnixTimeSeconds(response.Sys.Sunset).UtcDateTime,
            TimezoneOffset = response.Timezone,
            DataSource = "OpenWeatherMap",
            UpdatedAt = DateTime.UtcNow,
            Timestamp = DateTimeOffset.FromUnixTimeSeconds(response.Dt).UtcDateTime
        };

        // 4. 缓存结果（10分钟）
        _cache.Set(cacheKey, weather, TimeSpan.FromMinutes(10));

        return weather;
    }

    private string GetWindDirectionDescription(int degrees)
    {
        var directions = new[] { "北风", "东北风", "东风", "东南风", "南风", "西南风", "西风", "西北风" };
        var index = (int)Math.Round(((degrees % 360) / 45.0)) % 8;
        return directions[index];
    }
}
```

### 3. 更新 CityService

在获取城市列表时加载天气数据：

```csharp
// Services/CityService.cs
public async Task<List<CityDto>> GetCitiesWithWeatherAsync(int limit)
{
    var cities = await _cityRepository.GetCitiesAsync(limit);
    
    // 并行获取天气数据
    var weatherTasks = cities.Select(async city =>
    {
        city.Weather = await _weatherService.GetWeatherByCityAsync(city.Name);
        return city;
    });

    return await Task.WhenAll(weatherTasks).ToList();
}
```

### 4. 配置天气 API Key

在 `appsettings.json` 中添加：

```json
{
  "Weather": {
    "Provider": "OpenWeatherMap",
    "ApiKey": "your_api_key_here",
    "BaseUrl": "https://api.openweathermap.org/data/2.5",
    "CacheDuration": "00:10:00",
    "Language": "zh_cn"
  }
}
```

## 📱 前端集成示例

### React 城市卡片组件

```tsx
import React from 'react';
import { CityDto, WeatherDto } from '@/types';

interface CityCardProps {
  city: CityDto;
}

export function CityCard({ city }: CityCardProps) {
  const { weather } = city;

  return (
    <div className="city-card">
      {/* 城市信息 */}
      <div className="city-header">
        <img src={city.imageUrl} alt={city.name} />
        <div className="city-info">
          <h2>{city.name}</h2>
          <p>{city.country}</p>
        </div>
      </div>

      {/* 天气信息 */}
      {weather && (
        <div className="weather-section">
          {/* 主要温度显示 */}
          <div className="temperature-main">
            <div className="temp-value">
              {Math.round(weather.temperature)}°
            </div>
            <div className="weather-icon">
              <img 
                src={`https://openweathermap.org/img/wn/${weather.weatherIcon}@2x.png`}
                alt={weather.weatherDescription}
              />
            </div>
          </div>

          {/* 天气描述 */}
          <div className="weather-description">
            {weather.weatherDescription}
          </div>
          <div className="feels-like">
            体感 {Math.round(weather.feelsLike)}°
          </div>

          {/* 详细信息 */}
          <div className="weather-details">
            <WeatherDetail 
              icon="💧" 
              label="湿度" 
              value={`${weather.humidity}%`} 
            />
            <WeatherDetail 
              icon="💨" 
              label="风速" 
              value={`${weather.windSpeed} m/s`} 
            />
            <WeatherDetail 
              icon="👁️" 
              label="能见度" 
              value={`${(weather.visibility / 1000).toFixed(1)} km`} 
            />
            {weather.uvIndex && (
              <WeatherDetail 
                icon="☀️" 
                label="UV" 
                value={weather.uvIndex.toString()} 
              />
            )}
            {weather.airQualityIndex && (
              <WeatherDetail 
                icon="🌫️" 
                label="AQI" 
                value={getAQILevel(weather.airQualityIndex)} 
              />
            )}
          </div>
        </div>
      )}

      {/* 活动数量 */}
      <div className="meetup-count">
        📅 {city.meetupCount} 个活动
      </div>
    </div>
  );
}

function WeatherDetail({ icon, label, value }: { icon: string; label: string; value: string }) {
  return (
    <div className="weather-detail-item">
      <span className="icon">{icon}</span>
      <span className="label">{label}:</span>
      <span className="value">{value}</span>
    </div>
  );
}

function getAQILevel(aqi: number): string {
  if (aqi <= 50) return '优';
  if (aqi <= 100) return '良';
  if (aqi <= 150) return '轻度';
  if (aqi <= 200) return '中度';
  if (aqi <= 300) return '重度';
  return '严重';
}
```

## 🎨 UI 展示建议

### 1. 城市列表卡片
```
┌─────────────────────────────┐
│  Tokyo, Japan               │
│  [城市图片]                  │
│                             │
│  ☀️  22°C  体感 21°C        │
│  晴朗                        │
│                             │
│  💧 65%  💨 3.5m/s         │
│  👁️ 10km  ☀️ UV 6.5        │
│                             │
│  📅 156 个活动              │
└─────────────────────────────┘
```

### 2. 详细天气卡片
```
┌─────────────────────────────┐
│  Tokyo                      │
│                             │
│     ☀️                      │
│     22°C                   │
│   晴朗天气                   │
│   体感温度 21°C              │
│                             │
│  ├─ 温度范围: 20-25°C       │
│  ├─ 湿度: 65%               │
│  ├─ 风速: 3.5 m/s 南风      │
│  ├─ 气压: 1013 hPa          │
│  ├─ 能见度: 10 km           │
│  ├─ UV 指数: 6.5 (高)       │
│  └─ AQI: 45 (优)            │
│                             │
│  🌅 06:30  🌇 18:45         │
└─────────────────────────────┘
```

## 📝 测试

### API 测试
```bash
# 测试首页接口
curl http://localhost:5000/api/home/feed | jq '.data.cities[0].weather'
```

### 预期响应
```json
{
  "temperature": 22.5,
  "feelsLike": 21.8,
  "weather": "Clouds",
  "weatherDescription": "局部多云",
  "weatherIcon": "02d",
  "humidity": 65,
  "windSpeed": 3.5,
  "windDirection": 180,
  "windDirectionDescription": "南风",
  "sunrise": "2025-10-23T05:30:00Z",
  "sunset": "2025-10-23T17:45:00Z",
  "updatedAt": "2025-10-23T08:00:00Z"
}
```

## 🚀 部署清单

- [x] 创建 `WeatherDto.cs`
- [x] 更新 `CityDto.cs` 添加 Weather 字段
- [x] 重新构建 Gateway 镜像
- [x] 重启 Gateway 容器
- [x] 创建天气 API 文档
- [ ] 在 city-service 实现天气 API 集成
- [ ] 配置天气服务 API Key
- [ ] 添加缓存策略
- [ ] 前端集成天气显示
- [ ] 添加单元测试

## 📚 相关文档

- `WEATHER_API_DOCUMENTATION.md` - 完整天气 API 文档
- `BFF_IMPLEMENTATION.md` - BFF 架构实现文档

---

**更新时间**: 2025-10-23  
**版本**: 1.0  
**状态**: ✅ DTO 层完成，等待后端集成
