using Microsoft.AspNetCore.Mvc;
using Dapr.Client;
using Gateway.DTOs;
using GoNomads.Shared.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Gateway.Controllers;

/// <summary>
/// 首页控制器 - BFF 层聚合接口
/// </summary>
[ApiController]
[Route("api/v1/home")]
public class HomeController : ControllerBase
{
    private readonly DaprClient _daprClient;
    private readonly ILogger<HomeController> _logger;

    public HomeController(DaprClient daprClient, ILogger<HomeController> logger)
    {
        _daprClient = daprClient;
        _logger = logger;
    }

    /// <summary>
    /// 获取首页聚合数据 (城市列表 + Meetup 列表)
    /// </summary>
    /// <param name="cityLimit">城市列表数量限制</param>
    /// <param name="meetupLimit">Meetup 列表数量限制</param>
    /// <returns>首页聚合数据</returns>
    [HttpGet("feed")]
    [ProducesResponseType(typeof(ApiResponse<HomeFeedDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<HomeFeedDto>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<HomeFeedDto>>> GetHomeFeed(
        [FromQuery] int cityLimit = 6,
        [FromQuery] int meetupLimit = 6)
    {
        try
        {
            // 并行调用多个微服务
            var citiesTask = GetCitiesAsync(cityLimit);
            var meetupsTask = GetMeetupsAsync(meetupLimit);

            // 等待所有任务完成 (容错机制: 部分失败不影响整体)
            await Task.WhenAll(citiesTask, meetupsTask);

            // 获取结果
            var cities = await citiesTask;
            var meetups = await meetupsTask;

            if (!cities.Response.Success)
            {
                _logger.LogWarning("城市服务返回非成功结果: {Message}", cities.Response.Message);
            }

            if (!meetups.Response.Success)
            {
                _logger.LogWarning("活动服务返回非成功结果: {Message}", meetups.Response.Message);
            }

            var cityList = cities.Response.Data ?? new List<CityDto>();
            var meetupList = meetups.Response.Data ?? new List<MeetupDto>();

            // 构建聚合数据
            var homeFeed = new HomeFeedDto
            {
                Cities = cityList,
                Meetups = meetupList,
                Timestamp = DateTime.UtcNow,
                HasMoreCities = cities.HasMore,
                HasMoreMeetups = meetups.HasMore
            };

            _logger.LogInformation(
                "首页数据聚合成功: {CityCount} 个城市, {MeetupCount} 个活动",
                homeFeed.Cities.Count,
                homeFeed.Meetups.Count);

            return Ok(ApiResponse<HomeFeedDto>.SuccessResponse(
                homeFeed,
                "首页数据加载成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取首页聚合数据失败");
            return StatusCode(500, ApiResponse<HomeFeedDto>.ErrorResponse(
                "首页数据加载失败，请稍后重试",
                new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// 调用 City Service 获取城市列表
    /// </summary>
    private async Task<ServiceListResult<CityDto>> GetCitiesAsync(int limit)
    {
        try
        {
            // Dapr 服务调用: city-service (使用 Dapr app-id)
            var result = await _daprClient.InvokeApiAsync<PaginatedResponse<CityDto>>(
                HttpMethod.Get,
                "city-service",
                $"api/v1/cities?pageSize={limit}");

            if (!result.Success)
            {
                _logger.LogWarning("城市服务返回非成功结果: {Message}", result.Message);
                var errorResponse = ApiResponse<List<CityDto>>.ErrorResponse(
                    string.IsNullOrWhiteSpace(result.Message) ? "城市服务返回错误" : result.Message,
                    result.Errors?.ToList() ?? new List<string>());
                errorResponse.Data = new List<CityDto>();

                return new ServiceListResult<CityDto>
                {
                    Response = errorResponse,
                    HasMore = false
                };
            }

            var paginated = result.Data;
            var items = paginated?.Items ?? new List<CityDto>();
            var totalCount = paginated?.TotalCount ?? items.Count;
            var currentPage = paginated?.Page ?? 1;
            var pageSize = paginated?.PageSize ?? limit;
            var hasMore = totalCount > currentPage * pageSize;

            return new ServiceListResult<CityDto>
            {
                Response = ApiResponse<List<CityDto>>.SuccessResponse(items, result.Message),
                HasMore = hasMore
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "调用城市服务失败，返回空列表");
            // 容错: 返回空列表而不是抛出异常
            var fallback = ApiResponse<List<CityDto>>.ErrorResponse(
                "城市服务暂时不可用",
                new List<string> { ex.Message });
            fallback.Data = new List<CityDto>();

            return new ServiceListResult<CityDto>
            {
                Response = fallback,
                HasMore = false
            };
        }
    }

    /// <summary>
    /// 调用 Event Service 获取 Meetup 列表
    /// </summary>
    private async Task<ServiceListResult<MeetupDto>> GetMeetupsAsync(int limit)
    {
        try
        {
            // Dapr 服务调用: event-service
            var result = await _daprClient.InvokeApiAsync<PaginatedResponse<MeetupDto>>(
                HttpMethod.Get,
                "event-service",
                $"api/v1/events?status=upcoming&pageSize={limit}");

            if (!result.Success)
            {
                _logger.LogWarning("活动服务返回非成功结果: {Message}", result.Message);
                var errorResponse = ApiResponse<List<MeetupDto>>.ErrorResponse(
                    string.IsNullOrWhiteSpace(result.Message) ? "活动服务返回错误" : result.Message,
                    result.Errors?.ToList() ?? new List<string>());
                errorResponse.Data = new List<MeetupDto>();

                return new ServiceListResult<MeetupDto>
                {
                    Response = errorResponse,
                    HasMore = false
                };
            }

            var paginated = result.Data;
            var items = paginated?.Items ?? new List<MeetupDto>();
            var totalCount = paginated?.TotalCount ?? items.Count;
            var currentPage = paginated?.Page ?? 1;
            var pageSize = paginated?.PageSize ?? limit;
            var hasMore = totalCount > currentPage * pageSize;

            return new ServiceListResult<MeetupDto>
            {
                Response = ApiResponse<List<MeetupDto>>.SuccessResponse(items, result.Message),
                HasMore = hasMore
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "调用活动服务失败，返回空列表");
            // 容错: 返回空列表而不是抛出异常
            var fallback = ApiResponse<List<MeetupDto>>.ErrorResponse(
                "活动服务暂时不可用",
                new List<string> { ex.Message });
            fallback.Data = new List<MeetupDto>();

            return new ServiceListResult<MeetupDto>
            {
                Response = fallback,
                HasMore = false
            };
        }
    }

    private sealed class ServiceListResult<T>
    {
        public ApiResponse<List<T>> Response { get; init; } = ApiResponse<List<T>>.SuccessResponse(new List<T>());
        public bool HasMore { get; init; }
    }

    /// <summary>
    /// 健康检查接口
    /// </summary>
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
