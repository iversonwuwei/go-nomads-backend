using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

namespace EventService.Domain.Entities;

/// <summary>
///     EventParticipant 实体 - 参与者
/// </summary>
[Table("event_participants")]
public class EventParticipant : BaseModel
{
    [PrimaryKey("id")] public Guid Id { get; set; }

    [Required] [Column("event_id")] public Guid EventId { get; set; }

    [Required] [Column("user_id")] public Guid UserId { get; set; }

    [MaxLength(20)] [Column("status")] public string Status { get; set; } = "registered";

    [Column("registered_at")] public DateTime RegisteredAt { get; set; }

    // 公共无参构造函数 (Supabase 需要)

    /// <summary>
    ///     创建参与者 - 工厂方法
    /// </summary>
    public static EventParticipant Create(Guid eventId, Guid userId)
    {
        return new EventParticipant
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            Status = "registered",
            RegisteredAt = DateTime.UtcNow
        };
    }

    /// <summary>
    ///     更新状态
    /// </summary>
    public void UpdateStatus(string status)
    {
        Status = status;
    }
}