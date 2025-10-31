using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

namespace CityService.Domain.Entities;

/// <summary>
/// 用户对城市的评论实体
/// </summary>
[Table("user_city_reviews")]
public class UserCityReview : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("city_id")]
    public string CityId { get; set; } = string.Empty;

    [Required]
    [Range(1, 5)]
    [Column("rating")]
    public int Rating { get; set; }

    /// <summary>
    /// 评论标题
    /// </summary>
    [Required]
    [MaxLength(200)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 评论内容
    /// </summary>
    [Required]
    [MaxLength(2000)]
    [Column("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 访问日期(可选)
    /// </summary>
    [Column("visit_date")]
    public DateTime? VisitDate { get; set; }

    [MaxLength(2000)]
    [Column("review_text")]
    public string? ReviewText { get; set; }

    /// <summary>
    /// 互联网质量评分 (1-5)
    /// </summary>
    [Range(1, 5)]
    [Column("internet_quality_score")]
    public int? InternetQualityScore { get; set; }

    /// <summary>
    /// 安全评分 (1-5)
    /// </summary>
    [Range(1, 5)]
    [Column("safety_score")]
    public int? SafetyScore { get; set; }

    /// <summary>
    /// 费用评分 (1-5)
    /// </summary>
    [Range(1, 5)]
    [Column("cost_score")]
    public int? CostScore { get; set; }

    /// <summary>
    /// 社区评分 (1-5)
    /// </summary>
    [Range(1, 5)]
    [Column("community_score")]
    public int? CommunityScore { get; set; }

    /// <summary>
    /// 天气评分 (1-5)
    /// </summary>
    [Range(1, 5)]
    [Column("weather_score")]
    public int? WeatherScore { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
