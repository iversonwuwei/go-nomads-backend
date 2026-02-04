using CoworkingService.Application.DTOs;
using CoworkingService.Application.Services;
using CoworkingService.Domain.Entities;
using GoNomads.Shared.DTOs;
using GoNomads.Shared.Middleware;
using GoNomads.Shared.Services;
using Microsoft.AspNetCore.Mvc;
using SharedModels = GoNomads.Shared.Models;

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
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<CoworkingController> _logger;

    public CoworkingController(
        ICoworkingService coworkingService,
        ICurrentUserService currentUser,
        ILogger<CoworkingController> logger)
    {
        _coworkingService = coworkingService;
        _currentUser = currentUser;
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
            var userId = _currentUser.TryGetUserId();
            var result = await _coworkingService.GetCoworkingSpacesAsync(page, pageSize, cityId);
            ApplyOwnershipFlag(result, userId);

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

            var userId = _currentUser.TryGetUserId();
            var result = await _coworkingService.GetCoworkingSpacesAsync(page, pageSize, cityId);
            ApplyOwnershipFlag(result, userId);

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
            var userId = _currentUser.TryGetUserId();
            var result = await _coworkingService.GetCoworkingSpaceAsync(id);
            ApplyOwnershipFlag(result, userId);

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
            var userId = _currentUser.TryGetUserId();
            if (!userId.HasValue)
                return Unauthorized(ApiResponse<object>.ErrorResponse("需要登录", new List<string> { "未检测到用户信息" }));

            request.CreatedBy = userId.Value;

            var result = await _coworkingService.CreateCoworkingSpaceAsync(request);
            ApplyOwnershipFlag(result, userId);

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
            var userId = _currentUser.TryGetUserId();
            request.UpdatedBy = userId;

            var result = await _coworkingService.UpdateCoworkingSpaceAsync(id, request);
            ApplyOwnershipFlag(result, userId);

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
    ///     删除 Coworking 空间（仅管理员）
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<string>>> DeleteCoworkingSpace(Guid id)
    {
        try
        {
            // 检查管理员权限
            if (!_currentUser.IsAdmin())
            {
                return StatusCode(403, ApiResponse<object>.ErrorResponse(
                    "只有管理员可以删除 Coworking 空间",
                    new List<string> { "权限不足" }));
            }

            var userId = _currentUser.TryGetUserId();
            await _coworkingService.DeleteCoworkingSpaceAsync(id, userId);

            _logger.LogInformation("✅ 管理员 {UserId} 成功删除 Coworking 空间 {CoworkingId}", 
                userId, id);

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
            var userId = _currentUser.TryGetUserId();
            var result = await _coworkingService.SearchCoworkingSpacesAsync(searchTerm, page, pageSize);
            ApplyOwnershipFlag(result, userId);

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
            var userId = _currentUser.TryGetUserId();
            var result = await _coworkingService.GetTopRatedCoworkingSpacesAsync(limit);
            ApplyOwnershipFlag(result, userId);

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
    ///     检查当前用户是否可以验证指定的 Coworking 空间
    /// </summary>
    [HttpGet("{id}/verification-eligibility")]
    [ProducesResponseType(typeof(ApiResponse<VerificationEligibilityResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<VerificationEligibilityResponse>>> CheckVerificationEligibility(Guid id)
    {
        try
        {
            var userId = _currentUser.TryGetUserId();
            if (!userId.HasValue)
                return Unauthorized(ApiResponse<object>.ErrorResponse("需要登录", new List<string> { "未检测到用户信息" }));

            var result = await _coworkingService.CheckVerificationEligibilityAsync(id, userId.Value);
            return Ok(ApiResponse<VerificationEligibilityResponse>.SuccessResponse(result, "检查完成"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("未找到 Coworking", new List<string> { ex.Message }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查验证资格失败: {Id}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("检查失败", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    ///     用户提交 Coworking 认证
    /// </summary>
    [HttpPost("{id}/verifications")]
    [ProducesResponseType(typeof(ApiResponse<CoworkingSpaceResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CoworkingSpaceResponse>>> VerifyCoworking(Guid id)
    {
        try
        {
            var userId = _currentUser.TryGetUserId();
            if (!userId.HasValue)
                return Unauthorized(ApiResponse<object>.ErrorResponse("需要登录", new List<string> { "未检测到用户信息" }));

            var result = await _coworkingService.SubmitVerificationAsync(id, userId.Value);
            ApplyOwnershipFlag(result, userId);

            return Ok(ApiResponse<CoworkingSpaceResponse>.SuccessResponse(
                result,
                "认证提交成功"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("认证失败", new List<string> { ex.Message }));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("未找到 Coworking", new List<string> { ex.Message }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "提交 Coworking 认证失败: {Id}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("认证失败", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    ///     管理员 / 城市版主更新认证状态
    /// </summary>
    [HttpPut("{id}/verification-status")]
    [ProducesResponseType(typeof(ApiResponse<CoworkingSpaceResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CoworkingSpaceResponse>>> UpdateVerificationStatus(
        Guid id,
        [FromBody] UpdateCoworkingVerificationStatusRequest request)
    {
        if (!_currentUser.HasAdminOrModeratorPrivileges())
            return Forbid();

        var userId = _currentUser.TryGetUserId();
        request.UpdatedBy = userId;

        try
        {
            var result = await _coworkingService.UpdateVerificationStatusAsync(id, request);
            ApplyOwnershipFlag(result, userId);
            return Ok(ApiResponse<CoworkingSpaceResponse>.SuccessResponse(result, "认证状态已更新"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("未找到 Coworking", new List<string> { ex.Message }));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("状态无效", new List<string> { ex.Message }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新认证状态失败: {Id}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("更新认证状态失败", new List<string> { ex.Message }));
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

    #region Comment Endpoints

    /// <summary>
    ///     创建评论
    /// </summary>
    [HttpPost("{coworkingId}/comments")]
    [ProducesResponseType(typeof(ApiResponse<CoworkingCommentResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<CoworkingCommentResponse>>> CreateComment(
        Guid coworkingId,
        [FromBody] CreateCoworkingCommentRequest request)
    {
        try
        {
            var userId = _currentUser.TryGetUserId();
            if (!userId.HasValue)
                return Unauthorized(ApiResponse<object>.ErrorResponse("需要登录", new List<string> { "未检测到用户信息" }));

            var result = await _coworkingService.CreateCommentAsync(coworkingId, userId.Value, request);

            return CreatedAtAction(
                nameof(GetComments),
                new { coworkingId },
                ApiResponse<CoworkingCommentResponse>.SuccessResponse(
                    result,
                    "评论创建成功"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("未找到 Coworking", new List<string> { ex.Message }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建评论失败: CoworkingId={CoworkingId}", coworkingId);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("创建评论失败", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    ///     获取评论列表
    /// </summary>
    [HttpGet("{coworkingId}/comments")]
    [ProducesResponseType(typeof(ApiResponse<List<CoworkingCommentResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<CoworkingCommentResponse>>>> GetComments(
        Guid coworkingId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var comments = await _coworkingService.GetCommentsAsync(coworkingId, page, pageSize);
            var totalCount = await _coworkingService.GetCommentCountAsync(coworkingId);

            return Ok(ApiResponse<List<CoworkingCommentResponse>>.SuccessResponse(
                comments,
                $"成功获取 {comments.Count} 条评论，共 {totalCount} 条"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取评论列表失败: CoworkingId={CoworkingId}", coworkingId);
            return StatusCode(500, ApiResponse<List<CoworkingCommentResponse>>.ErrorResponse(
                "获取评论失败",
                new List<string> { ex.Message }));
        }
    }

    /// <summary>
    ///     删除评论
    /// </summary>
    [HttpDelete("comments/{id}")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<string>>> DeleteComment(Guid id)
    {
        try
        {
            var userId = _currentUser.TryGetUserId();
            if (!userId.HasValue)
                return Unauthorized(ApiResponse<object>.ErrorResponse("需要登录", new List<string> { "未检测到用户信息" }));

            await _coworkingService.DeleteCommentAsync(id, userId.Value);

            return Ok(ApiResponse<string>.SuccessResponse(
                "评论已删除",
                "评论删除成功"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("未找到评论", new List<string> { ex.Message }));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<object>.ErrorResponse("无权限", new List<string> { ex.Message }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除评论失败: Id={Id}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("删除评论失败", new List<string> { ex.Message }));
        }
    }

    #endregion

    #region Helper Methods

    private static void ApplyOwnershipFlag(CoworkingSpaceResponse? response, Guid? currentUserId)
    {
        if (response == null)
        {
            return;
        }

        response.IsOwner = currentUserId.HasValue && response.CreatedBy.HasValue &&
                           response.CreatedBy.Value == currentUserId.Value;
    }

    private static void ApplyOwnershipFlag(IEnumerable<CoworkingSpaceResponse>? responses, Guid? currentUserId)
    {
        if (responses == null)
        {
            return;
        }

        foreach (var response in responses)
        {
            ApplyOwnershipFlag(response, currentUserId);
        }
    }

    private static void ApplyOwnershipFlag(PaginatedCoworkingSpacesResponse? response, Guid? currentUserId)
    {
        if (response?.Items == null)
        {
            return;
        }

        ApplyOwnershipFlag(response.Items, currentUserId);
    }

    #endregion
    
    #region 城市统计 API

    /// <summary>
    ///     批量获取城市 Coworking 空间数量（供 CityService 调用）
    /// </summary>
    [HttpPost("cities/counts")]
    [ProducesResponseType(typeof(BatchCityCountResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BatchCityCountResponse>> GetCitiesCoworkingCounts([FromBody] List<string> cityIds)
    {
        try
        {
            _logger.LogInformation("📊 批量获取城市 Coworking 数量: {Count} 个城市", cityIds.Count);

            var counts = await _coworkingService.GetCitiesCoworkingCountsAsync(cityIds);

            return Ok(new BatchCityCountResponse
            {
                Counts = counts.Select(kvp => new CityCountItem
                {
                    CityId = kvp.Key,
                    Count = kvp.Value
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量获取城市 Coworking 数量失败");
            return StatusCode(500, new BatchCityCountResponse { Counts = new List<CityCountItem>() });
        }
    }

    #endregion
}

/// <summary>
/// 批量城市数量响应
/// </summary>
public class BatchCityCountResponse
{
    public List<CityCountItem> Counts { get; set; } = new();
}

/// <summary>
/// 城市数量项
/// </summary>
public class CityCountItem
{
    public string CityId { get; set; } = string.Empty;
    public int Count { get; set; }
}