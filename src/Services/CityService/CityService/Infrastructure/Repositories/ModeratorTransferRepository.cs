using CityService.Domain.Entities;
using CityService.Domain.Repositories;
using Supabase;

namespace CityService.Infrastructure.Repositories;

/// <summary>
///     版主转让仓储实现
/// </summary>
public class ModeratorTransferRepository : IModeratorTransferRepository
{
    private readonly ILogger<ModeratorTransferRepository> _logger;
    private readonly Client _supabaseClient;

    public ModeratorTransferRepository(
        Client supabaseClient,
        ILogger<ModeratorTransferRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<ModeratorTransfer> CreateAsync(ModeratorTransfer transfer)
    {
        try
        {
            var result = await _supabaseClient
                .From<ModeratorTransfer>()
                .Insert(transfer);

            var created = result.Models.FirstOrDefault();
            if (created == null) throw new InvalidOperationException("创建转让请求失败");

            _logger.LogInformation("✅ 版主转让请求创建成功: FromUserId={FromUserId}, ToUserId={ToUserId}, CityId={CityId}",
                transfer.FromUserId, transfer.ToUserId, transfer.CityId);
            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建版主转让请求失败");
            throw;
        }
    }

    public async Task<ModeratorTransfer> UpdateAsync(ModeratorTransfer transfer)
    {
        try
        {
            transfer.UpdatedAt = DateTime.UtcNow;

            var result = await _supabaseClient
                .From<ModeratorTransfer>()
                .Where(t => t.Id == transfer.Id)
                .Update(transfer);

            var updated = result.Models.FirstOrDefault();
            if (updated == null) throw new InvalidOperationException("更新转让请求失败");

            _logger.LogInformation("✅ 版主转让请求更新成功: Id={Id}, Status={Status}",
                transfer.Id, transfer.Status);
            return updated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新版主转让请求失败: Id={Id}", transfer.Id);
            throw;
        }
    }

    public async Task<ModeratorTransfer?> GetByIdAsync(Guid id)
    {
        try
        {
            var result = await _supabaseClient
                .From<ModeratorTransfer>()
                .Where(t => t.Id == id)
                .Get();

            return result.Models.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取版主转让请求失败: Id={Id}", id);
            throw;
        }
    }

    public async Task<List<ModeratorTransfer>> GetByFromUserIdAsync(Guid userId)
    {
        try
        {
            var result = await _supabaseClient
                .From<ModeratorTransfer>()
                .Where(t => t.FromUserId == userId)
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Get();

            return result.Models.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取用户发起的转让请求列表失败: UserId={UserId}", userId);
            throw;
        }
    }

    public async Task<List<ModeratorTransfer>> GetByToUserIdAsync(Guid userId)
    {
        try
        {
            var result = await _supabaseClient
                .From<ModeratorTransfer>()
                .Where(t => t.ToUserId == userId)
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Get();

            return result.Models.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取用户收到的转让请求列表失败: UserId={UserId}", userId);
            throw;
        }
    }

    public async Task<List<ModeratorTransfer>> GetByCityIdAsync(Guid cityId)
    {
        try
        {
            var result = await _supabaseClient
                .From<ModeratorTransfer>()
                .Where(t => t.CityId == cityId)
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Get();

            return result.Models.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取城市的转让请求列表失败: CityId={CityId}", cityId);
            throw;
        }
    }

    public async Task<List<ModeratorTransfer>> GetPendingTransfersForUserAsync(Guid userId)
    {
        try
        {
            var result = await _supabaseClient
                .From<ModeratorTransfer>()
                .Where(t => t.ToUserId == userId)
                .Filter("status", Postgrest.Constants.Operator.Equals, "pending")
                .Filter("expires_at", Postgrest.Constants.Operator.GreaterThan, DateTime.UtcNow.ToString("o"))
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Get();

            return result.Models.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取用户待处理的转让请求失败: UserId={UserId}", userId);
            throw;
        }
    }

    public async Task<bool> HasPendingTransferAsync(Guid cityId, Guid toUserId)
    {
        try
        {
            var result = await _supabaseClient
                .From<ModeratorTransfer>()
                .Where(t => t.CityId == cityId && t.ToUserId == toUserId)
                .Filter("status", Postgrest.Constants.Operator.Equals, "pending")
                .Filter("expires_at", Postgrest.Constants.Operator.GreaterThan, DateTime.UtcNow.ToString("o"))
                .Get();

            return result.Models.Any();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 检查是否存在待处理转让请求失败: CityId={CityId}, ToUserId={ToUserId}", cityId, toUserId);
            throw;
        }
    }

    public async Task<int> ExpirePendingTransfersAsync()
    {
        try
        {
            // 获取所有已过期但仍为 pending 状态的转让请求
            var expiredTransfers = await _supabaseClient
                .From<ModeratorTransfer>()
                .Filter("status", Postgrest.Constants.Operator.Equals, "pending")
                .Filter("expires_at", Postgrest.Constants.Operator.LessThanOrEqual, DateTime.UtcNow.ToString("o"))
                .Get();

            var count = 0;
            foreach (var transfer in expiredTransfers.Models)
            {
                transfer.Status = "expired";
                transfer.UpdatedAt = DateTime.UtcNow;
                await _supabaseClient
                    .From<ModeratorTransfer>()
                    .Where(t => t.Id == transfer.Id)
                    .Update(transfer);
                count++;
            }

            if (count > 0)
            {
                _logger.LogInformation("✅ 已将 {Count} 个转让请求标记为过期", count);
            }

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 使过期转让请求失效失败");
            throw;
        }
    }
}
