namespace CityService.Application.DTOs;

/// <summary>
///     天气信息 DTO
/// </summary>
public class WeatherDto
{
    public decimal Temperature { get; set; }
    public decimal FeelsLike { get; set; }
    public decimal? TempMin { get; set; }
    public decimal? TempMax { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string Weather { get; set; } = string.Empty;
    public string WeatherDescription { get; set; } = string.Empty;
    public string WeatherIcon { get; set; } = string.Empty;
    public int Humidity { get; set; }
    public decimal WindSpeed { get; set; }
    public int WindDirection { get; set; }
    public string? WindDirectionDescription { get; set; }
    public decimal? WindGust { get; set; }
    public int Pressure { get; set; }
    public int? SeaLevelPressure { get; set; }
    public int? GroundLevelPressure { get; set; }
    public int Visibility { get; set; }
    public int Cloudiness { get; set; }
    public decimal? Rain1h { get; set; }
    public decimal? Rain3h { get; set; }
    public decimal? Snow1h { get; set; }
    public decimal? Snow3h { get; set; }
    public DateTime Sunrise { get; set; }
    public DateTime Sunset { get; set; }
    public int? TimezoneOffset { get; set; }
    public decimal? UvIndex { get; set; }
    public int? AirQualityIndex { get; set; }
    public string? DataSource { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime Timestamp { get; set; }
    public WeatherForecastDto? Forecast { get; set; }
}