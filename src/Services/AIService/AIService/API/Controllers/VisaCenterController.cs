using AIService.Application.DTOs;
using AIService.Application.Services;
using GoNomads.Shared.DTOs;
using GoNomads.Shared.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIService.API.Controllers;

/// <summary>
///     Visa Center API
/// </summary>
[ApiController]
[Route("api/v1/visa/profiles")]
[Produces("application/json")]
public class VisaCenterController : ControllerBase
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<VisaCenterController> _logger;
    private readonly IMigrationCentersService _migrationCentersService;

    public VisaCenterController(
        IMigrationCentersService migrationCentersService,
        ICurrentUserService currentUserService,
        ILogger<VisaCenterController> logger)
    {
        _migrationCentersService = migrationCentersService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    ///     获取当前用户的 Visa Center 聚合数据
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<VisaCenterResponse>>> GetVisaProfiles()
    {
        try
        {
            var userId = _currentUserService.GetUserId();
            var result = await _migrationCentersService.GetVisaCenterAsync(userId);

            return Ok(new ApiResponse<VisaCenterResponse>
            {
                Success = true,
                Message = "Visa center 获取成功",
                Data = result
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "获取 Visa Center 时用户未认证");
            return Unauthorized(new ApiResponse<VisaCenterResponse>
            {
                Success = false,
                Message = "用户未认证"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取 Visa Center 失败");
            return StatusCode(500, new ApiResponse<VisaCenterResponse>
            {
                Success = false,
                Message = "获取 Visa Center 失败"
            });
        }
    }

    [HttpPost("{planId:guid}")]
    public async Task<ActionResult<ApiResponse<VisaCenterResponse>>> SaveVisaProfile(
        Guid planId,
        [FromBody] SaveVisaProfileRequest request)
    {
        try
        {
            var userId = _currentUserService.GetUserId();
            var result = await _migrationCentersService.SaveVisaProfileAsync(userId, planId, request);

            return Ok(new ApiResponse<VisaCenterResponse>
            {
                Success = true,
                Message = "Visa profile 保存成功",
                Data = result
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<VisaCenterResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<VisaCenterResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "保存 Visa profile 时用户未认证");
            return Unauthorized(new ApiResponse<VisaCenterResponse>
            {
                Success = false,
                Message = "用户未认证"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存 Visa profile 失败");
            return StatusCode(500, new ApiResponse<VisaCenterResponse>
            {
                Success = false,
                Message = "保存 Visa profile 失败"
            });
        }
    }
}