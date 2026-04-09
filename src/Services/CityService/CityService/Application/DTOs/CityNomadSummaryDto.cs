namespace CityService.Application.DTOs;

/// <summary>
///     City Detail 决策面板聚合摘要
/// </summary>
public class CityNomadSummaryDto
{
    public Guid CityId { get; set; }
    public string CityName { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? Timezone { get; set; }
    public CityBudgetRangeDto? MonthlyBudgetRange { get; set; }
    public CityDecisionSignalsDto DecisionSignals { get; set; } = new();
    public List<CityCoworkingPreviewDto> RecommendedCoworkings { get; set; } = new();
    public List<CityStayPreviewDto> RecommendedStays { get; set; } = new();
    public List<CityMeetupPreviewDto> UpcomingMeetups { get; set; } = new();
    public DateTime? LastUpdatedAt { get; set; }
}

public class CityBudgetRangeDto
{
    public string Currency { get; set; } = "USD";
    public decimal Min { get; set; }
    public decimal Max { get; set; }
}

public class CityDecisionSignalsDto
{
    public int? NetworkQualityScore { get; set; }
    public int? VideoCallFriendlinessScore { get; set; }
    public int? VisaFriendlinessScore { get; set; }
    public int? TimezoneOverlapScore { get; set; }
    public int? CommunityActivityScore { get; set; }
    public int? ClimateStabilityScore { get; set; }
    public int? SafetyScore { get; set; }
}

public class CityCoworkingPreviewDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public decimal? DayPassPrice { get; set; }
    public string Currency { get; set; } = "USD";
}

public class CityStayPreviewDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public decimal? PricePerNight { get; set; }
    public string Currency { get; set; } = "USD";
}

public class CityMeetupPreviewDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public string? Venue { get; set; }
    public int ParticipantCount { get; set; }
}