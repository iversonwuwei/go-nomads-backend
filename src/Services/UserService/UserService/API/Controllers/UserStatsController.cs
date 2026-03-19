using GoNomads.Shared.Communication;
using GoNomads.Shared.Middleware;
using GoNomads.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs;
using UserService.Application.Services;
using UserService.Domain.Repositories;

namespace UserService.API.Controllers;

/// <summary>
///     用户统计数据 API - RESTful endpoints for user stats management
/// </summary>
[ApiController]
[Route("api/v1/users")]
public class UserStatsController : ControllerBase
{
    private readonly ILogger<UserStatsController> _logger;
    private readonly ServiceInvocationClient _serviceInvocationClient;
    private readonly IUserStatsRepository _userStatsRepository;
    private readonly ITravelHistoryService _travelHistoryService;

    public UserStatsController(
        IUserStatsRepository userStatsRepository,
        ITravelHistoryService travelHistoryService,
        ServiceInvocationClient serviceInvocationClient,
        ILogger<UserStatsController> logger)
    {
        _userStatsRepository = userStatsRepository;
        _travelHistoryService = travelHistoryService;
        _serviceInvocationClient = serviceInvocationClient;
        _logger = logger;
    }

    /// <summary>
    ///     获取当前用户的统计数据（包含从其他服务聚合的数据）
    /// </summary>
    [HttpGet("me/stats")]
    public async Task<ActionResult<ApiResponse<UserStatsDto>>> GetCurrentUserStats(
        CancellationToken cancellationToken = default)
    {
        // 从 UserContext 获取当前用户 ID
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
        {
            return Unauthorized(new ApiResponse<UserStatsDto>
            {
                Success = false,
                Message = "未认证用户"
            });
        }

        _logger.LogInformation("🔍 获取当前用户统计数据: {UserId}", userContext.UserId);

        try
        {
            // 1. 获取用户基础统计数据
            var stats = await _userStatsRepository.GetOrCreateAsync(userContext.UserId, cancellationToken);

            // 2. 从 travel_history 表获取旅行统计（去重后的国家/城市数）
            var travelStats = await _travelHistoryService.GetUserTravelStatsAsync(userContext.UserId, cancellationToken);

            // 3. 并行获取其他服务的数据
            var meetupsCreatedTask = GetMeetupsCreatedCountAsync(userContext.UserId, cancellationToken);
            var meetupsJoinedTask = GetMeetupsJoinedCountAsync(userContext.UserId, cancellationToken);
            var favoriteCitiesTask = GetFavoriteCitiesCountAsync(userContext.UserId, cancellationToken);

            await Task.WhenAll(meetupsCreatedTask, meetupsJoinedTask, favoriteCitiesTask);

            var meetupsCreated = await meetupsCreatedTask;
            var meetupsJoined = await meetupsJoinedTask;
            var favoriteCitiesCount = await favoriteCitiesTask;

            // 使用 travel_history 的统计数据覆盖 user_stats 的数据
            return Ok(new ApiResponse<UserStatsDto>
            {
                Success = true,
                Message = "User stats retrieved successfully",
                Data = MapToDto(stats, meetupsCreated, meetupsJoined, favoriteCitiesCount, travelStats)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取用户统计数据失败: {UserId}", userContext.UserId);
            return StatusCode(500, new ApiResponse<UserStatsDto>
            {
                Success = false,
                Message = "获取用户统计数据失败"
            });
        }
    }

    /// <summary>
    ///     从 EventService 获取用户创建的 Meetup 数量
    /// </summary>
    private async Task<int> GetMeetupsCreatedCountAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _serviceInvocationClient.InvokeAsync<int>(
                HttpMethod.Get,
                "event-service",
                $"api/v1/events/user/{userId}/created/count",
                cancellationToken);

            _logger.LogInformation("✅ 获取用户 Meetups 创建数量: {Count}", response);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 获取用户 Meetups 创建数量失败，返回0");
            return 0;
        }
    }

    /// <summary>
    ///     从 EventService 获取用户参加的未结束 Meetup 数量
    /// </summary>
    private async Task<int> GetMeetupsJoinedCountAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _serviceInvocationClient.InvokeAsync<int>(
                HttpMethod.Get,
                "event-service",
                $"api/v1/events/user/{userId}/joined/count",
                cancellationToken);

            _logger.LogInformation("✅ 获取用户参加的未结束 Meetups 数量: {Count}", response);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 获取用户参加的 Meetups 数量失败，返回0");
            return 0;
        }
    }

    /// <summary>
    ///     从 CityService 获取用户收藏的城市数量
    /// </summary>
    private async Task<int> GetFavoriteCitiesCountAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _serviceInvocationClient.InvokeAsync<int>(
                HttpMethod.Get,
                "city-service",
                $"api/v1/user-favorite-cities/user/{userId}/count",
                cancellationToken);

            _logger.LogInformation("✅ 获取用户收藏城市数量: {Count}", response);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 获取用户收藏城市数量失败，返回0");
            return 0;
        }
    }

    /// <summary>
    ///     根据用户ID获取统计数据
    /// </summary>
    [HttpGet("{userId}/stats")]
    public async Task<ActionResult<ApiResponse<UserStatsDto>>> GetUserStats(
        string userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 获取用户统计数据: {UserId}", userId);

        try
        {
            var stats = await _userStatsRepository.GetOrCreateAsync(userId, cancellationToken);

            return Ok(new ApiResponse<UserStatsDto>
            {
                Success = true,
                Message = "User stats retrieved successfully",
                Data = MapToDto(stats)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取用户统计数据失败: {UserId}", userId);
            return StatusCode(500, new ApiResponse<UserStatsDto>
            {
                Success = false,
                Message = "获取用户统计数据失败"
            });
        }
    }

    /// <summary>
    ///     更新当前用户的统计数据
    /// </summary>
    [HttpPut("me/stats")]
    public async Task<ActionResult<ApiResponse<UserStatsDto>>> UpdateCurrentUserStats(
        [FromBody] UpdateUserStatsRequest request,
        CancellationToken cancellationToken = default)
    {
        // 从 UserContext 获取当前用户 ID
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
        {
            return Unauthorized(new ApiResponse<UserStatsDto>
            {
                Success = false,
                Message = "未认证用户"
            });
        }

        _logger.LogInformation("📝 更新当前用户统计数据: {UserId}", userContext.UserId);

        try
        {
            // 获取或创建统计数据
            var stats = await _userStatsRepository.GetOrCreateAsync(userContext.UserId, cancellationToken);

            // 更新字段
            stats.Update(
                request.CountriesVisited,
                request.CitiesLived,
                request.DaysNomading,
                request.TripsCompleted
            );

            // 保存更新
            var updatedStats = await _userStatsRepository.UpdateAsync(stats, cancellationToken);

            return Ok(new ApiResponse<UserStatsDto>
            {
                Success = true,
                Message = "User stats updated successfully",
                Data = MapToDto(updatedStats)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新用户统计数据失败: {UserId}", userContext.UserId);
            return StatusCode(500, new ApiResponse<UserStatsDto>
            {
                Success = false,
                Message = "更新用户统计数据失败"
            });
        }
    }

    /// <summary>
    ///     更新指定用户的统计数据（管理员功能）
    /// </summary>
    [HttpPut("{userId}/stats")]
    public async Task<ActionResult<ApiResponse<UserStatsDto>>> UpdateUserStats(
        string userId,
        [FromBody] UpdateUserStatsRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 更新用户统计数据: {UserId}", userId);

        try
        {
            // 获取或创建统计数据
            var stats = await _userStatsRepository.GetOrCreateAsync(userId, cancellationToken);

            // 更新字段
            stats.Update(
                request.CountriesVisited,
                request.CitiesLived,
                request.DaysNomading,
                request.TripsCompleted
            );

            // 保存更新
            var updatedStats = await _userStatsRepository.UpdateAsync(stats, cancellationToken);

            return Ok(new ApiResponse<UserStatsDto>
            {
                Success = true,
                Message = "User stats updated successfully",
                Data = MapToDto(updatedStats)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新用户统计数据失败: {UserId}", userId);
            return StatusCode(500, new ApiResponse<UserStatsDto>
            {
                Success = false,
                Message = "更新用户统计数据失败"
            });
        }
    }

    #region Private Methods

    private static UserStatsDto MapToDto(
        Domain.Entities.UserStats stats, 
        int meetupsCreated = 0, 
        int meetupsJoined = 0,
        int favoriteCitiesCount = 0,
        TravelHistoryStats? travelStats = null)
    {
        return new UserStatsDto
        {
            Id = stats.Id,
            UserId = stats.UserId,
            // 优先使用 travel_history 表的统计数据（去重后的真实数据）
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

    #endregion
}
