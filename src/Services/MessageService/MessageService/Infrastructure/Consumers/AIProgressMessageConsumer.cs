using MassTransit;
using MessageService.Application.Services;
using Microsoft.Extensions.Logging;
using Shared.Messages;

namespace MessageService.Infrastructure.Consumers;

/// <summary>
///     AI 进度消息消费者
/// </summary>
public class AIProgressMessageConsumer : IConsumer<AIProgressMessage>
{
    private readonly ILogger<AIProgressMessageConsumer> _logger;
    private readonly ISignalRNotifier _notifier;

    public AIProgressMessageConsumer(
        ISignalRNotifier notifier,
        ILogger<AIProgressMessageConsumer> logger)
    {
        _notifier = notifier;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AIProgressMessage> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "📊 收到 AI 进度消息: TaskId={TaskId}, UserId={UserId}, Progress={Progress}%, Message={Message}",
            message.TaskId, message.UserId, message.Progress, message.Message);

        try
        {
            // 将 Shared.Messages.AIProgressMessage 转换为内部格式
            var internalProgress = new Application.DTOs.AIProgressMessage
            {
                TaskId = message.TaskId,
                UserId = message.UserId,
                Progress = message.Progress,
                Status = "processing",
                CurrentStep = message.Message,
                Completed = message.Completed, // 映射 Completed 字段
                Timestamp = message.Timestamp
            };

            // 通过 SignalR 推送进度消息到前端
            await _notifier.SendAIProgressAsync(message.UserId, internalProgress);

            // 同时发送任务更新
            await _notifier.SendTaskUpdateAsync(message.TaskId, internalProgress);

            _logger.LogInformation(
                "✅ AI 进度消息已推送: TaskId={TaskId}, Progress={Progress}%",
                message.TaskId, message.Progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ 推送 AI 进度消息失败: TaskId={TaskId}",
                message.TaskId);
            throw;
        }
    }
}