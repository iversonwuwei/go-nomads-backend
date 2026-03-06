using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

/// <summary>
///     管理后台审计事件仓储接口
/// </summary>
public interface IAdminAuditEventRepository
{
    Task<AdminAuditEvent> CreateAsync(AdminAuditEvent entity, CancellationToken cancellationToken = default);
    Task<List<AdminAuditEvent>> GetByScopeAsync(string scope, int limit = 200, CancellationToken cancellationToken = default);
}
