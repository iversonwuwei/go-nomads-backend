namespace SearchService.Domain.Models;

/// <summary>
/// 共享办公空间搜索文档 - Elasticsearch索引模型
/// </summary>
public class CoworkingSearchDocument
{
    /// <summary>
    /// 唯一标识
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 城市ID
    /// </summary>
    public Guid? CityId { get; set; }

    /// <summary>
    /// 城市名称（冗余字段，便于搜索）
    /// </summary>
    public string? CityName { get; set; }

    /// <summary>
    /// 国家名称（冗余字段，便于搜索）
    /// </summary>
    public string? CountryName { get; set; }

    /// <summary>
    /// 地址
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// 描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 主图URL
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// 每日价格
    /// </summary>
    public decimal? PricePerDay { get; set; }

    /// <summary>
    /// 每月价格
    /// </summary>
    public decimal? PricePerMonth { get; set; }

    /// <summary>
    /// 每小时价格
    /// </summary>
    public decimal? PricePerHour { get; set; }

    /// <summary>
    /// 货币
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// 评分
    /// </summary>
    public decimal Rating { get; set; }

    /// <summary>
    /// 评论数
    /// </summary>
    public int ReviewCount { get; set; }

    /// <summary>
    /// WiFi速度
    /// </summary>
    public decimal? WifiSpeed { get; set; }

    /// <summary>
    /// 工位数
    /// </summary>
    public int? Desks { get; set; }

    /// <summary>
    /// 会议室数量
    /// </summary>
    public int? MeetingRooms { get; set; }

    /// <summary>
    /// 是否有会议室
    /// </summary>
    public bool HasMeetingRoom { get; set; }

    /// <summary>
    /// 是否提供咖啡
    /// </summary>
    public bool HasCoffee { get; set; }

    /// <summary>
    /// 是否有停车位
    /// </summary>
    public bool HasParking { get; set; }

    /// <summary>
    /// 是否24/7开放
    /// </summary>
    public bool Has247Access { get; set; }

    /// <summary>
    /// 设施列表
    /// </summary>
    public string[]? Amenities { get; set; }

    /// <summary>
    /// 容量
    /// </summary>
    public int? Capacity { get; set; }

    /// <summary>
    /// 纬度
    /// </summary>
    public decimal? Latitude { get; set; }

    /// <summary>
    /// 经度
    /// </summary>
    public decimal? Longitude { get; set; }

    /// <summary>
    /// 地理位置 (用于Elasticsearch geo_point)
    /// </summary>
    public GeoLocation? Location { get; set; }

    /// <summary>
    /// 电话
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// 邮箱
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// 网站
    /// </summary>
    public string? Website { get; set; }

    /// <summary>
    /// 营业时间
    /// </summary>
    public string? OpeningHours { get; set; }

    /// <summary>
    /// 是否激活
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 验证状态
    /// </summary>
    public string VerificationStatus { get; set; } = "unverified";

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// 搜索建议文本 (用于autocomplete)
    /// </summary>
    public string? Suggest { get; set; }

    /// <summary>
    /// 文档类型标识
    /// </summary>
    public string DocumentType => "coworking";
}
