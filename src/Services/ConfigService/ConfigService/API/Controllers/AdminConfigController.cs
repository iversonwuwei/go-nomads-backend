using ConfigService.Application.DTOs;
using ConfigService.Application.Services;
using GoNomads.Shared.DTOs;
using GoNomads.Shared.Services;
using Microsoft.AspNetCore.Mvc;

namespace ConfigService.API.Controllers;

[ApiController]
[Route("api/v1/admin/config")]
public class AdminConfigController : ControllerBase
{
    private readonly IConfigPublishService _configPublishService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<AdminConfigController> _logger;

    public AdminConfigController(
        IConfigPublishService configPublishService,
        ICurrentUserService currentUser,
        ILogger<AdminConfigController> logger)
    {
        _configPublishService = configPublishService;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpPost("publish")]
    public async Task<ActionResult<ApiResponse<ConfigSnapshotDto>>> Publish()
    {
        try
        {
            if (!_currentUser.IsAdmin())
                return Forbid();

            var userId = _currentUser.GetUserId();
            var snapshot = await _configPublishService.PublishAsync(userId);
            return Ok(ApiResponse<ConfigSnapshotDto>.SuccessResponse(snapshot, "配置发布成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "配置发布失败");
            return StatusCode(500, ApiResponse<ConfigSnapshotDto>.ErrorResponse("配置发布失败"));
        }
    }

    [HttpGet("snapshots")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<ConfigSnapshotDto>>>> GetSnapshots(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (!_currentUser.IsAdmin())
                return Forbid();

            var snapshots = await _configPublishService.GetSnapshotsAsync(page, pageSize);
            var totalCount = await _configPublishService.GetSnapshotCountAsync();

            return Ok(new ApiResponse<PaginatedResponse<ConfigSnapshotDto>>
            {
                Success = true,
                Message = "Snapshots retrieved successfully",
                Data = new PaginatedResponse<ConfigSnapshotDto>
                {
                    Items = snapshots.ToList(),
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取快照列表失败");
            return StatusCode(500, ApiResponse<PaginatedResponse<ConfigSnapshotDto>>.ErrorResponse("获取快照列表失败"));
        }
    }

    [HttpGet("snapshots/{id:guid}")]
    public async Task<ActionResult<ApiResponse<ConfigSnapshotDetailDto>>> GetSnapshotDetail(Guid id)
    {
        try
        {
            if (!_currentUser.IsAdmin())
                return Forbid();

            var detail = await _configPublishService.GetSnapshotDetailAsync(id);
            if (detail == null)
                return NotFound(ApiResponse<ConfigSnapshotDetailDto>.ErrorResponse("快照不存在"));

            return Ok(ApiResponse<ConfigSnapshotDetailDto>.SuccessResponse(detail));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取快照详情失败: {Id}", id);
            return StatusCode(500, ApiResponse<ConfigSnapshotDetailDto>.ErrorResponse("获取快照详情失败"));
        }
    }

    [HttpPost("snapshots/{id:guid}/rollback")]
    [HttpPost("rollback/{id:guid}")]
    public async Task<ActionResult<ApiResponse<ConfigSnapshotDto>>> Rollback(Guid id)
    {
        try
        {
            if (!_currentUser.IsAdmin())
                return Forbid();

            var userId = _currentUser.GetUserId();
            var result = await _configPublishService.RollbackAsync(id, userId);
            if (result == null)
                return NotFound(ApiResponse<ConfigSnapshotDto>.ErrorResponse("快照不存在"));

            return Ok(ApiResponse<ConfigSnapshotDto>.SuccessResponse(result, "配置回滚成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "配置回滚失败: {Id}", id);
            return StatusCode(500, ApiResponse<ConfigSnapshotDto>.ErrorResponse("配置回滚失败"));
        }
    }
}
