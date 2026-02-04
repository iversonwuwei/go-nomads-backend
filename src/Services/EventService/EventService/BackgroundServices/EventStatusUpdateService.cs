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
            _logger.LogInformation("🔄 开始扫描并更新活动状态...");

            var now = DateTime.UtcNow;

            // 获取所有状态为 upcoming 或 ongoing 的活动
            var activeEvents = await eventRepository.GetActiveEventsForStatusUpdateAsync();

            if (activeEvents.Count == 0)
            {
                _logger.LogInformation("✅ 没有需要更新状态的活动");
                return;
            }

            _logger.LogInformation("📋 找到 {Count} 个活动需要检查状态", activeEvents.Count);

            int updatedCount = 0;
            int failCount = 0;

            foreach (var @event in activeEvents)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    var oldStatus = @event.Status;
                    
                    // 使用领域方法更新状态
                    @event.UpdateStatusByTime();
                    
                    // 只有状态变化时才更新数据库
                    if (oldStatus != @event.Status)
                    {
                        await eventRepository.UpdateAsync(@event);
                        updatedCount++;

                        _logger.LogInformation("✅ 活动 {EventId} ({Title}) 状态从 {OldStatus} 更新为 {NewStatus}",
                            @event.Id, @event.Title, oldStatus, @event.Status);
                    }
                }
                catch (Exception ex)
                {
                    failCount++;
                    _logger.LogError(ex, "❌ 更新活动 {EventId} 状态失败", @event.Id);
                }
            }

            _logger.LogInformation("🎉 活动状态更新完成: 更新 {Updated} 个, 失败 {Fail} 个",
                updatedCount, failCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 扫描过期活动时发生错误");
            throw;
        }
    }
}
