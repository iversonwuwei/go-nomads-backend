using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

namespace EventService.Domain.Entities;

/// <summary>
/// EventFollower 实体 - 关注者
/// </summary>
[Table("event_followers")]
public class EventFollower : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; private set; }

    [Required]
    [Column("event_id")]
    public Guid EventId { get; private set; }

    [Required]
    [Column("user_id")]
    public Guid UserId { get; private set; }

    [Column("followed_at")]
    public DateTime FollowedAt { get; private set; }

    [Column("notification_enabled")]
    public bool NotificationEnabled { get; private set; }

    // 公共无参构造函数 (Supabase 需要)
    public EventFollower() { }

    /// <summary>
    /// 创建关注者 - 工厂方法
    /// </summary>
    public static EventFollower Create(Guid eventId, Guid userId, bool notificationEnabled = true)
    {
        return new EventFollower
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            FollowedAt = DateTime.UtcNow,
            NotificationEnabled = notificationEnabled
        };
    }

    /// <summary>
    /// 更新通知设置
    /// </summary>
    public void UpdateNotificationSetting(bool enabled)
    {
        NotificationEnabled = enabled;
    }
}
