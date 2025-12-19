using CoworkingService.Application.DTOs.Review;
using CoworkingService.Application.Services;
using GoNomads.Shared.DTOs;
using GoNomads.Shared.Middleware;
using GoNomads.Shared.Services;
using Microsoft.AspNetCore.Mvc;
using SharedModels = GoNomads.Shared.Models;

namespace CoworkingService.API.Controllers;

/// <summary>
/// Coworking 评论 API 控制器
/// </summary>
[ApiController]
[Route("api/v1/coworking")]
public class CoworkingReviewController : ControllerBase
{
    private readonly ICoworkingReviewService _reviewService;
    private readonly ILogger<CoworkingReviewController> _logger;
    private readonly ICurrentUserService _currentUser;

    public CoworkingReviewController(
        ICoworkingReviewService reviewService,
        ILogger<CoworkingReviewController> logger,
        ICurrentUserService currentUser)
    {
        _reviewService = reviewService;
        _logger = logger;
        _currentUser = currentUser;
    }

    /// <summary>
    /// 获取 Coworking 评论列表（分页）
    /// </summary>
    [HttpGet("{coworkingId}/reviews")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedReviewsResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedReviewsResponse>>> GetReviews(
        Guid coworkingId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            _logger.LogInformation("获取 Coworking {CoworkingId} 评论列表, Page={Page}, PageSize={PageSize}", 
                coworkingId, page, pageSize);

            var result = await _reviewService.GetReviewsByCoworkingIdAsync(coworkingId, page, pageSize);

            return Ok(ApiResponse<PaginatedReviewsResponse>.SuccessResponse(
                result,
                $"成功获取 {result.Items.Count} 条评论"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取评论列表失败");
            return StatusCode(500, ApiResponse<PaginatedReviewsResponse>.ErrorResponse(
                "获取评论列表失败",
                new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// 获取评论详情
    /// </summary>
    [HttpGet("reviews/{reviewId}")]
    [ProducesResponseType(typeof(ApiResponse<CoworkingReviewResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CoworkingReviewResponse>>> GetReviewById(Guid reviewId)
    {
        try
        {
            var review = await _reviewService.GetReviewByIdAsync(reviewId);
            
            if (review == null)
            {
                return NotFound(ApiResponse<CoworkingReviewResponse>.ErrorResponse(
                    "评论不存在",
                    new List<string> { $"未找到 ID 为 {reviewId} 的评论" }));
            }

            return Ok(ApiResponse<CoworkingReviewResponse>.SuccessResponse(review));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取评论详情失败");
            return StatusCode(500, ApiResponse<CoworkingReviewResponse>.ErrorResponse(
                "获取评论详情失败",
                new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// 获取当前用户对某 Coworking 的评论
    /// </summary>
    [HttpGet("{coworkingId}/reviews/my-review")]
    [ProducesResponseType(typeof(ApiResponse<CoworkingReviewResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CoworkingReviewResponse>>> GetMyReview(Guid coworkingId)
    {
        try
        {
            var userId = _currentUser.GetUserId();

            var review = await _reviewService.GetUserReviewForCoworkingAsync(coworkingId, userId);

            if (review == null)
            {
                return Ok(ApiResponse<CoworkingReviewResponse?>.SuccessResponse(
                    null,
                    "您还未评论该 Coworking 空间"));
            }

            return Ok(ApiResponse<CoworkingReviewResponse>.SuccessResponse(review));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户评论失败");
            return StatusCode(500, ApiResponse<CoworkingReviewResponse>.ErrorResponse(
                "获取用户评论失败",
                new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// 添加评论
    /// </summary>
    [HttpPost("{coworkingId}/reviews")]
    [ProducesResponseType(typeof(ApiResponse<CoworkingReviewResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<CoworkingReviewResponse>>> AddReview(
        Guid coworkingId,
        [FromBody] AddCoworkingReviewRequest request)
    {
        try
        {
            var userId = _currentUser.GetUserId();

            _logger.LogInformation("用户 {UserId} 添加 Coworking {CoworkingId} 评论", userId, coworkingId);
            _logger.LogInformation("📥 收到评论请求: Rating={Rating}, Title={Title}, ContentLength={ContentLength}, PhotoUrlsCount={PhotoUrlsCount}", 
                request.Rating, 
                request.Title, 
                request.Content?.Length ?? 0,
                request.PhotoUrls?.Count ?? 0);
            
            if (request.PhotoUrls != null && request.PhotoUrls.Count > 0)
            {
                _logger.LogInformation("📸 图片 URLs: {@PhotoUrls}", request.PhotoUrls);
            }

            var result = await _reviewService.AddReviewAsync(
                coworkingId, 
                userId, 
                request);

            return CreatedAtAction(
                nameof(GetReviewById),
                new { reviewId = result.Id },
                ApiResponse<CoworkingReviewResponse>.SuccessResponse(result, "评论添加成功"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<CoworkingReviewResponse>.ErrorResponse(
                "输入验证失败",
                new List<string> { ex.Message }));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<CoworkingReviewResponse>.ErrorResponse(
                "操作失败",
                new List<string> { ex.Message }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加评论失败");
            return StatusCode(500, ApiResponse<CoworkingReviewResponse>.ErrorResponse(
                "添加评论失败",
                new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// 更新评论
    /// </summary>
    [HttpPut("reviews/{reviewId}")]
    [ProducesResponseType(typeof(ApiResponse<CoworkingReviewResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CoworkingReviewResponse>>> UpdateReview(
        Guid reviewId,
        [FromBody] UpdateCoworkingReviewRequest request)
    {
        try
        {
            var userId = _currentUser.GetUserId();

            _logger.LogInformation("用户 {UserId} 更新评论 {ReviewId}", userId, reviewId);

            var result = await _reviewService.UpdateReviewAsync(reviewId, userId, request);

            return Ok(ApiResponse<CoworkingReviewResponse>.SuccessResponse(result, "评论更新成功"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<CoworkingReviewResponse>.ErrorResponse(
                "评论不存在",
                new List<string> { ex.Message }));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<CoworkingReviewResponse>.ErrorResponse(
                "权限不足",
                new List<string> { ex.Message }));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<CoworkingReviewResponse>.ErrorResponse(
                "输入验证失败",
                new List<string> { ex.Message }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新评论失败");
            return StatusCode(500, ApiResponse<CoworkingReviewResponse>.ErrorResponse(
                "更新评论失败",
                new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// 删除评论
    /// </summary>
    [HttpDelete("reviews/{reviewId}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteReview(Guid reviewId)
    {
        try
        {
            var userId = _currentUser.GetUserId();

            _logger.LogInformation("用户 {UserId} 删除评论 {ReviewId}", userId, reviewId);

            await _reviewService.DeleteReviewAsync(reviewId, userId);

            return Ok(ApiResponse<string>.SuccessResponse("删除成功", "评论删除成功"));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<object>.ErrorResponse(
                "评论不存在",
                new List<string> { ex.Message }));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, ApiResponse<object>.ErrorResponse(
                "权限不足",
                new List<string> { ex.Message }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除评论失败");
            return StatusCode(500, ApiResponse<object>.ErrorResponse(
                "删除评论失败",
                new List<string> { ex.Message }));
        }
    }
}
