using EventService.Domain.Repositories;
using MassTransit;
using Shared.Messages;

namespace EventService.Infrastructure.Consumers;

/// <summary>
///     城市信息更新消息消费者
///     当城市名称、国家等信息变更时，更新 events 中的冗余字段
/// </summary>
public class CityUpdatedMessageConsumer : IConsumer<CityUpdatedMessage>
{
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<CityUpdatedMessageConsumer> _logger;

    public CityUpdatedMessageConsumer(
        IEventRepository eventRepository,
        ILogger<CityUpdatedMessageConsumer> logger)
    {
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CityUpdatedMessage> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "🏙️ 收到城市更新消息: CityId={CityId}, Name={Name}, Country={Country}",
            message.CityId, message.Name, message.Country);

        try
        {
            if (!Guid.TryParse(message.CityId, out var cityId))
            {
                _logger.LogWarning("⚠️ 无效的城市ID格式: {CityId}", message.CityId);
                return;
            }

            // 更新该城市下所有活动的冗余字段
            var updatedCount = await _eventRepository.UpdateCityInfoAsync(
                cityId,
                message.Name,
                message.NameEn,
                message.Country);

            _logger.LogInformation(
                "✅ 已更新 {Count} 个活动的城市信息: CityId={CityId}",
                updatedCount, message.CityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 处理城市更新消息失败: CityId={CityId}", message.CityId);
            throw; // 让 MassTransit 处理重试
        }
    }
}
