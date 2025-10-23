using CityService.DTOs;
using CityService.Models;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace CityService.Services;

/// <summary>
/// 天气服务实现 - 使用 OpenWeatherMap API
/// </summary>
public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly ILogger<WeatherService> _logger;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly TimeSpan _cacheDuration;

    public WeatherService(
        HttpClient httpClient,
        IConfiguration configuration,
        IMemoryCache cache,
        ILogger<WeatherService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _cache = cache;
        _logger = logger;

        _apiKey = configuration["Weather:ApiKey"] ?? throw new InvalidOperationException("Weather API Key is not configured");
        _baseUrl = configuration["Weather:BaseUrl"] ?? "https://api.openweathermap.org/data/2.5";
        _cacheDuration = TimeSpan.Parse(configuration["Weather:CacheDuration"] ?? "00:10:00");
    }

    public async Task<WeatherDto?> GetWeatherByCityNameAsync(string cityName, string? countryCode = null)
    {
        try
        {
            // 构建缓存键
            var cacheKey = $"weather_{cityName}_{countryCode}".ToLower();

            // 检查缓存
            if (_cache.TryGetValue(cacheKey, out WeatherDto? cachedWeather))
            {
                _logger.LogInformation("返回缓存的天气数据: {CityName}", cityName);
                return cachedWeather;
            }

            // 构建查询字符串
            var query = string.IsNullOrEmpty(countryCode) ? cityName : $"{cityName},{countryCode}";
            var language = _configuration["Weather:Language"] ?? "zh_cn";
            var url = $"{_baseUrl}/weather?q={query}&appid={_apiKey}&units=metric&lang={language}";

            _logger.LogInformation("调用天气 API: {City}", cityName);

            // 调用 API
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("天气 API 调用失败: {StatusCode}, City: {City}", response.StatusCode, cityName);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var weatherData = JsonSerializer.Deserialize<OpenWeatherMapResponse>(json);

            if (weatherData == null)
            {
                _logger.LogWarning("天气数据解析失败: {City}", cityName);
                return null;
            }

            // 转换为 WeatherDto
            var weather = MapToWeatherDto(weatherData);

            // 缓存结果
            _cache.Set(cacheKey, weather, _cacheDuration);

            _logger.LogInformation("成功获取天气数据: {City}, 温度: {Temp}°C", cityName, weather.Temperature);

            return weather;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取天气数据异常: {City}", cityName);
            return null;
        }
    }

    public async Task<WeatherDto?> GetWeatherByCoordinatesAsync(double latitude, double longitude)
    {
        try
        {
            // 构建缓存键
            var cacheKey = $"weather_coord_{latitude}_{longitude}";

            // 检查缓存
            if (_cache.TryGetValue(cacheKey, out WeatherDto? cachedWeather))
            {
                _logger.LogInformation("返回缓存的天气数据: ({Lat}, {Lon})", latitude, longitude);
                return cachedWeather;
            }

            var language = _configuration["Weather:Language"] ?? "zh_cn";
            var url = $"{_baseUrl}/weather?lat={latitude}&lon={longitude}&appid={_apiKey}&units=metric&lang={language}";

            _logger.LogInformation("调用天气 API (坐标): ({Lat}, {Lon})", latitude, longitude);

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("天气 API 调用失败: {StatusCode}, 坐标: ({Lat}, {Lon})", response.StatusCode, latitude, longitude);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var weatherData = JsonSerializer.Deserialize<OpenWeatherMapResponse>(json);

            if (weatherData == null)
            {
                _logger.LogWarning("天气数据解析失败: ({Lat}, {Lon})", latitude, longitude);
                return null;
            }

            var weather = MapToWeatherDto(weatherData);

            // 缓存结果
            _cache.Set(cacheKey, weather, _cacheDuration);

            return weather;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取天气数据异常: ({Lat}, {Lon})", latitude, longitude);
            return null;
        }
    }

    public async Task<Dictionary<string, WeatherDto?>> GetWeatherForCitiesAsync(List<string> cityNames)
    {
        var result = new Dictionary<string, WeatherDto?>();

        // 并行获取所有城市的天气
        var tasks = cityNames.Select(async city =>
        {
            var weather = await GetWeatherByCityNameAsync(city);
            return new { City = city, Weather = weather };
        });

        var results = await Task.WhenAll(tasks);

        foreach (var item in results)
        {
            result[item.City] = item.Weather;
        }

        return result;
    }

    /// <summary>
    /// 将 OpenWeatherMap 响应转换为 WeatherDto
    /// </summary>
    private WeatherDto MapToWeatherDto(OpenWeatherMapResponse data)
    {
        var weather = data.Weather.FirstOrDefault();

        return new WeatherDto
        {
            Temperature = data.Main.Temp,
            FeelsLike = data.Main.FeelsLike,
            TempMin = data.Main.TempMin,
            TempMax = data.Main.TempMax,
            Weather = weather?.Main ?? "Unknown",
            WeatherDescription = weather?.Description ?? "未知",
            WeatherIcon = weather?.Icon ?? "01d",
            Humidity = data.Main.Humidity,
            WindSpeed = data.Wind.Speed,
            WindDirection = data.Wind.Deg,
            WindDirectionDescription = GetWindDirectionDescription(data.Wind.Deg),
            WindGust = data.Wind.Gust,
            Pressure = data.Main.Pressure,
            SeaLevelPressure = data.Main.SeaLevel,
            GroundLevelPressure = data.Main.GrndLevel,
            Visibility = data.Visibility,
            Cloudiness = data.Clouds.All,
            Rain1h = data.Rain?.OneHour,
            Rain3h = data.Rain?.ThreeHours,
            Snow1h = data.Snow?.OneHour,
            Snow3h = data.Snow?.ThreeHours,
            Sunrise = DateTimeOffset.FromUnixTimeSeconds(data.Sys.Sunrise).UtcDateTime,
            Sunset = DateTimeOffset.FromUnixTimeSeconds(data.Sys.Sunset).UtcDateTime,
            TimezoneOffset = data.Timezone,
            DataSource = "OpenWeatherMap",
            UpdatedAt = DateTime.UtcNow,
            Timestamp = DateTimeOffset.FromUnixTimeSeconds(data.Dt).UtcDateTime
        };
    }

    /// <summary>
    /// 将风向角度转换为中文描述
    /// </summary>
    private string GetWindDirectionDescription(int degrees)
    {
        var directions = new[] { "北风", "东北风", "东风", "东南风", "南风", "西南风", "西风", "西北风" };
        var index = (int)Math.Round(((degrees % 360) / 45.0)) % 8;
        return directions[index];
    }
}
