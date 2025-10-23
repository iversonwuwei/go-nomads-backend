using Microsoft.AspNetCore.Mvc;
using Dapr.Client;
using Gateway.DTOs;

namespace Gateway.Controllers;

/// <summary>
/// 首页控制器 - BFF 层聚合接口
/// </summary>
[ApiController]
[Route("api/[controller]")]
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
        [FromQuery] int cityLimit = 10,
        [FromQuery] int meetupLimit = 20)
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

            // 构建聚合数据
            var homeFeed = new HomeFeedDto
            {
                Cities = cities.Data ?? new List<CityDto>(),
                Meetups = meetups.Data ?? new List<MeetupDto>(),
                Timestamp = DateTime.UtcNow,
                HasMoreCities = cities.Data?.Count >= cityLimit,
                HasMoreMeetups = meetups.Data?.Count >= meetupLimit
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
    private async Task<ApiResponse<List<CityDto>>> GetCitiesAsync(int limit)
    {
        try
        {
            // Dapr 服务调用: city-service (使用 Dapr app-id)
            var response = await _daprClient.InvokeMethodAsync<List<CityDto>>(
                HttpMethod.Get,
                "city-service",
                $"api/v1/cities?pageSize={limit}");

            return ApiResponse<List<CityDto>>.SuccessResponse(response ?? new List<CityDto>());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "调用城市服务失败，返回空列表");
            // 容错: 返回空列表而不是抛出异常
            return ApiResponse<List<CityDto>>.SuccessResponse(
                new List<CityDto>(),
                "城市服务暂时不可用");
        }
    }

    /// <summary>
    /// 调用 Event Service 获取 Meetup 列表
    /// </summary>
    private async Task<ApiResponse<List<MeetupDto>>> GetMeetupsAsync(int limit)
    {
        try
        {
            // Dapr 服务调用: event-service
            var response = await _daprClient.InvokeMethodAsync<ApiResponse<List<MeetupDto>>>(
                HttpMethod.Get,
                "event-service",
                $"api/meetups?limit={limit}&status=upcoming");

            return response ?? ApiResponse<List<MeetupDto>>.ErrorResponse("活动服务无响应");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "调用活动服务失败，返回空列表");
            // 容错: 返回空列表而不是抛出异常
            return ApiResponse<List<MeetupDto>>.SuccessResponse(
                new List<MeetupDto>(),
                "活动服务暂时不可用");
        }
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
