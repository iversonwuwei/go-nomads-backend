using UserService.Domain.Entities;
using UserService.Domain.Repositories;
using Client = Supabase.Client;

namespace UserService.Infrastructure.Repositories;

/// <summary>
///     会员计划仓储 Supabase 实现
/// </summary>
public class MembershipPlanRepository : IMembershipPlanRepository
{
    private readonly ILogger<MembershipPlanRepository> _logger;
    private readonly Client _supabaseClient;

    public MembershipPlanRepository(Client supabaseClient, ILogger<MembershipPlanRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<List<MembershipPlan>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 获取所有会员计划");

        try
        {
            var response = await _supabaseClient
                .From<MembershipPlan>()
                .Where(p => p.IsActive == true)
                .Order("sort_order", Postgrest.Constants.Ordering.Ascending)
                .Get(cancellationToken);

            _logger.LogInformation("✅ 获取到 {Count} 个会员计划", response.Models.Count);
            return response.Models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取会员计划失败");
            return new List<MembershipPlan>();
        }
    }

    public async Task<MembershipPlan?> GetByLevelAsync(int level, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _supabaseClient
                .From<MembershipPlan>()
                .Where(p => p.Level == level)
                .Single(cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 未找到等级 {Level} 的会员计划", level);
            return null;
        }
    }
}
