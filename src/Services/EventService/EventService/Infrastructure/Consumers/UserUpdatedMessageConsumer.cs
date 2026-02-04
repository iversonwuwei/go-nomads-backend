using EventService.Domain.Repositories;
using MassTransit;
using Shared.Messages;

namespace EventService.Infrastructure.Consumers;

/// <summary>
///     用户信息更新消息消费者
///     当用户修改名称、头像时，更新 events 中的冗余字段
/// </summary>
public class UserUpdatedMessageConsumer : IConsumer<UserUpdatedMessage>
{
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<UserUpdatedMessageConsumer> _logger;

    public UserUpdatedMessageConsumer(
        IEventRepository eventRepository,
        ILogger<UserUpdatedMessageConsumer> logger)
    {
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserUpdatedMessage> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "👤 收到用户更新消息: UserId={UserId}, Name={Name}",
            message.UserId, message.Name);

        try
        {
            if (!Guid.TryParse(message.UserId, out var userId))
            {
                _logger.LogWarning("⚠️ 无效的用户ID格式: {UserId}", message.UserId);
                return;
            }

            // 更新该用户组织的所有活动的冗余字段
            var updatedCount = await _eventRepository.UpdateOrganizerInfoAsync(
                userId,
                message.Name,
                message.AvatarUrl);

            _logger.LogInformation(
                "✅ 已更新 {Count} 个活动的组织者信息: UserId={UserId}",
                updatedCount, message.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 处理用户更新消息失败: UserId={UserId}", message.UserId);
            throw; // 让 MassTransit 处理重试
        }
    }
}
