using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

namespace EventService.Domain.Entities;

/// <summary>
///     活动邀请实体 - 用于邀请用户参加活动
/// </summary>
[Table("event_invitations")]
public class EventInvitation : BaseModel
{
    /// <summary>
    ///     邀请ID
    /// </summary>
    [PrimaryKey("id")]
    public Guid Id { get; set; }

    /// <summary>
    ///     活动ID
    /// </summary>
    [Required]
    [Column("event_id")]
    public Guid EventId { get; set; }

    /// <summary>
    ///     邀请人ID（发起邀请的用户）
    /// </summary>
    [Required]
    [Column("inviter_id")]
    public Guid InviterId { get; set; }

    /// <summary>
    ///     被邀请人ID
    /// </summary>
    [Required]
    [Column("invitee_id")]
    public Guid InviteeId { get; set; }

    /// <summary>
    ///     邀请状态: pending, accepted, rejected, expired
    /// </summary>
    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "pending";

    /// <summary>
    ///     邀请留言
    /// </summary>
    [Column("message")]
    public string? Message { get; set; }

    /// <summary>
    ///     创建时间
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    ///     回复时间
    /// </summary>
    [Column("responded_at")]
    public DateTime? RespondedAt { get; set; }

    /// <summary>
    ///     过期时间
    /// </summary>
    [Column("expires_at")]
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    ///     创建邀请 - 工厂方法
    /// </summary>
    public static EventInvitation Create(Guid eventId, Guid inviterId, Guid inviteeId, string? message = null)
    {
        return new EventInvitation
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            InviterId = inviterId,
            InviteeId = inviteeId,
            Status = "pending",
            Message = message,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7) // 邀请7天后过期
        };
    }

    /// <summary>
    ///     接受邀请
    /// </summary>
    public void Accept()
    {
        Status = "accepted";
        RespondedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     拒绝邀请
    /// </summary>
    public void Reject()
    {
        Status = "rejected";
        RespondedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     检查邀请是否已过期
    /// </summary>
    public bool IsExpired()
    {
        return ExpiresAt.HasValue && DateTime.UtcNow > ExpiresAt.Value;
    }

    /// <summary>
    ///     检查邀请是否待处理
    /// </summary>
    public bool IsPending()
    {
        return Status == "pending" && !IsExpired();
    }
}
