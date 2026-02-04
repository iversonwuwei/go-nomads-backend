using GoNomads.Shared.Middleware;
using GoNomads.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs;
using UserService.Application.Services;

namespace UserService.API.Controllers;

/// <summary>
///     Visited Places API - RESTful endpoints for managing visited places
/// </summary>
[ApiController]
[Route("api/v1/visited-places")]
public class VisitedPlaceController : ControllerBase
{
    private readonly ILogger<VisitedPlaceController> _logger;
    private readonly IVisitedPlaceService _visitedPlaceService;

    public VisitedPlaceController(
        IVisitedPlaceService visitedPlaceService,
        ILogger<VisitedPlaceController> logger)
    {
        _visitedPlaceService = visitedPlaceService;
        _logger = logger;
    }

    /// <summary>
    ///     获取旅行的访问地点列表
    /// </summary>
    [HttpGet("by-travel-history/{travelHistoryId}")]
    public async Task<ActionResult<ApiResponse<List<VisitedPlaceDto>>>> GetByTravelHistoryId(
        [FromRoute] string travelHistoryId,
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
            return Unauthorized(new ApiResponse<List<VisitedPlaceDto>>
            {
                Success = false,
                Message = "未授权访问"
            });

        _logger.LogInformation("📋 GetByTravelHistoryId - TravelHistoryId: {TravelHistoryId}, UserId: {UserId}",
            travelHistoryId, userContext.UserId);

        try
        {
            var places = await _visitedPlaceService.GetVisitedPlacesByTravelHistoryIdAsync(
                travelHistoryId, cancellationToken);

            return Ok(new ApiResponse<List<VisitedPlaceDto>>
            {
                Success = true,
                Message = "获取访问地点列表成功",
                Data = places
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取访问地点列表失败");
            return StatusCode(500, new ApiResponse<List<VisitedPlaceDto>>
            {
                Success = false,
                Message = "获取访问地点列表失败"
            });
        }
    }

    /// <summary>
    ///     获取旅行的精选地点
    /// </summary>
    [HttpGet("by-travel-history/{travelHistoryId}/highlights")]
    public async Task<ActionResult<ApiResponse<List<VisitedPlaceDto>>>> GetHighlightsByTravelHistoryId(
        [FromRoute] string travelHistoryId,
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
            return Unauthorized(new ApiResponse<List<VisitedPlaceDto>>
            {
                Success = false,
                Message = "未授权访问"
            });

        _logger.LogInformation("📋 GetHighlightsByTravelHistoryId - TravelHistoryId: {TravelHistoryId}", travelHistoryId);

        try
        {
            var places = await _visitedPlaceService.GetHighlightPlacesByTravelHistoryIdAsync(
                travelHistoryId, cancellationToken);

            return Ok(new ApiResponse<List<VisitedPlaceDto>>
            {
                Success = true,
                Message = "获取精选地点列表成功",
                Data = places
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取精选地点列表失败");
            return StatusCode(500, new ApiResponse<List<VisitedPlaceDto>>
            {
                Success = false,
                Message = "获取精选地点列表失败"
            });
        }
    }

    /// <summary>
    ///     获取当前用户的所有访问地点（分页）
    /// </summary>
    [HttpGet("my")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<VisitedPlaceDto>>>> GetMyVisitedPlaces(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
            return Unauthorized(new ApiResponse<PaginatedResponse<VisitedPlaceDto>>
            {
                Success = false,
                Message = "未授权访问"
            });

        _logger.LogInformation("📋 GetMyVisitedPlaces - UserId: {UserId}, Page: {Page}", userContext.UserId, page);

        try
        {
            var (items, total) = await _visitedPlaceService.GetUserVisitedPlacesAsync(
                userContext.UserId!, page, pageSize, cancellationToken);

            return Ok(new ApiResponse<PaginatedResponse<VisitedPlaceDto>>
            {
                Success = true,
                Message = "获取访问地点列表成功",
                Data = new PaginatedResponse<VisitedPlaceDto>
                {
                    Items = items,
                    TotalCount = total,
                    Page = page,
                    PageSize = pageSize
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取访问地点列表失败");
            return StatusCode(500, new ApiResponse<PaginatedResponse<VisitedPlaceDto>>
            {
                Success = false,
                Message = "获取访问地点列表失败"
            });
        }
    }

    /// <summary>
    ///     获取城市访问摘要（用于 Visited Places 页面）
    ///     包含：城市信息、天气、评分、花费、共享办公数量、访问地点列表（分页）
    /// </summary>
    [HttpGet("city-summary/{cityId}")]
    public async Task<ActionResult<ApiResponse<VisitedPlacesCitySummaryDto>>> GetCitySummary(
        [FromRoute] string cityId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
            return Unauthorized(new ApiResponse<VisitedPlacesCitySummaryDto>
            {
                Success = false,
                Message = "未授权访问"
            });

        _logger.LogInformation("🏙️ GetCitySummary - CityId: {CityId}, UserId: {UserId}, Page: {Page}",
            cityId, userContext.UserId, page);

        try
        {
            var summary = await _visitedPlaceService.GetCitySummaryAsync(
                userContext.UserId!, cityId, page, pageSize, cancellationToken);

            return Ok(new ApiResponse<VisitedPlacesCitySummaryDto>
            {
                Success = true,
                Message = "获取城市访问摘要成功",
                Data = summary
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取城市访问摘要失败: CityId={CityId}", cityId);
            return StatusCode(500, new ApiResponse<VisitedPlacesCitySummaryDto>
            {
                Success = false,
                Message = "获取城市访问摘要失败"
            });
        }
    }

    /// <summary>
    ///     获取访问地点详情
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<VisitedPlaceDto>>> GetById(
        [FromRoute] string id,
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
            return Unauthorized(new ApiResponse<VisitedPlaceDto>
            {
                Success = false,
                Message = "未授权访问"
            });

        _logger.LogInformation("🔍 GetById - Id: {Id}, UserId: {UserId}", id, userContext.UserId);

        try
        {
            var place = await _visitedPlaceService.GetVisitedPlaceByIdAsync(id, cancellationToken);

            if (place == null)
                return NotFound(new ApiResponse<VisitedPlaceDto>
                {
                    Success = false,
                    Message = "访问地点不存在"
                });

            // 验证所有权
            if (place.UserId != userContext.UserId)
                return Forbid();

            return Ok(new ApiResponse<VisitedPlaceDto>
            {
                Success = true,
                Message = "获取访问地点详情成功",
                Data = place
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取访问地点详情失败");
            return StatusCode(500, new ApiResponse<VisitedPlaceDto>
            {
                Success = false,
                Message = "获取访问地点详情失败"
            });
        }
    }

    /// <summary>
    ///     创建访问地点
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<VisitedPlaceDto>>> Create(
        [FromBody] CreateVisitedPlaceDto dto,
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
            return Unauthorized(new ApiResponse<VisitedPlaceDto>
            {
                Success = false,
                Message = "未授权访问"
            });

        _logger.LogInformation("📝 Create - UserId: {UserId}, TravelHistoryId: {TravelHistoryId}",
            userContext.UserId, dto.TravelHistoryId);

        try
        {
            var place = await _visitedPlaceService.CreateVisitedPlaceAsync(
                userContext.UserId!, dto, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = place.Id }, new ApiResponse<VisitedPlaceDto>
            {
                Success = true,
                Message = "创建访问地点成功",
                Data = place
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<VisitedPlaceDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建访问地点失败");
            return StatusCode(500, new ApiResponse<VisitedPlaceDto>
            {
                Success = false,
                Message = "创建访问地点失败"
            });
        }
    }

    /// <summary>
    ///     批量创建访问地点（用于同步）
    /// </summary>
    [HttpPost("batch")]
    public async Task<ActionResult<ApiResponse<List<VisitedPlaceDto>>>> CreateBatch(
        [FromBody] BatchCreateVisitedPlaceDto dto,
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
            return Unauthorized(new ApiResponse<List<VisitedPlaceDto>>
            {
                Success = false,
                Message = "未授权访问"
            });

        _logger.LogInformation("📝 CreateBatch - UserId: {UserId}, TravelHistoryId: {TravelHistoryId}, Count: {Count}",
            userContext.UserId, dto.TravelHistoryId, dto.Items.Count);

        try
        {
            var places = await _visitedPlaceService.CreateBatchVisitedPlacesAsync(
                userContext.UserId!, dto, cancellationToken);

            return Ok(new ApiResponse<List<VisitedPlaceDto>>
            {
                Success = true,
                Message = $"成功创建 {places.Count} 个访问地点",
                Data = places
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<List<VisitedPlaceDto>>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 批量创建访问地点失败");
            return StatusCode(500, new ApiResponse<List<VisitedPlaceDto>>
            {
                Success = false,
                Message = "批量创建访问地点失败"
            });
        }
    }

    /// <summary>
    ///     更新访问地点
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<VisitedPlaceDto>>> Update(
        [FromRoute] string id,
        [FromBody] UpdateVisitedPlaceDto dto,
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
            return Unauthorized(new ApiResponse<VisitedPlaceDto>
            {
                Success = false,
                Message = "未授权访问"
            });

        _logger.LogInformation("📝 Update - Id: {Id}, UserId: {UserId}", id, userContext.UserId);

        try
        {
            var place = await _visitedPlaceService.UpdateVisitedPlaceAsync(
                id, userContext.UserId!, dto, cancellationToken);

            if (place == null)
                return NotFound(new ApiResponse<VisitedPlaceDto>
                {
                    Success = false,
                    Message = "访问地点不存在"
                });

            return Ok(new ApiResponse<VisitedPlaceDto>
            {
                Success = true,
                Message = "更新访问地点成功",
                Data = place
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新访问地点失败");
            return StatusCode(500, new ApiResponse<VisitedPlaceDto>
            {
                Success = false,
                Message = "更新访问地点失败"
            });
        }
    }

    /// <summary>
    ///     删除访问地点
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(
        [FromRoute] string id,
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
            return Unauthorized(new ApiResponse<bool>
            {
                Success = false,
                Message = "未授权访问"
            });

        _logger.LogInformation("🗑️ Delete - Id: {Id}, UserId: {UserId}", id, userContext.UserId);

        try
        {
            var result = await _visitedPlaceService.DeleteVisitedPlaceAsync(
                id, userContext.UserId!, cancellationToken);

            if (!result)
                return NotFound(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "访问地点不存在"
                });

            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Message = "删除访问地点成功",
                Data = true
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除访问地点失败");
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "删除访问地点失败"
            });
        }
    }

    /// <summary>
    ///     标记/取消标记为精选地点
    /// </summary>
    [HttpPatch("{id}/highlight")]
    public async Task<ActionResult<ApiResponse<VisitedPlaceDto>>> ToggleHighlight(
        [FromRoute] string id,
        [FromBody] ToggleHighlightRequest request,
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
            return Unauthorized(new ApiResponse<VisitedPlaceDto>
            {
                Success = false,
                Message = "未授权访问"
            });

        _logger.LogInformation("⭐ ToggleHighlight - Id: {Id}, IsHighlight: {IsHighlight}", id, request.IsHighlight);

        try
        {
            var place = await _visitedPlaceService.ToggleHighlightAsync(
                id, userContext.UserId!, request.IsHighlight, cancellationToken);

            if (place == null)
                return NotFound(new ApiResponse<VisitedPlaceDto>
                {
                    Success = false,
                    Message = "访问地点不存在"
                });

            return Ok(new ApiResponse<VisitedPlaceDto>
            {
                Success = true,
                Message = request.IsHighlight ? "已标记为精选" : "已取消精选",
                Data = place
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 切换精选状态失败");
            return StatusCode(500, new ApiResponse<VisitedPlaceDto>
            {
                Success = false,
                Message = "切换精选状态失败"
            });
        }
    }

    /// <summary>
    ///     获取旅行访问地点统计
    /// </summary>
    [HttpGet("by-travel-history/{travelHistoryId}/stats")]
    public async Task<ActionResult<ApiResponse<TravelVisitedPlaceStatsDto>>> GetStats(
        [FromRoute] string travelHistoryId,
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
            return Unauthorized(new ApiResponse<TravelVisitedPlaceStatsDto>
            {
                Success = false,
                Message = "未授权访问"
            });

        _logger.LogInformation("📊 GetStats - TravelHistoryId: {TravelHistoryId}", travelHistoryId);

        try
        {
            var stats = await _visitedPlaceService.GetVisitedPlaceStatsAsync(
                travelHistoryId, cancellationToken);

            return Ok(new ApiResponse<TravelVisitedPlaceStatsDto>
            {
                Success = true,
                Message = "获取统计成功",
                Data = stats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取统计失败");
            return StatusCode(500, new ApiResponse<TravelVisitedPlaceStatsDto>
            {
                Success = false,
                Message = "获取统计失败"
            });
        }
    }
}

/// <summary>
///     切换精选状态请求
/// </summary>
public class ToggleHighlightRequest
{
    public bool IsHighlight { get; set; }
}
