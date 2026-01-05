using System.ComponentModel.DataAnnotations;
using Postgrest.Attributes;
using Postgrest.Models;

namespace EventService.Models;

/// <summary>
///     活动/聚会实体模型
/// </summary>
[Table("events")]
public class Event : BaseModel
{
    [PrimaryKey("id")] public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")] public string? Description { get; set; }

    [Required] [Column("organizer_id")] public Guid OrganizerId { get; set; }

    [Column("city_id")] public Guid? CityId { get; set; }

    [MaxLength(200)] [Column("location")] public string? Location { get; set; }

    [Column("address")] public string? Address { get; set; }

    [Column("image_url")] public string? ImageUrl { get; set; }

    [Column("images")] public string[]? Images { get; set; }

    [MaxLength(50)]
    [Column("category")]
    public string? Category { get; set; } // networking, workshop, social, sports, cultural, tech, business, other

    [Required] [Column("start_time")] public DateTime StartTime { get; set; }

    [Column("end_time")] public DateTime? EndTime { get; set; }

    [Column("max_participants")] public int? MaxParticipants { get; set; }

    [Column("current_participants")] public int CurrentParticipants { get; set; } = 0;

    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "upcoming"; // upcoming, ongoing, completed, cancelled

    [MaxLength(20)]
    [Column("location_type")]
    public string LocationType { get; set; } = "physical"; // physical, online, hybrid

    [Column("meeting_link")] public string? MeetingLink { get; set; }

    [Column("latitude")] public decimal? Latitude { get; set; }

    [Column("longitude")] public decimal? Longitude { get; set; }

    [Column("tags")] public string[]? Tags { get; set; }

    [Column("is_featured")] public bool IsFeatured { get; set; }

    // 审计字段
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("created_by")] public Guid? CreatedBy { get; set; }

    [Column("updated_by")] public Guid? UpdatedBy { get; set; }

    // 软删除字段
    [Column("is_deleted")] public bool IsDeleted { get; set; } = false;

    [Column("deleted_at")] public DateTime? DeletedAt { get; set; }

    [Column("deleted_by")] public Guid? DeletedBy { get; set; }

    /// <summary>
    ///     标记为已删除
    /// </summary>
    public void MarkAsDeleted(Guid? deletedBy = null)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = deletedBy;
    }
}

/// <summary>
///     活动参与者实体模型
/// </summary>
[Table("event_participants")]
public class EventParticipant : BaseModel
{
    [PrimaryKey("id")] public Guid Id { get; set; }

    [Required] [Column("event_id")] public Guid EventId { get; set; }

    [Required] [Column("user_id")] public Guid UserId { get; set; }

    [MaxLength(20)]
    [Column("status")]
    public string Status { get; set; } = "registered"; // registered, attended, cancelled, no-show

    [Column("registered_at")] public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
///     活动关注者实体模型
/// </summary>
[Table("event_followers")]
public class EventFollower : BaseModel
{
    [PrimaryKey("id")] public Guid Id { get; set; }

    [Required] [Column("event_id")] public Guid EventId { get; set; }

    [Required] [Column("user_id")] public Guid UserId { get; set; }

    [Column("followed_at")] public DateTime FollowedAt { get; set; } = DateTime.UtcNow;

    [Column("notification_enabled")] public bool NotificationEnabled { get; set; } = true;
}