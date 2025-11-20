using CacheService.Domain.Entities;

namespace CacheService.Domain.Repositories;

/// <summary>
/// 分数缓存仓储接口
/// </summary>
public interface IScoreCacheRepository
{
    /// <summary>
    /// 获取缓存的分数
    /// </summary>
    Task<ScoreCache?> GetAsync(ScoreEntityType entityType, string entityId);

    /// <summary>
    /// 批量获取缓存的分数
    /// </summary>
    Task<Dictionary<string, ScoreCache>> GetBatchAsync(ScoreEntityType entityType, IEnumerable<string> entityIds);

    /// <summary>
    /// 设置缓存的分数
    /// </summary>
    Task SetAsync(ScoreCache scoreCache);

    /// <summary>
    /// 批量设置缓存的分数
    /// </summary>
    Task SetBatchAsync(IEnumerable<ScoreCache> scoreCaches);

    /// <summary>
    /// 删除缓存
    /// </summary>
    Task InvalidateAsync(ScoreEntityType entityType, string entityId);

    /// <summary>
    /// 批量删除缓存
    /// </summary>
    Task InvalidateBatchAsync(ScoreEntityType entityType, IEnumerable<string> entityIds);

    /// <summary>
    /// 检查缓存是否存在
    /// </summary>
    Task<bool> ExistsAsync(ScoreEntityType entityType, string entityId);
}
