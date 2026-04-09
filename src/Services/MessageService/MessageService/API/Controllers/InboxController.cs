using GoNomads.Shared.DTOs;
using GoNomads.Shared.Services;
using MessageService.Application.DTOs;
using MessageService.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace MessageService.API.Controllers;

/// <summary>
///     Inbox 聚合 API
/// </summary>
[ApiController]
[Route("api/v1/inbox")]
public class InboxController : ControllerBase
{
    private readonly ICurrentUserService _currentUserService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<InboxController> _logger;

    public InboxController(
        ICurrentUserService currentUserService,
        INotificationService notificationService,
        ILogger<InboxController> logger)
    {
        _currentUserService = currentUserService;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    ///     获取当前用户的 Inbox Summary
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<InboxSummaryDto>>> GetSummary(
        [FromQuery] int recentLimit = 5,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = _currentUserService.GetUserIdString();
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new ApiResponse<InboxSummaryDto>
                {
                    Success = false,
                    Message = "用户未认证"
                });
            }

            var summary = await _notificationService.GetInboxSummaryAsync(userId, recentLimit, cancellationToken);

            return Ok(new ApiResponse<InboxSummaryDto>
            {
                Success = true,
                Message = "Inbox summary 获取成功",
                Data = summary
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "获取 Inbox Summary 时用户未认证");
            return Unauthorized(new ApiResponse<InboxSummaryDto>
            {
                Success = false,
                Message = "用户未认证"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取 Inbox Summary 失败");
            return StatusCode(500, new ApiResponse<InboxSummaryDto>
            {
                Success = false,
                Message = "获取 Inbox Summary 失败"
            });
        }
    }
}