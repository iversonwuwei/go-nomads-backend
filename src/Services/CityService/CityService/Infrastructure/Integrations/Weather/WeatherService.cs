using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using CityService.Application.Abstractions.Services;
using CityService.Application.DTOs;
using CityService.Infrastructure.Integrations.Weather.Models;
using Microsoft.Extensions.Caching.Memory;

namespace CityService.Infrastructure.Integrations.Weather;

/// <summary>
/// OpenWeatherMap-backed implementation of <see cref="IWeatherService"/>.
/// </summary>
public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly ILogger<WeatherService> _logger;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly string _forecastBaseUrl;
    private readonly string _oneCallApiKey;
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
        _forecastBaseUrl = configuration["Weather:ForecastBaseUrl"] ?? "https://api.openweathermap.org/data/3.0";
        _oneCallApiKey = configuration["Weather:OneCallApiKey"] ?? _apiKey;
        _cacheDuration = TimeSpan.Parse(configuration["Weather:CacheDuration"] ?? "00:10:00");
    }

    public async Task<WeatherDto?> GetWeatherByCityNameAsync(string cityName, string? countryCode = null)
    {
        try
        {
            var cacheKey = $"weather_{cityName}_{countryCode}".ToLowerInvariant();

            if (_cache.TryGetValue(cacheKey, out WeatherDto? cachedWeather))
            {
                _logger.LogInformation("Returning cached weather for {City}", cityName);
                return cachedWeather;
            }

            var query = string.IsNullOrEmpty(countryCode) ? cityName : $"{cityName},{countryCode}";
            var language = _configuration["Weather:Language"] ?? "zh_cn";
            var url = $"{_baseUrl}/weather?q={query}&appid={_apiKey}&units=metric&lang={language}";

            _logger.LogInformation("Calling weather API for {City}", cityName);

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Weather API returned {StatusCode} for city {City}", response.StatusCode, cityName);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var weatherData = JsonSerializer.Deserialize<OpenWeatherMapResponse>(json);

            if (weatherData == null)
            {
                _logger.LogWarning("Failed to deserialize weather payload for {City}", cityName);
                return null;
            }

            var weather = MapToWeatherDto(weatherData);
            _cache.Set(cacheKey, weather, _cacheDuration);

            _logger.LogInformation("Weather API succeeded for {City} with {Temp}°C", cityName, weather.Temperature);

            return weather;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving weather for {City}", cityName);
            return null;
        }
    }

    public async Task<WeatherDto?> GetWeatherByCoordinatesAsync(double latitude, double longitude)
    {
        try
        {
            var cacheKey = $"weather_coord_{latitude}_{longitude}";

            if (_cache.TryGetValue(cacheKey, out WeatherDto? cachedWeather))
            {
                _logger.LogInformation("Returning cached weather for coordinates ({Lat}, {Lon})", latitude, longitude);
                return cachedWeather;
            }

            var language = _configuration["Weather:Language"] ?? "zh_cn";
            var url = $"{_baseUrl}/weather?lat={latitude}&lon={longitude}&appid={_apiKey}&units=metric&lang={language}";

            _logger.LogInformation("Calling weather API for coordinates ({Lat}, {Lon})", latitude, longitude);

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Weather API returned {StatusCode} for coordinates ({Lat}, {Lon})", response.StatusCode, latitude, longitude);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var weatherData = JsonSerializer.Deserialize<OpenWeatherMapResponse>(json);

            if (weatherData == null)
            {
                _logger.LogWarning("Failed to deserialize weather payload for coordinates ({Lat}, {Lon})", latitude, longitude);
                return null;
            }

            var weather = MapToWeatherDto(weatherData);
            _cache.Set(cacheKey, weather, _cacheDuration);

            return weather;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving weather for coordinates ({Lat}, {Lon})", latitude, longitude);
            return null;
        }
    }

    public async Task<Dictionary<string, WeatherDto?>> GetWeatherForCitiesAsync(List<string> cityNames)
    {
        var result = new Dictionary<string, WeatherDto?>();

        var tasks = cityNames.Select(async city =>
        {
            var weather = await GetWeatherByCityNameAsync(city);
            return new { City = city, Weather = weather };
        });

        var responses = await Task.WhenAll(tasks);

        foreach (var item in responses)
        {
            result[item.City] = item.Weather;
        }

        return result;
    }

    public async Task<WeatherForecastDto?> GetDailyForecastAsync(double latitude, double longitude, int days)
    {
        try
        {
            // 免费 API 2.5 最多支持 5 天预报
            var normalizedDays = Math.Clamp(days, 1, 5);
            var cacheKey = $"forecast_coord_{latitude}_{longitude}_{normalizedDays}";

            if (_cache.TryGetValue(cacheKey, out WeatherForecastDto? cachedForecast))
            {
                _logger.LogInformation(
                    "Returning cached forecast for coordinates ({Lat}, {Lon})",
                    latitude,
                    longitude);
                return cachedForecast;
            }

            var language = _configuration["Weather:Language"] ?? "zh_cn";
            // 使用免费的 2.5 forecast 端点（5天/3小时）
            var url = $"{_baseUrl}/forecast?lat={latitude}&lon={longitude}&appid={_apiKey}&units=metric&lang={language}";

            _logger.LogInformation("Calling forecast API for coordinates ({Lat}, {Lon})", latitude, longitude);

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Weather forecast API returned {StatusCode} for coordinates ({Lat}, {Lon})",
                    response.StatusCode,
                    latitude,
                    longitude);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var forecastData = JsonSerializer.Deserialize<ForecastResponse>(json);

            if (forecastData == null || forecastData.List.Count == 0)
            {
                _logger.LogWarning(
                    "Failed to deserialize weather forecast payload for coordinates ({Lat}, {Lon})",
                    latitude,
                    longitude);
                return null;
            }

            var forecast = MapToForecastDtoFrom25(forecastData, normalizedDays, latitude, longitude);
            _cache.Set(cacheKey, forecast, _cacheDuration);

            _logger.LogInformation(
                "Forecast API succeeded for coordinates ({Lat}, {Lon}) with {Days} days",
                latitude,
                longitude,
                forecast.Daily.Count);

            return forecast;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error while retrieving forecast for coordinates ({Lat}, {Lon})",
                latitude,
                longitude);
            return null;
        }
    }

    public async Task<WeatherForecastDto?> GetDailyForecastByCityNameAsync(string cityName, int days)
    {
        try
        {
            // 免费 API 2.5 最多支持 5 天预报
            var normalizedDays = Math.Clamp(days, 1, 5);
            var cacheKey = $"forecast_city_{cityName}_{normalizedDays}".ToLowerInvariant();

            if (_cache.TryGetValue(cacheKey, out WeatherForecastDto? cachedForecast))
            {
                _logger.LogInformation("Returning cached forecast for {City}", cityName);
                return cachedForecast;
            }

            var currentWeather = await GetWeatherByCityNameAsync(cityName);
            if (currentWeather?.Latitude == null || currentWeather.Longitude == null)
            {
                _logger.LogWarning(
                    "Cannot retrieve forecast for {City} because coordinates are unavailable",
                    cityName);
                return null;
            }

            var forecast = await GetDailyForecastAsync(
                currentWeather.Latitude.Value,
                currentWeather.Longitude.Value,
                normalizedDays);

            if (forecast != null)
            {
                _cache.Set(cacheKey, forecast, _cacheDuration);
            }

            return forecast;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while retrieving forecast for {City}", cityName);
            return null;
        }
    }

    private WeatherDto MapToWeatherDto(OpenWeatherMapResponse data)
    {
        var weather = data.Weather.FirstOrDefault();

        return new WeatherDto
        {
            Temperature = data.Main.Temp,
            FeelsLike = data.Main.FeelsLike,
            TempMin = data.Main.TempMin,
            TempMax = data.Main.TempMax,
            Latitude = data.Coord?.Lat,
            Longitude = data.Coord?.Lon,
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

    private WeatherForecastDto MapToForecastDto(OneCallResponse data, int days)
    {
        var dailyForecasts = data.Daily
            .OrderBy(d => d.Dt)
            .Take(Math.Clamp(days, 1, 7))
            .Select(day =>
            {
                var weather = day.Weather.FirstOrDefault();
                return new DailyForecastDto
                {
                    Date = DateTimeOffset.FromUnixTimeSeconds(day.Dt).UtcDateTime,
                    Sunrise = DateTimeOffset.FromUnixTimeSeconds(day.Sunrise).UtcDateTime,
                    Sunset = DateTimeOffset.FromUnixTimeSeconds(day.Sunset).UtcDateTime,
                    Moonrise = day.Moonrise.HasValue
                        ? DateTimeOffset.FromUnixTimeSeconds(day.Moonrise.Value).UtcDateTime
                        : (DateTime?)null,
                    Moonset = day.Moonset.HasValue
                        ? DateTimeOffset.FromUnixTimeSeconds(day.Moonset.Value).UtcDateTime
                        : (DateTime?)null,
                    MoonPhase = day.MoonPhase,
                    TempDay = day.Temp.Day,
                    TempNight = day.Temp.Night,
                    TempMin = day.Temp.Min,
                    TempMax = day.Temp.Max,
                    TempEvening = day.Temp.Eve,
                    TempMorning = day.Temp.Morn,
                    FeelsLikeDay = day.FeelsLike.Day,
                    FeelsLikeNight = day.FeelsLike.Night,
                    FeelsLikeEvening = day.FeelsLike.Eve,
                    FeelsLikeMorning = day.FeelsLike.Morn,
                    Humidity = day.Humidity,
                    Pressure = day.Pressure,
                    WindSpeed = day.WindSpeed,
                    WindDirection = day.WindDeg,
                    WindDirectionDescription = GetWindDirectionDescription(day.WindDeg),
                    WindGust = day.WindGust,
                    Cloudiness = day.Clouds,
                    ProbabilityOfPrecipitation = day.Pop,
                    RainVolume = day.Rain,
                    SnowVolume = day.Snow,
                    UvIndex = day.Uvi,
                    DewPoint = day.DewPoint,
                    Summary = day.Summary,
                    Weather = weather?.Main ?? "Unknown",
                    WeatherDescription = weather?.Description ?? "未知",
                    WeatherIcon = weather?.Icon ?? "01d"
                };
            })
            .ToList();

        return new WeatherForecastDto
        {
            Latitude = data.Lat,
            Longitude = data.Lon,
            Timezone = data.Timezone,
            TimezoneOffset = data.TimezoneOffset,
            GeneratedAt = DateTime.UtcNow,
            Daily = dailyForecasts
        };
    }

    /// <summary>
    /// 将 API 2.5 的 3 小时预报数据聚合为每日预报
    /// </summary>
    private WeatherForecastDto MapToForecastDtoFrom25(ForecastResponse data, int days, double latitude, double longitude)
    {
        // 按日期分组 3 小时数据
        var groupedByDate = data.List
            .GroupBy(item => DateTimeOffset.FromUnixTimeSeconds(item.Dt).UtcDateTime.Date)
            .OrderBy(g => g.Key)
            .Take(days)
            .ToList();

        var dailyForecasts = new List<DailyForecastDto>();

        foreach (var dayGroup in groupedByDate)
        {
            var items = dayGroup.ToList();
            if (items.Count == 0) continue;

            // 计算当天的温度范围和平均值
            var temps = items.Select(i => i.Main.Temp).ToList();
            var feelsLikes = items.Select(i => i.Main.FeelsLike).ToList();
            var humidities = items.Select(i => i.Main.Humidity).ToList();
            var pressures = items.Select(i => i.Main.Pressure).ToList();
            var windSpeeds = items.Select(i => i.Wind.Speed).ToList();

            // 选择白天（12:00）的数据作为代表，如果没有则选中间时段
            var dayItem = items.FirstOrDefault(i => 
                DateTimeOffset.FromUnixTimeSeconds(i.Dt).Hour == 12) ?? items[items.Count / 2];

            // 选择夜晚（00:00 或 21:00）的数据
            var nightItem = items.FirstOrDefault(i => 
                DateTimeOffset.FromUnixTimeSeconds(i.Dt).Hour == 0 || 
                DateTimeOffset.FromUnixTimeSeconds(i.Dt).Hour == 21) ?? items.Last();

            // 获取最常见的天气描述
            var weather = items
                .GroupBy(i => i.Weather.FirstOrDefault()?.Main ?? "Unknown")
                .OrderByDescending(g => g.Count())
                .First()
                .First()
                .Weather
                .FirstOrDefault();

            // 计算降水概率（取最大值）
            var maxPop = items.Max(i => i.Pop);

            // 汇总降雨/降雪量
            var totalRain = items.Where(i => i.Rain != null).Sum(i => i.Rain!.ThreeHours ?? 0);
            var totalSnow = items.Where(i => i.Snow != null).Sum(i => i.Snow!.ThreeHours ?? 0);

            dailyForecasts.Add(new DailyForecastDto
            {
                Date = dayGroup.Key,
                Sunrise = data.City.Sunrise > 0 
                    ? DateTimeOffset.FromUnixTimeSeconds(data.City.Sunrise).UtcDateTime 
                    : dayGroup.Key.AddHours(6),
                Sunset = data.City.Sunset > 0 
                    ? DateTimeOffset.FromUnixTimeSeconds(data.City.Sunset).UtcDateTime 
                    : dayGroup.Key.AddHours(18),
                TempDay = dayItem.Main.Temp,
                TempNight = nightItem.Main.Temp,
                TempMin = temps.Min(),
                TempMax = temps.Max(),
                TempEvening = items.FirstOrDefault(i => 
                    DateTimeOffset.FromUnixTimeSeconds(i.Dt).Hour == 18)?.Main.Temp,
                TempMorning = items.FirstOrDefault(i => 
                    DateTimeOffset.FromUnixTimeSeconds(i.Dt).Hour == 6)?.Main.Temp,
                FeelsLikeDay = dayItem.Main.FeelsLike,
                FeelsLikeNight = nightItem.Main.FeelsLike,
                FeelsLikeEvening = items.FirstOrDefault(i => 
                    DateTimeOffset.FromUnixTimeSeconds(i.Dt).Hour == 18)?.Main.FeelsLike,
                FeelsLikeMorning = items.FirstOrDefault(i => 
                    DateTimeOffset.FromUnixTimeSeconds(i.Dt).Hour == 6)?.Main.FeelsLike,
                Humidity = (int)humidities.Average(),
                Pressure = (int)pressures.Average(),
                WindSpeed = windSpeeds.Average(),
                WindDirection = dayItem.Wind.Deg,
                WindDirectionDescription = GetWindDirectionDescription(dayItem.Wind.Deg),
                WindGust = items.Max(i => i.Wind.Gust),
                Cloudiness = (int)items.Average(i => i.Clouds.All),
                ProbabilityOfPrecipitation = maxPop,
                RainVolume = totalRain > 0 ? totalRain : null,
                SnowVolume = totalSnow > 0 ? totalSnow : null,
                UvIndex = 0, // API 2.5 不提供 UV 指数
                DewPoint = items.Average(i => i.Main.Temp - ((100 - i.Main.Humidity) / 5.0m)),
                Weather = weather?.Main ?? "Unknown",
                WeatherDescription = weather?.Description ?? "未知",
                WeatherIcon = weather?.Icon ?? "01d",
                Summary = $"{weather?.Description ?? "未知"}, 温度 {temps.Min():F1}°C - {temps.Max():F1}°C"
            });
        }

        return new WeatherForecastDto
        {
            Latitude = latitude,
            Longitude = longitude,
            Timezone = data.City.Name,
            TimezoneOffset = data.City.Timezone,
            GeneratedAt = DateTime.UtcNow,
            Daily = dailyForecasts
        };
    }

    private async Task LogOneCallFailureAsync(HttpResponseMessage response, double? latitude = null, double? longitude = null)
    {
        var body = await response.Content.ReadAsStringAsync();
        var context = latitude.HasValue && longitude.HasValue
            ? $"coordinates ({latitude}, {longitude})"
            : "request";

        _logger.LogWarning(
            "One Call API returned {StatusCode} for {Context}. Payload: {Body}",
            response.StatusCode,
            context,
            string.IsNullOrWhiteSpace(body) ? "<empty>" : body);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogError(
                "One Call API responded with 401 Unauthorized. Verify that the configured API key is subscribed to One Call API 3.0 (Weather:OneCallApiKey). If the product is not enabled, daily forecasts cannot be retrieved.");
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            _logger.LogError(
                "One Call API responded with 403 Forbidden. Check the subscription tier and that the One Call endpoint is permitted for the API key in use.");
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            _logger.LogWarning(
                "One Call API rate limit exceeded. Consider caching responses longer or reducing request frequency.");
        }
    }

    private string GetWindDirectionDescription(int degrees)
    {
        var directions = new[] { "北风", "东北风", "东风", "东南风", "南风", "西南风", "西风", "西北风" };
        var index = (int)Math.Round(((degrees % 360) / 45.0)) % 8;
        return directions[index];
    }
}
