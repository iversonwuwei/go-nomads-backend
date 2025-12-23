namespace UserService.Application.DTOs;

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; } // 用户头像URL
    public string? Bio { get; set; } // 个人简介
    public string Role { get; set; } = "user"; // 用户角色: user, admin, etc.
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // 技能和兴趣列表
    public List<UserSkillDto>? Skills { get; set; }
    public List<UserInterestDto>? Interests { get; set; }
    
    // 会员信息
    public UserMembershipDto? Membership { get; set; }
    
    // 最新旅行历史（用于 Profile 页面显示）
    public TravelHistoryDto? LatestTravelHistory { get; set; }
}

/// <summary>
///     用户会员信息 DTO（嵌套在 UserDto 中）
/// </summary>
public class UserMembershipDto
{
    public int Level { get; set; }
    public string LevelName { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
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
}

/// <summary>
///     用户基本信息 DTO（简化版，用于跨服务调用）
/// </summary>
public class UserBasicDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string? Email { get; set; }
}