namespace AIService.Application.DTOs;

/// <summary>
///     Explore Dashboard 聚合响应
/// </summary>
public class ExploreDashboardResponse
{
    public MigrationWorkspaceResponse? MigrationWorkspace { get; set; }
    public BudgetCenterResponse? BudgetCenter { get; set; }
    public VisaCenterResponse? VisaCenter { get; set; }
    public ExploreDashboardInboxSummaryResponse? InboxSummary { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
}

/// <summary>
///     Explore Dashboard 中的 Inbox Summary 快照
/// </summary>
public class ExploreDashboardInboxSummaryResponse
{
    public int UnreadNotifications { get; set; }
    public int TotalNotifications { get; set; }
    public int ActionRequiredCount { get; set; }
    public DateTime? LatestNotificationAt { get; set; }
    public List<ExploreDashboardNotificationResponse> RecentNotifications { get; set; } = new();
}

/// <summary>
///     Explore Dashboard 中的通知快照
/// </summary>
public class ExploreDashboardNotificationResponse
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