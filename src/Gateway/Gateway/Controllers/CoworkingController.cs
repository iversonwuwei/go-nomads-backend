using Dapr.Client;
using Gateway.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Controllers;

/// <summary>
///     Coworking 空间控制器 - Gateway 代理层
/// </summary>
[ApiController]
[Route("api/v1/coworking")]
public class CoworkingController : ControllerBase
{
    private const string CoworkingServiceAppId = "coworking-service";
    private readonly DaprClient _daprClient;
    private readonly ILogger<CoworkingController> _logger;

    public CoworkingController(DaprClient daprClient, ILogger<CoworkingController> logger)
    {
        _daprClient = daprClient;
        _logger = logger;
    }

    /// <summary>
    ///     根据城市ID获取 Coworking 空间列表（分页）
    /// </summary>
    /// <param name="cityId">城市ID</param>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页数量</param>
    [HttpGet("city/{cityId}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<object>>> GetCoworkingSpacesByCity(
        string cityId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            _logger.LogInformation(
                "Gateway: 获取城市 {CityId} 的 Coworking 空间列表, Page={Page}, PageSize={PageSize}",
                cityId, page, pageSize);

            // 调用 CoworkingService
            var response = await _daprClient.InvokeMethodAsync<ApiResponse<object>>(
                HttpMethod.Get,
                CoworkingServiceAppId,
                $"api/v1/coworking/city/{cityId}?page={page}&pageSize={pageSize}");

            if (response?.Success != true)
            {
                _logger.LogWarning(
                    "CoworkingService 返回非成功结果: {Message}",
                    response?.Message ?? "Unknown");
                return StatusCode(500, response);
            }

            _logger.LogInformation(
                "Gateway: 成功获取城市 {CityId} 的 Coworking 空间列表",
                cityId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gateway: 获取城市 {CityId} 的 Coworking 空间列表失败", cityId);
            return StatusCode(500, ApiResponse<object>.ErrorResponse(
                "获取 Coworking 空间列表失败",
                new List<string> { ex.Message }));
        }
    }

    /// <summary>
    ///     获取所有 Coworking 空间列表（分页）
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> GetCoworkingSpaces(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? cityId = null)
    {
        try
        {
            var queryParams = $"page={page}&pageSize={pageSize}";
            if (!string.IsNullOrEmpty(cityId)) queryParams += $"&cityId={cityId}";

            var response = await _daprClient.InvokeMethodAsync<ApiResponse<object>>(
                HttpMethod.Get,
                CoworkingServiceAppId,
                $"api/v1/coworking?{queryParams}");

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gateway: 获取 Coworking 空间列表失败");
            return StatusCode(500, ApiResponse<object>.ErrorResponse(
                "获取 Coworking 空间列表失败",
                new List<string> { ex.Message }));
        }
    }

    /// <summary>
    ///     根据ID获取单个 Coworking 空间
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> GetCoworkingSpace(string id)
    {
        try
        {
            var response = await _daprClient.InvokeMethodAsync<ApiResponse<object>>(
                HttpMethod.Get,
                CoworkingServiceAppId,
                $"api/v1/coworking/{id}");

            if (response?.Success != true) return NotFound(response);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gateway: 获取 Coworking 空间 {Id} 失败", id);
            return StatusCode(500, ApiResponse<object>.ErrorResponse(
                "获取 Coworking 空间失败",
                new List<string> { ex.Message }));
        }
    }
}