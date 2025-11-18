using Postgrest.Attributes;
using Postgrest.Models;

namespace CityService.Domain.Entities;

/// <summary>
///     数字游民指南实体
/// </summary>
[Table("digital_nomad_guides")]
public class DigitalNomadGuide : BaseModel
{
    /// <summary>
    ///     指南ID (主键)
    /// </summary>
    [PrimaryKey("id")]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    ///     城市ID (外键)
    /// </summary>
    [Column("city_id")]
    public string CityId { get; set; } = string.Empty;

    /// <summary>
    ///     城市名称
    /// </summary>
    [Column("city_name")]
    public string CityName { get; set; } = string.Empty;

    /// <summary>
    ///     概览
    /// </summary>
    [Column("overview")]
    public string Overview { get; set; } = string.Empty;

    /// <summary>
    ///     签证信息 (JSON序列化)
    /// </summary>
    [Column("visa_info")]
    public VisaInfo VisaInfo { get; set; } = new();

    /// <summary>
    ///     最佳区域列表 (JSON序列化)
    /// </summary>
    [Column("best_areas")]
    public List<BestArea> BestAreas { get; set; } = new();

    /// <summary>
    ///     工作空间推荐 (JSON数组)
    /// </summary>
    [Column("workspace_recommendations")]
    public List<string> WorkspaceRecommendations { get; set; } = new();

    /// <summary>
    ///     实用建议 (JSON数组)
    /// </summary>
    [Column("tips")]
    public List<string> Tips { get; set; } = new();

    /// <summary>
    ///     重要信息字典 (JSON对象)
    /// </summary>
    [Column("essential_info")]
    public Dictionary<string, string> EssentialInfo { get; set; } = new();

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
///     签证信息
/// </summary>
public class VisaInfo
{
    public string Type { get; set; } = string.Empty;
    public int Duration { get; set; }
    public string Requirements { get; set; } = string.Empty;
    public double Cost { get; set; }
    public string Process { get; set; } = string.Empty;
}

/// <summary>
///     最佳区域
/// </summary>
public class BestArea
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double EntertainmentScore { get; set; }
    public string EntertainmentDescription { get; set; } = string.Empty;
    public double TourismScore { get; set; }
    public string TourismDescription { get; set; } = string.Empty;
    public double EconomyScore { get; set; }
    public string EconomyDescription { get; set; } = string.Empty;
    public double CultureScore { get; set; }
    public string CultureDescription { get; set; } = string.Empty;
}