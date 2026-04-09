using AIService.Application.DTOs;
using AIService.Application.Services;
using GoNomads.Shared.DTOs;
using GoNomads.Shared.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIService.API.Controllers;

/// <summary>
///     Explore Dashboard 聚合 API
/// </summary>
[ApiController]
[Route("api/v1/explore-dashboard")]
[Produces("application/json")]
public class ExploreDashboardController : ControllerBase
{
    private readonly IAIChatService _aiChatService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ExploreDashboardController> _logger;

    public ExploreDashboardController(
        IAIChatService aiChatService,
        ICurrentUserService currentUserService,
        ILogger<ExploreDashboardController> logger)
    {
        _aiChatService = aiChatService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    ///     获取当前用户的 Explore Dashboard 聚合数据
    /// </summary>
    [HttpGet("current")]
    public async Task<ActionResult<ApiResponse<ExploreDashboardResponse>>> GetCurrentDashboard()
    {
        try
        {
            var userId = _currentUserService.GetUserId();
            var result = await _aiChatService.GetExploreDashboardAsync(userId);

            return Ok(new ApiResponse<ExploreDashboardResponse>
            {
                Success = true,
                Message = "Explore dashboard 获取成功",
                Data = result
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "获取 Explore Dashboard 时用户未认证");
            return Unauthorized(new ApiResponse<ExploreDashboardResponse>
            {
                Success = false,
                Message = "用户未认证"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取 Explore Dashboard 失败");
            return StatusCode(500, new ApiResponse<ExploreDashboardResponse>
            {
                Success = false,
                Message = "获取 Explore Dashboard 失败"
            });
        }
    }
}