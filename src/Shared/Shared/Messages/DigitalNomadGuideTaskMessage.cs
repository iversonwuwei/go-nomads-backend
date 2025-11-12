namespace Shared.Messages;

/// <summary>
/// 数字游民指南任务消息
/// </summary>
public class DigitalNomadGuideTaskMessage
{
    public Guid TaskId { get; set; }
    public Guid UserId { get; set; }
    public Guid ConversationId { get; set; }
    public string? CityName { get; set; }
    public string? Question { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
