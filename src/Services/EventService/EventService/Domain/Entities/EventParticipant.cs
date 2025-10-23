using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

namespace EventService.Domain.Entities;

/// <summary>
/// EventParticipant 实体 - 参与者
/// </summary>
[Table("event_participants")]
public class EventParticipant : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; private set; }

    [Required]
    [Column("event_id")]
    public Guid EventId { get; private set; }

    [Required]
    [Column("user_id")]
    public Guid UserId { get; private set; }

    [MaxLength(20)]
    [Column("status")]
    public string Status { get; private set; } = "registered";

    [MaxLength(20)]
    [Column("payment_status")]
    public string PaymentStatus { get; private set; } = "pending";

    [Column("registered_at")]
    public DateTime RegisteredAt { get; private set; }

    // 公共无参构造函数 (Supabase 需要)
    public EventParticipant() { }

    /// <summary>
    /// 创建参与者 - 工厂方法
    /// </summary>
    public static EventParticipant Create(Guid eventId, Guid userId, string? paymentStatus = null)
    {
        return new EventParticipant
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            Status = "registered",
            PaymentStatus = paymentStatus ?? "pending",
            RegisteredAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 更新状态
    /// </summary>
    public void UpdateStatus(string status)
    {
        Status = status;
    }

    /// <summary>
    /// 更新支付状态
    /// </summary>
    public void UpdatePaymentStatus(string paymentStatus)
    {
        PaymentStatus = paymentStatus;
    }
}
