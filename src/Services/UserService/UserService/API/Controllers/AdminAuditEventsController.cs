using GoNomads.Shared.Models;
using GoNomads.Shared.Services;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs;
using UserService.Application.Services;

namespace UserService.API.Controllers;

[ApiController]
[Route("api/v1/admin/audit/events")]
public class AdminAuditEventsController : ControllerBase
{
    private readonly IAdminAuditEventService _adminAuditEventService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<AdminAuditEventsController> _logger;

    public AdminAuditEventsController(
        IAdminAuditEventService adminAuditEventService,
        ICurrentUserService currentUser,
        ILogger<AdminAuditEventsController> logger)
    {
        _adminAuditEventService = adminAuditEventService;
        _currentUser = currentUser;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<AdminAuditEventDto>>>> GetEvents(
        [FromQuery] string scope = "global",
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAdmin())
            return Forbid();

        try
        {
            var items = await _adminAuditEventService.GetEventsAsync(scope, cancellationToken);
            return Ok(new ApiResponse<List<AdminAuditEventDto>>
            {
                Success = true,
                Message = "OK",
                Data = items
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取后台审计事件失败: Scope={Scope}", scope);
            return StatusCode(500, new ApiResponse<List<AdminAuditEventDto>>
            {
                Success = false,
                Message = "获取后台审计事件失败"
            });
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<AdminAuditEventDto>>> Create(
        [FromBody] CreateAdminAuditEventRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAdmin())
            return Forbid();

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(new ApiResponse<AdminAuditEventDto>
            {
                Success = false,
                Message = "验证失败",
                Errors = errors
            });
        }

        try
        {
            var currentUserId = _currentUser.GetUserIdString();
            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized(new ApiResponse<AdminAuditEventDto>
                {
                    Success = false,
                    Message = "未认证用户"
                });

            var created = await _adminAuditEventService.CreateAsync(request, currentUserId, cancellationToken);
            return Ok(new ApiResponse<AdminAuditEventDto>
            {
                Success = true,
                Message = "Saved",
                Data = created
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建后台审计事件失败: Scope={Scope}, Action={Action}", request.Scope, request.Action);
            return StatusCode(500, new ApiResponse<AdminAuditEventDto>
            {
                Success = false,
                Message = "创建后台审计事件失败"
            });
        }
    }
}