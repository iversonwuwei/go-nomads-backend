namespace SearchService.Domain.Models;

/// <summary>
/// 城市搜索文档 - Elasticsearch索引模型
/// </summary>
public class CitySearchDocument
{
    /// <summary>
    /// 城市唯一标识
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 城市名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 城市英文名称
    /// </summary>
    public string? NameEn { get; set; }

    /// <summary>
    /// 国家名称
    /// </summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// 国家ID
    /// </summary>
    public Guid? CountryId { get; set; }

    /// <summary>
    /// 省份ID
    /// </summary>
    public Guid? ProvinceId { get; set; }

    /// <summary>
    /// 区域
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// 城市描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 纬度
    /// </summary>
    public double? Latitude { get; set; }

    /// <summary>
    /// 经度
    /// </summary>
    public double? Longitude { get; set; }

    /// <summary>
    /// 地理位置 (用于Elasticsearch geo_point)
    /// </summary>
    public GeoLocation? Location { get; set; }

    /// <summary>
    /// 人口
    /// </summary>
    public int? Population { get; set; }

    /// <summary>
    /// 气候
    /// </summary>
    public string? Climate { get; set; }

    /// <summary>
    /// 时区
    /// </summary>
    public string? TimeZone { get; set; }

    /// <summary>
    /// 货币
    /// </summary>
    public string? Currency { get; set; }

    /// <summary>
    /// 图片URL
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// 竖屏封面图URL
    /// </summary>
    public string? PortraitImageUrl { get; set; }

    /// <summary>
    /// 综合评分
    /// </summary>
    public decimal? OverallScore { get; set; }

    /// <summary>
    /// 网络质量评分
    /// </summary>
    public decimal? InternetQualityScore { get; set; }

    /// <summary>
    /// 安全评分
    /// </summary>
    public decimal? SafetyScore { get; set; }

    /// <summary>
    /// 成本评分
    /// </summary>
    public decimal? CostScore { get; set; }

    /// <summary>
    /// 社区评分
    /// </summary>
    public decimal? CommunityScore { get; set; }

    /// <summary>
    /// 天气评分
    /// </summary>
    public decimal? WeatherScore { get; set; }

    /// <summary>
    /// 标签
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 搜索建议文本 (用于autocomplete)
    /// </summary>
    public string? Suggest { get; set; }

    /// <summary>
    /// 文档类型标识
    /// </summary>
    public string DocumentType => "city";

    // ============ 扩展字段：用于列表展示 ============

    /// <summary>
    /// 月均花费 (美元)
    /// </summary>
    public decimal? AverageCost { get; set; }

    /// <summary>
    /// 活跃用户数量
    /// </summary>
    public int UserCount { get; set; }

    /// <summary>
    /// 版主ID
    /// </summary>
    public Guid? ModeratorId { get; set; }

    /// <summary>
    /// 版主名称
    /// </summary>
    public string? ModeratorName { get; set; }

    /// <summary>
    /// 版主数量 (通常0或1)
    /// </summary>
    public int ModeratorCount { get; set; }

    /// <summary>
    /// Coworking空间数量
    /// </summary>
    public int CoworkingCount { get; set; }

    /// <summary>
    /// Meetup数量
    /// </summary>
    public int MeetupCount { get; set; }

    /// <summary>
    /// 评论数量
    /// </summary>
    public int ReviewCount { get; set; }
}

/// <summary>
/// 地理位置
/// </summary>
public class GeoLocation
{
    public double Lat { get; set; }
    public double Lon { get; set; }
}
