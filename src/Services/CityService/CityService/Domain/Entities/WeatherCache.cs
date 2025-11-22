using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

namespace CityService.Domain.Entities;

/// <summary>
///     天气缓存实体 - 用于减少第三方 API 调用
/// </summary>
[Table("weather_cache")]
public class WeatherCache : BaseModel
{
    /// <summary>
    ///     城市ID（主键）
    /// </summary>
    [PrimaryKey("city_id")]
    public Guid CityId { get; set; }

    /// <summary>
    ///     城市名称
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column("city_name")]
    public string CityName { get; set; } = string.Empty;

    /// <summary>
    ///     国家代码（如 CN, US）
    /// </summary>
    [MaxLength(10)]
    [Column("country_code")]
    public string? CountryCode { get; set; }

    #region 天气基础信息

    /// <summary>
    ///     当前温度（摄氏度）
    /// </summary>
    [Required]
    [Column("temperature")]
    public decimal Temperature { get; set; }

    /// <summary>
    ///     体感温度（摄氏度）
    /// </summary>
    [Column("feels_like")]
    public decimal? FeelsLike { get; set; }

    /// <summary>
    ///     天气状况代码（如 Clear, Clouds, Rain）
    /// </summary>
    [Required]
    [MaxLength(50)]
    [Column("weather_condition")]
    public string WeatherCondition { get; set; } = string.Empty;

    /// <summary>
    ///     天气详细描述（如 晴朗, 多云）
    /// </summary>
    [Column("description")]
    public string? Description { get; set; }

    /// <summary>
    ///     OpenWeatherMap 图标代码（如 01d, 10n）
    /// </summary>
    [MaxLength(10)]
    [Column("icon_code")]
    public string? IconCode { get; set; }

    #endregion

    #region 详细气象数据

    /// <summary>
    ///     湿度（百分比）
    /// </summary>
    [Column("humidity")]
    public int? Humidity { get; set; }

    /// <summary>
    ///     气压（hPa）
    /// </summary>
    [Column("pressure")]
    public int? Pressure { get; set; }

    /// <summary>
    ///     风速（米/秒）
    /// </summary>
    [Column("wind_speed")]
    public decimal? WindSpeed { get; set; }

    /// <summary>
    ///     风向（度数，0-360）
    /// </summary>
    [Column("wind_direction")]
    public int? WindDirection { get; set; }

    /// <summary>
    ///     云量（百分比）
    /// </summary>
    [Column("clouds")]
    public int? Clouds { get; set; }

    /// <summary>
    ///     可见度（米）
    /// </summary>
    [Column("visibility")]
    public int? Visibility { get; set; }

    #endregion

    #region 日出日落时间

    /// <summary>
    ///     日出时间（UTC）
    /// </summary>
    [Column("sunrise")]
    public DateTime? Sunrise { get; set; }

    /// <summary>
    ///     日落时间（UTC）
    /// </summary>
    [Column("sunset")]
    public DateTime? Sunset { get; set; }

    #endregion

    #region 缓存元数据

    /// <summary>
    ///     数据最后更新时间
    /// </summary>
    [Required]
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     缓存过期时间（推荐 1-2 小时后）
    /// </summary>
    [Required]
    [Column("expired_at")]
    public DateTime ExpiredAt { get; set; }

    /// <summary>
    ///     API 数据来源（如 openweathermap）
    /// </summary>
    [MaxLength(50)]
    [Column("api_source")]
    public string ApiSource { get; set; } = "openweathermap";

    #endregion

    /// <summary>
    ///     检查缓存是否已过期
    /// </summary>
    public bool IsExpired() => ExpiredAt <= DateTime.UtcNow;

    /// <summary>
    ///     检查缓存是否仍然有效
    /// </summary>
    public bool IsValid() => !IsExpired();

    /// <summary>
    ///     获取缓存年龄（秒）
    /// </summary>
    public double GetAgeInSeconds() => (DateTime.UtcNow - UpdatedAt).TotalSeconds;
}
