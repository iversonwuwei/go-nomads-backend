namespace CityService.Application.DTOs;

/// <summary>
///     未来几日的天气预报信息。
/// </summary>
public class WeatherForecastDto
{
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Timezone { get; set; }
    public int? TimezoneOffset { get; set; }
    public DateTime GeneratedAt { get; set; }
    public IReadOnlyList<DailyForecastDto> Daily { get; set; } = Array.Empty<DailyForecastDto>();
}

/// <summary>
///     单日天气预报细节。
/// </summary>
public class DailyForecastDto
{
    public DateTime Date { get; set; }
    public DateTime Sunrise { get; set; }
    public DateTime Sunset { get; set; }
    public DateTime? Moonrise { get; set; }
    public DateTime? Moonset { get; set; }
    public decimal? MoonPhase { get; set; }
    public decimal TempDay { get; set; }
    public decimal TempNight { get; set; }
    public decimal TempMin { get; set; }
    public decimal TempMax { get; set; }
    public decimal? TempEvening { get; set; }
    public decimal? TempMorning { get; set; }
    public decimal FeelsLikeDay { get; set; }
    public decimal FeelsLikeNight { get; set; }
    public decimal? FeelsLikeEvening { get; set; }
    public decimal? FeelsLikeMorning { get; set; }
    public int Humidity { get; set; }
    public int Pressure { get; set; }
    public decimal WindSpeed { get; set; }
    public int WindDirection { get; set; }
    public string? WindDirectionDescription { get; set; }
    public decimal? WindGust { get; set; }
    public int Cloudiness { get; set; }
    public decimal ProbabilityOfPrecipitation { get; set; }
    public decimal? RainVolume { get; set; }
    public decimal? SnowVolume { get; set; }
    public decimal UvIndex { get; set; }
    public decimal? DewPoint { get; set; }
    public string Weather { get; set; } = string.Empty;
    public string WeatherDescription { get; set; } = string.Empty;
    public string WeatherIcon { get; set; } = string.Empty;
    public string? Summary { get; set; }
}