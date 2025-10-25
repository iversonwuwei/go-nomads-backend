using System.Collections.Generic;
using CityService.Application.DTOs;

namespace CityService.Application.Abstractions.Services;

/// <summary>
/// Defines read-only access to external weather data providers.
/// </summary>
public interface IWeatherService
{
    Task<WeatherDto?> GetWeatherByCityNameAsync(string cityName, string? countryCode = null);
    Task<WeatherDto?> GetWeatherByCoordinatesAsync(double latitude, double longitude);
    Task<Dictionary<string, WeatherDto?>> GetWeatherForCitiesAsync(List<string> cityNames);
}
