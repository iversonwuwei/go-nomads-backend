using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SearchService.Application.Interfaces;
using SearchService.Infrastructure.Configuration;

namespace SearchService.Infrastructure.HostedServices;

/// <summary>
/// 定期校验并回填索引的后台任务。
/// 策略：按配置周期全量拉取城市/共享办公数据并重建索引，确保长时间运行后仍然一致。
/// 与 ElasticsearchStartupSyncHostedService 配合：启动同步负责首次数据填充，本任务负责后续定期维护。
/// </summary>
public class IndexVerificationHostedService : BackgroundService
{
    private readonly ILogger<IndexVerificationHostedService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IndexMaintenanceSettings _settings;
    private readonly TimeSpan _interval;
    private readonly TimeSpan _initialDelay;

    public IndexVerificationHostedService(
        ILogger<IndexVerificationHostedService> logger,
        IServiceScopeFactory scopeFactory,
        IOptions<IndexMaintenanceSettings> settings)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _settings = settings.Value;

        var intervalMinutes = _settings.VerifyIntervalMinutes <= 0 ? 60 : _settings.VerifyIntervalMinutes;
        var initialDelayMinutes = _settings.InitialDelayMinutes < 0 ? 0 : _settings.InitialDelayMinutes;

        _interval = TimeSpan.FromMinutes(intervalMinutes);
        _initialDelay = TimeSpan.FromMinutes(initialDelayMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("[定期校验] 索引定期校验任务已通过配置禁用");
            return;
        }

        // 初始延迟：等待启动同步完成后再开始定期校验
        if (_initialDelay > TimeSpan.Zero)
        {
            _logger.LogInformation("[定期校验] 索引定期校验任务将在 {Delay} 后开始（等待启动同步完成）", _initialDelay);
            try
            {
                await Task.Delay(_initialDelay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        _logger.LogInformation("[定期校验] 开始定期校验循环，间隔: {Interval}", _interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var esService = scope.ServiceProvider.GetRequiredService<IElasticsearchService>();
                var syncService = scope.ServiceProvider.GetRequiredService<IIndexSyncService>();

                // 先检查 ES 是否可用
                var isHealthy = await esService.IsHealthyAsync();
                if (!isHealthy)
                {
                    _logger.LogWarning("[定期校验] Elasticsearch 不可用，跳过本次校验");
                }
                else
                {
                    // 记录同步前的文档数（用于比较）
                    var cityStatsBefore = await esService.GetIndexStatsAsync("cities");
                    var coworkingStatsBefore = await esService.GetIndexStatsAsync("coworking_spaces");

                    _logger.LogInformation(
                        "[定期校验] 开始执行 | 同步前文档数 - 城市: {CityCount}, 共享办公: {CoworkingCount}",
                        cityStatsBefore?.DocumentCount ?? 0,
                        coworkingStatsBefore?.DocumentCount ?? 0);

                    var sw = Stopwatch.StartNew();

                    var cityResult = await syncService.SyncAllCitiesAsync();
                    var coworkingResult = await syncService.SyncAllCoworkingsAsync();

                    sw.Stop();

                    _logger.LogInformation(
                        "[定期校验] 完成 | 城市 成功 {CitySuccess} 失败 {CityFail} | 共享办公 成功 {CoworkSuccess} 失败 {CoworkFail} | 耗时 {Elapsed}ms",
                        cityResult.SuccessCount,
                        cityResult.FailedCount,
                        coworkingResult.SuccessCount,
                        coworkingResult.FailedCount,
                        sw.ElapsedMilliseconds);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[定期校验] 执行索引定期校验任务时发生异常");
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("[定期校验] 索引定期校验任务已停止");
    }
}
