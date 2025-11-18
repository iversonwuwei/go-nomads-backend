namespace MessageService.Application.DTOs;

/// <summary>
///     AI 任务消息
/// </summary>
public class AITaskMessage
{
    public string TaskId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string TaskType { get; set; } = string.Empty; // "plan" | "guide"
    public Dictionary<string, object> Parameters { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
///     AI 进度消息
/// </summary>
public class AIProgressMessage
{
    public string TaskId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public int Progress { get; set; } // 0-100
    public string Status { get; set; } = string.Empty; // "processing" | "completed" | "failed"
    public string CurrentStep { get; set; } = string.Empty;
    public string? Result { get; set; } // JSON 结果
    public string? Error { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
///     通用通知消息
/// </summary>
public class NotificationMessage
{
    public string UserId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "info" | "warning" | "error" | "success"
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public Dictionary<string, object>? Data { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
///     事件更新消息
/// </summary>
public class EventUpdateMessage
{
    public string EventId { get; set; } = string.Empty;
    public string UpdateType { get; set; } = string.Empty; // "created" | "updated" | "cancelled"
    public string Title { get; set; } = string.Empty;
    public Dictionary<string, object>? Data { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}