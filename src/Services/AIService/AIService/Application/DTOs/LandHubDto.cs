namespace AIService.Application.DTOs;

/// <summary>
///     Land Hub 聚合响应
/// </summary>
public class LandHubResponse
{
    public MigrationWorkspaceResponse? MigrationWorkspace { get; set; }
    public BudgetCenterResponse? BudgetCenter { get; set; }
    public VisaCenterResponse? VisaCenter { get; set; }
    public TravelPlanResponse? FocusTravelPlan { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
}