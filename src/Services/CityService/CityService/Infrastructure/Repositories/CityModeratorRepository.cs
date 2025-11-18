using CityService.Domain.Entities;
using CityService.Domain.Repositories;
using Postgrest;
using Client = Supabase.Client;

namespace CityService.Infrastructure.Repositories;

/// <summary>
///     城市版主仓储实现
/// </summary>
public class CityModeratorRepository : ICityModeratorRepository
{
    private readonly ILogger<CityModeratorRepository> _logger;
    private readonly Client _supabaseClient;

    public CityModeratorRepository(
        Client supabaseClient,
        ILogger<CityModeratorRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<List<CityModerator>> GetByCityIdAsync(Guid cityId, bool activeOnly = true)
    {
        _logger.LogInformation("📋 查询城市版主 - CityId: {CityId}, ActiveOnly: {ActiveOnly}", cityId, activeOnly);

        try
        {
            var query = _supabaseClient
                .From<CityModerator>()
                .Where(m => m.CityId == cityId);

            if (activeOnly) query = query.Where(m => m.IsActive == true);

            var response = await query
                .Order(m => m.CreatedAt, Constants.Ordering.Descending)
                .Get();

            _logger.LogInformation("✅ 找到 {Count} 个版主", response.Models.Count);
            return response.Models.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 查询城市版主失败 - CityId: {CityId}", cityId);
            throw;
        }
    }

    /// <summary>
    ///     批量获取多个城市的版主（优化 N+1 查询）
    /// </summary>
    public async Task<List<CityModerator>> GetByCityIdsAsync(List<Guid> cityIds, bool activeOnly = true)
    {
        if (cityIds == null || cityIds.Count == 0) return new List<CityModerator>();

        _logger.LogDebug("📋 批量查询城市版主 - CityIds: {Count} 个, ActiveOnly: {ActiveOnly}",
            cityIds.Count, activeOnly);

        try
        {
            // 由于 Supabase 的限制，使用简化方法：分批查询
            // 对于大量数据，分批处理
            const int batchSize = 50; // 每批最多 50 个
            var allModerators = new List<CityModerator>();

            var batches = cityIds
                .Select((id, index) => new { id, index })
                .GroupBy(x => x.index / batchSize)
                .Select(g => g.Select(x => x.id).ToList())
                .ToList();

            foreach (var batch in batches)
            {
                // 对每个批次逐个查询（这里还是需要优化，但比之前好）
                var batchTasks = batch.Select(cityId => GetByCityIdAsync(cityId, activeOnly));
                var batchResults = await Task.WhenAll(batchTasks);

                foreach (var result in batchResults) allModerators.AddRange(result);
            }

            _logger.LogInformation("✅ 批量查询完成: 找到 {Count} 个版主", allModerators.Count);
            return allModerators;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 批量查询城市版主失败");
            throw;
        }
    }

    public async Task<List<CityModerator>> GetByUserIdAsync(Guid userId, bool activeOnly = true)
    {
        _logger.LogInformation("📋 查询用户管理的城市 - UserId: {UserId}, ActiveOnly: {ActiveOnly}", userId, activeOnly);

        try
        {
            var query = _supabaseClient
                .From<CityModerator>()
                .Where(m => m.UserId == userId);

            if (activeOnly) query = query.Where(m => m.IsActive == true);

            var response = await query
                .Order(m => m.CreatedAt, Constants.Ordering.Descending)
                .Get();

            _logger.LogInformation("✅ 找到 {Count} 个城市", response.Models.Count);
            return response.Models.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 查询用户管理的城市失败 - UserId: {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> IsModeratorAsync(Guid cityId, Guid userId)
    {
        _logger.LogInformation("🔍 检查版主权限 - CityId: {CityId}, UserId: {UserId}", cityId, userId);

        try
        {
            var response = await _supabaseClient
                .From<CityModerator>()
                .Where(m => m.CityId == cityId)
                .Where(m => m.UserId == userId)
                .Where(m => m.IsActive == true)
                .Get();

            var isModerator = response.Models.Any();
            _logger.LogInformation("✅ 版主检查结果: {IsModerator}", isModerator);
            return isModerator;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 检查版主权限失败");
            return false;
        }
    }

    public async Task<CityModerator> AddAsync(CityModerator moderator)
    {
        _logger.LogInformation("➕ 添加版主 - CityId: {CityId}, UserId: {UserId}",
            moderator.CityId, moderator.UserId);

        try
        {
            moderator.Id = Guid.NewGuid();
            moderator.CreatedAt = DateTime.UtcNow;
            moderator.UpdatedAt = DateTime.UtcNow;

            var response = await _supabaseClient
                .From<CityModerator>()
                .Insert(moderator);

            var inserted = response.Models.FirstOrDefault();
            if (inserted == null) throw new Exception("插入版主记录失败");

            _logger.LogInformation("✅ 版主添加成功 - Id: {Id}", inserted.Id);
            return inserted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 添加版主失败");
            throw;
        }
    }

    public async Task<bool> UpdateAsync(CityModerator moderator)
    {
        _logger.LogInformation("✏️ 更新版主信息 - Id: {Id}", moderator.Id);

        try
        {
            moderator.UpdatedAt = DateTime.UtcNow;

            await _supabaseClient
                .From<CityModerator>()
                .Where(m => m.Id == moderator.Id)
                .Update(moderator);

            _logger.LogInformation("✅ 版主信息更新成功");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新版主信息失败 - Id: {Id}", moderator.Id);
            return false;
        }
    }

    public async Task<bool> RemoveAsync(Guid cityId, Guid userId)
    {
        _logger.LogInformation("🗑️ 删除版主 - CityId: {CityId}, UserId: {UserId}", cityId, userId);

        try
        {
            // 软删除：设置为不激活
            var moderator = await _supabaseClient
                .From<CityModerator>()
                .Where(m => m.CityId == cityId)
                .Where(m => m.UserId == userId)
                .Single();

            if (moderator != null)
            {
                moderator.IsActive = false;
                moderator.UpdatedAt = DateTime.UtcNow;

                await _supabaseClient
                    .From<CityModerator>()
                    .Where(m => m.Id == moderator.Id)
                    .Update(moderator);

                _logger.LogInformation("✅ 版主已移除（软删除）");
                return true;
            }

            _logger.LogWarning("⚠️ 版主记录不存在");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除版主失败");
            return false;
        }
    }

    public async Task<CityModerator?> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("📋 查询版主记录 - Id: {Id}", id);

        try
        {
            var response = await _supabaseClient
                .From<CityModerator>()
                .Where(m => m.Id == id)
                .Single();

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 查询版主记录失败 - Id: {Id}", id);
            return null;
        }
    }
}