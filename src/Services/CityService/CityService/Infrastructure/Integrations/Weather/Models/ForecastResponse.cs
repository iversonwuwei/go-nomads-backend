using System.Text.Json.Serialization;

namespace CityService.Infrastructure.Integrations.Weather.Models;

/// <summary>
///     Represents the OpenWeather API 2.5 5-day/3-hour forecast response.
/// </summary>
public class ForecastResponse
{
    [JsonPropertyName("cod")] public string Cod { get; set; } = string.Empty;

    [JsonPropertyName("message")] public int Message { get; set; }

    [JsonPropertyName("cnt")] public int Cnt { get; set; }

    [JsonPropertyName("list")] public List<ForecastItem> List { get; set; } = new();

    [JsonPropertyName("city")] public CityInfo City { get; set; } = new();
}

public class ForecastItem
{
    [JsonPropertyName("dt")] public long Dt { get; set; }

    [JsonPropertyName("main")] public Main Main { get; set; } = new();

    [JsonPropertyName("weather")] public List<Weather> Weather { get; set; } = new();

    [JsonPropertyName("clouds")] public Clouds Clouds { get; set; } = new();

    [JsonPropertyName("wind")] public Wind Wind { get; set; } = new();

    [JsonPropertyName("visibility")] public int Visibility { get; set; }

    [JsonPropertyName("pop")] public decimal Pop { get; set; }

    [JsonPropertyName("rain")] public Precipitation? Rain { get; set; }

    [JsonPropertyName("snow")] public Precipitation? Snow { get; set; }

    [JsonPropertyName("sys")] public Sys Sys { get; set; } = new();

    [JsonPropertyName("dt_txt")] public string DtTxt { get; set; } = string.Empty;
}

public class CityInfo
{
    [JsonPropertyName("id")] public int Id { get; set; }

    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

    [JsonPropertyName("coord")] public Coord Coord { get; set; } = new();

    [JsonPropertyName("country")] public string Country { get; set; } = string.Empty;

    [JsonPropertyName("population")] public int Population { get; set; }

    [JsonPropertyName("timezone")] public int Timezone { get; set; }

    [JsonPropertyName("sunrise")] public long Sunrise { get; set; }

    [JsonPropertyName("sunset")] public long Sunset { get; set; }
}