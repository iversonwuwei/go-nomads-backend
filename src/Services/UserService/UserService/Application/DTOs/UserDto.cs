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