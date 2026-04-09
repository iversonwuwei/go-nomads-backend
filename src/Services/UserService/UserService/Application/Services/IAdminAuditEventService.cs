using UserService.Application.DTOs;

namespace UserService.Application.Services;

public interface IAdminAuditEventService
{
    Task<List<AdminAuditEventDto>> GetEventsAsync(string scope, CancellationToken cancellationToken = default);
    Task<AdminAuditEventDto> CreateAsync(CreateAdminAuditEventRequest request, string createdBy,
        CancellationToken cancellationToken = default);
}