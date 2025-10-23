using CityService.DTOs;

namespace CityService.Services;

/// <summary>
/// 天气服务接口
/// </summary>
public interface IWeatherService
{
    /// <summary>
    /// 根据城市名称获取天气信息
    /// </summary>
    Task<WeatherDto?> GetWeatherByCityNameAsync(string cityName, string? countryCode = null);

    /// <summary>
    /// 根据经纬度获取天气信息
    /// </summary>
    Task<WeatherDto?> GetWeatherByCoordinatesAsync(double latitude, double longitude);

    /// <summary>
    /// 批量获取多个城市的天气信息
    /// </summary>
    Task<Dictionary<string, WeatherDto?>> GetWeatherForCitiesAsync(List<string> cityNames);
}
