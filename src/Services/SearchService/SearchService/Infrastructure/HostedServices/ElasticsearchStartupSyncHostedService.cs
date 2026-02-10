using System.Diagnostics;
using Microsoft.Extensions.Options;
using SearchService.Application.Interfaces;
using SearchService.Infrastructure.Configuration;

namespace SearchService.Infrastructure.HostedServices;

/// <summary>
/// 服务启动时自动同步 Elasticsearch 数据的后台任务。
/// 
/// 工作流程：
/// 1. 等待 Elasticsearch 就绪（带重试机制）
/// 2. 检查索引文档数量
/// 3. 如索引为空或文档数低于阈值，自动执行全量同步
/// 4. 同步完成后退出（一次性任务）
/// </summary>
public class ElasticsearchStartupSyncHostedService : BackgroundService
{
    private readonly ILogger<ElasticsearchStartupSyncHostedService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly StartupSyncSettings _settings;

    public ElasticsearchStartupSyncHostedService(
        ILogger<ElasticsearchStartupSyncHostedService> logger,
        IServiceScopeFactory scopeFactory,
        IOptions<StartupSyncSettings> settings)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("[启动同步] 启动时自动同步已通过配置禁用");
            return;
        }

        _logger.LogInformation("[启动同步] 服务启动，准备自动同步 Elasticsearch 数据...");

        // Step 1: 等待 Elasticsearch 就绪
        var esReady = await WaitForElasticsearchAsync(stoppingToken);
        if (!esReady)
        {
            _logger.LogError("[启动同步] Elasticsearch 在超时时间内未就绪，跳过启动同步");
            return;
        }

        // Step 2: 等待依赖服务就绪（给上游服务启动的时间）
        if (_settings.WaitForServicesSeconds > 0)
        {
            _logger.LogInformation("[启动同步] 等待 {Seconds} 秒确保依赖服务就绪...",
                _settings.WaitForServicesSeconds);
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_settings.WaitForServicesSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        // Step 3: 检查索引状态并按需同步
        await CheckAndSyncAsync(stoppingToken);
    }

    /// <summary>
    /// 等待 Elasticsearch 就绪
    /// </summary>
    private async Task<bool> WaitForElasticsearchAsync(CancellationToken stoppingToken)
    {
        var maxRetries = _settings.MaxRetries;
        var retryIntervalSeconds = _settings.RetryIntervalSeconds;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            if (stoppingToken.IsCancellationRequested) return false;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var esService = scope.ServiceProvider.GetRequiredService<IElasticsearchService>();
                var isHealthy = await esService.IsHealthyAsync();

                if (isHealthy)
                {
                    _logger.LogInformation("[启动同步] Elasticsearch 连接成功 (第 {Attempt} 次尝试)", attempt);
                    return true;
                }

                _logger.LogWarning("[启动同步] Elasticsearch 未就绪 (第 {Attempt}/{MaxRetries} 次尝试)，{Interval} 秒后重试...",
                    attempt, maxRetries, retryIntervalSeconds);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[启动同步] 检测 Elasticsearch 时异常 (第 {Attempt}/{MaxRetries} 次尝试)",
                    attempt, maxRetries);
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(retryIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// 检查索引状态并按需执行同步
    /// </summary>
    private async Task CheckAndSyncAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var esService = scope.ServiceProvider.GetRequiredService<IElasticsearchService>();
            var syncService = scope.ServiceProvider.GetRequiredService<IIndexSyncService>();

            // 获取当前索引文档数量
            var cityStats = await esService.GetIndexStatsAsync("cities");
            var coworkingStats = await esService.GetIndexStatsAsync("coworking_spaces");

            var cityDocCount = cityStats?.DocumentCount ?? 0;
            var coworkingDocCount = coworkingStats?.DocumentCount ?? 0;

            _logger.LogInformation("[启动同步] 当前索引状态 - 城市: {CityCount} 条, 共享办公: {CoworkingCount} 条",
                cityDocCount, coworkingDocCount);

            var needsSync = false;
            var syncReason = "";

            // 策略：索引为空或文档数低于最小阈值时执行同步
            if (cityDocCount == 0 && coworkingDocCount == 0)
            {
                needsSync = true;
                syncReason = "所有索引均为空";
            }
            else if (cityDocCount < _settings.MinDocumentThreshold ||
                     coworkingDocCount < _settings.MinDocumentThreshold)
            {
                needsSync = true;
                syncReason = $"文档数低于最小阈值 ({_settings.MinDocumentThreshold})";
            }

            if (_settings.ForceSync)
            {
                needsSync = true;
                syncReason = "配置强制同步模式";
            }

            if (!needsSync)
            {
                _logger.LogInformation("[启动同步] 索引数据正常，跳过启动同步");
                return;
            }

            _logger.LogInformation("[启动同步] 触发全量同步，原因: {Reason}", syncReason);

            // 执行同步（带重试）
            await ExecuteSyncWithRetryAsync(syncService, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[启动同步] 检查索引状态或执行同步时发生异常");
        }
    }

    /// <summary>
    /// 带重试机制的同步执行
    /// </summary>
    private async Task ExecuteSyncWithRetryAsync(IIndexSyncService syncService, CancellationToken stoppingToken)
    {
        var maxSyncRetries = _settings.MaxSyncRetries;

        for (int attempt = 1; attempt <= maxSyncRetries; attempt++)
        {
            if (stoppingToken.IsCancellationRequested) return;

            try
            {
                var sw = Stopwatch.StartNew();

                _logger.LogInformation("[启动同步] 开始全量同步 (第 {Attempt}/{MaxRetries} 次尝试)...",
                    attempt, maxSyncRetries);

                var result = await syncService.SyncAllAsync();

                sw.Stop();

                if (result.Success)
                {
                    _logger.LogInformation(
                        "[启动同步] ✅ 全量同步成功 | 成功: {SuccessCount}, 失败: {FailedCount}, 耗时: {Elapsed}ms",
                        result.SuccessCount, result.FailedCount, sw.ElapsedMilliseconds);
                    return; // 成功，退出重试循环
                }
                else
                {
                    _logger.LogWarning(
                        "[启动同步] ⚠️ 同步部分失败 | 成功: {SuccessCount}, 失败: {FailedCount}, 错误: {Error}",
                        result.SuccessCount, result.FailedCount, result.ErrorMessage);

                    // 如果有部分成功，也认为可以接受
                    if (result.SuccessCount > 0)
                    {
                        _logger.LogInformation("[启动同步] 部分数据已同步成功，继续服务");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[启动同步] 同步异常 (第 {Attempt}/{MaxRetries} 次尝试)", attempt, maxSyncRetries);
            }

            if (attempt < maxSyncRetries)
            {
                var backoffSeconds = _settings.RetryIntervalSeconds * attempt; // 线性退避
                _logger.LogInformation("[启动同步] {BackoffSeconds} 秒后重试...", backoffSeconds);

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(backoffSeconds), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        _logger.LogError("[启动同步] ❌ 在 {MaxRetries} 次尝试后同步仍然失败，请手动执行 POST /api/v1/index/sync/all",
            maxSyncRetries);
    }
}
