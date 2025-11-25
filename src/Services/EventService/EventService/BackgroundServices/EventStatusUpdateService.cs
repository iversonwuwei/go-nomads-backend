using EventService.Domain.Repositories;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventService.BackgroundServices;

/// <summary>
///     后台服务：定期更新活动状态
///     将已过期的活动状态从 upcoming 更新为 completed
/// </summary>
public class EventStatusUpdateService : BackgroundService
{
    private readonly ILogger<EventStatusUpdateService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public EventStatusUpdateService(
        ILogger<EventStatusUpdateService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🕒 EventStatusUpdateService 已启动");

        // 等待应用完全启动
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await UpdateExpiredEventsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 更新活动状态时发生错误");
            }

            // 每 10 分钟执行一次
            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
        }

        _logger.LogInformation("🛑 EventStatusUpdateService 已停止");
    }

    private async Task UpdateExpiredEventsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

        try
        {
            _logger.LogInformation("🔄 开始扫描并更新过期活动状态...");

            var now = DateTime.UtcNow;

            // 获取所有状态为 upcoming 且结束时间已过的活动
            var expiredEvents = await eventRepository.GetExpiredEventsAsync(now);

            if (expiredEvents.Count == 0)
            {
                _logger.LogInformation("✅ 没有需要更新的过期活动");
                return;
            }

            _logger.LogInformation("📋 找到 {Count} 个过期活动需要更新", expiredEvents.Count);

            int successCount = 0;
            int failCount = 0;

            foreach (var @event in expiredEvents)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    // 更新状态为 completed
                    @event.Status = "completed";
                    @event.UpdatedAt = DateTime.UtcNow;

                    await eventRepository.UpdateAsync(@event);
                    successCount++;

                    _logger.LogInformation("✅ 活动 {EventId} ({Title}) 状态已更新为 completed",
                        @event.Id, @event.Title);
                }
                catch (Exception ex)
                {
                    failCount++;
                    _logger.LogError(ex, "❌ 更新活动 {EventId} 状态失败", @event.Id);
                }
            }

            _logger.LogInformation("🎉 活动状态更新完成: 成功 {Success} 个, 失败 {Fail} 个",
                successCount, failCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 扫描过期活动时发生错误");
            throw;
        }
    }
}
