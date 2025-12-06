using Postgrest.Attributes;
using Postgrest.Models;

namespace UserService.Domain.Entities;

/// <summary>
///     用户实体（带会员等级信息）- 用于 JOIN 查询优化
///     通过 Supabase 的 select 关联查询一次性获取用户和会员信息
/// </summary>
[Table("users")]
public class UserWithMembership : BaseModel
{
    [PrimaryKey("id", true)]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("avatar")]
    public string? AvatarUrl { get; set; }

    [Column("role_id")]
    public string RoleId { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    ///     关联的角色信息（通过 Supabase JOIN 获取）
    /// </summary>
    [Reference(typeof(Role), ReferenceAttribute.JoinType.Left, false, "role_id", "id")]
    public Role? Role { get; set; }

    /// <summary>
    ///     关联的会员信息（通过 Supabase JOIN 获取）
    ///     注意: 外键在 memberships 表的 user_id 字段
    /// </summary>
    [Reference(typeof(Membership), ReferenceAttribute.JoinType.Left, true, "id", "user_id")]
    public List<Membership>? Memberships { get; set; }

    /// <summary>
    ///     获取角色名称（便捷属性）
    /// </summary>
    public string RoleName => Role?.Name ?? "user";

    /// <summary>
    ///     是否是管理员
    /// </summary>
    public bool IsAdmin => RoleName.Equals("admin", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    ///     获取当前有效的会员等级
    /// </summary>
    public int MembershipLevel
    {
        get
        {
            var activeMembership = Memberships?
                .Where(m => !m.IsExpired)
                .OrderByDescending(m => m.Level)
                .FirstOrDefault();

            return activeMembership?.Level ?? 0;
        }
    }

    /// <summary>
    ///     是否可以成为版主候选人（Pro及以上会员或Admin）
    /// </summary>
    public bool CanBeModeratorCandidate => IsAdmin || MembershipLevel >= (int)Entities.MembershipLevel.Pro;
}
