using System.Text.Json;
using CacheService.Domain.Entities;
using CacheService.Domain.Repositories;
using StackExchange.Redis;

namespace CacheService.Infrastructure.Repositories;

/// <summary>
/// Redis 分数缓存仓储实现
/// </summary>
public class RedisScoreCacheRepository : IScoreCacheRepository
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly ILogger<RedisScoreCacheRepository> _logger;

    public RedisScoreCacheRepository(
        IConnectionMultiplexer redis,
        ILogger<RedisScoreCacheRepository> logger)
    {
        _redis = redis;
        _database = redis.GetDatabase();
        _logger = logger;
    }

    public async Task<ScoreCache?> GetAsync(ScoreEntityType entityType, string entityId)
    {
        try
        {
            var key = ScoreCache.GenerateKey(entityType, entityId);
            var value = await _database.StringGetAsync(key);
            
            if (value.IsNullOrEmpty)
            {
                return null;
            }

            return JsonSerializer.Deserialize<ScoreCacheData>(value!)?.ToEntity(entityType, entityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting score cache for {EntityType}:{EntityId}", entityType, entityId);
            return null;
        }
    }

    public async Task<Dictionary<string, ScoreCache>> GetBatchAsync(ScoreEntityType entityType, IEnumerable<string> entityIds)
    {
        var result = new Dictionary<string, ScoreCache>();
        var entityIdList = entityIds.ToList();

        try
        {
            var keys = entityIdList.Select(id => (RedisKey)ScoreCache.GenerateKey(entityType, id)).ToArray();
            var values = await _database.StringGetAsync(keys);

            for (int i = 0; i < entityIdList.Count; i++)
            {
                if (!values[i].IsNullOrEmpty)
                {
                    var cache = JsonSerializer.Deserialize<ScoreCacheData>(values[i]!)?.ToEntity(entityType, entityIdList[i]);
                    if (cache != null)
                    {
                        result[entityIdList[i]] = cache;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting batch score cache for {EntityType}", entityType);
        }

        return result;
    }

    public async Task SetAsync(ScoreCache scoreCache)
    {
        try
        {
            var key = scoreCache.Key;
            var data = ScoreCacheData.FromEntity(scoreCache);
            var value = JsonSerializer.Serialize(data);
            var expiry = scoreCache.ExpiresAt - DateTime.UtcNow;

            await _database.StringSetAsync(key, value, expiry);
            
            _logger.LogInformation("Set score cache: {Key}, Score: {Score}, TTL: {Ttl}s", 
                key, scoreCache.OverallScore, expiry.TotalSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting score cache for key: {Key}", scoreCache.Key);
        }
    }

    public async Task SetBatchAsync(IEnumerable<ScoreCache> scoreCaches)
    {
        var cacheList = scoreCaches.ToList();
        if (!cacheList.Any())
        {
            return;
        }

        try
        {
            var batch = _database.CreateBatch();
            var tasks = new List<Task>();

            foreach (var scoreCache in cacheList)
            {
                var key = scoreCache.Key;
                var data = ScoreCacheData.FromEntity(scoreCache);
                var value = JsonSerializer.Serialize(data);
                var expiry = scoreCache.ExpiresAt - DateTime.UtcNow;

                tasks.Add(batch.StringSetAsync(key, value, expiry));
            }

            batch.Execute();
            await Task.WhenAll(tasks);

            _logger.LogInformation("Set {Count} score caches in batch", cacheList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting batch score cache");
        }
    }

    public async Task InvalidateAsync(ScoreEntityType entityType, string entityId)
    {
        try
        {
            var key = ScoreCache.GenerateKey(entityType, entityId);
            await _database.KeyDeleteAsync(key);
            
            _logger.LogInformation("Invalidated score cache: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating score cache for {EntityType}:{EntityId}", entityType, entityId);
        }
    }

    public async Task InvalidateBatchAsync(ScoreEntityType entityType, IEnumerable<string> entityIds)
    {
        var entityIdList = entityIds.ToList();
        if (!entityIdList.Any())
        {
            return;
        }

        try
        {
            var keys = entityIdList.Select(id => (RedisKey)ScoreCache.GenerateKey(entityType, id)).ToArray();
            await _database.KeyDeleteAsync(keys);
            
            _logger.LogInformation("Invalidated {Count} score caches for {EntityType}", entityIdList.Count, entityType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating batch score cache for {EntityType}", entityType);
        }
    }

    public async Task<bool> ExistsAsync(ScoreEntityType entityType, string entityId)
    {
        try
        {
            var key = ScoreCache.GenerateKey(entityType, entityId);
            return await _database.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence for {EntityType}:{EntityId}", entityType, entityId);
            return false;
        }
    }
}

/// <summary>
/// Redis 存储的缓存数据结构
/// </summary>
internal class ScoreCacheData
{
    public decimal OverallScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? Statistics { get; set; }

    public static ScoreCacheData FromEntity(ScoreCache entity)
    {
        return new ScoreCacheData
        {
            OverallScore = entity.OverallScore,
            CreatedAt = entity.CreatedAt,
            ExpiresAt = entity.ExpiresAt,
            Statistics = entity.Statistics
        };
    }

    public ScoreCache ToEntity(ScoreEntityType entityType, string entityId)
    {
        var ttl = ExpiresAt - DateTime.UtcNow;
        if (ttl < TimeSpan.Zero)
        {
            ttl = TimeSpan.Zero;
        }

        return new ScoreCache(entityType, entityId, OverallScore, ttl, Statistics);
    }
}
