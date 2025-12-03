using UserService.Domain.Entities;
using UserService.Domain.Repositories;
using Client = Supabase.Client;

namespace UserService.Infrastructure.Repositories;

/// <summary>
///     会员仓储 Supabase 实现
/// </summary>
public class MembershipRepository : IMembershipRepository
{
    private readonly ILogger<MembershipRepository> _logger;
    private readonly Client _supabaseClient;

    public MembershipRepository(Client supabaseClient, ILogger<MembershipRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<Membership?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 查询用户会员信息: {UserId}", userId);

        try
        {
            var response = await _supabaseClient
                .From<Membership>()
                .Where(m => m.UserId == userId)
                .Single(cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 未找到会员记录: {UserId}", userId);
            return null;
        }
    }

    public async Task<Membership?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _supabaseClient
                .From<Membership>()
                .Where(m => m.Id == id)
                .Single(cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 未找到会员记录: {Id}", id);
            return null;
        }
    }

    public async Task<Membership> CreateAsync(Membership membership, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 创建会员记录: {UserId}", membership.UserId);

        var result = await _supabaseClient
            .From<Membership>()
            .Insert(membership, cancellationToken: cancellationToken);

        var created = result.Models.FirstOrDefault();
        if (created == null) throw new InvalidOperationException("创建会员记录失败");

        _logger.LogInformation("✅ 成功创建会员记录: {MembershipId}", created.Id);
        return created;
    }

    public async Task<Membership> UpdateAsync(Membership membership, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 更新会员记录: {MembershipId}", membership.Id);

        membership.UpdatedAt = DateTime.UtcNow;

        var result = await _supabaseClient
            .From<Membership>()
            .Where(m => m.Id == membership.Id)
            .Update(membership, cancellationToken: cancellationToken);

        var updated = result.Models.FirstOrDefault();
        if (updated == null) throw new InvalidOperationException("更新会员记录失败");

        return updated;
    }

    public async Task<List<Membership>> GetExpiringMembershipsAsync(int daysBeforeExpiry = 7, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 查询即将过期的会员 (未来 {Days} 天)", daysBeforeExpiry);

        var now = DateTime.UtcNow;
        var expiryThreshold = now.AddDays(daysBeforeExpiry);

        var response = await _supabaseClient
            .From<Membership>()
            .Where(m => m.Level > 0) // 付费会员
            .Filter("expiry_date", Postgrest.Constants.Operator.GreaterThan, now.ToString("o"))
            .Filter("expiry_date", Postgrest.Constants.Operator.LessThanOrEqual, expiryThreshold.ToString("o"))
            .Get(cancellationToken);

        return response.Models;
    }

    public async Task<List<Membership>> GetAutoRenewMembershipsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 查询需要自动续费的会员");

        var now = DateTime.UtcNow;
        var tomorrow = now.AddDays(1);

        var response = await _supabaseClient
            .From<Membership>()
            .Where(m => m.AutoRenew == true)
            .Where(m => m.Level > 0)
            .Filter("expiry_date", Postgrest.Constants.Operator.GreaterThan, now.ToString("o"))
            .Filter("expiry_date", Postgrest.Constants.Operator.LessThanOrEqual, tomorrow.ToString("o"))
            .Get(cancellationToken);

        return response.Models;
    }

    public async Task<List<Membership>> GetExpiredMembershipsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 查询已过期的会员");

        var now = DateTime.UtcNow;

        var response = await _supabaseClient
            .From<Membership>()
            .Where(m => m.Level > 0)
            .Filter("expiry_date", Postgrest.Constants.Operator.LessThan, now.ToString("o"))
            .Get(cancellationToken);

        return response.Models;
    }
}
