namespace MessageService.Domain.Entities;

/// <summary>
///     消息实体
/// </summary>
public class Message
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "notification", "ai_progress", "event_update"
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Data { get; set; } // JSON 数据
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
}