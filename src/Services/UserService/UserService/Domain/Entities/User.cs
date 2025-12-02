using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using GoNomads.Shared.Security;
using Postgrest.Attributes;
using Postgrest.Models;

namespace UserService.Domain.Entities;

/// <summary>
///     User 聚合根 - DDD 领域实体
/// </summary>
[Table("users")]
public class User : BaseModel
{
    // 公共无参构造函数 (ORM 需要)
    public User()
    {
        Id = Guid.NewGuid().ToString();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    // Supabase ORM 需要公共 setter，使用 internal set 限制外部修改
    [PrimaryKey("id", true)]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Phone] [Column("phone")] public string? Phone { get; set; }

    [Column("avatar")] public string? AvatarUrl { get; set; }

    [Column("bio")] public string? Bio { get; set; }

    [Column("password_hash")] public string PasswordHash { get; set; } = string.Empty;

    [Column("role_id")] public string RoleId { get; set; } = string.Empty;

    [Column("created_at")] public DateTime CreatedAt { get; set; }

    [Column("updated_at")] public DateTime UpdatedAt { get; set; }

    #region 私有辅助方法

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region 工厂方法

    /// <summary>
    ///     创建用户（不带密码）
    /// </summary>
    public static User Create(string name, string email, string phone, string roleId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("用户名不能为空", nameof(name));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("邮箱不能为空", nameof(email));

        if (!IsValidEmail(email))
            throw new ArgumentException("邮箱格式不正确", nameof(email));

        return new User
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Email = email,
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone,
            RoleId = roleId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    ///     创建用户（带密码）
    /// </summary>
    public static User CreateWithPassword(string name, string email, string password, string phone, string roleId)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("密码不能为空", nameof(password));

        if (password.Length < 6)
            throw new ArgumentException("密码至少需要6个字符", nameof(password));

        var user = Create(name, email, phone, roleId);
        user.PasswordHash = PasswordHasher.HashPassword(password);
        return user;
    }

    #endregion

    #region 领域方法

    /// <summary>
    ///     更新用户信息（完整更新，所有字段必填）
    /// </summary>
    public void Update(string name, string email, string phone, string? avatarUrl = null, string? bio = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("用户名不能为空", nameof(name));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("邮箱不能为空", nameof(email));

        if (!IsValidEmail(email))
            throw new ArgumentException("邮箱格式不正确", nameof(email));

        Name = name;
        Email = email;
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone;
        if (avatarUrl != null)
            AvatarUrl = avatarUrl;
        if (bio != null)
            Bio = bio;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     部分更新用户信息（只更新非null字段）
    /// </summary>
    public void PartialUpdate(string? name = null, string? email = null, string? phone = null, string? avatarUrl = null, string? bio = null)
    {
        if (name != null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("用户名不能为空", nameof(name));
            Name = name;
        }

        if (email != null)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("邮箱不能为空", nameof(email));
            if (!IsValidEmail(email))
                throw new ArgumentException("邮箱格式不正确", nameof(email));
            Email = email;
        }

        if (phone != null)
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone;

        if (avatarUrl != null)
            AvatarUrl = avatarUrl;

        if (bio != null)
            Bio = bio;

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     更新头像
    /// </summary>
    public void UpdateAvatar(string avatarUrl)
    {
        if (string.IsNullOrWhiteSpace(avatarUrl))
            throw new ArgumentException("头像URL不能为空", nameof(avatarUrl));

        AvatarUrl = avatarUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     修改密码
    /// </summary>
    public void ChangePassword(string oldPassword, string newPassword)
    {
        // 验证旧密码
        if (!ValidatePassword(oldPassword)) throw new UnauthorizedAccessException("旧密码不正确");

        if (string.IsNullOrWhiteSpace(newPassword))
            throw new ArgumentException("新密码不能为空", nameof(newPassword));

        if (newPassword.Length < 6)
            throw new ArgumentException("新密码至少需要6个字符", nameof(newPassword));

        PasswordHash = PasswordHasher.HashPassword(newPassword);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     设置密码（管理员重置密码）
    /// </summary>
    public void SetPassword(string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
            throw new ArgumentException("密码不能为空", nameof(newPassword));

        if (newPassword.Length < 6)
            throw new ArgumentException("密码至少需要6个字符", nameof(newPassword));

        PasswordHash = PasswordHasher.HashPassword(newPassword);
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     验证密码
    /// </summary>
    public bool ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(PasswordHash))
            return false;

        return PasswordHasher.VerifyPassword(password, PasswordHash);
    }

    /// <summary>
    ///     更改角色
    /// </summary>
    public void ChangeRole(string newRoleId)
    {
        if (string.IsNullOrWhiteSpace(newRoleId))
            throw new ArgumentException("角色ID不能为空", nameof(newRoleId));

        RoleId = newRoleId;
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion
}