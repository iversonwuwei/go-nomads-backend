using ConfigService.Application.DTOs;
using ConfigService.Application.Services;
using GoNomads.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ConfigService.API.Controllers;

[ApiController]
[Route("api/v1/app/config")]
public class AppConfigController : ControllerBase
{
    private readonly IConfigPublishService _configPublishService;
    private readonly ILogger<AppConfigController> _logger;

    public AppConfigController(IConfigPublishService configPublishService, ILogger<AppConfigController> logger)
    {
        _configPublishService = configPublishService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<AppConfigDto>>> GetConfig([FromQuery] string? locale = null)
    {
        try
        {
            var config = await _configPublishService.GetPublishedConfigAsync(locale);
            if (config == null)
                return NotFound(ApiResponse<AppConfigDto>.ErrorResponse("暂无已发布的配置"));

            return Ok(ApiResponse<AppConfigDto>.SuccessResponse(config));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取 App 配置失败");
            return StatusCode(500, ApiResponse<AppConfigDto>.ErrorResponse("获取配置失败"));
        }
    }

    [HttpGet("version")]
    public async Task<ActionResult<ApiResponse<AppConfigVersionDto>>> GetVersion()
    {
        try
        {
            var version = await _configPublishService.GetPublishedVersionAsync();
            if (version == null)
                return NotFound(ApiResponse<AppConfigVersionDto>.ErrorResponse("暂无已发布的配置"));

            return Ok(ApiResponse<AppConfigVersionDto>.SuccessResponse(version));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取配置版本失败");
            return StatusCode(500, ApiResponse<AppConfigVersionDto>.ErrorResponse("获取配置版本失败"));
        }
    }
}
