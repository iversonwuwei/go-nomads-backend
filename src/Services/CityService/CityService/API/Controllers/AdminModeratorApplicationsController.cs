using CityService.Application.DTOs;
using CityService.Application.Services;
using GoNomads.Shared.DTOs;
using GoNomads.Shared.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CityService.API.Controllers;

[ApiController]
[Route("api/v1/admin/moderator-applications")]
[Authorize]
public class AdminModeratorApplicationsController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly IModeratorApplicationService _applicationService;
    private readonly ILogger<AdminModeratorApplicationsController> _logger;

    public AdminModeratorApplicationsController(
        ICurrentUserService currentUser,
        IModeratorApplicationService applicationService,
        ILogger<AdminModeratorApplicationsController> logger)
    {
        _currentUser = currentUser;
        _applicationService = applicationService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<ModeratorApplicationResponse>>>> GetPending(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (!_currentUser.IsAdmin()) return Forbid();

            var applications = await _applicationService.GetPendingApplicationsAsync(page, pageSize);
            var stats = await _applicationService.GetStatisticsAsync();

            return Ok(new ApiResponse<PaginatedResponse<ModeratorApplicationResponse>>
            {
                Success = true,
                Message = "获取待审核申请成功",
                Data = new PaginatedResponse<ModeratorApplicationResponse>
                {
                    Items = applications,
                    TotalCount = stats.Pending,
                    Page = page,
                    PageSize = pageSize
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取待审核版主申请失败");
            return StatusCode(500,
                ApiResponse<PaginatedResponse<ModeratorApplicationResponse>>.ErrorResponse("获取待审核申请失败"));
        }
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse<ModeratorApplicationResponse>>> Approve(Guid id)
    {
        try
        {
            if (!_currentUser.IsAdmin()) return Forbid();

            var adminId = _currentUser.GetUserId();
            var result = await _applicationService.HandleApplicationAsync(adminId,
                new HandleModeratorApplicationRequest
                {
                    ApplicationId = id,
                    Action = "approve"
                });

            _logger.LogInformation("管理员批准版主申请: ApplicationId={Id}, AdminId={AdminId}", id, adminId);
            return Ok(ApiResponse<ModeratorApplicationResponse>.SuccessResponse(result, "申请已批准"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批准版主申请失败: Id={Id}", id);
            return StatusCode(500, ApiResponse<ModeratorApplicationResponse>.ErrorResponse("批准申请失败"));
        }
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<ApiResponse<ModeratorApplicationResponse>>> Reject(
        Guid id,
        [FromBody] RejectApplicationRequest? request = null)
    {
        try
        {
            if (!_currentUser.IsAdmin()) return Forbid();

            var adminId = _currentUser.GetUserId();
            var result = await _applicationService.HandleApplicationAsync(adminId,
                new HandleModeratorApplicationRequest
                {
                    ApplicationId = id,
                    Action = "reject",
                    RejectionReason = request?.Reason
                });

            _logger.LogInformation("管理员拒绝版主申请: ApplicationId={Id}, AdminId={AdminId}", id, adminId);
            return Ok(ApiResponse<ModeratorApplicationResponse>.SuccessResponse(result, "申请已拒绝"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "拒绝版主申请失败: Id={Id}", id);
            return StatusCode(500, ApiResponse<ModeratorApplicationResponse>.ErrorResponse("拒绝申请失败"));
        }
    }
}

public class RejectApplicationRequest
{
    public string? Reason { get; set; }
}
