using CityService.Application.Abstractions.Services;
using CityService.Domain.Repositories;
using Hangfire;
using System.Collections.Concurrent;

namespace CityService.Application.Jobs;

/// <summary>
///     天气数据定时刷新后台任务
/// </summary>
public class WeatherRefreshJob
{
    private readonly ICityRepository _cityRepo;
    private readonly IWeatherService _weatherService;
    private readonly IWeatherCacheRepository _weatherCacheRepo;
    private readonly ILogger<WeatherRefreshJob> _logger;

    // 性能指标统计
    private static readonly ConcurrentDictionary<string, long> _metrics = new();
    private static readonly object _metricsLock = new();

    public WeatherRefreshJob(
        ICityRepository cityRepository,
        IWeatherService weatherService,
        IWeatherCacheRepository weatherCacheRepository,
        ILogger<WeatherRefreshJob> logger)
    {
        _cityRepo = cityRepository;
        _weatherService = weatherService;
        _weatherCacheRepo = weatherCacheRepository;
        _logger = logger;
    }

    /// <summary>
    ///     刷新热门城市的天气数据
    /// </summary>
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 600 })]
    public async Task RefreshPopularCitiesWeatherAsync()
    {
        try
        {
            _logger.LogInformation("🌤️  开始刷新热门城市天气数据");
            var startTime = DateTime.UtcNow;

            // 获取前50个热门城市（按评分排序）
            var cities = (await _cityRepo.GetRecommendedAsync(50)).ToList();

            if (!cities.Any())
            {
                _logger.LogWarning("未找到热门城市，跳过天气刷新");
                return;
            }

            _logger.LogInformation("准备刷新 {Count} 个热门城市的天气", cities.Count);

            // 准备城市坐标字典
            var cityCoordinates = cities
                .Where(c => c.Latitude.HasValue && c.Longitude.HasValue)
                .ToDictionary(
                    c => c.Id,
                    c => (c.Latitude!.Value, c.Longitude!.Value, c.Name)
                );

            if (!cityCoordinates.Any())
            {
                _logger.LogWarning("没有城市具有有效坐标，跳过刷新");
                return;
            }

            // 批量获取天气数据（会自动保存到数据库缓存）
            var weatherData = await _weatherService.GetWeatherForCitiesByCoordinatesAsync(cityCoordinates);

            var successCount = weatherData.Count(w => w.Value != null);
            var elapsed = DateTime.UtcNow - startTime;

            // 更新性能指标
            IncrementMetric("total_weather_refreshes", 1);
            IncrementMetric("successful_weather_fetches", successCount);
            IncrementMetric("failed_weather_fetches", cityCoordinates.Count - successCount);

            _logger.LogInformation(
                "✅ 天气刷新完成: {SuccessCount}/{TotalCount} 成功, 耗时 {ElapsedSeconds:F1}秒",
                successCount, cityCoordinates.Count, elapsed.TotalSeconds);

            // 获取缓存统计和监控
            var stats = await _weatherCacheRepo.GetCacheStatsAsync();
            var cacheHitRate = stats.TotalCount > 0 
                ? (double)stats.ValidCount / stats.TotalCount * 100 
                : 0;

            _logger.LogInformation(
                "📊 缓存统计 - 总数: {Total}, 有效: {Valid}, 过期: {Expired}, 命中率: {HitRate:F1}%",
                stats.TotalCount, stats.ValidCount, stats.ExpiredCount, cacheHitRate);

            // 监控告警: 缓存命中率低于80%
            if (cacheHitRate < 80 && stats.TotalCount > 10)
            {
                _logger.LogWarning(
                    "⚠️  缓存命中率告警: {HitRate:F1}% (低于80%), 总缓存数: {Total}, 有效: {Valid}",
                    cacheHitRate, stats.TotalCount, stats.ValidCount);
            }

            // 监控告警: API调用失败率高于10%
            var failureRate = cityCoordinates.Count > 0
                ? (double)(cityCoordinates.Count - successCount) / cityCoordinates.Count * 100
                : 0;

            if (failureRate > 10)
            {
                _logger.LogWarning(
                    "⚠️  API调用失败率告警: {FailureRate:F1}% (高于10%), 失败: {Failed}/{Total}",
                    failureRate, cityCoordinates.Count - successCount, cityCoordinates.Count);
            }

            // 记录性能指标供监控
            SetMetric("cache_hit_rate_percent", (long)cacheHitRate);
            SetMetric("last_refresh_duration_ms", (long)elapsed.TotalMilliseconds);
            SetMetric("last_refresh_timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }
        catch (Exception ex)
        {
            IncrementMetric("refresh_job_failures", 1);
            _logger.LogError(ex, "刷新热门城市天气失败");
            throw; // 抛出异常以触发 Hangfire 重试
        }
    }

    /// <summary>
    ///     清理过期的天气缓存
    /// </summary>
    [AutomaticRetry(Attempts = 2)]
    public async Task CleanExpiredWeatherCacheAsync()
    {
        try
        {
            _logger.LogInformation("🧹 开始清理过期天气缓存");

            // 清理过期超过1天的缓存
            var deletedCount = await _weatherCacheRepo.CleanExpiredCacheAsync(TimeSpan.FromDays(1));

            if (deletedCount > 0)
            {
                IncrementMetric("cache_cleanups", 1);
                IncrementMetric("total_expired_caches_deleted", deletedCount);
                _logger.LogInformation("✅ 清理完成，删除 {Count} 条过期缓存", deletedCount);
            }
            else
            {
                _logger.LogDebug("没有需要清理的过期缓存");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理过期缓存失败");
            throw;
        }
    }

    /// <summary>
    ///     刷新热门城市的天气预报数据（5天）
    /// </summary>
    [AutomaticRetry(Attempts = 2)]
    public async Task RefreshWeatherForecastsAsync()
    {
        try
        {
            _logger.LogInformation("🌦️  开始刷新天气预报数据");
            var startTime = DateTime.UtcNow;

            // 获取前30个最热门城市
            var cities = (await _cityRepo.GetRecommendedAsync(30)).ToList();

            if (!cities.Any())
            {
                _logger.LogWarning("未找到热门城市，跳过预报刷新");
                return;
            }

            var successCount = 0;
            var failCount = 0;

            // 分批处理，避免过载
            const int batchSize = 5;
            var batches = cities
                .Where(c => c.Latitude.HasValue && c.Longitude.HasValue)
                .Select((city, index) => new { city, index })
                .GroupBy(x => x.index / batchSize)
                .Select(g => g.Select(x => x.city).ToList())
                .ToList();

            foreach (var batch in batches)
            {
                var tasks = batch.Select(async city =>
                {
                    try
                    {
                        var forecast = await _weatherService.GetDailyForecastAsync(
                            city.Latitude!.Value,
                            city.Longitude!.Value,
                            5);

                        return forecast != null;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "获取城市 {CityName} 预报失败", city.Name);
                        return false;
                    }
                });

                var results = await Task.WhenAll(tasks);
                successCount += results.Count(r => r);
                failCount += results.Count(r => !r);

                // 批次间延迟
                if (batches.IndexOf(batch) < batches.Count - 1)
                {
                    await Task.Delay(200);
                }
            }

            var elapsed = DateTime.UtcNow - startTime;

            IncrementMetric("forecast_refreshes", 1);
            IncrementMetric("successful_forecast_fetches", successCount);
            IncrementMetric("failed_forecast_fetches", failCount);

            _logger.LogInformation(
                "✅ 预报刷新完成: {SuccessCount}/{TotalCount} 成功, 耗时 {ElapsedSeconds:F1}秒",
                successCount, successCount + failCount, elapsed.TotalSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新天气预报失败");
            throw;
        }
    }

    /// <summary>
    ///     获取缓存健康状态（用于监控）
    /// </summary>
    public async Task<object> GetCacheHealthAsync()
    {
        var stats = await _weatherCacheRepo.GetCacheStatsAsync();

        return new
        {
            TotalCaches = stats.TotalCount,
            ValidCaches = stats.ValidCount,
            ExpiredCaches = stats.ExpiredCount,
            ValidPercentage = stats.TotalCount > 0
                ? (double)stats.ValidCount / stats.TotalCount * 100
                : 0,
            OldestCacheAge = $"{stats.OldestCacheAgeHours:F1} hours",
            NewestCacheAge = $"{stats.NewestCacheAgeMinutes:F1} minutes",
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    ///     获取性能指标
    /// </summary>
    public static Dictionary<string, long> GetMetrics()
    {
        lock (_metricsLock)
        {
            return new Dictionary<string, long>(_metrics);
        }
    }

    /// <summary>
    ///     重置所有指标
    /// </summary>
    public static void ResetMetrics()
    {
        lock (_metricsLock)
        {
            _metrics.Clear();
        }
    }

    private static void IncrementMetric(string key, long value)
    {
        _metrics.AddOrUpdate(key, value, (_, current) => current + value);
    }

    private static void SetMetric(string key, long value)
    {
        _metrics[key] = value;
    }
}
