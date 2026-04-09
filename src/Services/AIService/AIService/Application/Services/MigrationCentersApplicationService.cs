using System.Text.Json;
using System.Text.Json.Nodes;
using AIService.Application.DTOs;
using AIService.Domain.Entities;
using AIService.Domain.Repositories;

namespace AIService.Application.Services;

public class MigrationCentersApplicationService : IMigrationCentersService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ILogger<MigrationCentersApplicationService> _logger;
    private readonly ITravelPlanRepository _travelPlanRepository;

    public MigrationCentersApplicationService(
        ITravelPlanRepository travelPlanRepository,
        ILogger<MigrationCentersApplicationService> logger)
    {
        _travelPlanRepository = travelPlanRepository;
        _logger = logger;
    }

    public async Task<MigrationWorkspaceResponse> GetMigrationWorkspaceAsync(Guid userId, int page = 1, int pageSize = 20)
    {
        var plans = await _travelPlanRepository.GetByUserIdAsync(userId, page, pageSize);
        return BuildMigrationWorkspaceResponse(plans);
    }

    public async Task<MigrationWorkspaceResponse> SaveMigrationWorkspacePlanAsync(
        Guid userId,
        Guid planId,
        UpdateMigrationWorkspacePlanRequest request)
    {
        var plan = await GetOwnedPlanAsync(userId, planId);
        var root = ParsePlanData(plan.PlanData);
        var nextState = NormalizeMigrationState(request, plan);

        root["migrationWorkspace"] = JsonSerializer.SerializeToNode(nextState, JsonOptions);
        plan.PlanData = root.ToJsonString(JsonOptions);

        await _travelPlanRepository.UpdateAsync(plan);
        _logger.LogInformation("Migration workspace state saved. UserId: {UserId}, PlanId: {PlanId}, Stage: {Stage}",
            userId,
            planId,
            nextState.Stage);

        return await GetMigrationWorkspaceAsync(userId);
    }

    public async Task<BudgetCenterResponse> GetBudgetCenterAsync(Guid userId)
    {
        var plans = await _travelPlanRepository.GetByUserIdAsync(userId, 1, 20);
        return BuildBudgetCenterResponse(plans);
    }

    public async Task<BudgetCenterResponse> SaveBudgetPlanAsync(Guid userId, Guid planId, SaveBudgetPlanRequest request)
    {
        var plan = await GetOwnedPlanAsync(userId, planId);
        var root = ParsePlanData(plan.PlanData);
        var nextState = NormalizeBudgetState(request, plan);

        root["budgetWorkspace"] = JsonSerializer.SerializeToNode(nextState, JsonOptions);
        plan.PlanData = root.ToJsonString(JsonOptions);

        await _travelPlanRepository.UpdateAsync(plan);
        _logger.LogInformation(
            "Budget workspace state saved. UserId: {UserId}, PlanId: {PlanId}, Template: {Template}",
            userId,
            planId,
            nextState.TemplateName);

        return await GetBudgetCenterAsync(userId);
    }

    public async Task<VisaCenterResponse> GetVisaCenterAsync(Guid userId)
    {
        var plans = await _travelPlanRepository.GetByUserIdAsync(userId, 1, 20);
        return BuildVisaCenterResponse(plans);
    }

    public async Task<VisaCenterResponse> SaveVisaProfileAsync(Guid userId, Guid planId, SaveVisaProfileRequest request)
    {
        var plan = await GetOwnedPlanAsync(userId, planId);
        var root = ParsePlanData(plan.PlanData);
        var nextState = NormalizeVisaState(request, plan);

        root["visaWorkspace"] = JsonSerializer.SerializeToNode(nextState, JsonOptions);
        plan.PlanData = root.ToJsonString(JsonOptions);

        await _travelPlanRepository.UpdateAsync(plan);
        _logger.LogInformation(
            "Visa workspace state saved. UserId: {UserId}, PlanId: {PlanId}, VisaType: {VisaType}",
            userId,
            planId,
            nextState.VisaType);

        return await GetVisaCenterAsync(userId);
    }

    private async Task<AiTravelPlan> GetOwnedPlanAsync(Guid userId, Guid planId)
    {
        var plan = await _travelPlanRepository.GetByIdAsync(planId);
        if (plan == null || plan.UserId != userId)
            throw new KeyNotFoundException("迁移计划不存在或无权访问");

        return plan;
    }

    private static MigrationWorkspaceResponse BuildMigrationWorkspaceResponse(IReadOnlyCollection<AiTravelPlan> plans)
    {
        var summaries = plans.Select(MapToTravelPlanSummary).ToList();
        var latestPlan = summaries.FirstOrDefault();
        var today = DateTime.UtcNow.Date;

        return new MigrationWorkspaceResponse
        {
            TotalPlans = summaries.Count,
            ActivePlans = summaries.Count(plan =>
                !string.Equals(plan.Status, "archived", StringComparison.OrdinalIgnoreCase)),
            DraftPlans = summaries.Count(plan =>
                string.Equals(plan.Status, "draft", StringComparison.OrdinalIgnoreCase)),
            UpcomingDepartures = summaries.Count(plan =>
                plan.DepartureDate.HasValue && plan.DepartureDate.Value.Date >= today),
            RecommendedAction = BuildMigrationWorkspaceAction(latestPlan, summaries),
            LastUpdatedAt = GetLastUpdatedAt(plans),
            LatestPlan = latestPlan,
            Plans = summaries
        };
    }

    private static AiTravelPlanSummary MapToTravelPlanSummary(AiTravelPlan plan)
    {
        var state = ReadMigrationState(plan);
        var checklist = state.Checklist.Select(item => new MigrationChecklistItemResponse
        {
            Id = item.Id,
            Title = item.Title,
            IsCompleted = item.IsCompleted
        }).ToList();
        var timeline = state.Timeline.Select(item => new MigrationTimelineItemResponse
        {
            Id = item.Id,
            Title = item.Title,
            Status = item.Status,
            TargetDate = item.TargetDate
        }).ToList();

        return new AiTravelPlanSummary
        {
            Id = plan.Id,
            CityId = plan.CityId,
            CityName = plan.CityName,
            CityImage = plan.CityImage,
            Duration = plan.Duration,
            BudgetLevel = plan.BudgetLevel,
            TravelStyle = plan.TravelStyle,
            Status = plan.Status,
            DepartureDate = plan.DepartureDate,
            CreatedAt = plan.CreatedAt,
            MigrationStage = state.Stage,
            FocusNote = state.FocusNote,
            CompletedTaskCount = checklist.Count(item => item.IsCompleted),
            TotalTaskCount = checklist.Count,
            Checklist = checklist,
            Timeline = timeline
        };
    }

    private static string BuildMigrationWorkspaceAction(
        AiTravelPlanSummary? latestPlan,
        IReadOnlyCollection<AiTravelPlanSummary> plans)
    {
        if (plans.Count == 0)
            return "create-first-plan";

        if (latestPlan != null && latestPlan.TotalTaskCount > latestPlan.CompletedTaskCount)
            return "complete-open-tasks";

        var hasUpcomingDeparture = plans.Any(plan =>
            plan.DepartureDate.HasValue && plan.DepartureDate.Value.Date >= DateTime.UtcNow.Date);
        if (hasUpcomingDeparture)
            return "review-upcoming-departure";

        if (plans.Any(plan => string.Equals(plan.Status, "draft", StringComparison.OrdinalIgnoreCase)))
            return "continue-draft-plan";

        return latestPlan == null ? "create-first-plan" : "review-latest-plan";
    }

    private static BudgetCenterResponse BuildBudgetCenterResponse(IReadOnlyCollection<AiTravelPlan> plans)
    {
        var snapshots = plans.Select(MapToBudgetPlanSnapshot).ToList();
        var activeSnapshots = snapshots.Where(plan =>
            !string.Equals(plan.Status, "archived", StringComparison.OrdinalIgnoreCase)).ToList();
        var focusPlan = SelectBudgetFocusPlan(activeSnapshots);
        var monthlyBudgetTargetUsd = focusPlan?.DeclaredMonthlyBudgetUsd ?? 0m;
        var forecastMonthlyCostUsd = focusPlan?.EstimatedMonthlyCostUsd
            ?? (activeSnapshots.Count > 0 ? Math.Round(activeSnapshots.Average(plan => plan.EstimatedMonthlyCostUsd), 2) : 0m);
        var deltaUsd = Math.Round(monthlyBudgetTargetUsd - forecastMonthlyCostUsd, 2);

        return new BudgetCenterResponse
        {
            MonthlyBudgetTargetUsd = monthlyBudgetTargetUsd,
            ForecastMonthlyCostUsd = forecastMonthlyCostUsd,
            DeltaUsd = deltaUsd,
            ActivePlanCount = activeSnapshots.Count,
            TrackedCityCount = plans
                .Where(plan => !string.IsNullOrWhiteSpace(plan.CityId))
                .Select(plan => plan.CityId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count(),
            BudgetHealth = BuildBudgetHealth(focusPlan),
            RecommendedAction = BuildBudgetCenterAction(focusPlan, activeSnapshots.Count),
            LastUpdatedAt = GetLastUpdatedAt(plans),
            FocusPlan = focusPlan,
            Plans = snapshots
        };
    }

    private static BudgetPlanSnapshot MapToBudgetPlanSnapshot(AiTravelPlan plan)
    {
        var state = ReadBudgetState(plan);

        return new BudgetPlanSnapshot
        {
            Id = plan.Id,
            CityId = plan.CityId,
            CityName = plan.CityName,
            BudgetLevel = plan.BudgetLevel,
            TravelStyle = plan.TravelStyle,
            Status = plan.Status,
            DepartureDate = plan.DepartureDate,
            DeclaredMonthlyBudgetUsd = state.MonthlyBudgetTargetUsd,
            EstimatedMonthlyCostUsd = state.ForecastMonthlyCostUsd,
            TemplateName = state.TemplateName,
            AlertThresholdPercent = state.AlertThresholdPercent,
            OverrunAlertEnabled = state.OverrunAlertEnabled,
            Categories = state.Categories.Select(category => new BudgetCategoryAllocationResponse
            {
                Category = category.Category,
                BudgetUsd = category.BudgetUsd
            }).ToList()
        };
    }

    private static BudgetPlanSnapshot? SelectBudgetFocusPlan(IReadOnlyCollection<BudgetPlanSnapshot> plans)
    {
        if (plans.Count == 0)
            return null;

        var today = DateTime.UtcNow.Date;

        return plans
            .OrderBy(plan => string.Equals(plan.Status, "archived", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
            .ThenBy(plan => plan.DepartureDate.HasValue && plan.DepartureDate.Value.Date >= today ? 0 : 1)
            .ThenBy(plan => plan.DepartureDate ?? DateTime.MaxValue)
            .FirstOrDefault();
    }

    private static string BuildBudgetHealth(BudgetPlanSnapshot? focusPlan)
    {
        if (focusPlan == null || focusPlan.DeclaredMonthlyBudgetUsd <= 0)
            return "no_data";

        var warningLine = focusPlan.DeclaredMonthlyBudgetUsd * (1m + focusPlan.AlertThresholdPercent / 100m);

        if (focusPlan.EstimatedMonthlyCostUsd <= focusPlan.DeclaredMonthlyBudgetUsd * 0.9m)
            return "on_track";

        if (focusPlan.EstimatedMonthlyCostUsd <= warningLine)
            return "watch";

        return "over_budget";
    }

    private static string BuildBudgetCenterAction(BudgetPlanSnapshot? focusPlan, int activePlanCount)
    {
        if (focusPlan == null)
            return "create-first-plan";

        if (string.Equals(BuildBudgetHealth(focusPlan), "over_budget", StringComparison.OrdinalIgnoreCase))
            return "review-over-budget";

        if (focusPlan.DepartureDate.HasValue &&
            (focusPlan.DepartureDate.Value.Date - DateTime.UtcNow.Date).TotalDays <= 21)
            return "lock-first-month-budget";

        if (activePlanCount > 1)
            return "compare-city-budget";

        if (string.Equals(focusPlan.Status, "draft", StringComparison.OrdinalIgnoreCase))
            return "finalize-budget-baseline";

        return "review-latest-plan";
    }

    private static VisaCenterResponse BuildVisaCenterResponse(IReadOnlyCollection<AiTravelPlan> plans)
    {
        var profiles = plans.Select(MapToVisaProfileSnapshot).ToList();
        var activeProfiles = profiles.Where(profile =>
            !string.Equals(profile.Status, "archived", StringComparison.OrdinalIgnoreCase)).ToList();
        var focusProfile = SelectVisaFocusProfile(activeProfiles);

        return new VisaCenterResponse
        {
            ActiveProfileCount = activeProfiles.Count,
            AttentionRequiredCount = activeProfiles.Count(profile =>
                string.Equals(profile.Status, "attention_required", StringComparison.OrdinalIgnoreCase)),
            ReminderReadyCount = activeProfiles.Count(profile => profile.ReminderSuggestedAt.HasValue),
            RecommendedAction = BuildVisaCenterAction(focusProfile, activeProfiles),
            LastUpdatedAt = GetLastUpdatedAt(plans),
            FocusProfile = focusProfile,
            Profiles = profiles
        };
    }

    private static VisaProfileSnapshot MapToVisaProfileSnapshot(AiTravelPlan plan)
    {
        var state = ReadVisaState(plan);
        int? daysRemaining = state.ExpiryDate == null
            ? null
            : (int)Math.Ceiling((state.ExpiryDate.Value.Date - DateTime.UtcNow.Date).TotalDays);

        return new VisaProfileSnapshot
        {
            Id = plan.Id,
            CityId = plan.CityId,
            CityName = plan.CityName,
            VisaType = state.VisaType,
            StayDurationDays = state.StayDurationDays,
            DaysRemaining = daysRemaining,
            Status = ResolveVisaStatus(plan.Status, daysRemaining, plan.DepartureDate, state.ReminderDates),
            RequirementsSummary = state.RequirementsSummary,
            ProcessSummary = state.ProcessSummary,
            EstimatedCostUsd = state.EstimatedCostUsd,
            EntryDate = state.EntryDate,
            ExpiryDate = state.ExpiryDate,
            ReminderSuggestedAt = state.ReminderDates.OrderBy(date => date).FirstOrDefault(),
            RequiredDocuments = state.RequiredDocuments,
            ReminderDates = state.ReminderDates
        };
    }

    private static VisaProfileSnapshot? SelectVisaFocusProfile(IReadOnlyCollection<VisaProfileSnapshot> profiles)
    {
        if (profiles.Count == 0)
            return null;

        return profiles
            .OrderBy(profile => string.Equals(profile.Status, "attention_required", StringComparison.OrdinalIgnoreCase)
                ? 0
                : string.Equals(profile.Status, "review_soon", StringComparison.OrdinalIgnoreCase)
                    ? 1
                    : 2)
            .ThenBy(profile => profile.DaysRemaining ?? int.MaxValue)
            .FirstOrDefault();
    }

    private static string BuildVisaCenterAction(VisaProfileSnapshot? focusProfile, IReadOnlyCollection<VisaProfileSnapshot> profiles)
    {
        if (focusProfile == null)
            return "create-first-plan";

        if (string.Equals(focusProfile.Status, "attention_required", StringComparison.OrdinalIgnoreCase))
            return "set-reminder-now";

        if (profiles.Count > 1)
            return "compare-entry-options";

        if (string.Equals(focusProfile.Status, "planning", StringComparison.OrdinalIgnoreCase))
            return "complete-visa-brief";

        return "review-latest-visa";
    }

    private static string ResolveVisaStatus(
        string planStatus,
        int? daysRemaining,
        DateTime? departureDate,
        IReadOnlyCollection<DateTime> reminderDates)
    {
        if (string.Equals(planStatus, "archived", StringComparison.OrdinalIgnoreCase))
            return "archived";

        if (!departureDate.HasValue)
            return "planning";

        if (reminderDates.Any(date => date.Date <= DateTime.UtcNow.Date.AddDays(2)))
            return "attention_required";

        if (!daysRemaining.HasValue)
            return "planning";

        if (daysRemaining.Value <= 14)
            return "attention_required";

        if (daysRemaining.Value <= 30)
            return "review_soon";

        return "on_track";
    }

    private static DateTime? GetLastUpdatedAt(IReadOnlyCollection<AiTravelPlan> plans)
    {
        return plans
            .OrderByDescending(plan => plan.UpdatedAt)
            .Select(plan => (DateTime?)plan.UpdatedAt)
            .FirstOrDefault();
    }

    private static MigrationWorkspaceState ReadMigrationState(AiTravelPlan plan)
    {
        var root = ParsePlanData(plan.PlanData);
        var state = root["migrationWorkspace"]?.Deserialize<MigrationWorkspaceState>(JsonOptions);
        return state ?? BuildDefaultMigrationState(plan);
    }

    private static BudgetWorkspaceState ReadBudgetState(AiTravelPlan plan)
    {
        var root = ParsePlanData(plan.PlanData);
        var state = root["budgetWorkspace"]?.Deserialize<BudgetWorkspaceState>(JsonOptions);
        return state ?? BuildDefaultBudgetState(plan);
    }

    private static VisaWorkspaceState ReadVisaState(AiTravelPlan plan)
    {
        var root = ParsePlanData(plan.PlanData);
        var state = root["visaWorkspace"]?.Deserialize<VisaWorkspaceState>(JsonOptions);
        return state ?? BuildDefaultVisaState(plan);
    }

    private static JsonObject ParsePlanData(string? planData)
    {
        if (string.IsNullOrWhiteSpace(planData))
            return new JsonObject();

        try
        {
            return JsonNode.Parse(planData)?.AsObject() ?? new JsonObject();
        }
        catch
        {
            return new JsonObject();
        }
    }

    private static MigrationWorkspaceState NormalizeMigrationState(UpdateMigrationWorkspacePlanRequest request, AiTravelPlan plan)
    {
        return new MigrationWorkspaceState
        {
            Stage = string.IsNullOrWhiteSpace(request.Stage) ? BuildDefaultMigrationState(plan).Stage : request.Stage.Trim(),
            FocusNote = string.IsNullOrWhiteSpace(request.FocusNote) ? null : request.FocusNote.Trim(),
            Checklist = request.Checklist
                .Where(item => !string.IsNullOrWhiteSpace(item.Title))
                .Select(item => new MigrationChecklistItemState
                {
                    Id = string.IsNullOrWhiteSpace(item.Id) ? Guid.NewGuid().ToString("N") : item.Id.Trim(),
                    Title = item.Title.Trim(),
                    IsCompleted = item.IsCompleted
                })
                .ToList(),
            Timeline = request.Timeline
                .Where(item => !string.IsNullOrWhiteSpace(item.Title))
                .Select(item => new MigrationTimelineItemState
                {
                    Id = string.IsNullOrWhiteSpace(item.Id) ? Guid.NewGuid().ToString("N") : item.Id.Trim(),
                    Title = item.Title.Trim(),
                    Status = string.IsNullOrWhiteSpace(item.Status) ? "pending" : item.Status.Trim(),
                    TargetDate = item.TargetDate
                })
                .OrderBy(item => item.TargetDate ?? DateTime.MaxValue)
                .ToList()
        };
    }

    private static BudgetWorkspaceState NormalizeBudgetState(SaveBudgetPlanRequest request, AiTravelPlan plan)
    {
        return new BudgetWorkspaceState
        {
            TemplateName = string.IsNullOrWhiteSpace(request.TemplateName)
                ? BuildDefaultBudgetState(plan).TemplateName
                : request.TemplateName.Trim(),
            MonthlyBudgetTargetUsd = request.MonthlyBudgetTargetUsd > 0
                ? request.MonthlyBudgetTargetUsd
                : BuildDefaultBudgetState(plan).MonthlyBudgetTargetUsd,
            ForecastMonthlyCostUsd = request.ForecastMonthlyCostUsd > 0
                ? request.ForecastMonthlyCostUsd
                : BuildDefaultBudgetState(plan).ForecastMonthlyCostUsd,
            AlertThresholdPercent = request.AlertThresholdPercent > 0 ? request.AlertThresholdPercent : 8m,
            OverrunAlertEnabled = request.OverrunAlertEnabled,
            Categories = request.Categories
                .Where(category => !string.IsNullOrWhiteSpace(category.Category))
                .Select(category => new BudgetCategoryAllocationState
                {
                    Category = category.Category.Trim(),
                    BudgetUsd = category.BudgetUsd
                })
                .ToList()
        };
    }

    private static VisaWorkspaceState NormalizeVisaState(SaveVisaProfileRequest request, AiTravelPlan plan)
    {
        var defaults = BuildDefaultVisaState(plan);
        var entryDate = request.EntryDate ?? defaults.EntryDate;
        var expiryDate = request.ExpiryDate ?? entryDate?.Date.AddDays(request.StayDurationDays > 0 ? request.StayDurationDays : defaults.StayDurationDays);

        return new VisaWorkspaceState
        {
            VisaType = string.IsNullOrWhiteSpace(request.VisaType) ? defaults.VisaType : request.VisaType.Trim(),
            StayDurationDays = request.StayDurationDays > 0 ? request.StayDurationDays : defaults.StayDurationDays,
            EntryDate = entryDate,
            ExpiryDate = expiryDate,
            EstimatedCostUsd = request.EstimatedCostUsd > 0 ? request.EstimatedCostUsd : defaults.EstimatedCostUsd,
            RequirementsSummary = string.IsNullOrWhiteSpace(request.RequirementsSummary)
                ? defaults.RequirementsSummary
                : request.RequirementsSummary.Trim(),
            ProcessSummary = string.IsNullOrWhiteSpace(request.ProcessSummary)
                ? defaults.ProcessSummary
                : request.ProcessSummary.Trim(),
            RequiredDocuments = request.RequiredDocuments
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            ReminderDates = request.ReminderDates
                .Select(date => date.ToUniversalTime())
                .Distinct()
                .OrderBy(date => date)
                .ToList()
        };
    }

    private static MigrationWorkspaceState BuildDefaultMigrationState(AiTravelPlan plan)
    {
        var stage = string.Equals(plan.Status, "draft", StringComparison.OrdinalIgnoreCase)
            ? "researching"
            : plan.DepartureDate.HasValue && (plan.DepartureDate.Value.Date - DateTime.UtcNow.Date).TotalDays <= 30
                ? "booking"
                : "budgeting";

        return new MigrationWorkspaceState
        {
            Stage = stage,
            Checklist = new List<MigrationChecklistItemState>
            {
                new() { Id = "city-fit", Title = "Confirm target city fit", IsCompleted = stage != "researching" },
                new() { Id = "budget", Title = "Lock first-month budget", IsCompleted = stage is "booking" or "landing" or "settled" },
                new() { Id = "visa", Title = "Prepare visa and entry documents", IsCompleted = stage is "visa_ready" or "booking" or "landing" or "settled" }
            },
            Timeline = plan.DepartureDate.HasValue
                ? new List<MigrationTimelineItemState>
                {
                    new()
                    {
                        Id = "departure",
                        Title = "Departure window",
                        Status = "planned",
                        TargetDate = plan.DepartureDate.Value.Date
                    }
                }
                : new List<MigrationTimelineItemState>()
        };
    }

    private static BudgetWorkspaceState BuildDefaultBudgetState(AiTravelPlan plan)
    {
        var target = ResolveMonthlyBudgetTargetUsd(plan.BudgetLevel);
        var forecast = EstimateMonthlyCostUsd(plan, target);

        return new BudgetWorkspaceState
        {
            TemplateName = ResolveBudgetTemplate(plan.BudgetLevel),
            MonthlyBudgetTargetUsd = target,
            ForecastMonthlyCostUsd = forecast,
            AlertThresholdPercent = 8m,
            OverrunAlertEnabled = true,
            Categories = BuildDefaultBudgetCategories(target)
        };
    }

    private static VisaWorkspaceState BuildDefaultVisaState(AiTravelPlan plan)
    {
        var visaType = ResolveVisaType(plan.Duration, plan.BudgetLevel);
        var stayDurationDays = plan.Duration > 0 ? plan.Duration : 30;
        var entryDate = plan.DepartureDate?.Date;
        var expiryDate = entryDate?.AddDays(stayDurationDays);

        return new VisaWorkspaceState
        {
            VisaType = visaType,
            StayDurationDays = stayDurationDays,
            EntryDate = entryDate,
            ExpiryDate = expiryDate,
            EstimatedCostUsd = EstimateVisaCostUsd(visaType, stayDurationDays),
            RequirementsSummary = BuildVisaRequirementsSummary(visaType),
            ProcessSummary = BuildVisaProcessSummary(visaType),
            RequiredDocuments = BuildDefaultVisaDocuments(visaType),
            ReminderDates = expiryDate == null
                ? new List<DateTime>()
                : new List<DateTime> { expiryDate.Value.AddDays(-14), expiryDate.Value.AddDays(-7) }
        };
    }

    private static decimal ResolveMonthlyBudgetTargetUsd(string budgetLevel)
    {
        return budgetLevel.ToLowerInvariant() switch
        {
            "low" => 1400m,
            "high" => 3800m,
            _ => 2400m
        };
    }

    private static decimal EstimateMonthlyCostUsd(AiTravelPlan plan, decimal declaredBudget)
    {
        var estimate = declaredBudget * ResolveTravelStyleFactor(plan.TravelStyle);

        if (plan.DepartureDate.HasValue)
        {
            var daysUntilDeparture = (plan.DepartureDate.Value.Date - DateTime.UtcNow.Date).TotalDays;
            if (daysUntilDeparture <= 21)
                estimate *= 1.08m;
            else if (daysUntilDeparture <= 45)
                estimate *= 1.04m;
        }

        return Math.Round(estimate, 2);
    }

    private static decimal ResolveTravelStyleFactor(string travelStyle)
    {
        return travelStyle.ToLowerInvariant() switch
        {
            "adventure" => 1.05m,
            "relaxation" => 1.10m,
            "nightlife" => 1.15m,
            _ => 1.00m
        };
    }

    private static string ResolveBudgetTemplate(string budgetLevel)
    {
        return budgetLevel.ToLowerInvariant() switch
        {
            "low" => "lean-landing",
            "high" => "comfort",
            _ => "balanced"
        };
    }

    private static List<BudgetCategoryAllocationState> BuildDefaultBudgetCategories(decimal target)
    {
        return new List<BudgetCategoryAllocationState>
        {
            new() { Category = "accommodation", BudgetUsd = Math.Round(target * 0.4m, 2) },
            new() { Category = "coworking", BudgetUsd = Math.Round(target * 0.08m, 2) },
            new() { Category = "food", BudgetUsd = Math.Round(target * 0.18m, 2) },
            new() { Category = "transport", BudgetUsd = Math.Round(target * 0.08m, 2) },
            new() { Category = "visa", BudgetUsd = Math.Round(target * 0.06m, 2) },
            new() { Category = "buffer", BudgetUsd = Math.Round(target * 0.2m, 2) }
        };
    }

    private static string ResolveVisaType(int duration, string budgetLevel)
    {
        if (duration >= 90)
            return "long_stay_visa";

        if (duration >= 45)
            return "digital_nomad";

        return string.Equals(budgetLevel, "high", StringComparison.OrdinalIgnoreCase)
            ? "priority_evisa"
            : "short_stay_visa";
    }

    private static string BuildVisaRequirementsSummary(string visaType)
    {
        return visaType switch
        {
            "long_stay_visa" => "Passport validity, bank statements, insurance, and background documents.",
            "digital_nomad" => "Remote income proof, accommodation booking, insurance, and passport validity.",
            "priority_evisa" => "Passport, onward travel proof, accommodation, and online application details.",
            _ => "Passport validity, onward ticket, and first-stay confirmation."
        };
    }

    private static string BuildVisaProcessSummary(string visaType)
    {
        return visaType switch
        {
            "long_stay_visa" => "Prepare documents early, validate entry window, and track embassy processing time.",
            "digital_nomad" => "Confirm remote-work eligibility, submit digital documents, and book insurance before departure.",
            "priority_evisa" => "Submit e-visa online, keep PDFs ready, and recheck approval before boarding.",
            _ => "Verify entry rules, keep proof of onward travel ready, and set a renewal reminder in advance."
        };
    }

    private static decimal EstimateVisaCostUsd(string visaType, int stayDurationDays)
    {
        var baseCost = visaType switch
        {
            "long_stay_visa" => 180m,
            "digital_nomad" => 120m,
            "priority_evisa" => 75m,
            _ => 45m
        };

        if (stayDurationDays >= 90)
            baseCost += 40m;

        return baseCost;
    }

    private static List<string> BuildDefaultVisaDocuments(string visaType)
    {
        var documents = new List<string> { "Passport", "Accommodation confirmation" };

        if (visaType is "long_stay_visa" or "digital_nomad")
            documents.Add("Remote income proof");

        if (visaType == "priority_evisa")
            documents.Add("Return or onward travel proof");

        return documents;
    }

    private sealed class MigrationWorkspaceState
    {
        public string Stage { get; set; } = string.Empty;
        public string? FocusNote { get; set; }
        public List<MigrationChecklistItemState> Checklist { get; set; } = new();
        public List<MigrationTimelineItemState> Timeline { get; set; } = new();
    }

    private sealed class MigrationChecklistItemState
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
    }

    private sealed class MigrationTimelineItemState
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? TargetDate { get; set; }
    }

    private sealed class BudgetWorkspaceState
    {
        public string TemplateName { get; set; } = string.Empty;
        public decimal MonthlyBudgetTargetUsd { get; set; }
        public decimal ForecastMonthlyCostUsd { get; set; }
        public decimal AlertThresholdPercent { get; set; }
        public bool OverrunAlertEnabled { get; set; }
        public List<BudgetCategoryAllocationState> Categories { get; set; } = new();
    }

    private sealed class BudgetCategoryAllocationState
    {
        public string Category { get; set; } = string.Empty;
        public decimal BudgetUsd { get; set; }
    }

    private sealed class VisaWorkspaceState
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
}