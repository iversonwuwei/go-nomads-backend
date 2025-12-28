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

    // 旅行历史列表（用于 Profile 页面显示，最多返回最近 10 条已确认的旅行记录）
    public List<TravelHistoryDto>? TravelHistory { get; set; }

    // 旅行统计数据（从 travel_history 表计算，去重后的国家/城市数）
    public UserTravelStatsDto? Stats { get; set; }
}

/// <summary>
///     用户旅行统计 DTO（嵌套在 UserDto 中，用于 Profile 页面显示）
/// </summary>
public class UserTravelStatsDto
{
    /// <summary>
    ///     访问过的国家数（去重）
    /// </summary>
    public int CountriesVisited { get; set; }
    
    /// <summary>
    ///     访问过的城市数（去重）
    /// </summary>
    public int CitiesVisited { get; set; }
    
    /// <summary>
    ///     总旅行天数
    /// </summary>
    public int TotalDays { get; set; }
    
    /// <summary>
    ///     总行程数（已确认）
    /// </summary>
    public int TotalTrips { get; set; }
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