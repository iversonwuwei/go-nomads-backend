using AIService.Application.DTOs;
using AIService.Application.Services;
using GoNomads.Shared.DTOs;
using GoNomads.Shared.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIService.API.Controllers;

/// <summary>
///     Community Snapshot 聚合 API
/// </summary>
[ApiController]
[Route("api/v1/community-snapshot")]
[Produces("application/json")]
public class CommunitySnapshotController : ControllerBase
{
    private readonly IAIChatService _aiChatService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CommunitySnapshotController> _logger;

    public CommunitySnapshotController(
        IAIChatService aiChatService,
        ICurrentUserService currentUserService,
        ILogger<CommunitySnapshotController> logger)
    {
        _aiChatService = aiChatService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    ///     获取当前用户的 Community Snapshot 聚合数据
    /// </summary>
    [HttpGet("current")]
    public async Task<ActionResult<ApiResponse<CommunitySnapshotResponse>>> GetCurrentSnapshot()
    {
        try
        {
            var userId = _currentUserService.GetUserId();
            var result = await _aiChatService.GetCommunitySnapshotAsync(userId);

            return Ok(new ApiResponse<CommunitySnapshotResponse>
            {
                Success = true,
                Message = "Community snapshot 获取成功",
                Data = result
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "获取 Community Snapshot 时用户未认证");
            return Unauthorized(new ApiResponse<CommunitySnapshotResponse>
            {
                Success = false,
                Message = "用户未认证"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取 Community Snapshot 失败");
            return StatusCode(500, new ApiResponse<CommunitySnapshotResponse>
            {
                Success = false,
                Message = "获取 Community Snapshot 失败"
            });
        }
    }
}