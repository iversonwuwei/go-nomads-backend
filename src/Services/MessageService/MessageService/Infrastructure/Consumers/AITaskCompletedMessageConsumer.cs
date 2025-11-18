using System.Text.Json;
using MassTransit;
using MessageService.Application.DTOs;
using MessageService.Application.Services;
using Microsoft.Extensions.Logging;
using Shared.Messages;
using AIProgressMessage = MessageService.Application.DTOs.AIProgressMessage;

namespace MessageService.Infrastructure.Consumers;

/// <summary>
///     AI 任务完成消息消费者
/// </summary>
public class AITaskCompletedMessageConsumer : IConsumer<AITaskCompletedMessage>
{
    private readonly ILogger<AITaskCompletedMessageConsumer> _logger;
    private readonly ISignalRNotifier _notifier;

    public AITaskCompletedMessageConsumer(
        ISignalRNotifier notifier,
        ILogger<AITaskCompletedMessageConsumer> logger)
    {
        _notifier = notifier;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AITaskCompletedMessage> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "🎉 收到 AI 任务完成消息: TaskId={TaskId}, TaskType={TaskType}, ResultId={ResultId}",
            message.TaskId, message.TaskType, message.ResultId);

        try
        {
            // 构造完成通知消息数据
            var notificationData = new Dictionary<string, object>
            {
                ["TaskId"] = message.TaskId,
                ["TaskType"] = message.TaskType,
                ["Status"] = "completed",
                ["ResultId"] = message.ResultId,
                ["Result"] = message.Result,
                ["CompletedAt"] = message.CompletedAt,
                ["DurationSeconds"] = message.DurationSeconds
            };

            // 发送 TaskCompleted 事件（Flutter 端监听的事件）
            await _notifier.SendTaskCompletedAsync(message.TaskId, message.UserId, notificationData);

            // 发送进度消息（100%完成）
            var progressMessage = new AIProgressMessage
            {
                TaskId = message.TaskId,
                UserId = message.UserId,
                Progress = 100,
                Status = "completed",
                CurrentStep = $"任务完成！耗时 {message.DurationSeconds} 秒",
                Result = JsonSerializer.Serialize(message.Result),
                Timestamp = message.CompletedAt
            };

            await _notifier.SendAIProgressAsync(message.UserId, progressMessage);

            // 发送任务完成通知
            var notification = new NotificationMessage
            {
                UserId = message.UserId,
                Type = "success",
                Title = $"{GetTaskTypeName(message.TaskType)}已完成",
                Content = $"您的{GetTaskTypeName(message.TaskType)}已生成完成",
                Data = notificationData,
                CreatedAt = DateTime.UtcNow
            };

            await _notifier.SendNotificationAsync(message.UserId, notification);

            _logger.LogInformation(
                "✅ AI 任务完成消息已推送: TaskId={TaskId}",
                message.TaskId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ 推送 AI 任务完成消息失败: TaskId={TaskId}",
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