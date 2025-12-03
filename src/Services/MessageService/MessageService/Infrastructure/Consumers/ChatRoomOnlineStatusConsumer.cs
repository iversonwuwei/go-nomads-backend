using MassTransit;
using MessageService.Application.Services;
using Microsoft.Extensions.Logging;
using Shared.Messages;

namespace MessageService.Infrastructure.Consumers;

/// <summary>
///     聊天室在线状态消息消费者
/// </summary>
public class ChatRoomOnlineStatusConsumer : IConsumer<ChatRoomOnlineStatusMessage>
{
    private readonly ILogger<ChatRoomOnlineStatusConsumer> _logger;
    private readonly ISignalRNotifier _notifier;

    public ChatRoomOnlineStatusConsumer(
        ISignalRNotifier notifier,
        ILogger<ChatRoomOnlineStatusConsumer> logger)
    {
        _notifier = notifier;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ChatRoomOnlineStatusMessage> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "收到聊天室在线状态消息: RoomId={RoomId}, UserId={UserId}, EventType={EventType}, OnlineCount={OnlineCount}",
            message.RoomId, message.UserId, message.EventType, message.OnlineCount);

        try
        {
            // 通过 SignalR 推送到聊天室组
            await _notifier.SendChatRoomOnlineStatusAsync(message.RoomId, new
            {
                message.RoomId,
                message.UserId,
                message.UserName,
                message.UserAvatar,
                message.Role,
                message.EventType,
                message.OnlineCount,
                message.OnlineUsers,
                message.Timestamp
            });

            _logger.LogInformation("聊天室在线状态消息处理成功: RoomId={RoomId}", message.RoomId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理聊天室在线状态消息失败: RoomId={RoomId}", message.RoomId);
            throw; // 重新抛出异常触发 RabbitMQ 重试
        }
    }
}
