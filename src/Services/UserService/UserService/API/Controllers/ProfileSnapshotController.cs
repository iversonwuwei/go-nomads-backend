using GoNomads.Shared.Middleware;
using GoNomads.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs;
using UserService.Application.Services;

namespace UserService.API.Controllers;

[ApiController]
[Route("api/v1/profile-snapshot")]
public class ProfileSnapshotController : ControllerBase
{
    private readonly ILogger<ProfileSnapshotController> _logger;
    private readonly IProfileSnapshotService _profileSnapshotService;

    public ProfileSnapshotController(
        IProfileSnapshotService profileSnapshotService,
        ILogger<ProfileSnapshotController> logger)
    {
        _profileSnapshotService = profileSnapshotService;
        _logger = logger;
    }

    [HttpGet("current")]
    public async Task<ActionResult<ApiResponse<ProfileSnapshotResponse>>> GetCurrent(
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true || string.IsNullOrWhiteSpace(userContext.UserId))
        {
            return Unauthorized(new ApiResponse<ProfileSnapshotResponse>
            {
                Success = false,
                Message = "未认证用户"
            });
        }

        try
        {
            var snapshot = await _profileSnapshotService.GetCurrentAsync(userContext.UserId, cancellationToken);
            if (snapshot == null)
            {
                return NotFound(new ApiResponse<ProfileSnapshotResponse>
                {
                    Success = false,
                    Message = "用户不存在"
                });
            }

            return Ok(new ApiResponse<ProfileSnapshotResponse>
            {
                Success = true,
                Message = "Profile snapshot retrieved successfully",
                Data = snapshot
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取 Profile Snapshot 失败: {UserId}", userContext.UserId);
            return StatusCode(500, new ApiResponse<ProfileSnapshotResponse>
            {
                Success = false,
                Message = "获取 Profile Snapshot 失败"
            });
        }
    }
}