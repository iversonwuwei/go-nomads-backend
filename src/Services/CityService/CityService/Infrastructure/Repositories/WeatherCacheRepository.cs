using CityService.Domain.Entities;
using CityService.Domain.Repositories;
using Postgrest;
using Shared.Repositories;
using Supabase;
using Client = Supabase.Client;

namespace CityService.Infrastructure.Repositories;

/// <summary>
///     天气缓存仓储实现（基于 Supabase）
/// </summary>
public class WeatherCacheRepository : SupabaseRepositoryBase<WeatherCache>, IWeatherCacheRepository
{
    public WeatherCacheRepository(Client supabaseClient, ILogger<WeatherCacheRepository> logger)
        : base(supabaseClient, logger)
    {
    }

    /// <summary>
    ///     根据城市ID获取有效的天气缓存
    /// </summary>
    public async Task<WeatherCache?> GetValidCacheByCityIdAsync(Guid cityId)
    {
        try
        {
            var response = await SupabaseClient
                .From<WeatherCache>()
                .Where(x => x.CityId == cityId)
                .Where(x => x.ExpiredAt > DateTime.UtcNow)
                .Single();

            return response;
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "未找到城市 {CityId} 的有效天气缓存", cityId);
            return null;
        }
    }

    /// <summary>
    ///     根据多个城市ID批量获取有效的天气缓存
    /// </summary>
    public async Task<Dictionary<Guid, WeatherCache>> GetValidCacheByIdsAsync(IEnumerable<Guid> cityIds)
    {
        var cityIdList = cityIds.ToList();
        if (!cityIdList.Any())
            return new Dictionary<Guid, WeatherCache>();

        try
        {
            var response = await SupabaseClient
                .From<WeatherCache>()
                .Filter("city_id", Constants.Operator.In, cityIdList.Select(id => id.ToString()).ToList())
                .Filter("expired_at", Constants.Operator.GreaterThan, DateTime.UtcNow.ToString("O"))
                .Get();

            return response.Models.ToDictionary(w => w.CityId, w => w);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "批量获取天气缓存失败，城市数量: {Count}", cityIdList.Count);
            return new Dictionary<Guid, WeatherCache>();
        }
    }

    /// <summary>
    ///     保存或更新天气缓存（Upsert）
    /// </summary>
    public async Task<WeatherCache> UpsertAsync(WeatherCache weatherCache)
    {
        try
        {
            weatherCache.UpdatedAt = DateTime.UtcNow;

            var response = await SupabaseClient
                .From<WeatherCache>()
                .Upsert(weatherCache);

            Logger.LogDebug("已保存城市 {CityId} 的天气缓存，过期时间: {ExpiredAt}",
                weatherCache.CityId, weatherCache.ExpiredAt);

            return response.Models.First();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "保存天气缓存失败，城市: {CityId}", weatherCache.CityId);
            throw;
        }
    }

    /// <summary>
    ///     批量保存或更新天气缓存
    /// </summary>
    public async Task<List<WeatherCache>> UpsertBatchAsync(IEnumerable<WeatherCache> weatherCaches)
    {
        var cacheList = weatherCaches.ToList();
        if (!cacheList.Any())
            return new List<WeatherCache>();

        try
        {
            var now = DateTime.UtcNow;
            foreach (var cache in cacheList)
            {
                cache.UpdatedAt = now;
            }

            var response = await SupabaseClient
                .From<WeatherCache>()
                .Upsert(cacheList);

            Logger.LogInformation("✅ 批量保存天气缓存成功，数量: {Count}", cacheList.Count);

            return response.Models;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "批量保存天气缓存失败，数量: {Count}", cacheList.Count);
            throw;
        }
    }

    /// <summary>
    ///     删除指定城市的天气缓存
    /// </summary>
    public async Task<bool> DeleteByCityIdAsync(Guid cityId)
    {
        try
        {
            await SupabaseClient
                .From<WeatherCache>()
                .Where(x => x.CityId == cityId)
                .Delete();

            Logger.LogDebug("已删除城市 {CityId} 的天气缓存", cityId);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "删除天气缓存失败，城市: {CityId}", cityId);
            return false;
        }
    }

    /// <summary>
    ///     清理所有过期的缓存（过期超过指定时长）
    /// </summary>
    public async Task<int> CleanExpiredCacheAsync(TimeSpan? olderThan = null)
    {
        try
        {
            var threshold = olderThan.HasValue
                ? DateTime.UtcNow - olderThan.Value
                : DateTime.UtcNow;

            // 先查询要删除的记录数量
            var toDeleteResponse = await SupabaseClient
                .From<WeatherCache>()
                .Filter("expired_at", Constants.Operator.LessThan, threshold.ToString("O"))
                .Get();

            var count = toDeleteResponse.Models.Count;

            if (count > 0)
            {
                // 执行删除
                await SupabaseClient
                    .From<WeatherCache>()
                    .Filter("expired_at", Constants.Operator.LessThan, threshold.ToString("O"))
                    .Delete();

                Logger.LogInformation("🧹 清理过期天气缓存完成，删除数量: {Count}", count);
            }

            return count;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "清理过期天气缓存失败");
            return 0;
        }
    }

    /// <summary>
    ///     获取缓存统计信息
    /// </summary>
    public async Task<WeatherCacheStats> GetCacheStatsAsync()
    {
        try
        {
            var now = DateTime.UtcNow;

            // 获取所有缓存
            var allCaches = await SupabaseClient
                .From<WeatherCache>()
                .Get();

            var caches = allCaches.Models.ToList();
            var validCaches = caches.Where(c => c.ExpiredAt > now).ToList();
            var expiredCaches = caches.Where(c => c.ExpiredAt <= now).ToList();

            var stats = new WeatherCacheStats
            {
                TotalCount = caches.Count,
                ValidCount = validCaches.Count,
                ExpiredCount = expiredCaches.Count,
                OldestCacheAgeHours = caches.Any()
                    ? (now - caches.Min(c => c.UpdatedAt)).TotalHours
                    : 0,
                NewestCacheAgeMinutes = caches.Any()
                    ? (now - caches.Max(c => c.UpdatedAt)).TotalMinutes
                    : 0
            };

            Logger.LogDebug("天气缓存统计 - 总数: {Total}, 有效: {Valid}, 过期: {Expired}",
                stats.TotalCount, stats.ValidCount, stats.ExpiredCount);

            return stats;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "获取天气缓存统计失败");
            return new WeatherCacheStats();
        }
    }

    /// <summary>
    ///     检查城市是否有有效缓存
    /// </summary>
    public async Task<bool> HasValidCacheAsync(Guid cityId)
    {
        var cache = await GetValidCacheByCityIdAsync(cityId);
        return cache != null;
    }
}
