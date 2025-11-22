using CityService.Application.Abstractions.Services;
using CityService.Application.Services;
using CityService.Domain.Repositories;

namespace CityService.Infrastructure.Services;

/// <summary>
/// 天气缓存后台刷新服务
/// 使用 .NET BackgroundService 替代 Hangfire，避免额外的数据库依赖
/// </summary>
public class WeatherCacheRefreshService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WeatherCacheRefreshService> _logger;
    private readonly TimeSpan _refreshInterval = TimeSpan.FromMinutes(30); // 每30分钟刷新一次
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(24); // 每24小时清理一次
    private DateTime _lastCleanupTime = DateTime.UtcNow;

    public WeatherCacheRefreshService(
        IServiceProvider serviceProvider,
        ILogger<WeatherCacheRefreshService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Weather cache refresh service started");
        _logger.LogInformation("Refresh interval: {Interval} minutes", _refreshInterval.TotalMinutes);

        // 等待应用完全启动
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RefreshWeatherCacheAsync(stoppingToken);

                // 检查是否需要清理过期缓存
                if (DateTime.UtcNow - _lastCleanupTime >= _cleanupInterval)
                {
                    await CleanupExpiredCacheAsync(stoppingToken);
                    _lastCleanupTime = DateTime.UtcNow;
                }

                await Task.Delay(_refreshInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Weather cache refresh service is stopping...");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Weather cache refresh failed");
                // 出错后等待5分钟再重试
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Weather cache refresh service stopped");
    }

    private async Task RefreshWeatherCacheAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var cityService = scope.ServiceProvider.GetRequiredService<ICityService>();
        var weatherService = scope.ServiceProvider.GetRequiredService<IWeatherService>();

        try
        {
            _logger.LogInformation("Starting weather cache refresh for popular cities...");

            // 获取热门城市（根据访问量、评分等）- 这里简化为前50个城市
            var cities = await cityService.GetRecommendedCitiesAsync(50, null);
            
            if (!cities.Any())
            {
                _logger.LogWarning("No cities found for refresh");
                return;
            }

            var successCount = 0;
            var failureCount = 0;

            // 批量获取天气（避免过多 API 调用）
            foreach (var city in cities.Take(30)) // 限制为30个城市，避免 API 限流
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    if (city.Latitude.HasValue && city.Longitude.HasValue)
                    {
                        await weatherService.GetWeatherByCoordinatesAsync(
                            city.Latitude.Value,
                            city.Longitude.Value);
                        
                        successCount++;
                    }

                    // 避免 API 限流，每个请求间隔100ms
                    await Task.Delay(100, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to refresh weather for city {CityName}", city.Name);
                    failureCount++;
                }
            }

            var hitRate = successCount > 0 ? (successCount * 100.0 / (successCount + failureCount)) : 0;
            _logger.LogInformation(
                "Weather cache refresh completed: Success {Success}/{Total} ({HitRate:F1}%)",
                successCount,
                successCount + failureCount,
                hitRate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during weather cache refresh");
            throw;
        }
    }

    private async Task CleanupExpiredCacheAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting cache cleanup...");

        // 内存缓存会自动清理，这里只是记录日志
        // 如果未来使用数据库缓存，可以在这里添加清理逻辑

        _logger.LogInformation("Cache cleanup completed");
        await Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping weather cache refresh service...");
        await base.StopAsync(cancellationToken);
    }
}
