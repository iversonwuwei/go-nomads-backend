using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

namespace TravelPlanningService.Models;

/// <summary>
/// 旅行计划实体模型
/// </summary>
[Table("travel_plans")]
public class TravelPlan : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Required]
    [Column("start_date")]
    public DateTime StartDate { get; set; }

    [Required]
    [Column("end_date")]
    public DateTime EndDate { get; set; }

    [Column("cities")]
    public Guid[]? Cities { get; set; }

    [Column("budget")]
    public decimal? Budget { get; set; }

    [MaxLength(10)]
    [Column("currency")]
    public string Currency { get; set; } = "USD";

    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "planning"; // planning, booked, ongoing, completed, cancelled

    [Column("is_public")]
    public bool IsPublic { get; set; }

    [Column("itinerary")]
    public string? Itinerary { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_by")]
    public Guid? CreatedBy { get; set; }

    [Column("updated_by")]
    public Guid? UpdatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 旅行计划协作者实体模型
/// </summary>
[Table("travel_plan_collaborators")]
public class TravelPlanCollaborator : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Required]
    [Column("travel_plan_id")]
    public Guid TravelPlanId { get; set; }

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [MaxLength(20)]
    [Column("role")]
    public string Role { get; set; } = "viewer"; // owner, editor, viewer

    [Column("added_at")]
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
