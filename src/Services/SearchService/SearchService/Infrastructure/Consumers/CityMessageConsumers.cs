using MassTransit;
using SearchService.Application.Interfaces;
using Shared.Messages;

namespace SearchService.Infrastructure.Consumers;

/// <summary>
/// 城市更新消息消费者
/// </summary>
public class CityUpdatedMessageConsumer : IConsumer<CityUpdatedMessage>
{
    private readonly IIndexSyncService _indexSyncService;
    private readonly ILogger<CityUpdatedMessageConsumer> _logger;

    public CityUpdatedMessageConsumer(
        IIndexSyncService indexSyncService,
        ILogger<CityUpdatedMessageConsumer> logger)
    {
        _indexSyncService = indexSyncService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CityUpdatedMessage> context)
    {
        var message = context.Message;
        _logger.LogInformation("收到城市更新消息: CityId={CityId}, Name={CityName}",
            message.CityId, message.Name);

        try
        {
            // CityId 是 string 类型，需要解析为 Guid
            if (!Guid.TryParse(message.CityId, out var cityGuid))
            {
                _logger.LogWarning("无效的城市ID格式: {CityId}", message.CityId);
                return;
            }

            var success = await _indexSyncService.SyncCityAsync(cityGuid);
            if (success)
            {
                _logger.LogInformation("城市 {CityId} 索引同步成功", message.CityId);
            }
            else
            {
                _logger.LogWarning("城市 {CityId} 索引同步失败", message.CityId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理城市更新消息时发生异常: CityId={CityId}", message.CityId);
            throw;
        }
    }
}

/// <summary>
/// 城市删除消息消费者
/// </summary>
public class CityDeletedMessageConsumer : IConsumer<CityDeletedMessage>
{
    private readonly IIndexSyncService _indexSyncService;
    private readonly ILogger<CityDeletedMessageConsumer> _logger;

    public CityDeletedMessageConsumer(
        IIndexSyncService indexSyncService,
        ILogger<CityDeletedMessageConsumer> logger)
    {
        _indexSyncService = indexSyncService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CityDeletedMessage> context)
    {
        var message = context.Message;
        _logger.LogInformation("收到城市删除消息: CityId={CityId}", message.CityId);

        try
        {
            var success = await _indexSyncService.DeleteCityAsync(message.CityId);
            if (success)
            {
                _logger.LogInformation("城市 {CityId} 索引删除成功", message.CityId);
            }
            else
            {
                _logger.LogWarning("城市 {CityId} 索引删除失败", message.CityId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理城市删除消息时发生异常: CityId={CityId}", message.CityId);
            throw;
        }
    }
}
