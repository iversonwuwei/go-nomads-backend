# 城市搜索接口返回天气数据修复

## 问题描述
`/api/v1/cities/search` 接口没有返回 `weather` 数据，虽然 `CityDto` 已经包含了 `Weather` 字段。

## 根本原因
在 `CityApplicationService.SearchCitiesAsync()` 方法中，返回的城市列表没有调用 `EnrichCitiesWithWeatherAsync()` 方法来填充天气数据。

## 修复内容

### 文件：`src/Services/CityService/CityService/Application/Services/CityApplicationService.cs`

**修改前（第49-67行）：**
```csharp
public async Task<IEnumerable<CityDto>> SearchCitiesAsync(CitySearchDto searchDto)
{
    var criteria = new CitySearchCriteria
    {
        Name = searchDto.Name,
        Country = searchDto.Country,
        Region = searchDto.Region,
        MinCostOfLiving = searchDto.MinCostOfLiving,
        MaxCostOfLiving = searchDto.MaxCostOfLiving,
        MinScore = searchDto.MinScore,
        Tags = searchDto.Tags,
        PageNumber = searchDto.PageNumber,
        PageSize = searchDto.PageSize
    };

    var cities = await _cityRepository.SearchAsync(criteria);
    return cities.Select(MapToDto);  // ❌ 没有填充天气数据
}
```

**修改后：**
```csharp
public async Task<IEnumerable<CityDto>> SearchCitiesAsync(CitySearchDto searchDto)
{
    var criteria = new CitySearchCriteria
    {
        Name = searchDto.Name,
        Country = searchDto.Country,
        Region = searchDto.Region,
        MinCostOfLiving = searchDto.MinCostOfLiving,
        MaxCostOfLiving = searchDto.MaxCostOfLiving,
        MinScore = searchDto.MinScore,
        Tags = searchDto.Tags,
        PageNumber = searchDto.PageNumber,
        PageSize = searchDto.PageSize
    };

    var cities = await _cityRepository.SearchAsync(criteria);
    var cityDtos = cities.Select(MapToDto).ToList();  // ✅ 转换为 List
    await EnrichCitiesWithWeatherAsync(cityDtos);     // ✅ 填充天气数据
    return cityDtos;
}
```

## 关键改动
1. **转换为 List**：将 `IEnumerable<CityDto>` 转换为 `List<CityDto>`，以便传递给 `EnrichCitiesWithWeatherAsync` 方法
2. **调用天气填充方法**：添加 `await EnrichCitiesWithWeatherAsync(cityDtos);` 调用
3. **返回填充后的数据**：返回已经包含天气数据的城市列表

## 测试验证

### 测试命令
```bash
# 搜索泰国的城市
curl "http://localhost:5000/api/v1/cities/search?country=Thailand"

# 格式化输出天气信息
curl -s "http://localhost:5000/api/v1/cities/search?country=Thailand" | \
  jq '.data[0] | {name, country, weather: {temperature: .weather.temperature, weather: .weather.weather, description: .weather.weatherDescription, humidity: .weather.humidity}}'
```

### 测试结果
```json
{
  "name": "Chiang Mai",
  "country": "Thailand",
  "weather": {
    "temperature": 24.92,
    "weather": "Clouds",
    "description": "多云",
    "humidity": 89
  }
}
```

✅ **验证通过**：搜索接口现在成功返回天气数据

## 相关方法对比

| 方法 | 是否填充天气 | 说明 |
|------|-------------|------|
| `GetAllCitiesAsync` | ✅ 是 | 已经调用 `EnrichCitiesWithWeatherAsync` |
| `GetCityByIdAsync` | ❌ 否 | 返回单个城市，未填充（可能需要单独处理） |
| `SearchCitiesAsync` | ✅ 是 | **本次修复** |
| `GetCitiesByCountryAsync` | ❓ 待确认 | 需要检查是否已填充 |

## 部署
```bash
cd /Users/walden/Workspaces/WaldenProjects/go-noma/deployment
./deploy-services-local.sh
```

## 完成时间
2025-11-04

## 影响范围
- ✅ 前端搜索页面现在可以显示城市天气信息
- ✅ 提升用户体验，搜索结果更加完整
- ✅ 与其他城市列表接口保持一致

## 注意事项
- 天气数据通过 OpenWeatherMap API 获取
- 如果某个城市获取天气失败，不会影响整体搜索结果，只是该城市的 `weather` 字段为 `null`
- 批量获取天气数据使用 `Task.WhenAll` 并行处理，提高性能
