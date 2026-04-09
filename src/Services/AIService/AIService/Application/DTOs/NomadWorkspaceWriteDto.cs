namespace AIService.Application.DTOs;

public class UpdateMigrationWorkspacePlanRequest
{
    public string Stage { get; set; } = string.Empty;
    public string? FocusNote { get; set; }
    public List<MigrationChecklistItemRequest> Checklist { get; set; } = new();
    public List<MigrationTimelineItemRequest> Timeline { get; set; } = new();
}

public class MigrationChecklistItemRequest
{
    public string? Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
}

public class MigrationTimelineItemRequest
{
    public string? Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? TargetDate { get; set; }
}

public class SaveBudgetPlanRequest
{
    public string TemplateName { get; set; } = string.Empty;
    public decimal MonthlyBudgetTargetUsd { get; set; }
    public decimal ForecastMonthlyCostUsd { get; set; }
    public decimal AlertThresholdPercent { get; set; }
    public bool OverrunAlertEnabled { get; set; } = true;
    public List<BudgetCategoryAllocationRequest> Categories { get; set; } = new();
}

public class BudgetCategoryAllocationRequest
{
    public string Category { get; set; } = string.Empty;
    public decimal BudgetUsd { get; set; }
}

public class SaveVisaProfileRequest
{
    public string VisaType { get; set; } = string.Empty;
    public int StayDurationDays { get; set; }
    public DateTime? EntryDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal EstimatedCostUsd { get; set; }
    public string RequirementsSummary { get; set; } = string.Empty;
    public string ProcessSummary { get; set; } = string.Empty;
    public List<string> RequiredDocuments { get; set; } = new();
    public List<DateTime> ReminderDates { get; set; } = new();
}

public class CreateCommunityQuestionRequest
{
    public string City { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
}

public class CreateCommunityAnswerRequest
{
    public string Content { get; set; } = string.Empty;
}