namespace AIService.Application.DTOs;

/// <summary>
///     Visa Center 聚合响应
/// </summary>
public class VisaCenterResponse
{
    public int ActiveProfileCount { get; set; }
    public int AttentionRequiredCount { get; set; }
    public int ReminderReadyCount { get; set; }
    public string RecommendedAction { get; set; } = string.Empty;
    public DateTime? LastUpdatedAt { get; set; }
    public VisaProfileSnapshot? FocusProfile { get; set; }
    public List<VisaProfileSnapshot> Profiles { get; set; } = new();
}

/// <summary>
///     Visa Center 中的签证档案快照
/// </summary>
public class VisaProfileSnapshot
{
    public Guid Id { get; set; }
    public string CityId { get; set; } = string.Empty;
    public string CityName { get; set; } = string.Empty;
    public string VisaType { get; set; } = string.Empty;
    public int StayDurationDays { get; set; }
    public int? DaysRemaining { get; set; }
    public string Status { get; set; } = string.Empty;
    public string RequirementsSummary { get; set; } = string.Empty;
    public string ProcessSummary { get; set; } = string.Empty;
    public decimal EstimatedCostUsd { get; set; }
    public DateTime? EntryDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime? ReminderSuggestedAt { get; set; }
    public List<string> RequiredDocuments { get; set; } = new();
    public List<DateTime> ReminderDates { get; set; } = new();
}