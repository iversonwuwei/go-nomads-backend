namespace CityService.DTOs;

/// <summary>
/// 天气信息 DTO
/// </summary>
public class WeatherDto
{
    /// <summary>
    /// 当前温度（摄氏度）
    /// </summary>
    public decimal Temperature { get; set; }

    /// <summary>
    /// 体感温度（摄氏度）
    /// </summary>
    public decimal FeelsLike { get; set; }

    /// <summary>
    /// 最低温度（摄氏度）
    /// </summary>
    public decimal? TempMin { get; set; }

    /// <summary>
    /// 最高温度（摄氏度）
    /// </summary>
    public decimal? TempMax { get; set; }

    /// <summary>
    /// 天气状况（如：Clear, Clouds, Rain, Snow 等）
    /// </summary>
    public string Weather { get; set; } = string.Empty;

    /// <summary>
    /// 天气详细描述（如：晴朗、局部多云、小雨等）
    /// </summary>
    public string WeatherDescription { get; set; } = string.Empty;

    /// <summary>
    /// 天气图标代码（用于显示天气图标，如：01d, 02n, 10d 等）
    /// </summary>
    public string WeatherIcon { get; set; } = string.Empty;

    /// <summary>
    /// 湿度百分比（0-100）
    /// </summary>
    public int Humidity { get; set; }

    /// <summary>
    /// 风速（米/秒）
    /// </summary>
    public decimal WindSpeed { get; set; }

    /// <summary>
    /// 风向（度数，0-360，0=北，90=东，180=南，270=西）
    /// </summary>
    public int WindDirection { get; set; }

    /// <summary>
    /// 风向描述（如：北风、东北风等）
    /// </summary>
    public string? WindDirectionDescription { get; set; }

    /// <summary>
    /// 阵风速度（米/秒）
    /// </summary>
    public decimal? WindGust { get; set; }

    /// <summary>
    /// 气压（百帕/hPa）
    /// </summary>
    public int Pressure { get; set; }

    /// <summary>
    /// 海平面气压（百帕/hPa）
    /// </summary>
    public int? SeaLevelPressure { get; set; }

    /// <summary>
    /// 地面气压（百帕/hPa）
    /// </summary>
    public int? GroundLevelPressure { get; set; }

    /// <summary>
    /// 能见度（米）
    /// </summary>
    public int Visibility { get; set; }

    /// <summary>
    /// 云量百分比（0-100）
    /// </summary>
    public int Cloudiness { get; set; }

    /// <summary>
    /// 降雨量 - 过去1小时（毫米）
    /// </summary>
    public decimal? Rain1h { get; set; }

    /// <summary>
    /// 降雨量 - 过去3小时（毫米）
    /// </summary>
    public decimal? Rain3h { get; set; }

    /// <summary>
    /// 降雪量 - 过去1小时（毫米）
    /// </summary>
    public decimal? Snow1h { get; set; }

    /// <summary>
    /// 降雪量 - 过去3小时（毫米）
    /// </summary>
    public decimal? Snow3h { get; set; }

    /// <summary>
    /// 日出时间（UTC）
    /// </summary>
    public DateTime Sunrise { get; set; }

    /// <summary>
    /// 日落时间（UTC）
    /// </summary>
    public DateTime Sunset { get; set; }

    /// <summary>
    /// 时区偏移（秒）
    /// </summary>
    public int? TimezoneOffset { get; set; }

    /// <summary>
    /// UV 指数
    /// </summary>
    public decimal? UvIndex { get; set; }

    /// <summary>
    /// 空气质量指数（AQI）
    /// </summary>
    public int? AirQualityIndex { get; set; }

    /// <summary>
    /// 天气数据来源（如：OpenWeatherMap, WeatherAPI 等）
    /// </summary>
    public string? DataSource { get; set; }

    /// <summary>
    /// 天气数据更新时间（UTC）
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 数据时间戳（UTC）
    /// </summary>
    public DateTime Timestamp { get; set; }
}
