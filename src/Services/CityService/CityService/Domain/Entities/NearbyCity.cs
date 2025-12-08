using Postgrest.Attributes;
using Postgrest.Models;

namespace CityService.Domain.Entities;

/// <summary>
///     附近城市实体
///     存储城市之间的相邻关系信息，包括距离、交通方式、旅行时间等
/// </summary>
[Table("nearby_cities")]
public class NearbyCity : BaseModel
{
    /// <summary>
    ///     记录ID (主键)
    /// </summary>
    [PrimaryKey("id")]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    ///     源城市ID (外键)
    /// </summary>
    [Column("source_city_id")]
    public string SourceCityId { get; set; } = string.Empty;

    /// <summary>
    ///     目标城市ID (外键，可能为null表示该城市不在数据库中)
    /// </summary>
    [Column("target_city_id")]
    public string? TargetCityId { get; set; }

    /// <summary>
    ///     目标城市名称
    /// </summary>
    [Column("target_city_name")]
    public string TargetCityName { get; set; } = string.Empty;

    /// <summary>
    ///     目标城市所属国家
    /// </summary>
    [Column("country")]
    public string Country { get; set; } = string.Empty;

    /// <summary>
    ///     距离 (公里)
    /// </summary>
    [Column("distance_km")]
    public double DistanceKm { get; set; }

    /// <summary>
    ///     主要交通方式 (train/bus/car/flight/ferry)
    /// </summary>
    [Column("transportation_type")]
    public string TransportationType { get; set; } = string.Empty;

    /// <summary>
    ///     预计旅行时间 (分钟)
    /// </summary>
    [Column("travel_time_minutes")]
    public int TravelTimeMinutes { get; set; }

    /// <summary>
    ///     城市亮点/特色 (JSON数组)
    /// </summary>
    [Column("highlights")]
    public List<string> Highlights { get; set; } = new();

    /// <summary>
    ///     数字游民相关特色 (JSON对象)
    /// </summary>
    [Column("nomad_features")]
    public NearbyCityNomadFeatures NomadFeatures { get; set; } = new();

    /// <summary>
    ///     城市图片URL
    /// </summary>
    [Column("image_url")]
    public string? ImageUrl { get; set; }

    /// <summary>
    ///     综合评分
    /// </summary>
    [Column("overall_score")]
    public double? OverallScore { get; set; }

    /// <summary>
    ///     目标城市纬度 (用于在地图上显示)
    /// </summary>
    [Column("latitude")]
    public double? Latitude { get; set; }

    /// <summary>
    ///     目标城市经度 (用于在地图上显示)
    /// </summary>
    [Column("longitude")]
    public double? Longitude { get; set; }

    /// <summary>
    ///     是否由AI生成
    /// </summary>
    [Column("is_ai_generated")]
    public bool IsAIGenerated { get; set; } = true;

    /// <summary>
    ///     创建时间
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     更新时间
    /// </summary>
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
///     附近城市的数字游民相关特色
/// </summary>
public class NearbyCityNomadFeatures
{
    /// <summary>
    ///     预计月生活成本 (美元)
    /// </summary>
    public double? MonthlyCostUsd { get; set; }

    /// <summary>
    ///     网络速度 (Mbps)
    /// </summary>
    public int? InternetSpeedMbps { get; set; }

    /// <summary>
    ///     联合办公空间数量
    /// </summary>
    public int? CoworkingSpaces { get; set; }

    /// <summary>
    ///     签证便利性描述
    /// </summary>
    public string? VisaInfo { get; set; }

    /// <summary>
    ///     安全评分 (1-5)
    /// </summary>
    public double? SafetyScore { get; set; }

    /// <summary>
    ///     生活质量描述
    /// </summary>
    public string? QualityOfLife { get; set; }
}
