using CityService.Application.DTOs;
using CityService.Application.Services;
using GoNomads.Shared.Middleware;
using GoNomads.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CityService.API.Controllers;

/// <summary>
/// 用户城市内容 API（照片、费用、评论）
/// </summary>
[ApiController]
[Route("api/v1/cities/{cityId}/user-content")]
public class UserCityContentController : ControllerBase
{
    private readonly IUserCityContentService _contentService;
    private readonly ILogger<UserCityContentController> _logger;

    public UserCityContentController(
        IUserCityContentService contentService,
        ILogger<UserCityContentController> logger)
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

    #region 照片 API

    /// <summary>
    /// 添加城市照片
    /// POST /api/v1/cities/{cityId}/user-content/photos
    /// </summary>
    [HttpPost("photos")]
    public async Task<ActionResult<ApiResponse<UserCityPhotoDto>>> AddPhoto(string cityId, [FromBody] AddCityPhotoRequest request)
    {
        try
        {
            var userId = GetUserId();
            request.CityId = cityId;

            var photo = await _contentService.AddPhotoAsync(userId, request);

            return CreatedAtAction(
                nameof(GetCityPhotos),
                new { cityId },
                new ApiResponse<UserCityPhotoDto>
                {
                    Success = true,
                    Message = "照片添加成功",
                    Data = photo
                });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "未授权添加照片");
            return Unauthorized(new ApiResponse<UserCityPhotoDto>
            {
                Success = false,
                Message = "未授权",
                Errors = new List<string> { ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加照片失败: {CityId}", cityId);
            return StatusCode(500, new ApiResponse<UserCityPhotoDto>
            {
                Success = false,
                Message = "添加照片失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// 批量添加城市照片
    /// POST /api/v1/cities/{cityId}/user-content/photos/batch
    /// </summary>
    [HttpPost("photos/batch")]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserCityPhotoDto>>>> SubmitPhotoBatch(string cityId, [FromBody] SubmitCityPhotoBatchRequest request)
    {
        try
        {
            var userId = GetUserId();
            request.CityId = cityId;

            var photos = await _contentService.SubmitPhotoCollectionAsync(userId, request);

            return Ok(new ApiResponse<IEnumerable<UserCityPhotoDto>>
            {
                Success = true,
                Message = "照片批量上传成功",
                Data = photos
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<IEnumerable<UserCityPhotoDto>>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "未授权批量上传照片");
            return Unauthorized(new ApiResponse<IEnumerable<UserCityPhotoDto>>
            {
                Success = false,
                Message = "未授权",
                Errors = new List<string> { ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量上传城市照片失败: {CityId}", cityId);
            return StatusCode(500, new ApiResponse<IEnumerable<UserCityPhotoDto>>
            {
                Success = false,
                Message = "批量上传照片失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// 获取城市的所有照片
    /// GET /api/v1/cities/{cityId}/user-content/photos?onlyMine=false
    /// </summary>
    [HttpGet("photos")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserCityPhotoDto>>>> GetCityPhotos(
        string cityId,
        [FromQuery] bool onlyMine = false)
    {
        try
        {
            Guid? userId = null;
            if (onlyMine)
            {
                userId = GetUserId();
            }

            var photos = await _contentService.GetCityPhotosAsync(cityId, userId);

            return Ok(new ApiResponse<IEnumerable<UserCityPhotoDto>>
            {
                Success = true,
                Message = "获取照片成功",
                Data = photos
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取城市照片失败: {CityId}", cityId);
            return StatusCode(500, new ApiResponse<IEnumerable<UserCityPhotoDto>>
            {
                Success = false,
                Message = "获取照片失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// 删除照片
    /// DELETE /api/v1/cities/{cityId}/user-content/photos/{photoId}
    /// </summary>
    [HttpDelete("photos/{photoId}")]
    public async Task<ActionResult<ApiResponse<object>>> DeletePhoto(string cityId, Guid photoId)
    {
        try
        {
            var userId = GetUserId();
            var deleted = await _contentService.DeletePhotoAsync(userId, photoId);

            if (!deleted)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "照片不存在或无权删除"
                });
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "照片删除成功"
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "未授权删除照片");
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "未授权",
                Errors = new List<string> { ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除照片失败: {PhotoId}", photoId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "删除照片失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    #endregion

    #region 费用 API

    /// <summary>
    /// 添加城市费用
    /// POST /api/v1/cities/{cityId}/user-content/expenses
    /// </summary>
    [HttpPost("expenses")]
    public async Task<ActionResult<ApiResponse<UserCityExpenseDto>>> AddExpense(string cityId, [FromBody] AddCityExpenseRequest request)
    {
        try
        {
            var userId = GetUserId();
            request.CityId = cityId;

            var expense = await _contentService.AddExpenseAsync(userId, request);

            return CreatedAtAction(
                nameof(GetCityExpenses),
                new { cityId },
                new ApiResponse<UserCityExpenseDto>
                {
                    Success = true,
                    Message = "费用添加成功",
                    Data = expense
                });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "未授权添加费用");
            return Unauthorized(new ApiResponse<UserCityExpenseDto>
            {
                Success = false,
                Message = "未授权",
                Errors = new List<string> { ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加费用失败: {CityId}", cityId);
            return StatusCode(500, new ApiResponse<UserCityExpenseDto>
            {
                Success = false,
                Message = "添加费用失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// 获取城市的所有费用
    /// GET /api/v1/cities/{cityId}/user-content/expenses?onlyMine=false
    /// </summary>
    [HttpGet("expenses")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserCityExpenseDto>>>> GetCityExpenses(
        string cityId,
        [FromQuery] bool onlyMine = false)
    {
        try
        {
            Guid? userId = null;
            if (onlyMine)
            {
                userId = GetUserId();
            }

            var expenses = await _contentService.GetCityExpensesAsync(cityId, userId);

            return Ok(new ApiResponse<IEnumerable<UserCityExpenseDto>>
            {
                Success = true,
                Message = "获取费用成功",
                Data = expenses
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取城市费用失败: {CityId}", cityId);
            return StatusCode(500, new ApiResponse<IEnumerable<UserCityExpenseDto>>
            {
                Success = false,
                Message = "获取费用失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// 删除费用
    /// DELETE /api/v1/cities/{cityId}/user-content/expenses/{expenseId}
    /// </summary>
    [HttpDelete("expenses/{expenseId}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteExpense(string cityId, Guid expenseId)
    {
        try
        {
            var userId = GetUserId();
            var deleted = await _contentService.DeleteExpenseAsync(userId, expenseId);

            if (!deleted)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "费用不存在或无权删除"
                });
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "费用删除成功"
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "未授权删除费用");
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "未授权",
                Errors = new List<string> { ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除费用失败: {ExpenseId}", expenseId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "删除费用失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    #endregion

    #region 评论 API

    /// <summary>
    /// 创建评论 (每次都新增一条记录)
    /// POST /api/v1/cities/{cityId}/user-content/reviews
    /// </summary>
    [HttpPost("reviews")]
    public async Task<ActionResult<ApiResponse<UserCityReviewDto>>> CreateReview(string cityId, [FromBody] UpsertCityReviewRequest request)
    {
        try
        {
            var userId = GetUserId();
            request.CityId = cityId;

            var review = await _contentService.CreateReviewAsync(userId, request);

            return Ok(new ApiResponse<UserCityReviewDto>
            {
                Success = true,
                Message = "评论提交成功",
                Data = review
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "未授权提交评论");
            return Unauthorized(new ApiResponse<UserCityReviewDto>
            {
                Success = false,
                Message = "未授权",
                Errors = new List<string> { ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "提交评论失败: {CityId}", cityId);
            return StatusCode(500, new ApiResponse<UserCityReviewDto>
            {
                Success = false,
                Message = "提交评论失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// 获取城市的所有评论
    /// GET /api/v1/cities/{cityId}/user-content/reviews
    /// </summary>
    [HttpGet("reviews")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserCityReviewDto>>>> GetCityReviews(string cityId)
    {
        try
        {
            var reviews = await _contentService.GetCityReviewsAsync(cityId);

            return Ok(new ApiResponse<IEnumerable<UserCityReviewDto>>
            {
                Success = true,
                Message = "获取评论成功",
                Data = reviews
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取城市评论失败: {CityId}", cityId);
            return StatusCode(500, new ApiResponse<IEnumerable<UserCityReviewDto>>
            {
                Success = false,
                Message = "获取评论失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// 删除评论
    /// DELETE /api/v1/cities/{cityId}/user-content/reviews/{reviewId}
    /// </summary>
    [HttpDelete("reviews/{reviewId}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteReview(string cityId, [FromRoute] Guid reviewId)
    {
        try
        {
            var userId = GetUserId();
            var deleted = await _contentService.DeleteReviewAsync(userId, reviewId);

            if (!deleted)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "评论不存在"
                });
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "评论删除成功"
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "未授权删除评论");
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "未授权",
                Errors = new List<string> { ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除评论失败: {CityId}", cityId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "删除评论失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    #endregion

    #region 统计 API

    /// <summary>
    /// 获取城市内容统计
    /// GET /api/v1/cities/{cityId}/user-content/stats
    /// </summary>
    [HttpGet("stats")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<CityUserContentStatsDto>>> GetCityStats(string cityId)
    {
        try
        {
            var stats = await _contentService.GetCityStatsAsync(cityId);

            return Ok(new ApiResponse<CityUserContentStatsDto>
            {
                Success = true,
                Message = "获取统计成功",
                Data = stats
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取城市统计失败: {CityId}", cityId);
            return StatusCode(500, new ApiResponse<CityUserContentStatsDto>
            {
                Success = false,
                Message = "获取统计失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// 获取城市综合费用统计 - 基于用户提交的实际费用数据
    /// GET /api/v1/cities/{cityId}/user-content/cost-summary
    /// </summary>
    [HttpGet("cost-summary")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<CityCostSummaryDto>>> GetCityCostSummary(string cityId)
    {
        try
        {
            var costSummary = await _contentService.GetCityCostSummaryAsync(cityId);

            return Ok(new ApiResponse<CityCostSummaryDto>
            {
                Success = true,
                Message = "获取费用统计成功",
                Data = costSummary
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取城市费用统计失败: {CityId}", cityId);
            return StatusCode(500, new ApiResponse<CityCostSummaryDto>
            {
                Success = false,
                Message = "获取费用统计失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    #endregion

    #region Pros & Cons API

    /// <summary>
    /// 添加 Pros & Cons
    /// POST /api/v1/cities/{cityId}/user-content/pros-cons
    /// </summary>
    [HttpPost("pros-cons")]
    public async Task<ActionResult<ApiResponse<CityProsConsDto>>> AddProsCons(
        string cityId, 
        [FromBody] AddCityProsConsRequest request)
    {
        try
        {
            var userId = GetUserId();
            request.CityId = cityId;

            var prosCons = await _contentService.AddProsConsAsync(userId, request);

            return CreatedAtAction(
                nameof(GetCityProsCons),
                new { cityId },
                new ApiResponse<CityProsConsDto>
                {
                    Success = true,
                    Message = request.IsPro ? "优点添加成功" : "挑战添加成功",
                    Data = prosCons
                });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "未授权添加 Pros & Cons");
            return Unauthorized(new ApiResponse<CityProsConsDto>
            {
                Success = false,
                Message = "未授权",
                Errors = new List<string> { ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加 Pros & Cons 失败: {CityId}", cityId);
            return StatusCode(500, new ApiResponse<CityProsConsDto>
            {
                Success = false,
                Message = "添加失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// 获取城市的 Pros & Cons
    /// GET /api/v1/cities/{cityId}/user-content/pros-cons?isPro=true
    /// </summary>
    [HttpGet("pros-cons")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<CityProsConsDto>>>> GetCityProsCons(
        string cityId,
        [FromQuery] bool? isPro = null)
    {
        try
        {
            var prosConsList = await _contentService.GetCityProsConsAsync(cityId, isPro);

            return Ok(new ApiResponse<List<CityProsConsDto>>
            {
                Success = true,
                Message = "获取成功",
                Data = prosConsList
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取 Pros & Cons 失败: {CityId}", cityId);
            return StatusCode(500, new ApiResponse<List<CityProsConsDto>>
            {
                Success = false,
                Message = "获取失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// 更新 Pros & Cons
    /// PUT /api/v1/cities/{cityId}/user-content/pros-cons/{id}
    /// </summary>
    [HttpPut("pros-cons/{id}")]
    public async Task<ActionResult<ApiResponse<CityProsConsDto>>> UpdateProsCons(
        string cityId,
        Guid id,
        [FromBody] UpdateCityProsConsRequest request)
    {
        try
        {
            var userId = GetUserId();
            var updated = await _contentService.UpdateProsConsAsync(userId, id, request);

            return Ok(new ApiResponse<CityProsConsDto>
            {
                Success = true,
                Message = "更新成功",
                Data = updated
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "未授权更新 Pros & Cons: {Id}", id);
            return Unauthorized(new ApiResponse<CityProsConsDto>
            {
                Success = false,
                Message = "未授权",
                Errors = new List<string> { ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新 Pros & Cons 失败: {Id}", id);
            return StatusCode(500, new ApiResponse<CityProsConsDto>
            {
                Success = false,
                Message = "更新失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    /// 删除 Pros & Cons
    /// DELETE /api/v1/cities/{cityId}/user-content/pros-cons/{id}
    /// </summary>
    [HttpDelete("pros-cons/{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteProsCons(string cityId, Guid id)
    {
        try
        {
            var userId = GetUserId();
            var success = await _contentService.DeleteProsConsAsync(userId, id);

            if (!success)
            {
                return NotFound(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "记录不存在或您没有权限删除",
                    Data = false
                });
            }

            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Message = "删除成功",
                Data = true
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "未授权删除 Pros & Cons: {Id}", id);
            return Unauthorized(new ApiResponse<bool>
            {
                Success = false,
                Message = "未授权",
                Errors = new List<string> { ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除 Pros & Cons 失败: {Id}", id);
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "删除失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    #endregion
}
