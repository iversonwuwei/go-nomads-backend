namespace Gateway.DTOs;

/// <summary>
///     城市信息 DTO
/// </summary>
public class CityDto
{
    /// <summary>
    ///     城市ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    ///     城市名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     城市英文名称
    /// </summary>
    public string? NameEn { get; set; }

    /// <summary>
    ///     国家
    /// </summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>
    ///     城市图片URL
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    ///     城市描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     活动数量
    /// </summary>
    public int MeetupCount { get; set; }

    /// <summary>
    ///     Coworking 空间数量
    /// </summary>
    public int CoworkingCount { get; set; }

    /// <summary>
    ///     当前天气信息
    /// </summary>
    public WeatherDto? Weather { get; set; }
}