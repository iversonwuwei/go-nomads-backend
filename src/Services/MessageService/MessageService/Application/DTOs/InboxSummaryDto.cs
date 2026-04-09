namespace MessageService.Application.DTOs;

/// <summary>
///     Inbox Summary 聚合响应
/// </summary>
public class InboxSummaryDto
{
    public int UnreadNotifications { get; set; }
    public int TotalNotifications { get; set; }
    public int ActionRequiredCount { get; set; }
    public DateTime? LatestNotificationAt { get; set; }
    public List<NotificationDto> RecentNotifications { get; set; } = new();
}