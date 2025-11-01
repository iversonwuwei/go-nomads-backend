using CityService.Application.DTOs;
using CityService.Application.Services;
using GoNomads.Shared.Middleware;
using GoNomads.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace CityService.API.Controllers;

/// <summary>
/// 我的城市内容 API（用户自己的所有内容）
/// </summary>
[ApiController]
[Route("api/v1/user/city-content")]
public class MyContentController : ControllerBase
{
    private readonly IUserCityContentService _contentService;
    private readonly ILogger<MyContentController> _logger;

    public MyContentController(
        IUserCityContentService contentService,
        ILogger<MyContentController> logger)
    {
        _contentService = contentService;
        _logger = logger;
    }

    /// <summary>
    /// 从 UserContext 中获取用户 ID
    /// </summary>
    private Guid GetUserId()
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
        {
            throw new UnauthorizedAccessException("用户未认证");
        }

        return Guid.Parse(userContext.UserId);
    }

    /// <summary>
    /// 获取我的所有照片
    /// GET /api/v1/user/city-content/photos
    /// </summary>
    [HttpGet("photos")]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserCityPhotoDto>>>> GetMyPhotos()
    {
        try
        {
            var userId = GetUserId();
            var photos = await _contentService.GetUserPhotosAsync(userId);

            return Ok(new ApiResponse<IEnumerable<UserCityPhotoDto>>
            {
                Success = true,
                Message = "获取照片成功",
                Data = photos
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "未授权访问照片");
            return Unauthorized(new ApiResponse<IEnumerable<UserCityPhotoDto>>
            {
                Success = false,
                Message = "未授权",
                Errors = new List<string> { ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户照片失败");
            return StatusCode(500, new ApiResponse<IEnumerable<UserCityPhotoDto>>
            {
                Success = false,
                Message = "获取照片失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// 获取我的所有费用
    /// GET /api/v1/user/city-content/expenses
    /// </summary>
    [HttpGet("expenses")]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserCityExpenseDto>>>> GetMyExpenses()
    {
        try
        {
            var userId = GetUserId();
            var expenses = await _contentService.GetUserExpensesAsync(userId);

            return Ok(new ApiResponse<IEnumerable<UserCityExpenseDto>>
            {
                Success = true,
                Message = "获取费用成功",
                Data = expenses
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "未授权访问费用");
            return Unauthorized(new ApiResponse<IEnumerable<UserCityExpenseDto>>
            {
                Success = false,
                Message = "未授权",
                Errors = new List<string> { ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户费用失败");
            return StatusCode(500, new ApiResponse<IEnumerable<UserCityExpenseDto>>
            {
                Success = false,
                Message = "获取费用失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// 获取我对某个城市的评论
    /// GET /api/v1/user/city-content/reviews/{cityId}
    /// </summary>
    [HttpGet("reviews/{cityId}")]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserCityReviewDto>>>> GetMyReviews(string cityId)
    {
        try
        {
            var userId = GetUserId();
            var reviews = await _contentService.GetUserReviewsAsync(userId, cityId);

            return Ok(new ApiResponse<IEnumerable<UserCityReviewDto>>
            {
                Success = true,
                Message = "获取评论成功",
                Data = reviews
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "未授权访问评论");
            return Unauthorized(new ApiResponse<IEnumerable<UserCityReviewDto>>
            {
                Success = false,
                Message = "未授权",
                Errors = new List<string> { ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户评论失败: {CityId}", cityId);
            return StatusCode(500, new ApiResponse<IEnumerable<UserCityReviewDto>>
            {
                Success = false,
                Message = "获取评论失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }
}
