namespace MessageService.Application.DTOs;

/// <summary>
///     通知响应 DTO
/// </summary>
public class NotificationDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? RelatedId { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}

/// <summary>
///     创建通知请求 DTO
/// </summary>
public class CreateNotificationDto
{
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? RelatedId { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
///     发送通知给管理员请求 DTO
/// </summary>
public class SendToAdminsDto
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? RelatedId { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
///     批量创建通知请求 DTO
/// </summary>
public class CreateBatchNotificationDto
{
    public List<string> UserIds { get; set; } = new();
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? RelatedId { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
///     批量通知响应 DTO
/// </summary>
public class BatchNotificationResponse
{
    public int CreatedCount { get; set; }
    public List<string> NotificationIds { get; set; } = new();
}

/// <summary>
///     批量标记已读请求 DTO
/// </summary>
public class MarkMultipleAsReadDto
{
    public List<string> NotificationIds { get; set; } = new();
}

/// <summary>
///     通知统计 DTO
/// </summary>
public class NotificationStatsDto
{
    public int UnreadCount { get; set; }
    public int TotalCount { get; set; }
}