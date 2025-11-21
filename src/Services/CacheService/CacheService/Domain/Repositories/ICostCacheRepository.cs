using CacheService.Domain.Entities;

namespace CacheService.Domain.Repositories;

/// <summary>
/// 费用缓存仓储接口
/// </summary>
public interface ICostCacheRepository
{
    /// <summary>
    /// 获取缓存的费用
    /// </summary>
    Task<CostCache?> GetAsync(CostEntityType entityType, string entityId);

    /// <summary>
    /// 批量获取缓存的费用
    /// </summary>
    Task<Dictionary<string, CostCache>> GetBatchAsync(CostEntityType entityType, IEnumerable<string> entityIds);

    /// <summary>
    /// 设置缓存的费用
    /// </summary>
    Task SetAsync(CostCache costCache);

    /// <summary>
    /// 批量设置缓存的费用
    /// </summary>
    Task SetBatchAsync(IEnumerable<CostCache> costCaches);

    /// <summary>
    /// 删除缓存
    /// </summary>
    Task InvalidateAsync(CostEntityType entityType, string entityId);

    /// <summary>
    /// 检查缓存是否存在
    /// </summary>
    Task<bool> ExistsAsync(CostEntityType entityType, string entityId);
}
