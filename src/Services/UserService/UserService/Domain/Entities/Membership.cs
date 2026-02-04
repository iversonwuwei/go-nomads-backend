using Postgrest.Attributes;
using Postgrest.Models;

namespace UserService.Domain.Entities;

/// <summary>
///     会员等级枚举
/// </summary>
public enum MembershipLevel
{
    Free = 0,
    Basic = 1,
    Pro = 2,
    Premium = 3
}

/// <summary>
///     会员实体 - DDD 领域实体
/// </summary>
[Table("memberships")]
public class Membership : BaseModel
{
    public Membership()
    {
        Id = Guid.NewGuid().ToString();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    [PrimaryKey("id", true)]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    [Column("level")]
    public int Level { get; set; } = (int)MembershipLevel.Free;

    [Column("start_date")]
    public DateTime StartDate { get; set; }

    [Column("expiry_date")]
    public DateTime? ExpiryDate { get; set; }

    [Column("auto_renew")]
    public bool AutoRenew { get; set; } = false;

    [Column("ai_usage_this_month")]
    public int AiUsageThisMonth { get; set; } = 0;

    [Column("ai_usage_reset_date")]
    public DateTime? AiUsageResetDate { get; set; }

    [Column("moderator_deposit")]
    public decimal? ModeratorDeposit { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    #region 计算属性（不映射到数据库）

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public MembershipLevel MembershipLevel => (MembershipLevel)Level;

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.UtcNow;

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public bool IsActive => !IsExpired && Level > (int)MembershipLevel.Free;

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public int RemainingDays => ExpiryDate.HasValue 
        ? Math.Max(0, (ExpiryDate.Value - DateTime.UtcNow).Days) 
        : 0;

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public bool IsExpiringSoon => RemainingDays > 0 && RemainingDays <= 7;

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public int AiUsageLimit => MembershipLevel switch
    {
        MembershipLevel.Free => 3,    // 免费用户每月3次
        MembershipLevel.Basic => 30,  // 基础会员每月30次
        MembershipLevel.Pro => 60,    // 专业会员每月60次
        MembershipLevel.Premium => -1, // 高级会员无限制
        _ => 3
    };

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public bool CanUseAI
    {
        get
        {
            // Premium 会员无限制
            if (MembershipLevel == MembershipLevel.Premium && !IsExpired)
                return true;

            // 其他用户检查配额（包括免费用户）
            if (AiUsageLimit < 0) return true; // 无限制
            return AiUsageThisMonth < AiUsageLimit;
        }
    }

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public bool CanApplyModerator => Level >= (int)MembershipLevel.Pro && !IsExpired;

    #endregion

    #region 工厂方法

    public static Membership CreateFree(string userId)
    {
        return new Membership
        {
            UserId = userId,
            Level = (int)MembershipLevel.Free,
            StartDate = DateTime.UtcNow
        };
    }

    public static Membership Create(string userId, MembershipLevel level, int durationDays = 365)
    {
        var now = DateTime.UtcNow;
        return new Membership
        {
            UserId = userId,
            Level = (int)level,
            StartDate = now,
            ExpiryDate = now.AddDays(durationDays),
            AutoRenew = true,
            AiUsageResetDate = new DateTime(now.Year, now.Month, 1).AddMonths(1)
        };
    }

    #endregion

    #region 领域方法

    public void Upgrade(MembershipLevel newLevel, int durationDays = 365)
    {
        var now = DateTime.UtcNow;
        Level = (int)newLevel;
        StartDate = now;
        ExpiryDate = now.AddDays(durationDays);
        AutoRenew = true;
        UpdatedAt = now;
    }

    public void IncrementAiUsage()
    {
        ResetAiUsageIfNeeded();
        AiUsageThisMonth++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ResetAiUsageIfNeeded()
    {
        var now = DateTime.UtcNow;
        if (!AiUsageResetDate.HasValue || now >= AiUsageResetDate.Value)
        {
            AiUsageThisMonth = 0;
            AiUsageResetDate = new DateTime(now.Year, now.Month, 1).AddMonths(1);
        }
    }

    public void SetAutoRenew(bool enabled)
    {
        AutoRenew = enabled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void PayModeratorDeposit(decimal amount)
    {
        ModeratorDeposit = (ModeratorDeposit ?? 0) + amount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Renew(int durationDays = 365)
    {
        var now = DateTime.UtcNow;
        // 如果未过期，从过期日延续；否则从今天开始
        var startFrom = ExpiryDate.HasValue && ExpiryDate.Value > now 
            ? ExpiryDate.Value 
            : now;
        ExpiryDate = startFrom.AddDays(durationDays);
        UpdatedAt = now;
    }

    public void Downgrade()
    {
        Level = (int)MembershipLevel.Free;
        ExpiryDate = null;
        AutoRenew = false;
        AiUsageThisMonth = 0;
        UpdatedAt = DateTime.UtcNow;
    }

    public void PayDeposit(decimal amount)
    {
        ModeratorDeposit = (ModeratorDeposit ?? 0) + amount;
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion
}
