namespace AIService.Application.DTOs;

/// <summary>
///     Budget Center 聚合响应
/// </summary>
public class BudgetCenterResponse
{
    public decimal MonthlyBudgetTargetUsd { get; set; }
    public decimal ForecastMonthlyCostUsd { get; set; }
    public decimal DeltaUsd { get; set; }
    public int ActivePlanCount { get; set; }
    public int TrackedCityCount { get; set; }
    public string BudgetHealth { get; set; } = "no_data";
    public string RecommendedAction { get; set; } = string.Empty;
    public DateTime? LastUpdatedAt { get; set; }
    public BudgetPlanSnapshot? FocusPlan { get; set; }
    public List<BudgetPlanSnapshot> Plans { get; set; } = new();
}

/// <summary>
///     Budget Center 中的计划预算快照
/// </summary>
public class BudgetPlanSnapshot
{
    public Guid Id { get; set; }
    public string CityId { get; set; } = string.Empty;
    public string CityName { get; set; } = string.Empty;
    public string BudgetLevel { get; set; } = string.Empty;
    public string TravelStyle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? DepartureDate { get; set; }
    public decimal DeclaredMonthlyBudgetUsd { get; set; }
    public decimal EstimatedMonthlyCostUsd { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public decimal AlertThresholdPercent { get; set; }
    public bool OverrunAlertEnabled { get; set; }
    public List<BudgetCategoryAllocationResponse> Categories { get; set; } = new();
}

public class BudgetCategoryAllocationResponse
{
    public string Category { get; set; } = string.Empty;
    public decimal BudgetUsd { get; set; }
}