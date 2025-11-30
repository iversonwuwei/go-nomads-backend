using Postgrest.Attributes;
using Postgrest.Models;

namespace UserService.Domain.Entities;

/// <summary>
///     用户实体（带角色信息）- 用于 JOIN 查询优化
///     通过 Supabase 的 select 关联查询一次性获取用户和角色
/// </summary>
[Table("users")]
public class UserWithRole : BaseModel
{
    [PrimaryKey("id", true)]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("phone")]
    public string Phone { get; set; } = string.Empty;

    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [Column("role_id")]
    public string RoleId { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    ///     关联的角色信息（通过 Supabase JOIN 获取）
    /// </summary>
    [Reference(typeof(Role), ReferenceAttribute.JoinType.Left, false, "role_id", "id")]
    public Role? Role { get; set; }

    /// <summary>
    ///     获取角色名称（便捷属性）
    /// </summary>
    public string RoleName => Role?.Name ?? "user";

    /// <summary>
    ///     验证密码
    /// </summary>
    public bool ValidatePassword(string password)
    {
        if (string.IsNullOrEmpty(PasswordHash)) return false;
        return GoNomads.Shared.Security.PasswordHasher.VerifyPassword(password, PasswordHash);
    }
}
