using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

namespace EventService.Domain.Entities;

/// <summary>
///     聚会类型实体
/// </summary>
[Table("event_types")]
public class EventType : BaseModel
{
    [PrimaryKey("id")] 
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [Column("en_name")]
    public string EnName { get; set; } = string.Empty;

    [Column("description")] 
    public string? Description { get; set; }

    [MaxLength(50)]
    [Column("icon")] 
    public string? Icon { get; set; }

    [Column("sort_order")] 
    public int SortOrder { get; set; }

    [Column("is_active")] 
    public bool IsActive { get; set; } = true;

    [Column("is_system")] 
    public bool IsSystem { get; set; }

    [Column("created_at")] 
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")] 
    public DateTime UpdatedAt { get; set; }

    public EventType()
    {
    }

    /// <summary>
    ///     创建新的聚会类型 - 工厂方法
    /// </summary>
    public static EventType Create(
        string name,
        string enName,
        string? description = null,
        string? icon = null,
        int sortOrder = 0,
        bool isSystem = false)
    {
        return new EventType
        {
            Id = Guid.NewGuid(),
            Name = name,
            EnName = enName,
            Description = description,
            Icon = icon,
            SortOrder = sortOrder,
            IsActive = true,
            IsSystem = isSystem,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    ///     更新聚会类型信息
    /// </summary>
    public void Update(
        string? name = null,
        string? enName = null,
        string? description = null,
        string? icon = null,
        int? sortOrder = null,
        bool? isActive = null)
    {
        if (!string.IsNullOrWhiteSpace(name))
            Name = name;

        if (!string.IsNullOrWhiteSpace(enName))
            EnName = enName;

        if (description != null)
            Description = description;

        if (icon != null)
            Icon = icon;

        if (sortOrder.HasValue)
            SortOrder = sortOrder.Value;

        if (isActive.HasValue)
            IsActive = isActive.Value;

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     停用类型（软删除）
    /// </summary>
    public void Deactivate()
    {
        if (IsSystem)
            throw new InvalidOperationException("系统预设类型不能停用");

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     激活类型
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
