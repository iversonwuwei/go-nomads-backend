using CityService.Application.Jobs;
using Microsoft.AspNetCore.Mvc;

namespace CityService.API.Controllers;

/// <summary>
///     性能监控指标 API
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MetricsController : ControllerBase
{
    private readonly WeatherRefreshJob _weatherJob;
    private readonly ILogger<MetricsController> _logger;

    public MetricsController(
        WeatherRefreshJob weatherJob,
        ILogger<MetricsController> logger)
    {
        _weatherJob = weatherJob;
        _logger = logger;
    }

    /// <summary>
    ///     获取天气服务性能指标
    /// </summary>
    /// <returns>性能指标数据</returns>
    [HttpGet("weather")]
    [ProducesResponseType(typeof(WeatherMetricsResponse), 200)]
    public IActionResult GetWeatherMetrics()
    {
        try
        {
            var metrics = WeatherRefreshJob.GetMetrics();

            var response = new WeatherMetricsResponse
            {
                TotalRefreshes = metrics.GetValueOrDefault("total_weather_refreshes", 0),
                SuccessfulFetches = metrics.GetValueOrDefault("successful_weather_fetches", 0),
                FailedFetches = metrics.GetValueOrDefault("failed_weather_fetches", 0),
                CacheHitRatePercent = metrics.GetValueOrDefault("cache_hit_rate_percent", 0),
                LastRefreshDurationMs = metrics.GetValueOrDefault("last_refresh_duration_ms", 0),
                LastRefreshTimestamp = metrics.ContainsKey("last_refresh_timestamp")
                    ? DateTimeOffset.FromUnixTimeSeconds(metrics["last_refresh_timestamp"]).DateTime
                    : null,
                RefreshJobFailures = metrics.GetValueOrDefault("refresh_job_failures", 0),
                CacheCleanups = metrics.GetValueOrDefault("cache_cleanups", 0),
                ExpiredCachesDeleted = metrics.GetValueOrDefault("total_expired_caches_deleted", 0),
                ForecastRefreshes = metrics.GetValueOrDefault("forecast_refreshes", 0),
                SuccessfulForecastFetches = metrics.GetValueOrDefault("successful_forecast_fetches", 0),
                FailedForecastFetches = metrics.GetValueOrDefault("failed_forecast_fetches", 0),
                Timestamp = DateTime.UtcNow
            };

            // 计算衍生指标
            var totalFetches = response.SuccessfulFetches + response.FailedFetches;
            response.SuccessRate = totalFetches > 0
                ? (double)response.SuccessfulFetches / totalFetches * 100
                : 100;

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取天气指标失败");
            return StatusCode(500, new { error = "Failed to retrieve metrics" });
        }
    }

    /// <summary>
    ///     获取缓存健康状态
    /// </summary>
    [HttpGet("cache/health")]
    public async Task<IActionResult> GetCacheHealth()
    {
        try
        {
            var health = await _weatherJob.GetCacheHealthAsync();
            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取缓存健康状态失败");
            return StatusCode(500, new { error = "Failed to retrieve cache health" });
        }
    }

    /// <summary>
    ///     重置性能指标
    /// </summary>
    [HttpPost("reset")]
    public IActionResult ResetMetrics()
    {
        try
        {
            WeatherRefreshJob.ResetMetrics();
            _logger.LogInformation("性能指标已重置");
            return Ok(new { message = "Metrics reset successfully", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重置指标失败");
            return StatusCode(500, new { error = "Failed to reset metrics" });
        }
    }

    /// <summary>
    ///     Prometheus 格式指标导出
    /// </summary>
    [HttpGet("prometheus")]
    [Produces("text/plain")]
    public IActionResult GetPrometheusMetrics()
    {
        try
        {
            var metrics = WeatherRefreshJob.GetMetrics();
            var lines = new List<string>
            {
                "# HELP city_service_weather_refreshes_total Total number of weather refresh operations",
                "# TYPE city_service_weather_refreshes_total counter",
                $"city_service_weather_refreshes_total {metrics.GetValueOrDefault("total_weather_refreshes", 0)}",
                "",
                "# HELP city_service_weather_fetches_success_total Successful weather data fetches",
                "# TYPE city_service_weather_fetches_success_total counter",
                $"city_service_weather_fetches_success_total {metrics.GetValueOrDefault("successful_weather_fetches", 0)}",
                "",
                "# HELP city_service_weather_fetches_failed_total Failed weather data fetches",
                "# TYPE city_service_weather_fetches_failed_total counter",
                $"city_service_weather_fetches_failed_total {metrics.GetValueOrDefault("failed_weather_fetches", 0)}",
                "",
                "# HELP city_service_cache_hit_rate_percent Cache hit rate percentage",
                "# TYPE city_service_cache_hit_rate_percent gauge",
                $"city_service_cache_hit_rate_percent {metrics.GetValueOrDefault("cache_hit_rate_percent", 0)}",
                "",
                "# HELP city_service_last_refresh_duration_ms Last refresh duration in milliseconds",
                "# TYPE city_service_last_refresh_duration_ms gauge",
                $"city_service_last_refresh_duration_ms {metrics.GetValueOrDefault("last_refresh_duration_ms", 0)}",
                "",
                "# HELP city_service_refresh_job_failures_total Refresh job failures",
                "# TYPE city_service_refresh_job_failures_total counter",
                $"city_service_refresh_job_failures_total {metrics.GetValueOrDefault("refresh_job_failures", 0)}",
                ""
            };

            return Content(string.Join("\n", lines), "text/plain; version=0.0.4");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出 Prometheus 指标失败");
            return StatusCode(500);
        }
    }
}

/// <summary>
///     天气服务指标响应
/// </summary>
public class WeatherMetricsResponse
{
    public long TotalRefreshes { get; set; }
    public long SuccessfulFetches { get; set; }
    public long FailedFetches { get; set; }
    public double SuccessRate { get; set; }
    public long CacheHitRatePercent { get; set; }
    public long LastRefreshDurationMs { get; set; }
    public DateTime? LastRefreshTimestamp { get; set; }
    public long RefreshJobFailures { get; set; }
    public long CacheCleanups { get; set; }
    public long ExpiredCachesDeleted { get; set; }
    public long ForecastRefreshes { get; set; }
    public long SuccessfulForecastFetches { get; set; }
    public long FailedForecastFetches { get; set; }
    public DateTime Timestamp { get; set; }
}
