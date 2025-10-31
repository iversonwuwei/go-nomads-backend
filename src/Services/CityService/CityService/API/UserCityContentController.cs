using CityService.DTOs;
using CityService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CityService.API;

/// <summary>
/// 用户城市内容 API（照片、费用、评论）
/// </summary>
[Authorize]
[ApiController]
[Route("api/cities/{cityId}/user-content")]
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

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? throw new UnauthorizedAccessException("User ID not found in claims");

        return Guid.Parse(userIdClaim);
    }

    #region 照片 API

    /// <summary>
    /// 添加城市照片
    /// </summary>
    [HttpPost("photos")]
    [ProducesResponseType(typeof(UserCityPhotoDto), 201)]
    public async Task<IActionResult> AddPhoto(string cityId, [FromBody] AddCityPhotoRequest request)
    {
        try
        {
            var userId = GetUserId();
            request.CityId = cityId; // 确保从路由参数获取
            
            var photo = await _contentService.AddPhotoAsync(userId, request);
            
            _logger.LogInformation("用户 {UserId} 为城市 {CityId} 添加了照片 {PhotoId}", userId, cityId, photo.Id);
            
            return CreatedAtAction(nameof(GetCityPhotos), new { cityId }, photo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加照片失败");
            return StatusCode(500, new { error = "添加照片失败", details = ex.Message });
        }
    }

    /// <summary>
    /// 获取城市的所有照片（可选筛选当前用户）
    /// </summary>
    [HttpGet("photos")]
    [ProducesResponseType(typeof(List<UserCityPhotoDto>), 200)]
    public async Task<IActionResult> GetCityPhotos(string cityId, [FromQuery] bool onlyMine = false)
    {
        try
        {
            var userId = onlyMine ? GetUserId() : (Guid?)null;
            var photos = await _contentService.GetCityPhotosAsync(cityId, userId);
            
            return Ok(photos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取城市照片失败");
            return StatusCode(500, new { error = "获取照片失败", details = ex.Message });
        }
    }

    /// <summary>
    /// 删除照片
    /// </summary>
    [HttpDelete("photos/{photoId}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeletePhoto(string cityId, Guid photoId)
    {
        try
        {
            var userId = GetUserId();
            var deleted = await _contentService.DeletePhotoAsync(userId, photoId);
            
            if (!deleted)
                return NotFound(new { error = "照片不存在或无权删除" });
            
            _logger.LogInformation("用户 {UserId} 删除了照片 {PhotoId}", userId, photoId);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除照片失败");
            return StatusCode(500, new { error = "删除照片失败", details = ex.Message });
        }
    }

    #endregion

    #region 费用 API

    /// <summary>
    /// 添加城市费用
    /// </summary>
    [HttpPost("expenses")]
    [ProducesResponseType(typeof(UserCityExpenseDto), 201)]
    public async Task<IActionResult> AddExpense(string cityId, [FromBody] AddCityExpenseRequest request)
    {
        try
        {
            var userId = GetUserId();
            request.CityId = cityId;
            
            var expense = await _contentService.AddExpenseAsync(userId, request);
            
            _logger.LogInformation("用户 {UserId} 为城市 {CityId} 添加了费用 {ExpenseId}", userId, cityId, expense.Id);
            
            return CreatedAtAction(nameof(GetCityExpenses), new { cityId }, expense);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加费用失败");
            return StatusCode(500, new { error = "添加费用失败", details = ex.Message });
        }
    }

    /// <summary>
    /// 获取城市的所有费用（可选筛选当前用户）
    /// </summary>
    [HttpGet("expenses")]
    [ProducesResponseType(typeof(List<UserCityExpenseDto>), 200)]
    public async Task<IActionResult> GetCityExpenses(string cityId, [FromQuery] bool onlyMine = false)
    {
        try
        {
            var userId = onlyMine ? GetUserId() : (Guid?)null;
            var expenses = await _contentService.GetCityExpensesAsync(cityId, userId);
            
            return Ok(expenses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取城市费用失败");
            return StatusCode(500, new { error = "获取费用失败", details = ex.Message });
        }
    }

    /// <summary>
    /// 删除费用
    /// </summary>
    [HttpDelete("expenses/{expenseId}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteExpense(string cityId, Guid expenseId)
    {
        try
        {
            var userId = GetUserId();
            var deleted = await _contentService.DeleteExpenseAsync(userId, expenseId);
            
            if (!deleted)
                return NotFound(new { error = "费用不存在或无权删除" });
            
            _logger.LogInformation("用户 {UserId} 删除了费用 {ExpenseId}", userId, expenseId);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除费用失败");
            return StatusCode(500, new { error = "删除费用失败", details = ex.Message });
        }
    }

    #endregion

    #region 评论 API

    /// <summary>
    /// 添加或更新城市评论
    /// </summary>
    [HttpPost("reviews")]
    [ProducesResponseType(typeof(UserCityReviewDto), 200)]
    public async Task<IActionResult> UpsertReview(string cityId, [FromBody] UpsertCityReviewRequest request)
    {
        try
        {
            var userId = GetUserId();
            request.CityId = cityId;
            
            var review = await _contentService.UpsertReviewAsync(userId, request);
            
            _logger.LogInformation("用户 {UserId} 为城市 {CityId} 添加/更新了评论", userId, cityId);
            
            return Ok(review);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加/更新评论失败");
            return StatusCode(500, new { error = "添加/更新评论失败", details = ex.Message });
        }
    }

    /// <summary>
    /// 获取城市的所有评论
    /// </summary>
    [AllowAnonymous]
    [HttpGet("reviews")]
    [ProducesResponseType(typeof(List<UserCityReviewDto>), 200)]
    public async Task<IActionResult> GetCityReviews(string cityId)
    {
        try
        {
            var reviews = await _contentService.GetCityReviewsAsync(cityId);
            return Ok(reviews);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取城市评论失败");
            return StatusCode(500, new { error = "获取评论失败", details = ex.Message });
        }
    }

    /// <summary>
    /// 获取当前用户对该城市的评论
    /// </summary>
    [HttpGet("reviews/mine")]
    [ProducesResponseType(typeof(UserCityReviewDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetMyReview(string cityId)
    {
        try
        {
            var userId = GetUserId();
            var review = await _contentService.GetUserReviewAsync(userId, cityId);
            
            if (review == null)
                return NotFound(new { error = "未找到评论" });
            
            return Ok(review);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户评论失败");
            return StatusCode(500, new { error = "获取评论失败", details = ex.Message });
        }
    }

    /// <summary>
    /// 删除评论
    /// </summary>
    [HttpDelete("reviews")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteReview(string cityId)
    {
        try
        {
            var userId = GetUserId();
            var deleted = await _contentService.DeleteReviewAsync(userId, cityId);
            
            if (!deleted)
                return NotFound(new { error = "评论不存在或无权删除" });
            
            _logger.LogInformation("用户 {UserId} 删除了城市 {CityId} 的评论", userId, cityId);
            
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除评论失败");
            return StatusCode(500, new { error = "删除评论失败", details = ex.Message });
        }
    }

    #endregion

    #region 统计 API

    /// <summary>
    /// 获取城市用户内容统计
    /// </summary>
    [AllowAnonymous]
    [HttpGet("stats")]
    [ProducesResponseType(typeof(CityUserContentStatsDto), 200)]
    public async Task<IActionResult> GetCityStats(string cityId)
    {
        try
        {
            var stats = await _contentService.GetCityStatsAsync(cityId);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取城市统计失败");
            return StatusCode(500, new { error = "获取统计失败", details = ex.Message });
        }
    }

    #endregion
}

/// <summary>
/// 用户内容管理 API（查看用户自己的所有内容）
/// </summary>
[Authorize]
[ApiController]
[Route("api/user/city-content")]
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

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? throw new UnauthorizedAccessException("User ID not found in claims");

        return Guid.Parse(userIdClaim);
    }

    /// <summary>
    /// 获取我的所有照片
    /// </summary>
    [HttpGet("photos")]
    [ProducesResponseType(typeof(List<UserCityPhotoDto>), 200)]
    public async Task<IActionResult> GetMyPhotos()
    {
        try
        {
            var userId = GetUserId();
            var photos = await _contentService.GetUserPhotosAsync(userId);
            return Ok(photos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户照片失败");
            return StatusCode(500, new { error = "获取照片失败", details = ex.Message });
        }
    }

    /// <summary>
    /// 获取我的所有费用
    /// </summary>
    [HttpGet("expenses")]
    [ProducesResponseType(typeof(List<UserCityExpenseDto>), 200)]
    public async Task<IActionResult> GetMyExpenses()
    {
        try
        {
            var userId = GetUserId();
            var expenses = await _contentService.GetUserExpensesAsync(userId);
            return Ok(expenses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户费用失败");
            return StatusCode(500, new { error = "获取费用失败", details = ex.Message });
        }
    }
}
