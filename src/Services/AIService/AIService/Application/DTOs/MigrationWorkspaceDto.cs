namespace AIService.Application.DTOs;

/// <summary>
///     Migration Workspace 聚合响应
/// </summary>
public class MigrationWorkspaceResponse
{
    public int TotalPlans { get; set; }
    public int ActivePlans { get; set; }
    public int DraftPlans { get; set; }
    public int UpcomingDepartures { get; set; }
    public string RecommendedAction { get; set; } = string.Empty;
    public DateTime? LastUpdatedAt { get; set; }
    public AiTravelPlanSummary? LatestPlan { get; set; }
    public List<AiTravelPlanSummary> Plans { get; set; } = new();
}

public class MigrationChecklistItemResponse
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
}

public class MigrationTimelineItemResponse
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? TargetDate { get; set; }
}