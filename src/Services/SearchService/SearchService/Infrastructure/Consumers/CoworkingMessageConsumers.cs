using MassTransit;
using SearchService.Application.Interfaces;
using Shared.Messages;

namespace SearchService.Infrastructure.Consumers;

/// <summary>
/// 共享办公空间更新消息消费者
/// </summary>
public class CoworkingUpdatedMessageConsumer : IConsumer<CoworkingUpdatedMessage>
{
    private readonly IIndexSyncService _indexSyncService;
    private readonly ILogger<CoworkingUpdatedMessageConsumer> _logger;

    public CoworkingUpdatedMessageConsumer(
        IIndexSyncService indexSyncService,
        ILogger<CoworkingUpdatedMessageConsumer> logger)
    {
        _indexSyncService = indexSyncService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CoworkingUpdatedMessage> context)
    {
        var message = context.Message;
        _logger.LogInformation("收到共享办公空间更新消息: CoworkingId={CoworkingId}, Name={Name}",
            message.CoworkingId, message.Name);

        try
        {
            var success = await _indexSyncService.SyncCoworkingAsync(message.CoworkingId);
            if (success)
            {
                _logger.LogInformation("共享办公空间 {CoworkingId} 索引同步成功", message.CoworkingId);
            }
            else
            {
                _logger.LogWarning("共享办公空间 {CoworkingId} 索引同步失败", message.CoworkingId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理共享办公空间更新消息时发生异常: CoworkingId={CoworkingId}", message.CoworkingId);
            throw;
        }
    }
}

/// <summary>
/// 共享办公空间删除消息消费者
/// </summary>
public class CoworkingDeletedMessageConsumer : IConsumer<CoworkingDeletedMessage>
{
    private readonly IIndexSyncService _indexSyncService;
    private readonly ILogger<CoworkingDeletedMessageConsumer> _logger;

    public CoworkingDeletedMessageConsumer(
        IIndexSyncService indexSyncService,
        ILogger<CoworkingDeletedMessageConsumer> logger)
    {
        _indexSyncService = indexSyncService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CoworkingDeletedMessage> context)
    {
        var message = context.Message;
        _logger.LogInformation("收到共享办公空间删除消息: CoworkingId={CoworkingId}", message.CoworkingId);

        try
        {
            var success = await _indexSyncService.DeleteCoworkingAsync(message.CoworkingId);
            if (success)
            {
                _logger.LogInformation("共享办公空间 {CoworkingId} 索引删除成功", message.CoworkingId);
            }
            else
            {
                _logger.LogWarning("共享办公空间 {CoworkingId} 索引删除失败", message.CoworkingId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理共享办公空间删除消息时发生异常: CoworkingId={CoworkingId}", message.CoworkingId);
            throw;
        }
    }
}
