using UserService.Domain.Entities;

namespace UserService.Application.DTOs;

/// <summary>
///     会员信息响应 DTO
/// </summary>
public class MembershipResponse
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public int Level { get; set; }
    public string LevelName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool AutoRenew { get; set; }
    public int AiUsageThisMonth { get; set; }
    public int AiUsageLimit { get; set; }
    public decimal? ModeratorDeposit { get; set; }
    public bool IsActive { get; set; }
    public bool IsExpired { get; set; }
    public int RemainingDays { get; set; }
    public bool IsExpiringSoon { get; set; }
    public bool CanUseAI { get; set; }
    public bool CanApplyModerator { get; set; }

    public static MembershipResponse FromEntity(Membership entity)
    {
        return new MembershipResponse
        {
            Id = entity.Id,
            UserId = entity.UserId,
            Level = entity.Level,
            LevelName = entity.MembershipLevel.ToString(),
            StartDate = entity.StartDate,
            ExpiryDate = entity.ExpiryDate,
            AutoRenew = entity.AutoRenew,
            AiUsageThisMonth = entity.AiUsageThisMonth,
            AiUsageLimit = entity.AiUsageLimit,
            ModeratorDeposit = entity.ModeratorDeposit,
            IsActive = entity.IsActive,
            IsExpired = entity.IsExpired,
            RemainingDays = entity.RemainingDays,
            IsExpiringSoon = entity.IsExpiringSoon,
            CanUseAI = entity.CanUseAI,
            CanApplyModerator = entity.CanApplyModerator
        };
    }
}

/// <summary>
///     升级会员请求 DTO
/// </summary>
public class UpgradeMembershipRequest
{
    public int Level { get; set; }
    public int DurationDays { get; set; } = 365;
}

/// <summary>
///     缴纳保证金请求 DTO
/// </summary>
public class PayDepositRequest
{
    public decimal Amount { get; set; }
}

/// <summary>
///     设置自动续费请求 DTO
/// </summary>
public class SetAutoRenewRequest
{
    public bool Enabled { get; set; }
}
