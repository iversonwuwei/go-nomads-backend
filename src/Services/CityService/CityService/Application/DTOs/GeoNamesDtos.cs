using System.Text.Json.Serialization;

namespace CityService.Application.DTOs;

/// <summary>
///     GeoNames API 响应模型
/// </summary>
public class GeoNamesSearchResponse
{
    [JsonPropertyName("totalResultsCount")]
    public int TotalResultsCount { get; set; }

    [JsonPropertyName("geonames")] public List<GeoNamesCity> Geonames { get; set; } = new();
}

/// <summary>
///     GeoNames 城市信息
/// </summary>
public class GeoNamesCity
{
    [JsonPropertyName("geonameId")] public int GeonameId { get; set; }

    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

    [JsonPropertyName("asciiName")] public string AsciiName { get; set; } = string.Empty;

    [JsonPropertyName("lat")] public string Lat { get; set; } = string.Empty;

    [JsonPropertyName("lng")] public string Lng { get; set; } = string.Empty;

    [JsonPropertyName("countryCode")] public string CountryCode { get; set; } = string.Empty;

    [JsonPropertyName("countryName")] public string CountryName { get; set; } = string.Empty;

    [JsonPropertyName("fcl")] public string FeatureClass { get; set; } = string.Empty;

    [JsonPropertyName("fcode")] public string FeatureCode { get; set; } = string.Empty;

    [JsonPropertyName("population")] public int Population { get; set; }

    [JsonPropertyName("adminName1")] public string AdminName1 { get; set; } = string.Empty; // 省份/州

    [JsonPropertyName("timezone")] public GeoNamesTimezone? Timezone { get; set; }
}

/// <summary>
///     GeoNames 时区信息
/// </summary>
public class GeoNamesTimezone
{
    [JsonPropertyName("timeZoneId")] public string TimeZoneId { get; set; } = string.Empty;

    [JsonPropertyName("gmtOffset")] public double GmtOffset { get; set; }

    [JsonPropertyName("dstOffset")] public double DstOffset { get; set; }
}

/// <summary>
///     GeoNames 导入配置
/// </summary>
public class GeoNamesImportOptions
{
    /// <summary>
    ///     最小人口数 (默认 100,000)
    /// </summary>
    public int MinPopulation { get; set; } = 100000;

    /// <summary>
    ///     要导入的国家代码列表 (为空则导入所有)
    /// </summary>
    public List<string>? CountryCodes { get; set; }

    /// <summary>
    ///     Feature Class (P = 城市/村庄)
    /// </summary>
    public string FeatureClass { get; set; } = "P";

    /// <summary>
    ///     Feature Code (PPLA = 一级行政区首府, PPLC = 首都, PPL = 城市)
    ///     空列表表示不过滤,获取所有类型的城市
    /// </summary>
    public List<string> FeatureCodes { get; set; } = new();

    /// <summary>
    ///     每批次处理数量
    /// </summary>
    public int BatchSize { get; set; } = 50;

    /// <summary>
    ///     是否覆盖已存在的城市
    /// </summary>
    public bool OverwriteExisting { get; set; } = false;

    /// <summary>
    ///     是否只更新坐标信息
    /// </summary>
    public bool UpdateCoordinatesOnly { get; set; } = false;
}

/// <summary>
///     导入结果
/// </summary>
public class GeoNamesImportResult
{
    public int TotalProcessed { get; set; }
    public int SuccessCount { get; set; }
    public int SkippedCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
}