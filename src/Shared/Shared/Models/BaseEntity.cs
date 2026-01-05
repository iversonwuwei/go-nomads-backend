using Postgrest.Attributes;
using Postgrest.Models;

namespace GoNomads.Shared.Models;

/// <summary>
///     实体基类 - 包含通用字段
/// </summary>
/// <remarks>
///     所有实体都应该继承此基类，以获得统一的审计字段和逻辑删除支持
/// </remarks>
public abstract class BaseEntity : BaseModel
{
    /// <summary>
    ///     创建时间
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     更新时间
    /// </summary>
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     创建者ID
    /// </summary>
    [Column("created_by")]
    public Guid? CreatedBy { get; set; }

    /// <summary>
    ///     更新者ID
    /// </summary>
    [Column("updated_by")]
    public Guid? UpdatedBy { get; set; }

    /// <summary>
    ///     是否已删除（逻辑删除标记）
    /// </summary>
    [Column("is_deleted")]
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    ///     删除时间
    /// </summary>
    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    ///     删除者ID
    /// </summary>
    [Column("deleted_by")]
    public Guid? DeletedBy { get; set; }

    /// <summary>
    ///     标记为已删除
    /// </summary>
    /// <param name="deletedBy">删除操作执行者ID</param>
    public void MarkAsDeleted(Guid? deletedBy = null)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = deletedBy;
    }

    /// <summary>
    ///     恢复删除
    /// </summary>
    /// <param name="restoredBy">恢复操作执行者ID</param>
    public void Restore(Guid? restoredBy = null)
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = restoredBy;
    }
}

/// <summary>
///     带有主键的实体基类
/// </summary>
public abstract class BaseEntityWithId : BaseEntity
{
    /// <summary>
    ///     主键ID
    /// </summary>
    [PrimaryKey("id")]
    public Guid Id { get; set; }
}
