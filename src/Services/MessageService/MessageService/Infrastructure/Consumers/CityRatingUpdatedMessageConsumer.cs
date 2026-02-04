using MassTransit;
using MessageService.Application.Services;
using Microsoft.Extensions.Logging;
using Shared.Messages;

namespace MessageService.Infrastructure.Consumers;

/// <summary>
///     城市评分更新消息消费者
///     接收来自 CityService 的评分更新消息，通过 SignalR 广播给客户端
/// </summary>
public class CityRatingUpdatedMessageConsumer : IConsumer<CityRatingUpdatedMessage>
{
    private readonly ILogger<CityRatingUpdatedMessageConsumer> _logger;
    private readonly ISignalRNotifier _notifier;

    public CityRatingUpdatedMessageConsumer(
        ISignalRNotifier notifier,
        ILogger<CityRatingUpdatedMessageConsumer> logger)
    {
        _notifier = notifier;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CityRatingUpdatedMessage> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "⭐ 收到城市评分更新消息: CityId={CityId}, OverallScore={OverallScore}, ReviewCount={ReviewCount}",
            message.CityId, message.OverallScore, message.ReviewCount);

        try
        {
            // 构造评分更新通知数据
            var ratingData = new Dictionary<string, object>
            {
                ["CityId"] = message.CityId,
                ["OverallScore"] = message.OverallScore,
                ["ReviewCount"] = message.ReviewCount,
                ["UpdatedAt"] = message.UpdatedAt
            };

            if (!string.IsNullOrEmpty(message.CityName))
                ratingData["CityName"] = message.CityName;

            if (!string.IsNullOrEmpty(message.CityNameEn))
                ratingData["CityNameEn"] = message.CityNameEn;

            if (!string.IsNullOrEmpty(message.UserId))
                ratingData["UserId"] = message.UserId;

            // 广播城市评分更新
            await _notifier.BroadcastCityRatingUpdatedAsync(message.CityId, ratingData);

            _logger.LogInformation(
                "✅ 城市评分更新已广播: CityId={CityId}, Score={Score}",
                message.CityId, message.OverallScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ 广播城市评分更新失败: CityId={CityId}",
                message.CityId);
            throw;
        }
    }
}
