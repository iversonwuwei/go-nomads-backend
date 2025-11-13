using Postgrest.Attributes;
using Postgrest.Models;

namespace MessageService.Domain.Entities;

/// <summary>
/// 通知实体 - 用于版主申请审批、系统消息等通知
/// </summary>
[Table("notifications")]
public class Notification : BaseModel
{
    /// <summary>
    /// 通知ID
    /// </summary>
    [PrimaryKey("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// 接收用户ID
    /// </summary>
    [Column("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 通知标题
    /// </summary>
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 通知消息内容
    /// </summary>
    [Column("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 通知类型
    /// </summary>
    [Column("type")]
    public string Type { get; set; } = string.Empty; // moderator_application, moderator_approved, moderator_rejected, city_update, system_announcement, other

    /// <summary>
    /// 关联的资源ID（如城市ID、申请ID等）
    /// </summary>
    [Column("related_id")]
    public string? RelatedId { get; set; }

    /// <summary>
    /// 元数据（JSON格式，存储额外信息）
    /// </summary>
    [Column("metadata")]
    public string? Metadata { get; set; }

    /// <summary>
    /// 是否已读
    /// </summary>
    [Column("is_read")]
    public bool IsRead { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 阅读时间
    /// </summary>
    [Column("read_at")]
    public DateTime? ReadAt { get; set; }
}
