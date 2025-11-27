using MassTransit;
using MessageService.Application.DTOs;
using MessageService.Application.Services;
using Microsoft.Extensions.Logging;
using Shared.Messages;
using AIProgressMessageDto = MessageService.Application.DTOs.AIProgressMessage;

namespace MessageService.Infrastructure.Consumers;

/// <summary>
///     城市图片生成完成消息消费者
/// </summary>
public class CityImageGeneratedMessageConsumer : IConsumer<CityImageGeneratedMessage>
{
    private readonly ILogger<CityImageGeneratedMessageConsumer> _logger;
    private readonly ISignalRNotifier _notifier;

    public CityImageGeneratedMessageConsumer(
        ISignalRNotifier notifier,
        ILogger<CityImageGeneratedMessageConsumer> logger)
    {
        _notifier = notifier;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CityImageGeneratedMessage> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "🖼️ 收到城市图片生成完成消息: TaskId={TaskId}, CityId={CityId}, Success={Success}",
            message.TaskId, message.CityId, message.Success);

        try
        {
            // 构造图片更新通知数据
            var notificationData = new Dictionary<string, object>
            {
                ["TaskId"] = message.TaskId,
                ["CityId"] = message.CityId,
                ["CityName"] = message.CityName,
                ["Success"] = message.Success,
                ["CompletedAt"] = message.CompletedAt,
                ["DurationSeconds"] = message.DurationSeconds
            };

            if (message.Success)
            {
                if (!string.IsNullOrEmpty(message.PortraitImageUrl))
                    notificationData["PortraitImageUrl"] = message.PortraitImageUrl;

                if (message.LandscapeImageUrls != null && message.LandscapeImageUrls.Count > 0)
                    notificationData["LandscapeImageUrls"] = message.LandscapeImageUrls;
            }
            else
            {
                notificationData["ErrorMessage"] = message.ErrorMessage ?? "图片生成失败";
            }

            // 发送城市图片更新事件
            await _notifier.SendCityImageUpdatedAsync(message.CityId, message.UserId, notificationData);

            // 发送进度消息（100%完成）
            var progressMessage = new AIProgressMessageDto
            {
                TaskId = message.TaskId,
                UserId = message.UserId,
                Progress = 100,
                Status = message.Success ? "completed" : "failed",
                CurrentStep = message.Success
                    ? $"图片生成完成！耗时 {message.DurationSeconds} 秒"
                    : $"图片生成失败: {message.ErrorMessage}",
                Timestamp = message.CompletedAt
            };

            await _notifier.SendAIProgressAsync(message.UserId, progressMessage);

            // 发送通知消息
            var notification = new NotificationMessage
            {
                UserId = message.UserId,
                Type = message.Success ? "success" : "error",
                Title = message.Success ? "城市图片生成完成" : "城市图片生成失败",
                Content = message.Success
                    ? $"{message.CityName} 的城市图片已生成完成"
                    : $"{message.CityName} 的城市图片生成失败: {message.ErrorMessage}",
                Data = notificationData,
                CreatedAt = DateTime.UtcNow
            };

            await _notifier.SendNotificationAsync(message.UserId, notification);

            _logger.LogInformation(
                "✅ 城市图片更新消息已推送: TaskId={TaskId}, CityId={CityId}",
                message.TaskId, message.CityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ 推送城市图片更新消息失败: TaskId={TaskId}, CityId={CityId}",
                message.TaskId, message.CityId);
            throw;
        }
    }
}
