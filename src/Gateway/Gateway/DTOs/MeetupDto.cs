using System.Text.Json.Serialization;

namespace Gateway.DTOs;

/// <summary>
///     组织者信息
/// </summary>
public class OrganizerDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

/// <summary>
///     活动 DTO
/// </summary>
public class MeetupDto
{
    /// <summary>
    ///     活动ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    ///     活动标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    ///     活动描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     开始时间
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    ///     结束时间
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    ///     地点
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    ///     城市ID
    /// </summary>
    public string CityId { get; set; } = string.Empty;

    /// <summary>
    ///     城市名称
    /// </summary>
    public string? CityName { get; set; }

    /// <summary>
    ///     参与人数
    /// </summary>
    public int ParticipantCount { get; set; }

    /// <summary>
    ///     最大参与人数
    /// </summary>
    public int? MaxParticipants { get; set; }

    /// <summary>
    ///     活动图片URL
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    ///     活动状态 (upcoming, ongoing, completed, cancelled)
    /// </summary>
    public string Status { get; set; } = "upcoming";

    /// <summary>
    ///     创建者ID
    /// </summary>
    public string? CreatorId { get; set; }

    /// <summary>
    ///     创建者名称
    /// </summary>
    public string? CreatorName { get; set; }

    /// <summary>
    ///     组织者信息（从 Event Service 获取）
    /// </summary>
    [JsonInclude]
    public OrganizerDto? Organizer { get; set; }

    /// <summary>
    ///     当前用户是否已参加
    /// </summary>
    public bool IsParticipant { get; set; }

    /// <summary>
    ///     当前用户是否是组织者
    /// </summary>
    public bool IsOrganizer { get; set; }
}