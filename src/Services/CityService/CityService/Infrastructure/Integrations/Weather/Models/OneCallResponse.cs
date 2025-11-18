using System.Text.Json.Serialization;

namespace CityService.Infrastructure.Integrations.Weather.Models;

/// <summary>
///     Represents the OpenWeather One Call API response payload.
/// </summary>
public class OneCallResponse
{
    [JsonPropertyName("lat")] public double Lat { get; set; }

    [JsonPropertyName("lon")] public double Lon { get; set; }

    [JsonPropertyName("timezone")] public string Timezone { get; set; } = string.Empty;

    [JsonPropertyName("timezone_offset")] public int TimezoneOffset { get; set; }

    [JsonPropertyName("daily")] public List<DailyForecast> Daily { get; set; } = new();
}

public class DailyForecast
{
    [JsonPropertyName("dt")] public long Dt { get; set; }

    [JsonPropertyName("sunrise")] public long Sunrise { get; set; }

    [JsonPropertyName("sunset")] public long Sunset { get; set; }

    [JsonPropertyName("moonrise")] public long? Moonrise { get; set; }

    [JsonPropertyName("moonset")] public long? Moonset { get; set; }

    [JsonPropertyName("moon_phase")] public decimal? MoonPhase { get; set; }

    [JsonPropertyName("temp")] public TemperatureBreakdown Temp { get; set; } = new();

    [JsonPropertyName("feels_like")] public FeelsLikeBreakdown FeelsLike { get; set; } = new();

    [JsonPropertyName("pressure")] public int Pressure { get; set; }

    [JsonPropertyName("humidity")] public int Humidity { get; set; }

    [JsonPropertyName("wind_speed")] public decimal WindSpeed { get; set; }

    [JsonPropertyName("wind_deg")] public int WindDeg { get; set; }

    [JsonPropertyName("wind_gust")] public decimal? WindGust { get; set; }

    [JsonPropertyName("weather")] public List<Weather> Weather { get; set; } = new();

    [JsonPropertyName("clouds")] public int Clouds { get; set; }

    [JsonPropertyName("pop")] public decimal Pop { get; set; }

    [JsonPropertyName("rain")] public decimal? Rain { get; set; }

    [JsonPropertyName("snow")] public decimal? Snow { get; set; }

    [JsonPropertyName("dew_point")] public decimal? DewPoint { get; set; }

    [JsonPropertyName("uvi")] public decimal Uvi { get; set; }

    [JsonPropertyName("summary")] public string? Summary { get; set; }
}

public class TemperatureBreakdown
{
    [JsonPropertyName("day")] public decimal Day { get; set; }

    [JsonPropertyName("min")] public decimal Min { get; set; }

    [JsonPropertyName("max")] public decimal Max { get; set; }

    [JsonPropertyName("night")] public decimal Night { get; set; }

    [JsonPropertyName("eve")] public decimal? Eve { get; set; }

    [JsonPropertyName("morn")] public decimal? Morn { get; set; }
}

public class FeelsLikeBreakdown
{
    [JsonPropertyName("day")] public decimal Day { get; set; }

    [JsonPropertyName("night")] public decimal Night { get; set; }

    [JsonPropertyName("eve")] public decimal? Eve { get; set; }

    [JsonPropertyName("morn")] public decimal? Morn { get; set; }
}