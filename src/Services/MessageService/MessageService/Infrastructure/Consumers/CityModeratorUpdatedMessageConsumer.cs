using MassTransit;
using MessageService.Application.Services;
using Microsoft.Extensions.Logging;
using Shared.Messages;

namespace MessageService.Infrastructure.Consumers;

/// <summary>
///     城市版主变更消息消费者
///     接收来自 CityService 的版主变更消息，通过 SignalR 广播给客户端
/// </summary>
public class CityModeratorUpdatedMessageConsumer : IConsumer<CityModeratorUpdatedMessage>
{
    private readonly ILogger<CityModeratorUpdatedMessageConsumer> _logger;
    private readonly ISignalRNotifier _notifier;

    public CityModeratorUpdatedMessageConsumer(
        ISignalRNotifier notifier,
        ILogger<CityModeratorUpdatedMessageConsumer> logger)
    {
        _notifier = notifier;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CityModeratorUpdatedMessage> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "👤 收到城市版主变更消息: CityId={CityId}, ChangeType={ChangeType}, UserId={UserId}",
            message.CityId, message.ChangeType, message.UserId);

        try
        {
            var moderatorData = new Dictionary<string, object>
            {
                ["CityId"] = message.CityId,
                ["ChangeType"] = message.ChangeType,
                ["UpdatedAt"] = message.UpdatedAt
            };

            if (!string.IsNullOrEmpty(message.CityName))
                moderatorData["CityName"] = message.CityName;

            if (!string.IsNullOrEmpty(message.CityNameEn))
                moderatorData["CityNameEn"] = message.CityNameEn;

            if (!string.IsNullOrEmpty(message.UserId))
                moderatorData["UserId"] = message.UserId;

            // 广播城市版主变更
            await _notifier.BroadcastCityModeratorUpdatedAsync(message.CityId, moderatorData);

            _logger.LogInformation(
                "✅ 城市版主变更已广播: CityId={CityId}, ChangeType={ChangeType}",
                message.CityId, message.ChangeType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ 处理城市版主变更消息失败: CityId={CityId}",
                message.CityId);
            throw;
        }
    }
}
