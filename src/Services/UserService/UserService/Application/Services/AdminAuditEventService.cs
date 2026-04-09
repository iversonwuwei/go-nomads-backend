using System.Text.Json;
using UserService.Application.DTOs;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Application.Services;

public class AdminAuditEventService : IAdminAuditEventService
{
    private readonly IAdminAuditEventRepository _adminAuditEventRepository;
    private readonly ILogger<AdminAuditEventService> _logger;

    public AdminAuditEventService(
        IAdminAuditEventRepository adminAuditEventRepository,
        ILogger<AdminAuditEventService> logger)
    {
        _adminAuditEventRepository = adminAuditEventRepository;
        _logger = logger;
    }

    public async Task<List<AdminAuditEventDto>> GetEventsAsync(string scope, CancellationToken cancellationToken = default)
    {
        var normalizedScope = string.IsNullOrWhiteSpace(scope) ? "global" : scope.Trim();
        var events = await _adminAuditEventRepository.ListByScopeAsync(normalizedScope, cancellationToken);
        return events.Select(MapToDto).ToList();
    }

    public async Task<AdminAuditEventDto> CreateAsync(CreateAdminAuditEventRequest request, string createdBy,
        CancellationToken cancellationToken = default)
    {
        var auditEvent = new AdminAuditEvent
        {
            Id = Guid.NewGuid().ToString(),
            Scope = request.Scope.Trim(),
            EntityId = request.EntityId?.Trim() ?? string.Empty,
            Action = request.Action.Trim(),
            Note = request.Note.Trim(),
            MetadataJson = JsonSerializer.Serialize(request.Metadata ?? new Dictionary<string, object?>()),
            HappenedAt = request.HappenedAt ?? DateTime.UtcNow,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _adminAuditEventRepository.CreateAsync(auditEvent, cancellationToken);
        _logger.LogInformation("✅ 已创建后台审计事件: Scope={Scope}, EntityId={EntityId}, Action={Action}, CreatedBy={CreatedBy}",
            created.Scope, created.EntityId, created.Action, createdBy);

        return MapToDto(created);
    }

    private static AdminAuditEventDto MapToDto(AdminAuditEvent auditEvent)
    {
        Dictionary<string, object?> metadata;
        try
        {
            metadata = JsonSerializer.Deserialize<Dictionary<string, object?>>(auditEvent.MetadataJson) ?? new Dictionary<string, object?>();
        }
        catch
        {
            metadata = new Dictionary<string, object?>();
        }

        return new AdminAuditEventDto
        {
            Id = auditEvent.Id,
            Scope = auditEvent.Scope,
            EntityId = auditEvent.EntityId,
            Action = auditEvent.Action,
            Note = auditEvent.Note,
            Metadata = metadata,
            HappenedAt = auditEvent.HappenedAt
        };
    }
}