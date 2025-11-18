using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

namespace UserService.Domain.Entities;

/// <summary>
///     Role 实体
/// </summary>
[Table("roles")]
public class Role : BaseModel
{
    // 公共无参构造函数 (ORM 需要)
    public Role()
    {
        Id = Guid.NewGuid().ToString();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    [PrimaryKey("id")] [Column("id")] public string Id { get; set; } = string.Empty;

    [Required] [Column("name")] public string Name { get; set; } = string.Empty;

    [Column("description")] public string? Description { get; set; }

    [Column("created_at")] public DateTime CreatedAt { get; set; }

    [Column("updated_at")] public DateTime UpdatedAt { get; set; }

    #region 工厂方法

    /// <summary>
    ///     创建角色
    /// </summary>
    public static Role Create(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("角色名称不能为空", nameof(name));

        return new Role
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    #endregion

    #region 领域方法

    /// <summary>
    ///     更新角色信息
    /// </summary>
    public void Update(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("角色名称不能为空", nameof(name));

        Name = name;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion

    #region 预定义角色常量

    public static class RoleNames
    {
        public const string User = "user";
        public const string Admin = "admin";
    }

    #endregion
}