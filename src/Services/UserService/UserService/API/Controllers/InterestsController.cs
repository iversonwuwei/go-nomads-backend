using Microsoft.AspNetCore.Mvc;
using GoNomads.Shared.Models;
using GoNomads.Shared.Middleware;
using UserService.Application.DTOs;
using UserService.Application.Services;

namespace UserService.API.Controllers;

/// <summary>
/// 兴趣爱好 API - RESTful endpoints for interests management
/// </summary>
[ApiController]
[Route("api/v1/interests")]
public class InterestsController : ControllerBase
{
    private readonly IInterestService _interestService;
    private readonly ILogger<InterestsController> _logger;

    public InterestsController(IInterestService interestService, ILogger<InterestsController> logger)
    {
        _interestService = interestService;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有兴趣
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<InterestDto>>>> GetAllInterests(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📋 获取所有兴趣");

        try
        {
            var interests = await _interestService.GetAllInterestsAsync(cancellationToken);

            return Ok(new ApiResponse<List<InterestDto>>
            {
                Success = true,
                Message = "Interests retrieved successfully",
                Data = interests
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取兴趣列表失败");
            return StatusCode(500, new ApiResponse<List<InterestDto>>
            {
                Success = false,
                Message = "Failed to retrieve interests"
            });
        }
    }

    /// <summary>
    /// 获取按类别分组的兴趣
    /// </summary>
    [HttpGet("by-category")]
    public async Task<ActionResult<ApiResponse<List<InterestsByCategoryDto>>>> GetInterestsByCategory(
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📋 获取分类兴趣");

        try
        {
            var interests = await _interestService.GetInterestsByCategoryAsync(cancellationToken);

            return Ok(new ApiResponse<List<InterestsByCategoryDto>>
            {
                Success = true,
                Message = "Interests by category retrieved successfully",
                Data = interests
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取分类兴趣失败");
            return StatusCode(500, new ApiResponse<List<InterestsByCategoryDto>>
            {
                Success = false,
                Message = "Failed to retrieve interests by category"
            });
        }
    }

    /// <summary>
    /// 根据类别获取兴趣
    /// </summary>
    [HttpGet("category/{category}")]
    public async Task<ActionResult<ApiResponse<List<InterestDto>>>> GetInterestsBySpecificCategory(
        string category,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📋 获取类别兴趣: {Category}", category);

        try
        {
            var interests = await _interestService.GetInterestsBySpecificCategoryAsync(category, cancellationToken);

            return Ok(new ApiResponse<List<InterestDto>>
            {
                Success = true,
                Message = $"Interests in category '{category}' retrieved successfully",
                Data = interests
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取类别兴趣失败: {Category}", category);
            return StatusCode(500, new ApiResponse<List<InterestDto>>
            {
                Success = false,
                Message = "Failed to retrieve interests for category"
            });
        }
    }

    /// <summary>
    /// 根据ID获取兴趣
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<InterestDto>>> GetInterest(
        string id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 获取兴趣: {InterestId}", id);

        try
        {
            var interest = await _interestService.GetInterestByIdAsync(id, cancellationToken);

            if (interest == null)
            {
                return NotFound(new ApiResponse<InterestDto>
                {
                    Success = false,
                    Message = "Interest not found"
                });
            }

            return Ok(new ApiResponse<InterestDto>
            {
                Success = true,
                Message = "Interest retrieved successfully",
                Data = interest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取兴趣失败: {InterestId}", id);
            return StatusCode(500, new ApiResponse<InterestDto>
            {
                Success = false,
                Message = "Failed to retrieve interest"
            });
        }
    }

    /// <summary>
    /// 获取用户的所有兴趣
    /// </summary>
    [HttpGet("users/{userId}")]
    public async Task<ActionResult<ApiResponse<List<UserInterestDto>>>> GetUserInterests(
        string userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📋 获取用户兴趣: {UserId}", userId);

        try
        {
            var interests = await _interestService.GetUserInterestsAsync(userId, cancellationToken);

            return Ok(new ApiResponse<List<UserInterestDto>>
            {
                Success = true,
                Message = "User interests retrieved successfully",
                Data = interests
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取用户兴趣失败: {UserId}", userId);
            return StatusCode(500, new ApiResponse<List<UserInterestDto>>
            {
                Success = false,
                Message = "Failed to retrieve user interests"
            });
        }
    }

    /// <summary>
    /// 获取当前用户的所有兴趣（使用 UserContext）
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<List<UserInterestDto>>>> GetCurrentUserInterests(
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
        {
            return Unauthorized(new ApiResponse<List<UserInterestDto>>
            {
                Success = false,
                Message = "未认证用户"
            });
        }

        _logger.LogInformation("📋 获取当前用户兴趣: {UserId}", userContext.UserId);

        try
        {
            var interests = await _interestService.GetUserInterestsAsync(userContext.UserId!, cancellationToken);

            return Ok(new ApiResponse<List<UserInterestDto>>
            {
                Success = true,
                Message = "User interests retrieved successfully",
                Data = interests
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取当前用户兴趣失败: {UserId}", userContext.UserId);
            return StatusCode(500, new ApiResponse<List<UserInterestDto>>
            {
                Success = false,
                Message = "Failed to retrieve user interests"
            });
        }
    }

    /// <summary>
    /// 添加用户兴趣
    /// </summary>
    [HttpPost("users/{userId}")]
    public async Task<ActionResult<ApiResponse<UserInterestDto>>> AddUserInterest(
        string userId,
        [FromBody] AddUserInterestRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("➕ 添加用户兴趣: UserId={UserId}, InterestId={InterestId}", userId, request.InterestId);

        try
        {
            var interest = await _interestService.AddUserInterestAsync(
                userId,
                request.InterestId,
                request.IntensityLevel,
                cancellationToken);

            return Ok(new ApiResponse<UserInterestDto>
            {
                Success = true,
                Message = "User interest added successfully",
                Data = interest
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<UserInterestDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 添加用户兴趣失败: UserId={UserId}, InterestId={InterestId}", userId, request.InterestId);
            return StatusCode(500, new ApiResponse<UserInterestDto>
            {
                Success = false,
                Message = "Failed to add user interest"
            });
        }
    }

    /// <summary>
    /// 添加当前用户兴趣（使用 UserContext）
    /// </summary>
    [HttpPost("me")]
    public async Task<ActionResult<ApiResponse<UserInterestDto>>> AddCurrentUserInterest(
        [FromBody] AddUserInterestRequest request,
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
        {
            return Unauthorized(new ApiResponse<UserInterestDto>
            {
                Success = false,
                Message = "未认证用户"
            });
        }

        _logger.LogInformation("➕ 添加当前用户兴趣: UserId={UserId}, InterestId={InterestId}", userContext.UserId, request.InterestId);

        try
        {
            var interest = await _interestService.AddUserInterestAsync(
                userContext.UserId!,
                request.InterestId,
                request.IntensityLevel,
                cancellationToken);

            return Ok(new ApiResponse<UserInterestDto>
            {
                Success = true,
                Message = "User interest added successfully",
                Data = interest
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<UserInterestDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 添加当前用户兴趣失败: UserId={UserId}, InterestId={InterestId}", userContext.UserId, request.InterestId);
            return StatusCode(500, new ApiResponse<UserInterestDto>
            {
                Success = false,
                Message = "Failed to add user interest"
            });
        }
    }

    /// <summary>
    /// 批量添加当前用户兴趣（使用 UserContext）
    /// </summary>
    [HttpPost("me/batch")]
    public async Task<ActionResult<ApiResponse<List<UserInterestDto>>>> AddCurrentUserInterestsBatch(
        [FromBody] List<AddUserInterestRequest> request,
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
        {
            return Unauthorized(new ApiResponse<List<UserInterestDto>>
            {
                Success = false,
                Message = "未认证用户"
            });
        }

        _logger.LogInformation("➕ 批量添加当前用户兴趣: UserId={UserId}, Count={Count}", userContext.UserId, request.Count);

        try
        {
            var interests = await _interestService.AddUserInterestsBatchAsync(userContext.UserId!, request, cancellationToken);

            return Ok(new ApiResponse<List<UserInterestDto>>
            {
                Success = true,
                Message = $"Successfully added {interests.Count} interests",
                Data = interests
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 批量添加当前用户兴趣失败: UserId={UserId}", userContext.UserId);
            return StatusCode(500, new ApiResponse<List<UserInterestDto>>
            {
                Success = false,
                Message = "Failed to add user interests"
            });
        }
    }

    /// <summary>
    /// 批量添加用户兴趣
    /// </summary>
    [HttpPost("users/{userId}/batch")]
    public async Task<ActionResult<ApiResponse<List<UserInterestDto>>>> AddUserInterestsBatch(
        string userId,
        [FromBody] List<AddUserInterestRequest> request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("➕ 批量添加用户兴趣: UserId={UserId}, Count={Count}", userId, request.Count);

        try
        {
            var interests = await _interestService.AddUserInterestsBatchAsync(userId, request, cancellationToken);

            return Ok(new ApiResponse<List<UserInterestDto>>
            {
                Success = true,
                Message = $"Successfully added {interests.Count} interests",
                Data = interests
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 批量添加用户兴趣失败: UserId={UserId}", userId);
            return StatusCode(500, new ApiResponse<List<UserInterestDto>>
            {
                Success = false,
                Message = "Failed to add user interests"
            });
        }
    }

    /// <summary>
    /// 删除当前用户兴趣（使用 UserContext）
    /// </summary>
    [HttpDelete("me/{interestId}")]
    public async Task<ActionResult<ApiResponse<object>>> RemoveCurrentUserInterest(
        string interestId,
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
        {
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "未认证用户"
            });
        }

        _logger.LogInformation("➖ 删除当前用户兴趣: UserId={UserId}, InterestId={InterestId}", userContext.UserId, interestId);

        try
        {
            var result = await _interestService.RemoveUserInterestAsync(userContext.UserId!, interestId, cancellationToken);

            if (!result)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "User interest not found"
                });
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "User interest removed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除当前用户兴趣失败: UserId={UserId}, InterestId={InterestId}", userContext.UserId, interestId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Failed to remove user interest"
            });
        }
    }

    /// <summary>
    /// 删除用户兴趣
    /// </summary>
    [HttpDelete("users/{userId}/{interestId}")]
    public async Task<ActionResult<ApiResponse<object>>> RemoveUserInterest(
        string userId,
        string interestId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("➖ 删除用户兴趣: UserId={UserId}, InterestId={InterestId}", userId, interestId);

        try
        {
            var result = await _interestService.RemoveUserInterestAsync(userId, interestId, cancellationToken);

            if (!result)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "User interest not found"
                });
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "User interest removed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除用户兴趣失败: UserId={UserId}, InterestId={InterestId}", userId, interestId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Failed to remove user interest"
            });
        }
    }

    /// <summary>
    /// 更新当前用户兴趣（使用 UserContext）
    /// </summary>
    [HttpPut("me/{interestId}")]
    public async Task<ActionResult<ApiResponse<UserInterestDto>>> UpdateCurrentUserInterest(
        string interestId,
        [FromBody] AddUserInterestRequest request,
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
        {
            return Unauthorized(new ApiResponse<UserInterestDto>
            {
                Success = false,
                Message = "未认证用户"
            });
        }

        _logger.LogInformation("✏️ 更新当前用户兴趣: UserId={UserId}, InterestId={InterestId}", userContext.UserId, interestId);

        try
        {
            var interest = await _interestService.UpdateUserInterestAsync(
                userContext.UserId!,
                interestId,
                request.IntensityLevel,
                cancellationToken);

            return Ok(new ApiResponse<UserInterestDto>
            {
                Success = true,
                Message = "User interest updated successfully",
                Data = interest
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<UserInterestDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新当前用户兴趣失败: UserId={UserId}, InterestId={InterestId}", userContext.UserId, interestId);
            return StatusCode(500, new ApiResponse<UserInterestDto>
            {
                Success = false,
                Message = "Failed to update user interest"
            });
        }
    }

    /// <summary>
    /// 更新用户兴趣
    /// </summary>
    [HttpPut("users/{userId}/{interestId}")]
    public async Task<ActionResult<ApiResponse<UserInterestDto>>> UpdateUserInterest(
        string userId,
        string interestId,
        [FromBody] AddUserInterestRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("✏️ 更新用户兴趣: UserId={UserId}, InterestId={InterestId}", userId, interestId);

        try
        {
            var interest = await _interestService.UpdateUserInterestAsync(
                userId,
                interestId,
                request.IntensityLevel,
                cancellationToken);

            return Ok(new ApiResponse<UserInterestDto>
            {
                Success = true,
                Message = "User interest updated successfully",
                Data = interest
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<UserInterestDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新用户兴趣失败: UserId={UserId}, InterestId={InterestId}", userId, interestId);
            return StatusCode(500, new ApiResponse<UserInterestDto>
            {
                Success = false,
                Message = "Failed to update user interest"
            });
        }
    }
}
