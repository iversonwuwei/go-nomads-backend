using GoNomads.Shared.Communication;
using GoNomads.Shared.Extensions;
using GoNomads.Shared.Models;
using UserService.Application.DTOs;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Application.Services;

public class ProfileSnapshotService : IProfileSnapshotService
{
    private readonly ILogger<ProfileSnapshotService> _logger;
    private readonly ServiceInvocationClient _serviceInvocationClient;
    private readonly ITravelHistoryService _travelHistoryService;
    private readonly IUserService _userService;
    private readonly IUserStatsRepository _userStatsRepository;

    public ProfileSnapshotService(
        IUserService userService,
        IUserStatsRepository userStatsRepository,
        ITravelHistoryService travelHistoryService,
        ServiceInvocationClient serviceInvocationClient,
        ILogger<ProfileSnapshotService> logger)
    {
        _userService = userService;
        _userStatsRepository = userStatsRepository;
        _travelHistoryService = travelHistoryService;
        _serviceInvocationClient = serviceInvocationClient;
        _logger = logger;
    }

    public async Task<ProfileSnapshotResponse?> GetCurrentAsync(string userId, CancellationToken cancellationToken = default)
    {
        var userTask = _userService.GetUserByIdAsync(userId, cancellationToken);
        var favoriteCityIdsTask = GetFavoriteCityIdsAsync(userId, cancellationToken);
        var latestTravelPlanTask = GetLatestTravelPlanAsync(userId, cancellationToken);
        var userStatsTask = GetOrCreateUserStatsAsync(userId, cancellationToken);
        var travelStatsTask = GetTravelHistoryStatsAsync(userId, cancellationToken);
        var meetupsCreatedTask = GetMeetupsCreatedCountAsync(userId, cancellationToken);
        var meetupsJoinedTask = GetMeetupsJoinedCountAsync(userId, cancellationToken);

        await Task.WhenAll(
            userTask,
            favoriteCityIdsTask,
            latestTravelPlanTask,
            userStatsTask,
            travelStatsTask,
            meetupsCreatedTask,
            meetupsJoinedTask);

        var user = await userTask;
        if (user == null)
        {
            return null;
        }

        var favoriteCityIds = await favoriteCityIdsTask;
        var latestTravelPlan = await latestTravelPlanTask;
        var nextDestinationCity = await GetNextDestinationCityAsync(userId, latestTravelPlan, cancellationToken);

        var nomadStats = MapToUserStatsDto(
            await userStatsTask,
            await meetupsCreatedTask,
            await meetupsJoinedTask,
            favoriteCityIds.Count,
            await travelStatsTask);

        return new ProfileSnapshotResponse
        {
            User = user,
            NomadStats = nomadStats,
            FavoriteCityIds = favoriteCityIds,
            LatestTravelPlan = latestTravelPlan,
            NextDestinationCity = nextDestinationCity,
            LastUpdatedAt = GetMostRecentTimestamp(user.UpdatedAt, nomadStats.UpdatedAt, latestTravelPlan?.CreatedAt)
        };
    }

    private async Task<UserStats> GetOrCreateUserStatsAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            return await _userStatsRepository.GetOrCreateAsync(userId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 获取 Profile Snapshot 用户统计实体失败，回退空统计: {UserId}", userId);
            return UserStats.CreateForUser(userId);
        }
    }

    private async Task<TravelHistoryStats> GetTravelHistoryStatsAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            return await _travelHistoryService.GetUserTravelStatsAsync(userId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 获取 Profile Snapshot 旅行统计失败，回退空统计: {UserId}", userId);
            return new TravelHistoryStats();
        }
    }

    private async Task<int> GetMeetupsCreatedCountAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            return await _serviceInvocationClient.InvokeAsync<int>(
                HttpMethod.Get,
                "event-service",
                $"api/v1/events/user/{userId}/created/count",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 获取 Profile Snapshot meetup 创建数失败，返回0: {UserId}", userId);
            return 0;
        }
    }

    private async Task<int> GetMeetupsJoinedCountAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            return await _serviceInvocationClient.InvokeAsync<int>(
                HttpMethod.Get,
                "event-service",
                $"api/v1/events/user/{userId}/joined/count",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 获取 Profile Snapshot meetup 参与数失败，返回0: {UserId}", userId);
            return 0;
        }
    }

    private async Task<List<string>> GetFavoriteCityIdsAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            using var request = _serviceInvocationClient.CreateRequest(
                HttpMethod.Get,
                "city-service",
                "api/v1/user-favorite-cities/ids");

            AddUserHeaders(request, userId);

            var cityIds = await _serviceInvocationClient.InvokeAsync<List<string>>(request, cancellationToken);
            return cityIds ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 获取 Profile Snapshot 收藏城市失败，返回空数组: {UserId}", userId);
            return new List<string>();
        }
    }

    private async Task<ProfileSnapshotTravelPlanDto?> GetLatestTravelPlanAsync(string userId,
        CancellationToken cancellationToken)
    {
        try
        {
            using var request = _serviceInvocationClient.CreateRequest(
                HttpMethod.Get,
                "ai-service",
                "api/v1/ai/chat/travel-plans?page=1&pageSize=1");

            AddUserHeaders(request, userId);

            var envelope = await _serviceInvocationClient.InvokeAsync<ApiResponse<List<AiTravelPlanSummaryContract>>>(
                request,
                cancellationToken);

            var plans = envelope?.UnwrapOrThrow("AIService:GetUserTravelPlans") ?? new List<AiTravelPlanSummaryContract>();
            var latestPlan = plans.FirstOrDefault();

            return latestPlan == null
                ? null
                : new ProfileSnapshotTravelPlanDto
                {
                    Id = latestPlan.Id,
                    CityId = latestPlan.CityId,
                    CityName = latestPlan.CityName,
                    CityImage = latestPlan.CityImage,
                    Duration = latestPlan.Duration,
                    BudgetLevel = latestPlan.BudgetLevel,
                    TravelStyle = latestPlan.TravelStyle,
                    Status = latestPlan.Status,
                    DepartureDate = latestPlan.DepartureDate,
                    CreatedAt = latestPlan.CreatedAt
                };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 获取 Profile Snapshot 最新旅行计划失败，返回空计划: {UserId}", userId);
            return null;
        }
    }

    private async Task<ProfileSnapshotCityDto?> GetNextDestinationCityAsync(
        string userId,
        ProfileSnapshotTravelPlanDto? latestTravelPlan,
        CancellationToken cancellationToken)
    {
        if (latestTravelPlan == null || !Guid.TryParse(latestTravelPlan.CityId, out var cityId))
        {
            return null;
        }

        try
        {
            using var request = _serviceInvocationClient.CreateRequest(
                HttpMethod.Get,
                "city-service",
                $"api/v1/cities/{cityId}");

            AddUserHeaders(request, userId);

            var envelope = await _serviceInvocationClient.InvokeAsync<ApiResponse<CitySummaryContract>>(request, cancellationToken);
            var city = envelope?.UnwrapOrThrow("CityService:GetCityById");

            return city == null
                ? null
                : new ProfileSnapshotCityDto
                {
                    Id = city.Id.ToString(),
                    Name = city.Name,
                    Country = city.Country,
                    Timezone = city.TimeZone
                };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 获取 Profile Snapshot 下一站城市失败，返回空城市: {UserId}, CityId={CityId}", userId,
                latestTravelPlan.CityId);
            return null;
        }
    }

    private static UserStatsDto MapToUserStatsDto(
        UserStats stats,
        int meetupsCreated,
        int meetupsJoined,
        int favoriteCitiesCount,
        TravelHistoryStats? travelStats)
    {
        return new UserStatsDto
        {
            Id = stats.Id,
            UserId = stats.UserId,
            CountriesVisited = travelStats?.CountriesVisited ?? stats.CountriesVisited,
            CitiesLived = travelStats?.CitiesVisited ?? stats.CitiesLived,
            DaysNomading = travelStats?.TotalDays ?? stats.DaysNomading,
            TripsCompleted = travelStats?.ConfirmedTrips ?? stats.TripsCompleted,
            MeetupsCreated = meetupsCreated,
            MeetupsJoined = meetupsJoined,
            FavoriteCitiesCount = favoriteCitiesCount,
            CreatedAt = stats.CreatedAt,
            UpdatedAt = stats.UpdatedAt
        };
    }

    private static DateTime GetMostRecentTimestamp(params DateTime?[] timestamps)
    {
        return timestamps
            .Where(timestamp => timestamp.HasValue)
            .Select(timestamp => timestamp!.Value)
            .DefaultIfEmpty(DateTime.UtcNow)
            .Max();
    }

    private static void AddUserHeaders(HttpRequestMessage request, string userId)
    {
        request.Headers.Add("X-User-Id", userId);
    }

    private sealed class AiTravelPlanSummaryContract
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

    private sealed class CitySummaryContract
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string? TimeZone { get; set; }
    }
}