using AIService.Application.DTOs;
using AIService.Application.Services;
using GoNomads.Shared.DTOs;
using GoNomads.Shared.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIService.API.Controllers;

/// <summary>
///     Migration Workspace API
/// </summary>
[ApiController]
[Route("api/v1/migration-workspace")]
[Produces("application/json")]
public class MigrationWorkspaceController : ControllerBase
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<MigrationWorkspaceController> _logger;
    private readonly IMigrationCentersService _migrationCentersService;

    public MigrationWorkspaceController(
        IMigrationCentersService migrationCentersService,
        ICurrentUserService currentUserService,
        ILogger<MigrationWorkspaceController> logger)
    {
        _migrationCentersService = migrationCentersService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    ///     获取当前用户的 Migration Workspace 聚合数据
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<MigrationWorkspaceResponse>>> GetWorkspace(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = _currentUserService.GetUserId();
            var result = await _migrationCentersService.GetMigrationWorkspaceAsync(userId, page, pageSize);

            return Ok(new ApiResponse<MigrationWorkspaceResponse>
            {
                Success = true,
                Message = "Migration workspace 获取成功",
                Data = result
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "获取 Migration Workspace 时用户未认证");
            return Unauthorized(new ApiResponse<MigrationWorkspaceResponse>
            {
                Success = false,
                Message = "用户未认证"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取 Migration Workspace 失败");
            return StatusCode(500, new ApiResponse<MigrationWorkspaceResponse>
            {
                Success = false,
                Message = "获取 Migration Workspace 失败"
            });
        }
    }

    [HttpPost("plans/{planId:guid}/state")]
    public async Task<ActionResult<ApiResponse<MigrationWorkspaceResponse>>> SavePlanState(
        Guid planId,
        [FromBody] UpdateMigrationWorkspacePlanRequest request)
    {
        try
        {
            var userId = _currentUserService.GetUserId();
            var result = await _migrationCentersService.SaveMigrationWorkspacePlanAsync(userId, planId, request);

            return Ok(new ApiResponse<MigrationWorkspaceResponse>
            {
                Success = true,
                Message = "Migration workspace 状态保存成功",
                Data = result
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<MigrationWorkspaceResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<MigrationWorkspaceResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "保存 Migration Workspace 时用户未认证");
            return Unauthorized(new ApiResponse<MigrationWorkspaceResponse>
            {
                Success = false,
                Message = "用户未认证"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存 Migration Workspace 状态失败");
            return StatusCode(500, new ApiResponse<MigrationWorkspaceResponse>
            {
                Success = false,
                Message = "保存 Migration Workspace 状态失败"
            });
        }
    }
}