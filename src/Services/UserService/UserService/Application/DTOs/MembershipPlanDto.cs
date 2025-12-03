using UserService.Domain.Entities;

namespace UserService.Application.DTOs;

/// <summary>
///     会员计划响应 DTO
/// </summary>
public class MembershipPlanResponse
{
    public string Id { get; set; } = string.Empty;
    public int Level { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal PriceYearly { get; set; }
    public decimal PriceMonthly { get; set; }
    public string Currency { get; set; } = "USD";
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public List<string> Features { get; set; } = new();
    public int AiUsageLimit { get; set; }
    public bool CanUseAI { get; set; }
    public bool CanApplyModerator { get; set; }
    public decimal ModeratorDeposit { get; set; }

    public static MembershipPlanResponse FromEntity(MembershipPlan entity)
    {
        return new MembershipPlanResponse
        {
            Id = entity.Id,
            Level = entity.Level,
            Name = entity.Name,
            Description = entity.Description,
            PriceYearly = entity.PriceYearly,
            PriceMonthly = entity.PriceMonthly,
            Currency = entity.Currency,
            Icon = entity.Icon,
            Color = entity.Color,
            Features = entity.Features ?? new List<string>(),
            AiUsageLimit = entity.AiUsageLimit,
            CanUseAI = entity.CanUseAI,
            CanApplyModerator = entity.CanApplyModerator,
            ModeratorDeposit = entity.ModeratorDeposit
        };
    }
}
