using System.Text.Json;
using StackExchange.Redis;

namespace AIService.Infrastructure.Cache;

/// <summary>
///     Redis 缓存服务实现（使用 Aspire 注入的 IConnectionMultiplexer）
/// </summary>
public class RedisCache : IRedisCache
{
    private readonly IDatabase _db;
    private readonly ILogger<RedisCache> _logger;
    private readonly IConnectionMultiplexer _redis;

    public RedisCache(IConnectionMultiplexer redis, ILogger<RedisCache> logger)
    {
        _logger = logger;
        _redis = redis;
        _db = _redis.GetDatabase();
        _logger.LogInformation("✅ Redis 缓存服务已初始化（通过 Aspire 集成）");
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            var value = await _db.StringGetAsync(key);
            if (value.IsNullOrEmpty) return null;

            return JsonSerializer.Deserialize<T>(value!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [Redis] 获取缓存失败: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            await _db.StringSetAsync(key, json, expiry);

            _logger.LogDebug("📝 [Redis] 设置缓存: {Key}, 过期时间: {Expiry}", key, expiry?.ToString() ?? "永久");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [Redis] 设置缓存失败: {Key}", key);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            return await _db.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [Redis] 检查键存在失败: {Key}", key);
            return false;
        }
    }

    public async Task<bool> DeleteAsync(string key)
    {
        try
        {
            return await _db.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [Redis] 删除键失败: {Key}", key);
            return false;
        }
    }

    public async Task<string?> GetStringAsync(string key)
    {
        try
        {
            var value = await _db.StringGetAsync(key);
            return value.IsNullOrEmpty ? null : value.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [Redis] 获取字符串失败: {Key}", key);
            return null;
        }
    }

    public async Task SetStringAsync(string key, string value, TimeSpan? expiry = null)
    {
        try
        {
            await _db.StringSetAsync(key, value, expiry);
            _logger.LogDebug("📝 [Redis] 设置字符串: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [Redis] 设置字符串失败: {Key}", key);
            throw;
        }
    }
}