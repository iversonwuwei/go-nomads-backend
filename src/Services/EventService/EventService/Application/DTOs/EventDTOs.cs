using System.ComponentModel.DataAnnotations;

namespace EventService.Application.DTOs;

/// <summary>
/// 创建 Meetup/Event 请求 DTO
/// </summary>
public class CreateEventRequest
{
    [Required(ErrorMessage = "标题不能为空")]
    [MaxLength(200, ErrorMessage = "标题最多200个字符")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000, ErrorMessage = "描述最多2000个字符")]
    public string? Description { get; set; }

    public Guid? CityId { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    public string? Address { get; set; }

    public string? ImageUrl { get; set; }

    public List<string>? Images { get; set; }

    [MaxLength(50)]
    public string? Category { get; set; }

    [Required(ErrorMessage = "开始时间不能为空")]
    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    [Range(1, 10000, ErrorMessage = "最大参与人数必须在1-10000之间")]
    public int? MaxParticipants { get; set; }

    [MaxLength(20)]
    public string LocationType { get; set; } = "physical"; // physical, online, hybrid

    public string? MeetingLink { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public List<string>? Tags { get; set; }
}

/// <summary>
/// 更新 Meetup/Event 请求 DTO
/// </summary>
public class UpdateEventRequest
{
    [MaxLength(200)]
    public string? Title { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public Guid? CityId { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    public string? Address { get; set; }

    public string? ImageUrl { get; set; }

    public List<string>? Images { get; set; }

    [MaxLength(50)]
    public string? Category { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    [Range(1, 10000)]
    public int? MaxParticipants { get; set; }

    [MaxLength(20)]
    public string? Status { get; set; }

    [MaxLength(20)]
    public string? LocationType { get; set; }

    public string? MeetingLink { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public List<string>? Tags { get; set; }
}

/// <summary>
/// Event 响应 DTO
/// </summary>
public class EventResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid OrganizerId { get; set; }
    public Guid? CityId { get; set; }
    public string? Location { get; set; }
    public string? Address { get; set; }
    public string? ImageUrl { get; set; }
    public List<string>? Images { get; set; }
    public string? Category { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? MaxParticipants { get; set; }
    public int CurrentParticipants { get; set; }
    public string Status { get; set; } = "upcoming";
    public string LocationType { get; set; } = "physical";
    public string? MeetingLink { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public List<string>? Tags { get; set; }
    public bool IsFeatured { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // 扩展字段
    public bool IsFollowing { get; set; }
    public bool IsParticipant { get; set; }
    public int FollowerCount { get; set; }
}

/// <summary>
/// 参加 Meetup 请求 DTO
/// </summary>
public class JoinEventRequest
{
}

/// <summary>
/// 关注 Meetup 请求 DTO
/// </summary>
public class FollowEventRequest
{
    public bool NotificationEnabled { get; set; } = true;
}

/// <summary>
/// 参与者响应 DTO
/// </summary>
public class ParticipantResponse
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public string Status { get; set; } = "registered";
    public DateTime RegisteredAt { get; set; }
}

/// <summary>
/// 关注者响应 DTO
/// </summary>
public class FollowerResponse
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public DateTime FollowedAt { get; set; }
    public bool NotificationEnabled { get; set; }
}
