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

/// <summary>
///     AI 使用配额检查响应 DTO
/// </summary>
public class AiUsageCheckResponse
{
    /// <summary>是否可以使用 AI</summary>
    public bool CanUse { get; set; }

    /// <summary>会员等级</summary>
    public int Level { get; set; }

    /// <summary>会员等级名称</summary>
    public string LevelName { get; set; } = string.Empty;

    /// <summary>每月限制次数（-1 表示无限制）</summary>
    public int Limit { get; set; }

    /// <summary>本月已使用次数</summary>
    public int Used { get; set; }

    /// <summary>剩余可用次数（-1 表示无限制）</summary>
    public int Remaining { get; set; }

    /// <summary>是否无限制</summary>
    public bool IsUnlimited { get; set; }

    /// <summary>配额重置日期</summary>
    public DateTime? ResetDate { get; set; }
}
