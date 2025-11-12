namespace Shared.Messages;

/// <summary>
/// 旅行计划任务消息
/// </summary>
public class TravelPlanTaskMessage
{
    public Guid TaskId { get; set; }
    public Guid UserId { get; set; }
    public Guid ConversationId { get; set; }
    public string? CityName { get; set; }
    public int Days { get; set; }
    public decimal Budget { get; set; }
    public List<string> Interests { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
