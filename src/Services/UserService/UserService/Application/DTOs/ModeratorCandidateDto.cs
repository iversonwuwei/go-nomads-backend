using UserService.Domain.Entities;

namespace UserService.Application.DTOs;

/// <summary>
///     版主候选人 DTO
///     用于指定版主时的用户列表显示
/// </summary>
public class ModeratorCandidateDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = "user";
    public int MembershipLevel { get; set; }
    public string MembershipLevelName { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>
    ///     从 UserWithMembership 实体创建 DTO
    /// </summary>
    public static ModeratorCandidateDto FromEntity(UserWithMembership entity)
    {
        var membershipLevel = entity.MembershipLevel;
        var membershipLevelName = ((MembershipLevel)membershipLevel).ToString();

        return new ModeratorCandidateDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Email = entity.Email,
            AvatarUrl = entity.AvatarUrl,
            Role = entity.RoleName,
            MembershipLevel = membershipLevel,
            MembershipLevelName = membershipLevelName,
            IsAdmin = entity.IsAdmin,
            CreatedAt = entity.CreatedAt
        };
    }
}
