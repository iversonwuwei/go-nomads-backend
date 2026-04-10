using ConfigService.Application.DTOs;
using ConfigService.Application.Services;
using GoNomads.Shared.DTOs;
using GoNomads.Shared.Services;
using Microsoft.AspNetCore.Mvc;

namespace ConfigService.API.Controllers;

[ApiController]
[Route("api/v1/admin/config/system-settings")]
public class AdminSystemSettingsController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<AdminSystemSettingsController> _logger;
    private readonly ISystemSettingService _systemSettingService;

    public AdminSystemSettingsController(
        ISystemSettingService systemSettingService,
        ICurrentUserService currentUser,
        ILogger<AdminSystemSettingsController> logger)
    {
        _systemSettingService = systemSettingService;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<SystemSettingDto>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? section = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_currentUser.IsAdmin())
                return Forbid();

            var result = await _systemSettingService.GetAllAsync(page, pageSize, section, search, cancellationToken);
            return Ok(new ApiResponse<PaginatedResponse<SystemSettingDto>>
            {
                Success = true,
                Message = "系统配置获取成功",
                Data = new PaginatedResponse<SystemSettingDto>
                {
                    Items = result.Items,
                    TotalCount = result.TotalCount,
                    Page = page,
                    PageSize = pageSize
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取系统配置列表失败");
            return StatusCode(500, ApiResponse<PaginatedResponse<SystemSettingDto>>.ErrorResponse("获取系统配置失败"));
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SystemSettingDto>>> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_currentUser.IsAdmin())
                return Forbid();

            var setting = await _systemSettingService.GetByIdAsync(id, cancellationToken);
            if (setting == null)
                return NotFound(ApiResponse<SystemSettingDto>.ErrorResponse("系统配置不存在"));

            return Ok(ApiResponse<SystemSettingDto>.SuccessResponse(setting));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取系统配置详情失败: {Id}", id);
            return StatusCode(500, ApiResponse<SystemSettingDto>.ErrorResponse("获取系统配置详情失败"));
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SystemSettingDto>>> Create(
        [FromBody] CreateSystemSettingDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_currentUser.IsAdmin())
                return Forbid();

            var created = await _systemSettingService.CreateAsync(dto, _currentUser.GetUserId(), cancellationToken);
            return CreatedAtAction(
                nameof(GetById),
                new { id = created.Id },
                ApiResponse<SystemSettingDto>.SuccessResponse(created, "系统配置创建成功"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<SystemSettingDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建系统配置失败");
            return StatusCode(500, ApiResponse<SystemSettingDto>.ErrorResponse("创建系统配置失败"));
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SystemSettingDto>>> Update(
        Guid id,
        [FromBody] UpdateSystemSettingDto dto,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_currentUser.IsAdmin())
                return Forbid();

            var updated = await _systemSettingService.UpdateAsync(id, dto, _currentUser.GetUserId(), cancellationToken);
            if (updated == null)
                return NotFound(ApiResponse<SystemSettingDto>.ErrorResponse("系统配置不存在"));

            return Ok(ApiResponse<SystemSettingDto>.SuccessResponse(updated, "系统配置更新成功"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<SystemSettingDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新系统配置失败: {Id}", id);
            return StatusCode(500, ApiResponse<SystemSettingDto>.ErrorResponse("更新系统配置失败"));
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_currentUser.IsAdmin())
                return Forbid();

            var deleted = await _systemSettingService.DeleteAsync(id, _currentUser.GetUserId(), cancellationToken);
            if (!deleted)
                return NotFound(ApiResponse<bool>.ErrorResponse("系统配置不存在"));

            return Ok(ApiResponse<bool>.SuccessResponse(true, "系统配置删除成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除系统配置失败: {Id}", id);
            return StatusCode(500, ApiResponse<bool>.ErrorResponse("删除系统配置失败"));
        }
    }
}