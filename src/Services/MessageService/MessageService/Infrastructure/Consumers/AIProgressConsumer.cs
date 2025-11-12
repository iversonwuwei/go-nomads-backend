using MassTransit;
using MessageService.Application.DTOs;
using MessageService.Application.Services;
using Microsoft.Extensions.Logging;

namespace MessageService.Infrastructure.Consumers;

/// <summary>
/// AI 进度消息消费者
/// </summary>
public class AIProgressConsumer : IConsumer<AIProgressMessage>
{
    private readonly ISignalRNotifier _notifier;
    private readonly ILogger<AIProgressConsumer> _logger;

    public AIProgressConsumer(ISignalRNotifier notifier, ILogger<AIProgressConsumer> logger)
    {
        _notifier = notifier;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AIProgressMessage> context)
    {
        var message = context.Message;
        
        _logger.LogInformation("收到 AI 进度消息: TaskId={TaskId}, UserId={UserId}, Progress={Progress}%",
            message.TaskId, message.UserId, message.Progress);

        try
        {
            // 推送到用户组
            await _notifier.SendAIProgressAsync(message.UserId, message);

            // 同时推送到任务订阅者
            await _notifier.SendTaskUpdateAsync(message.TaskId, message);

            _logger.LogInformation("AI 进度消息处理成功: TaskId={TaskId}", message.TaskId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理 AI 进度消息失败: TaskId={TaskId}", message.TaskId);
            throw; // 重新抛出异常触发 RabbitMQ 重试
        }
    }
}
