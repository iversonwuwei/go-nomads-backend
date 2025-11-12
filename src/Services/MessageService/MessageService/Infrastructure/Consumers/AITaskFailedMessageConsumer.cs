using MassTransit;
using MessageService.Application.Services;
using Microsoft.Extensions.Logging;
using Shared.Messages;

namespace MessageService.Infrastructure.Consumers;

/// <summary>
/// AI 任务失败消息消费者
/// </summary>
public class AITaskFailedMessageConsumer : IConsumer<AITaskFailedMessage>
{
    private readonly ISignalRNotifier _notifier;
    private readonly ILogger<AITaskFailedMessageConsumer> _logger;

    public AITaskFailedMessageConsumer(
        ISignalRNotifier notifier,
        ILogger<AITaskFailedMessageConsumer> logger)
    {
        _notifier = notifier;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AITaskFailedMessage> context)
    {
        var message = context.Message;

        _logger.LogWarning(
            "⚠️ 收到 AI 任务失败消息: TaskId={TaskId}, TaskType={TaskType}, Error={Error}",
            message.TaskId, message.TaskType, message.ErrorMessage);

        try
        {
            // 构造失败通知消息数据
            var notificationData = new Dictionary<string, object>
            {
                ["TaskId"] = message.TaskId,
                ["TaskType"] = message.TaskType,
                ["Status"] = "failed",
                ["ErrorMessage"] = message.ErrorMessage,
                ["ErrorCode"] = message.ErrorCode ?? "",
                ["FailedAt"] = message.FailedAt
            };

            // 发送进度消息（失败状态）
            var progressMessage = new Application.DTOs.AIProgressMessage
            {
                TaskId = message.TaskId,
                UserId = message.UserId,
                Progress = 0,
                Status = "failed",
                CurrentStep = "任务失败",
                Error = message.ErrorMessage,
                Timestamp = message.FailedAt
            };

            await _notifier.SendAIProgressAsync(message.UserId, progressMessage);

            // 发送任务失败通知
            var notification = new Application.DTOs.NotificationMessage
            {
                UserId = message.UserId,
                Type = "error",
                Title = $"{GetTaskTypeName(message.TaskType)}失败",
                Content = message.ErrorMessage,
                Data = notificationData,
                CreatedAt = DateTime.UtcNow
            };

            await _notifier.SendNotificationAsync(message.UserId, notification);

            _logger.LogInformation(
                "✅ AI 任务失败消息已推送: TaskId={TaskId}",
                message.TaskId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ 推送 AI 任务失败消息失败: TaskId={TaskId}",
                message.TaskId);
            throw;
        }
    }

    private static string GetTaskTypeName(string taskType)
    {
        return taskType switch
        {
            "travel-plan" => "旅行计划",
            "digital-nomad-guide" => "数字游民指南",
            _ => "任务"
        };
    }
}
