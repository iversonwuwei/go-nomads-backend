using Postgrest.Attributes;
using Postgrest.Models;

namespace AIService.Models;

/// <summary>
///     基础领域实体
/// </summary>
public abstract class BaseAIModel : BaseModel
{
    [PrimaryKey("id")] public Guid Id { get; set; } = Guid.NewGuid();

    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")] public DateTime? UpdatedAt { get; set; }

    [Column("deleted_at")] public DateTime? DeletedAt { get; set; }

    /// <summary>
    ///     软删除标记
    /// </summary>
    public bool IsDeleted => DeletedAt.HasValue;

    /// <summary>
    ///     标记删除
    /// </summary>
    public virtual void Delete()
    {
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     更新时间戳
    /// </summary>
    public virtual void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}