namespace AIService.Infrastructure.Cache;

/// <summary>
///     Redis 缓存服务接口
/// </summary>
public interface IRedisCache
{
    Task<T?> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class;
    Task<bool> ExistsAsync(string key);
    Task<bool> DeleteAsync(string key);
    Task<string?> GetStringAsync(string key);
    Task SetStringAsync(string key, string value, TimeSpan? expiry = null);
}