using CityService.Application.DTOs;
using CityService.Application.Services;
using GoNomads.Shared.Middleware;
using GoNomads.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace CityService.API.Controllers;

/// <summary>
///     版主申请管理控制器
/// </summary>
[ApiController]
[Route("api/v1/cities/moderator")]
public class ModeratorApplicationController : ControllerBase
{
    private readonly ILogger<ModeratorApplicationController> _logger;
    private readonly IModeratorApplicationService _service;

    public ModeratorApplicationController(
        IModeratorApplicationService service,
        ILogger<ModeratorApplicationController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    ///     用户申请成为版主
    /// </summary>
    [HttpPost("apply")]
    [ProducesResponseType(typeof(ApiResponse<ModeratorApplicationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<ModeratorApplicationResponse>>> Apply(
        [FromBody] ApplyModeratorRequest request)
    {
        try
        {
            // 从 UserContext 获取当前用户
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
                return Unauthorized(new ApiResponse<ModeratorApplicationResponse>
                {
                    Success = false,
                    Message = "用户未认证"
                });

            var userId = Guid.Parse(userContext.UserId);
            var result = await _service.ApplyAsync(userId, request);

            return Ok(new ApiResponse<ModeratorApplicationResponse>
            {
                Success = true,
                Message = "申请已提交，请等待管理员审核",
                Data = result
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<ModeratorApplicationResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "提交版主申请失败");
            return StatusCode(500, new ApiResponse<ModeratorApplicationResponse>
            {
                Success = false,
                Message = "提交申请失败"
            });
        }
    }

    /// <summary>
    ///     管理员处理申请（批准或拒绝）
    /// </summary>
    [HttpPost("handle")]
    [ProducesResponseType(typeof(ApiResponse<ModeratorApplicationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ModeratorApplicationResponse>>> HandleApplication(
        [FromBody] HandleModeratorApplicationRequest request)
    {
        try
        {
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
                return Unauthorized(new ApiResponse<ModeratorApplicationResponse>
                {
                    Success = false,
                    Message = "用户未认证"
                });

            var adminId = Guid.Parse(userContext.UserId);
            var result = await _service.HandleApplicationAsync(adminId, request);

            var message = request.Action.ToLower() == "approve"
                ? "申请已批准"
                : "申请已拒绝";

            return Ok(new ApiResponse<ModeratorApplicationResponse>
            {
                Success = true,
                Message = message,
                Data = result
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<ModeratorApplicationResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<ModeratorApplicationResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理版主申请失败");
            return StatusCode(500, new ApiResponse<ModeratorApplicationResponse>
            {
                Success = false,
                Message = "处理申请失败"
            });
        }
    }

    /// <summary>
    ///     获取待处理的申请列表（管理员使用）
    ///     权限检查在 Gateway 完成
    /// </summary>
    [HttpGet("applications/pending")]
    [ProducesResponseType(typeof(ApiResponse<List<ModeratorApplicationResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<ModeratorApplicationResponse>>>> GetPendingApplications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var applications = await _service.GetPendingApplicationsAsync(page, pageSize);

            return Ok(new ApiResponse<List<ModeratorApplicationResponse>>
            {
                Success = true,
                Message = "获取成功",
                Data = applications
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取待处理申请列表失败");
            return StatusCode(500, new ApiResponse<List<ModeratorApplicationResponse>>
            {
                Success = false,
                Message = "获取申请列表失败"
            });
        }
    }

    /// <summary>
    ///     获取当前用户的申请列表
    /// </summary>
    [HttpGet("applications/my")]
    [ProducesResponseType(typeof(ApiResponse<List<ModeratorApplicationResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<ModeratorApplicationResponse>>>> GetMyApplications()
    {
        try
        {
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
                return Unauthorized(new ApiResponse<List<ModeratorApplicationResponse>>
                {
                    Success = false,
                    Message = "用户未认证"
                });

            var userId = Guid.Parse(userContext.UserId);
            var applications = await _service.GetUserApplicationsAsync(userId);

            return Ok(new ApiResponse<List<ModeratorApplicationResponse>>
            {
                Success = true,
                Message = "获取成功",
                Data = applications
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户申请列表失败");
            return StatusCode(500, new ApiResponse<List<ModeratorApplicationResponse>>
            {
                Success = false,
                Message = "获取申请列表失败"
            });
        }
    }

    /// <summary>
    ///     获取申请详情
    /// </summary>
    [HttpGet("applications/{id}")]
    [ProducesResponseType(typeof(ApiResponse<ModeratorApplicationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ModeratorApplicationResponse>>> GetApplicationById(Guid id)
    {
        try
        {
            var application = await _service.GetApplicationByIdAsync(id);
            if (application == null)
                return NotFound(new ApiResponse<ModeratorApplicationResponse>
                {
                    Success = false,
                    Message = "申请不存在"
                });

            return Ok(new ApiResponse<ModeratorApplicationResponse>
            {
                Success = true,
                Data = application
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取申请详情失败");
            return StatusCode(500, new ApiResponse<ModeratorApplicationResponse>
            {
                Success = false,
                Message = "获取申请详情失败"
            });
        }
    }

    /// <summary>
    ///     获取申请统计（管理员使用）
    ///     权限检查在 Gateway 完成
    /// </summary>
    [HttpGet("applications/statistics")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<object>>> GetStatistics()
    {
        try
        {
            var (total, pending, approved, rejected) = await _service.GetStatisticsAsync();

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new
                {
                    Total = total,
                    Pending = pending,
                    Approved = approved,
                    Rejected = rejected
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取申请统计失败");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "获取统计失败"
            });
        }
    }

    /// <summary>
    ///     撤销版主资格（管理员使用）
    /// </summary>
    [HttpPost("revoke")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> RevokeModerator(
        [FromBody] RevokeModeratorRequest request)
    {
        try
        {
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Message = "用户未认证"
                });

            var adminId = Guid.Parse(userContext.UserId);
            await _service.RevokeModeratorAsync(adminId, request.ApplicationId);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "版主资格已撤销"
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "撤销版主资格失败");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "撤销版主失败"
            });
        }
    }
}
