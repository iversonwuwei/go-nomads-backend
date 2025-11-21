using System.Text.Json;
using CacheService.Domain.Entities;
using CacheService.Domain.Repositories;
using StackExchange.Redis;

namespace CacheService.Infrastructure.Repositories;

/// <summary>
/// Redis 费用缓存仓储实现
/// </summary>
public class RedisCostCacheRepository : ICostCacheRepository
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly ILogger<RedisCostCacheRepository> _logger;

    public RedisCostCacheRepository(
        IConnectionMultiplexer redis,
        ILogger<RedisCostCacheRepository> logger)
    {
        _redis = redis;
        _database = redis.GetDatabase();
        _logger = logger;
    }

    public async Task<CostCache?> GetAsync(CostEntityType entityType, string entityId)
    {
        try
        {
            var key = CostCache.GenerateKey(entityType, entityId);
            var value = await _database.StringGetAsync(key);
            
            if (value.IsNullOrEmpty)
            {
                return null;
            }

            return JsonSerializer.Deserialize<CostCacheData>(value!)?.ToEntity(entityType, entityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cost cache for {EntityType}:{EntityId}", entityType, entityId);
            return null;
        }
    }

    public async Task<Dictionary<string, CostCache>> GetBatchAsync(CostEntityType entityType, IEnumerable<string> entityIds)
    {
        var result = new Dictionary<string, CostCache>();
        var entityIdList = entityIds.ToList();

        try
        {
            var keys = entityIdList.Select(id => (RedisKey)CostCache.GenerateKey(entityType, id)).ToArray();
            var values = await _database.StringGetAsync(keys);

            for (int i = 0; i < entityIdList.Count; i++)
            {
                if (!values[i].IsNullOrEmpty)
                {
                    var cache = JsonSerializer.Deserialize<CostCacheData>(values[i]!)?.ToEntity(entityType, entityIdList[i]);
                    if (cache != null)
                    {
                        result[entityIdList[i]] = cache;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting batch cost cache for {EntityType}", entityType);
        }

        return result;
    }

    public async Task SetAsync(CostCache costCache)
    {
        try
        {
            var key = costCache.Key;
            var data = CostCacheData.FromEntity(costCache);
            var value = JsonSerializer.Serialize(data);
            var expiry = costCache.ExpiresAt - DateTime.UtcNow;

            await _database.StringSetAsync(key, value, expiry);
            
            _logger.LogInformation("Set cost cache: {Key}, Cost: {Cost}, TTL: {Ttl}s", 
                key, costCache.AverageCost, expiry.TotalSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cost cache for key: {Key}", costCache.Key);
        }
    }

    public async Task SetBatchAsync(IEnumerable<CostCache> costCaches)
    {
        var cacheList = costCaches.ToList();
        if (!cacheList.Any())
        {
            return;
        }

        try
        {
            var batch = _database.CreateBatch();
            var tasks = new List<Task>();

            foreach (var cache in cacheList)
            {
                var key = cache.Key;
                var data = CostCacheData.FromEntity(cache);
                var value = JsonSerializer.Serialize(data);
                var expiry = cache.ExpiresAt - DateTime.UtcNow;

                tasks.Add(batch.StringSetAsync(key, value, expiry));
            }

            batch.Execute();
            await Task.WhenAll(tasks);

            _logger.LogInformation("Set {Count} cost caches in batch", cacheList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting batch cost cache");
        }
    }

    public async Task InvalidateAsync(CostEntityType entityType, string entityId)
    {
        try
        {
            var key = CostCache.GenerateKey(entityType, entityId);
            await _database.KeyDeleteAsync(key);
            
            _logger.LogInformation("Invalidated cost cache: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cost cache for {EntityType}:{EntityId}", entityType, entityId);
        }
    }

    public async Task<bool> ExistsAsync(CostEntityType entityType, string entityId)
    {
        try
        {
            var key = CostCache.GenerateKey(entityType, entityId);
            return await _database.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cost cache existence for {EntityType}:{EntityId}", entityType, entityId);
            return false;
        }
    }
}

/// <summary>
/// 费用缓存数据传输对象 (用于 JSON 序列化)
/// </summary>
internal class CostCacheData
{
    public decimal AverageCost { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? Statistics { get; set; }

    public static CostCacheData FromEntity(CostCache cache)
    {
        return new CostCacheData
        {
            AverageCost = cache.AverageCost,
            CreatedAt = cache.CreatedAt,
            ExpiresAt = cache.ExpiresAt,
            Statistics = cache.Statistics
        };
    }

    public CostCache ToEntity(CostEntityType entityType, string entityId)
    {
        var ttl = ExpiresAt - CreatedAt;
        return new CostCache(entityType, entityId, AverageCost, ttl, Statistics);
    }
}
