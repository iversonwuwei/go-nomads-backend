using UserService.Domain.Entities;
using UserService.Domain.Repositories;
using Client = Supabase.Client;

namespace UserService.Infrastructure.Repositories;

/// <summary>
///     UserStats 仓储 Supabase 实现
/// </summary>
public class UserStatsRepository : IUserStatsRepository
{
    private readonly ILogger<UserStatsRepository> _logger;
    private readonly Client _supabaseClient;

    public UserStatsRepository(Client supabaseClient, ILogger<UserStatsRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<UserStats> CreateAsync(UserStats stats, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 创建用户统计数据: UserId={UserId}", stats.UserId);

        try
        {
            var result = await _supabaseClient
                .From<UserStats>()
                .Insert(stats, cancellationToken: cancellationToken);

            var createdStats = result.Models.FirstOrDefault();
            if (createdStats == null) 
                throw new InvalidOperationException("创建用户统计数据失败");

            _logger.LogInformation("✅ 成功创建用户统计数据: {StatsId}", createdStats.Id);
            return createdStats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建用户统计数据失败: UserId={UserId}", stats.UserId);
            throw;
        }
    }

    public async Task<UserStats?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 根据用户ID查询统计数据: {UserId}", userId);

        try
        {
            var response = await _supabaseClient
                .From<UserStats>()
                .Where(s => s.UserId == userId)
                .Single(cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "⚠️ 未找到用户统计数据: {UserId}", userId);
            return null;
        }
    }

    public async Task<UserStats> UpdateAsync(UserStats stats, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 更新用户统计数据: UserId={UserId}", stats.UserId);

        try
        {
            var result = await _supabaseClient
                .From<UserStats>()
                .Where(s => s.Id == stats.Id)
                .Update(stats, cancellationToken: cancellationToken);

            var updatedStats = result.Models.FirstOrDefault();
            if (updatedStats == null)
                throw new InvalidOperationException("更新用户统计数据失败");

            _logger.LogInformation("✅ 成功更新用户统计数据: {StatsId}", updatedStats.Id);
            return updatedStats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新用户统计数据失败: UserId={UserId}", stats.UserId);
            throw;
        }
    }

    public async Task<UserStats> GetOrCreateAsync(string userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 获取或创建用户统计数据: UserId={UserId}", userId);

        // 先尝试获取
        var existingStats = await GetByUserIdAsync(userId, cancellationToken);
        if (existingStats != null)
        {
            _logger.LogInformation("✅ 找到已有用户统计数据: {StatsId}", existingStats.Id);
            return existingStats;
        }

        // 不存在则创建
        _logger.LogInformation("📝 用户统计数据不存在，创建新记录: UserId={UserId}", userId);
        var newStats = UserStats.CreateForUser(userId);
        return await CreateAsync(newStats, cancellationToken);
    }
}
