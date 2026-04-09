using GoNomads.Shared.Models;

namespace UserService.Application.DTOs;

public class ProfileSnapshotResponse
{
    public UserDto User { get; set; } = new();
    public UserStatsDto NomadStats { get; set; } = new();
    public List<string> FavoriteCityIds { get; set; } = new();
    public ProfileSnapshotTravelPlanDto? LatestTravelPlan { get; set; }
    public ProfileSnapshotCityDto? NextDestinationCity { get; set; }
    public DateTime LastUpdatedAt { get; set; }
}

public class ProfileSnapshotTravelPlanDto
{
    public Guid Id { get; set; }
    public string CityId { get; set; } = string.Empty;
    public string CityName { get; set; } = string.Empty;
    public string? CityImage { get; set; }
    public int Duration { get; set; }
    public string BudgetLevel { get; set; } = string.Empty;
    public string TravelStyle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? DepartureDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ProfileSnapshotCityDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? Timezone { get; set; }
}