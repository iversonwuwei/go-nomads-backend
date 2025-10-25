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

    private string GetWindDirectionDescription(int degrees)
    {
        var directions = new[] { "北风", "东北风", "东风", "东南风", "南风", "西南风", "西风", "西北风" };
        var index = (int)Math.Round(((degrees % 360) / 45.0)) % 8;
        return directions[index];
    }
}
