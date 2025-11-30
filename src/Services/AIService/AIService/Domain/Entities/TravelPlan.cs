using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

namespace AIService.Domain.Entities;

/// <summary>
///     AI 生成的旅行计划实体
/// </summary>
[Table("ai_travel_plans")]
public class AiTravelPlan : BaseModel
{
    [PrimaryKey("id")] public Guid Id { get; set; }

    /// <summary>
    ///     用户 ID - 关联请求生成计划的用户
    /// </summary>
    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    /// <summary>
    ///     城市 ID
    /// </summary>
    [Required]
    [Column("city_id")]
    public string CityId { get; set; } = string.Empty;

    /// <summary>
    ///     城市名称
    /// </summary>
    [Required]
    [Column("city_name")]
    public string CityName { get; set; } = string.Empty;

    /// <summary>
    ///     城市图片 URL
    /// </summary>
    [Column("city_image")]
    public string? CityImage { get; set; }

    /// <summary>
    ///     旅行天数
    /// </summary>
    [Column("duration")]
    public int Duration { get; set; }

    /// <summary>
    ///     预算等级: low, medium, high
    /// </summary>
    [Column("budget_level")]
    public string BudgetLevel { get; set; } = "medium";

    /// <summary>
    ///     旅行风格: adventure, relaxation, culture, nightlife
    /// </summary>
    [Column("travel_style")]
    public string TravelStyle { get; set; } = "culture";

    /// <summary>
    ///     兴趣标签 (JSON 数组)
    /// </summary>
    [Column("interests")]
    public string[]? Interests { get; set; }

    /// <summary>
    ///     出发地
    /// </summary>
    [Column("departure_location")]
    public string? DepartureLocation { get; set; }

    /// <summary>
    ///     出发日期
    /// </summary>
    [Column("departure_date")]
    public DateTime? DepartureDate { get; set; }

    /// <summary>
    ///     完整的旅行计划数据 (JSON 格式)
    /// </summary>
    [Column("plan_data")]
    public string PlanData { get; set; } = "{}";

    /// <summary>
    ///     计划状态: draft, published, archived
    /// </summary>
    [Column("status")]
    public string Status { get; set; } = "draft";

    /// <summary>
    ///     是否公开可见
    /// </summary>
    [Column("is_public")]
    public bool IsPublic { get; set; } = false;

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
