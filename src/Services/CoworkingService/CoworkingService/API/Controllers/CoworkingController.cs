using CoworkingService.Application.DTOs;
using CoworkingService.Application.Services;
using GoNomads.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace CoworkingService.API.Controllers;

/// <summary>
///     Coworking 空间 API 控制器
///     薄控制器 - 只负责 HTTP 请求/响应处理
/// </summary>
[ApiController]
[Route("api/v1/coworking")]
public class CoworkingController : ControllerBase
{
    private readonly ICoworkingService _coworkingService;
    private readonly ILogger<CoworkingController> _logger;

    public CoworkingController(
        ICoworkingService coworkingService,
        ILogger<CoworkingController> logger)
    {
        _coworkingService = coworkingService;
        _logger = logger;
    }

    /// <summary>
    ///     获取所有 Coworking 空间列表（分页）
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedCoworkingSpacesResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedCoworkingSpacesResponse>>> GetCoworkingSpaces(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? cityId = null)
    {
        try
        {
            var result = await _coworkingService.GetCoworkingSpacesAsync(page, pageSize, cityId);

            return Ok(ApiResponse<PaginatedCoworkingSpacesResponse>.SuccessResponse(
                result,
                $"成功获取 {result.Items.Count} 个 Coworking 空间"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取 Coworking 空间列表失败");
            return StatusCode(500, ApiResponse<PaginatedCoworkingSpacesResponse>.ErrorResponse(
                "获取 Coworking 空间列表失败",
                new List<string> { ex.Message }));
        }
    }

    /// <summary>
    ///     根据城市ID获取 Coworking 空间列表（分页）
    /// </summary>
    [HttpGet("city/{cityId}")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedCoworkingSpacesResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedCoworkingSpacesResponse>>> GetCoworkingSpacesByCity(
        Guid cityId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            _logger.LogInformation("获取城市 {CityId} 的 Coworking 空间列表, Page={Page}, PageSize={PageSize}",
                cityId, page, pageSize);

            var result = await _coworkingService.GetCoworkingSpacesAsync(page, pageSize, cityId);

            return Ok(ApiResponse<PaginatedCoworkingSpacesResponse>.SuccessResponse(
                result,
                $"成功获取城市的 {result.Items.Count} 个 Coworking 空间"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取城市 {CityId} 的 Coworking 空间列表失败", cityId);
            return StatusCode(500, ApiResponse<PaginatedCoworkingSpacesResponse>.ErrorResponse(
                "获取 Coworking 空间列表失败",
                new List<string> { ex.Message }));
        }
    }

    /// <summary>
    ///     根据 ID 获取单个 Coworking 空间
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<CoworkingSpaceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CoworkingSpaceResponse>>> GetCoworkingSpace(Guid id)
    {
        try
        {
            var result = await _coworkingService.GetCoworkingSpaceAsync(id);

            return Ok(ApiResponse<CoworkingSpaceResponse>.SuccessResponse(
                result,
                "成功获取 Coworking 空间信息"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(
                "未找到指定的 Coworking 空间",
                new List<string> { ex.Message }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取 Coworking 空间失败: {Id}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResponse(
                "获取 Coworking 空间失败",
                new List<string> { ex.Message }));
        }
    }

    /// <summary>
    ///     创建新的 Coworking 空间
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CoworkingSpaceResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<CoworkingSpaceResponse>>> CreateCoworkingSpace(
        [FromBody] CreateCoworkingSpaceRequest request)
    {
        try
        {
            var result = await _coworkingService.CreateCoworkingSpaceAsync(request);

            return CreatedAtAction(
                nameof(GetCoworkingSpace),
                new { id = result.Id },
                ApiResponse<CoworkingSpaceResponse>.SuccessResponse(
                    result,
                    "Coworking 空间创建成功"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(
                "输入参数无效",
                new List<string> { ex.Message }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建 Coworking 空间失败");
            return StatusCode(500, ApiResponse<object>.ErrorResponse(
                "创建 Coworking 空间失败",
                new List<string> { ex.Message }));
        }
    }

    /// <summary>
    ///     更新 Coworking 空间
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<CoworkingSpaceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<CoworkingSpaceResponse>>> UpdateCoworkingSpace(
        Guid id,
        [FromBody] UpdateCoworkingSpaceRequest request)
    {
        try
        {
            var result = await _coworkingService.UpdateCoworkingSpaceAsync(id, request);

            return Ok(ApiResponse<CoworkingSpaceResponse>.SuccessResponse(
                result,
                "Coworking 空间更新成功"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(
                "未找到指定的 Coworking 空间",
                new List<string> { ex.Message }));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(
                "输入参数无效",
                new List<string> { ex.Message }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新 Coworking 空间失败: {Id}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResponse(
                "更新 Coworking 空间失败",
                new List<string> { ex.Message }));
        }
    }

    /// <summary>
    ///     删除 Coworking 空间
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<string>>> DeleteCoworkingSpace(Guid id)
    {
        try
        {
            await _coworkingService.DeleteCoworkingSpaceAsync(id);

            return Ok(ApiResponse<string>.SuccessResponse(
                "删除成功",
                "Coworking 空间删除成功"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(
                "未找到指定的 Coworking 空间",
                new List<string> { ex.Message }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除 Coworking 空间失败: {Id}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResponse(
                "删除 Coworking 空间失败",
                new List<string> { ex.Message }));
        }
    }

    /// <summary>
    ///     搜索 Coworking 空间
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<List<CoworkingSpaceResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<CoworkingSpaceResponse>>>> SearchCoworkingSpaces(
        [FromQuery] string searchTerm,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await _coworkingService.SearchCoworkingSpacesAsync(searchTerm, page, pageSize);

            return Ok(ApiResponse<List<CoworkingSpaceResponse>>.SuccessResponse(
                result,
                $"找到 {result.Count} 个匹配的 Coworking 空间"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索 Coworking 空间失败");
            return StatusCode(500, ApiResponse<List<CoworkingSpaceResponse>>.ErrorResponse(
                "搜索失败",
                new List<string> { ex.Message }));
        }
    }

    /// <summary>
    ///     获取评分最高的 Coworking 空间
    /// </summary>
    [HttpGet("top-rated")]
    [ProducesResponseType(typeof(ApiResponse<List<CoworkingSpaceResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<CoworkingSpaceResponse>>>> GetTopRatedCoworkingSpaces(
        [FromQuery] int limit = 10)
    {
        try
        {
            var result = await _coworkingService.GetTopRatedCoworkingSpacesAsync(limit);

            return Ok(ApiResponse<List<CoworkingSpaceResponse>>.SuccessResponse(
                result,
                $"获取到 {result.Count} 个评分最高的 Coworking 空间"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取评分最高的 Coworking 空间失败");
            return StatusCode(500, ApiResponse<List<CoworkingSpaceResponse>>.ErrorResponse(
                "获取失败",
                new List<string> { ex.Message }));
        }
    }

    /// <summary>
    ///     批量获取多个城市的 Coworking 空间数量
    /// </summary>
    [HttpGet("count-by-cities")]
    [ProducesResponseType(typeof(ApiResponse<Dictionary<Guid, int>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<Dictionary<Guid, int>>>> GetCoworkingCountByCities(
        [FromQuery] string cityIds)
    {
        try
        {
            // 解析城市 ID 列表 (格式: "id1,id2,id3")
            if (string.IsNullOrWhiteSpace(cityIds))
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "参数无效",
                    new List<string> { "cityIds 参数不能为空" }));

            var cityIdList = cityIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => Guid.Parse(id.Trim()))
                .ToList();

            if (cityIdList.Count == 0)
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "参数无效",
                    new List<string> { "至少需要提供一个城市 ID" }));

            var result = await _coworkingService.GetCoworkingCountByCitiesAsync(cityIdList);

            return Ok(ApiResponse<Dictionary<Guid, int>>.SuccessResponse(
                result,
                $"成功获取 {result.Count} 个城市的 Coworking 空间数量"));
        }
        catch (FormatException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(
                "城市 ID 格式无效",
                new List<string> { ex.Message }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量获取城市 Coworking 空间数量失败");
            return StatusCode(500, ApiResponse<object>.ErrorResponse(
                "获取失败",
                new List<string> { ex.Message }));
        }
    }

    #region Booking Endpoints

    /// <summary>
    ///     创建预订
    /// </summary>
    [HttpPost("{coworkingId}/bookings")]
    [ProducesResponseType(typeof(ApiResponse<CoworkingBookingResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<CoworkingBookingResponse>>> CreateBooking(
        Guid coworkingId,
        [FromBody] CreateBookingRequest request)
    {
        try
        {
            // 确保 coworkingId 一致
            request.CoworkingId = coworkingId;

            var result = await _coworkingService.CreateBookingAsync(request);

            return CreatedAtAction(
                nameof(GetBooking),
                new { id = result.Id },
                ApiResponse<CoworkingBookingResponse>.SuccessResponse(
                    result,
                    "预订创建成功"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(
                "未找到指定的 Coworking 空间",
                new List<string> { ex.Message }));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(
                "预订失败",
                new List<string> { ex.Message }));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(
                "输入参数无效",
                new List<string> { ex.Message }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建预订失败");
            return StatusCode(500, ApiResponse<object>.ErrorResponse(
                "创建预订失败",
                new List<string> { ex.Message }));
        }
    }

    /// <summary>
    ///     获取预订详情
    /// </summary>
    [HttpGet("bookings/{id}")]
    [ProducesResponseType(typeof(ApiResponse<CoworkingBookingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CoworkingBookingResponse>>> GetBooking(Guid id)
    {
        try
        {
            var result = await _coworkingService.GetBookingAsync(id);

            return Ok(ApiResponse<CoworkingBookingResponse>.SuccessResponse(
                result,
                "获取预订成功"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(
                "未找到指定的预订",
                new List<string> { ex.Message }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取预订失败: {Id}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResponse(
                "获取预订失败",
                new List<string> { ex.Message }));
        }
    }

    /// <summary>
    ///     取消预订
    /// </summary>
    [HttpPost("bookings/{id}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<string>>> CancelBooking(
        Guid id,
        [FromQuery] Guid userId)
    {
        try
        {
            await _coworkingService.CancelBookingAsync(id, userId);

            return Ok(ApiResponse<string>.SuccessResponse(
                "取消成功",
                "预订已取消"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(
                "未找到指定的预订",
                new List<string> { ex.Message }));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<object>.ErrorResponse(
                "无权操作",
                new List<string> { ex.Message }));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(
                "取消失败",
                new List<string> { ex.Message }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消预订失败: {Id}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResponse(
                "取消预订失败",
                new List<string> { ex.Message }));
        }
    }

    /// <summary>
    ///     获取用户的预订列表
    /// </summary>
    [HttpGet("bookings/user/{userId}")]
    [ProducesResponseType(typeof(ApiResponse<List<CoworkingBookingResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<CoworkingBookingResponse>>>> GetUserBookings(Guid userId)
    {
        try
        {
            var result = await _coworkingService.GetUserBookingsAsync(userId);

            return Ok(ApiResponse<List<CoworkingBookingResponse>>.SuccessResponse(
                result,
                $"获取到 {result.Count} 个预订"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户预订列表失败: {UserId}", userId);
            return StatusCode(500, ApiResponse<List<CoworkingBookingResponse>>.ErrorResponse(
                "获取预订列表失败",
                new List<string> { ex.Message }));
        }
    }

    #endregion
}