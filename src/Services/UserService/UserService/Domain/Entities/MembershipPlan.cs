using Postgrest.Attributes;
using Postgrest.Models;

namespace UserService.Domain.Entities;

/// <summary>
///     会员计划实体
/// </summary>
[Table("membership_plans")]
public class MembershipPlan : BaseModel
{
    [PrimaryKey("id", true)]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Column("level")]
    public int Level { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("price_yearly")]
    public decimal PriceYearly { get; set; }

    [Column("price_monthly")]
    public decimal PriceMonthly { get; set; }

    [Column("currency")]
    public string Currency { get; set; } = "USD";

    [Column("icon")]
    public string? Icon { get; set; }

    [Column("color")]
    public string? Color { get; set; }

    [Column("features")]
    public List<string> Features { get; set; } = new(); // JSONB 数组

    [Column("ai_usage_limit")]
    public int AiUsageLimit { get; set; }

    [Column("can_use_ai")]
    public bool CanUseAI { get; set; }

    [Column("can_apply_moderator")]
    public bool CanApplyModerator { get; set; }

    [Column("moderator_deposit")]
    public decimal ModeratorDeposit { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
