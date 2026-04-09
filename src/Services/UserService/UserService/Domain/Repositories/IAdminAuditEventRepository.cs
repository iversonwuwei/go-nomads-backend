using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

public interface IAdminAuditEventRepository
{
    Task<List<AdminAuditEvent>> ListByScopeAsync(string scope, CancellationToken cancellationToken = default);
    Task<AdminAuditEvent> CreateAsync(AdminAuditEvent auditEvent, CancellationToken cancellationToken = default);
}