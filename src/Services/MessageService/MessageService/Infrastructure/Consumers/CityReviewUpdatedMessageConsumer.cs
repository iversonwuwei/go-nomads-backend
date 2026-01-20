using MassTransit;
using MessageService.Application.Services;
using Microsoft.Extensions.Logging;
using Shared.Messages;

namespace MessageService.Infrastructure.Consumers;

/// <summary>
///     城市评论更新消息消费者
///     接收来自 CityService 的评论更新消息，通过 SignalR 广播给客户端
/// </summary>
public class CityReviewUpdatedMessageConsumer : IConsumer<CityReviewUpdatedMessage>
{
    private readonly ILogger<CityReviewUpdatedMessageConsumer> _logger;
    private readonly ISignalRNotifier _notifier;

    public CityReviewUpdatedMessageConsumer(
        ISignalRNotifier notifier,
        ILogger<CityReviewUpdatedMessageConsumer> logger)
    {
        _notifier = notifier;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CityReviewUpdatedMessage> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "💬 收到城市评论更新消息: CityId={CityId}, ChangeType={ChangeType}, ReviewCount={ReviewCount}",
            message.CityId, message.ChangeType, message.ReviewCount);

        try
        {
            // 构造评论更新通知数据
            var reviewData = new Dictionary<string, object>
            {
                ["CityId"] = message.CityId,
                ["ChangeType"] = message.ChangeType,
                ["OverallScore"] = message.OverallScore,
                ["ReviewCount"] = message.ReviewCount,
                ["UpdatedAt"] = message.UpdatedAt
            };

            if (!string.IsNullOrEmpty(message.CityName))
                reviewData["CityName"] = message.CityName;

            if (!string.IsNullOrEmpty(message.CityNameEn))
                reviewData["CityNameEn"] = message.CityNameEn;

            if (!string.IsNullOrEmpty(message.ReviewId))
                reviewData["ReviewId"] = message.ReviewId;

            if (!string.IsNullOrEmpty(message.UserId))
                reviewData["UserId"] = message.UserId;

            // 广播城市评论更新
            await _notifier.BroadcastCityReviewUpdatedAsync(message.CityId, reviewData);

            _logger.LogInformation(
                "✅ 城市评论更新已广播: CityId={CityId}, ChangeType={ChangeType}",
                message.CityId, message.ChangeType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ 广播城市评论更新失败: CityId={CityId}",
                message.CityId);
            throw;
        }
    }
}
