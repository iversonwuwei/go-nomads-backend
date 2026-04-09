using AIService.Application.DTOs;
using AIService.Application.Services;
using GoNomads.Shared.DTOs;
using GoNomads.Shared.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIService.API.Controllers;

/// <summary>
///     Land Hub 聚合 API
/// </summary>
[ApiController]
[Route("api/v1/land-hub")]
[Produces("application/json")]
public class LandHubController : ControllerBase
{
    private readonly IAIChatService _aiChatService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<LandHubController> _logger;

    public LandHubController(
        IAIChatService aiChatService,
        ICurrentUserService currentUserService,
        ILogger<LandHubController> logger)
    {
        _aiChatService = aiChatService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    ///     获取当前用户的 Land Hub 聚合数据
    /// </summary>
    [HttpGet("current")]
    public async Task<ActionResult<ApiResponse<LandHubResponse>>> GetCurrentHub()
    {
        try
        {
            var userId = _currentUserService.GetUserId();
            var result = await _aiChatService.GetLandHubAsync(userId);

            return Ok(new ApiResponse<LandHubResponse>
            {
                Success = true,
                Message = "Land hub 获取成功",
                Data = result
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "获取 Land Hub 时用户未认证");
            return Unauthorized(new ApiResponse<LandHubResponse>
            {
                Success = false,
                Message = "用户未认证"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取 Land Hub 失败");
            return StatusCode(500, new ApiResponse<LandHubResponse>
            {
                Success = false,
                Message = "获取 Land Hub 失败"
            });
        }
    }
}