using GoNomads.Shared.Middleware;
using GoNomads.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.API.Controllers;

/// <summary>
///     管理后台审计事件 API（最小可用版本）
/// </summary>
[ApiController]
[Route("api/v1/admin/audit/events")]
public class AdminAuditController : ControllerBase
{
    private readonly IAdminAuditEventRepository _repository;

    public AdminAuditController(IAdminAuditEventRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    ///     查询审计事件
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<AdminAuditEventDto>>>> Get(
        [FromQuery] string? scope = null,
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true || userContext.IsAdmin != true)
            return StatusCode(403, new ApiResponse<List<AdminAuditEventDto>>
            {
                Success = false,
                Message = "无权限，仅管理员可访问审计事件"
            });

        var normalizedScope = string.IsNullOrWhiteSpace(scope) ? "global" : scope.Trim();
        var entities = await _repository.GetByScopeAsync(normalizedScope, 200, cancellationToken);
        var rows = entities.Select(MapToDto).ToList();

        return Ok(new ApiResponse<List<AdminAuditEventDto>>
        {
            Success = true,
            Message = "获取审计事件成功",
            Data = rows
        });
    }

    /// <summary>
    ///     写入审计事件
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<AdminAuditEventDto>>> Create(
        [FromBody] CreateAdminAuditEventRequest request,
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true || userContext.IsAdmin != true)
            return StatusCode(403, new ApiResponse<AdminAuditEventDto>
            {
                Success = false,
                Message = "无权限，仅管理员可写入审计事件"
            });

        if (string.IsNullOrWhiteSpace(request.Action) || string.IsNullOrWhiteSpace(request.Note))
            return BadRequest(new ApiResponse<AdminAuditEventDto>
            {
                Success = false,
                Message = "Action 和 Note 不能为空"
            });

        var entity = new AdminAuditEvent
        {
            Scope = string.IsNullOrWhiteSpace(request.Scope) ? "global" : request.Scope.Trim(),
            EntityId = request.EntityId?.Trim() ?? string.Empty,
            Action = request.Action.Trim(),
            Note = request.Note.Trim(),
            Metadata = request.Metadata ?? new Dictionary<string, object>(),
            HappenedAt = request.HappenedAt ?? DateTime.UtcNow
        };

        var created = await _repository.CreateAsync(entity, cancellationToken);
        var evt = MapToDto(created);

        return Ok(new ApiResponse<AdminAuditEventDto>
        {
            Success = true,
            Message = "写入审计事件成功",
            Data = evt
        });
    }

    private static AdminAuditEventDto MapToDto(AdminAuditEvent entity)
    {
        return new AdminAuditEventDto
        {
            Id = entity.Id,
            Scope = entity.Scope,
            EntityId = entity.EntityId,
            Action = entity.Action,
            Note = entity.Note,
            Metadata = entity.Metadata,
            HappenedAt = entity.HappenedAt
        };
    }
}
