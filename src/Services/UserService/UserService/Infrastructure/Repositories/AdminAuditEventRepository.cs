using Postgrest;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;
using Client = Supabase.Client;

namespace UserService.Infrastructure.Repositories;

/// <summary>
///     管理后台审计事件仓储实现
/// </summary>
public class AdminAuditEventRepository : IAdminAuditEventRepository
{
    private readonly ILogger<AdminAuditEventRepository> _logger;
    private readonly Client _supabaseClient;

    public AdminAuditEventRepository(Client supabaseClient, ILogger<AdminAuditEventRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<AdminAuditEvent> CreateAsync(AdminAuditEvent entity, CancellationToken cancellationToken = default)
    {
        entity.CreatedAt = DateTime.UtcNow;
        if (entity.HappenedAt == default) entity.HappenedAt = DateTime.UtcNow;

        var result = await _supabaseClient
            .From<AdminAuditEvent>()
            .Insert(entity, cancellationToken: cancellationToken);

        var created = result.Models.FirstOrDefault();
        if (created == null)
            throw new InvalidOperationException("Create admin audit event failed");

        _logger.LogInformation("✅ 写入审计事件: Scope={Scope}, Action={Action}, EntityId={EntityId}",
            created.Scope, created.Action, created.EntityId);

        return created;
    }

    public async Task<List<AdminAuditEvent>> GetByScopeAsync(
        string scope,
        int limit = 200,
        CancellationToken cancellationToken = default)
    {
        var normalizedScope = string.IsNullOrWhiteSpace(scope) ? "global" : scope.Trim();
        var safeLimit = Math.Max(1, Math.Min(limit, 500));

        var response = await _supabaseClient
            .From<AdminAuditEvent>()
            .Where(x => x.Scope == normalizedScope)
            .Order(x => x.HappenedAt, Constants.Ordering.Descending)
            .Limit(safeLimit)
            .Get(cancellationToken);

        return response.Models;
    }
}
