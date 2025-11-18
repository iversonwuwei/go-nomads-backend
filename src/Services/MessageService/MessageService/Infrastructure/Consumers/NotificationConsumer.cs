using MassTransit;
using MessageService.Application.DTOs;
using MessageService.Application.Services;
using Microsoft.Extensions.Logging;

namespace MessageService.Infrastructure.Consumers;

/// <summary>
///     通知消息消费者
/// </summary>
public class NotificationConsumer : IConsumer<NotificationMessage>
{
    private readonly ILogger<NotificationConsumer> _logger;
    private readonly ISignalRNotifier _notifier;

    public NotificationConsumer(ISignalRNotifier notifier, ILogger<NotificationConsumer> logger)
    {
        _notifier = notifier;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<NotificationMessage> context)
    {
        var message = context.Message;

        _logger.LogInformation("收到通知消息: UserId={UserId}, Type={Type}, Title={Title}",
            message.UserId, message.Type, message.Title);

        try
        {
            if (string.IsNullOrEmpty(message.UserId))
            {
                // 广播到所有用户
                await _notifier.BroadcastNotificationAsync(message);
                _logger.LogInformation("广播通知成功: Title={Title}", message.Title);
            }
            else
            {
                // 推送到特定用户
                await _notifier.SendNotificationAsync(message.UserId, message);
                _logger.LogInformation("通知推送成功: UserId={UserId}", message.UserId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理通知消息失败: UserId={UserId}", message.UserId);
            throw;
        }
    }
}