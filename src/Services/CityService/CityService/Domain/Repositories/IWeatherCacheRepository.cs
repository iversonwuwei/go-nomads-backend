using CityService.Domain.Entities;

namespace CityService.Domain.Repositories;

/// <summary>
///     天气缓存仓储接口
/// </summary>
public interface IWeatherCacheRepository
{
    /// <summary>
    ///     根据城市ID获取有效的天气缓存
    /// </summary>
    Task<WeatherCache?> GetValidCacheByCityIdAsync(Guid cityId);

    /// <summary>
    ///     根据多个城市ID批量获取有效的天气缓存
    /// </summary>
    Task<Dictionary<Guid, WeatherCache>> GetValidCacheByIdsAsync(IEnumerable<Guid> cityIds);

    /// <summary>
    ///     保存或更新天气缓存（Upsert）
    /// </summary>
    Task<WeatherCache> UpsertAsync(WeatherCache weatherCache);

    /// <summary>
    ///     批量保存或更新天气缓存
    /// </summary>
    Task<List<WeatherCache>> UpsertBatchAsync(IEnumerable<WeatherCache> weatherCaches);

    /// <summary>
    ///     删除指定城市的天气缓存
    /// </summary>
    Task<bool> DeleteByCityIdAsync(Guid cityId);

    /// <summary>
    ///     清理所有过期的缓存（过期超过指定时长）
    /// </summary>
    Task<int> CleanExpiredCacheAsync(TimeSpan? olderThan = null);

    /// <summary>
    ///     获取缓存统计信息
    /// </summary>
    Task<WeatherCacheStats> GetCacheStatsAsync();

    /// <summary>
    ///     检查城市是否有有效缓存
    /// </summary>
    Task<bool> HasValidCacheAsync(Guid cityId);
}

/// <summary>
///     天气缓存统计信息
/// </summary>
public class WeatherCacheStats
{
    /// <summary>
    ///     总缓存数量
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    ///     有效缓存数量
    /// </summary>
    public int ValidCount { get; set; }

    /// <summary>
    ///     过期缓存数量
    /// </summary>
    public int ExpiredCount { get; set; }

    /// <summary>
    ///     最旧缓存的年龄（小时）
    /// </summary>
    public double OldestCacheAgeHours { get; set; }

    /// <summary>
    ///     最新缓存的年龄（分钟）
    /// </summary>
    public double NewestCacheAgeMinutes { get; set; }
}
