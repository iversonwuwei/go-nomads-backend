using Postgrest;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;
using Client = Supabase.Client;

namespace UserService.Infrastructure.Repositories;

public class AdminAuditEventRepository : IAdminAuditEventRepository
{
    private readonly ILogger<AdminAuditEventRepository> _logger;
    private readonly Client _supabaseClient;

    public AdminAuditEventRepository(Client supabaseClient, ILogger<AdminAuditEventRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<List<AdminAuditEvent>> ListByScopeAsync(string scope, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _supabaseClient
                .From<AdminAuditEvent>()
                .Where(e => e.Scope == scope)
                .Order("happened_at", Constants.Ordering.Descending)
                .Get(cancellationToken: cancellationToken);

            return response.Models.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取后台审计事件失败: Scope={Scope}", scope);
            throw;
        }
    }

    public async Task<AdminAuditEvent> CreateAsync(AdminAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _supabaseClient
                .From<AdminAuditEvent>()
                .Insert(auditEvent, cancellationToken: cancellationToken);

            var created = response.Models.FirstOrDefault();
            if (created == null)
            {
                throw new InvalidOperationException("创建后台审计事件失败");
            }

            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建后台审计事件失败: Scope={Scope}, Action={Action}", auditEvent.Scope, auditEvent.Action);
            throw;
        }
    }
}