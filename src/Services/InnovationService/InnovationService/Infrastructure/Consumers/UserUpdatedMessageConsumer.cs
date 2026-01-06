using InnovationService.Repositories;
using MassTransit;
using Shared.Messages;

namespace InnovationService.Infrastructure.Consumers;

/// <summary>
///     用户信息更新消息消费者
///     当用户修改名称、头像时，更新 innovations 和 innovation_comments 中的冗余字段
/// </summary>
public class UserUpdatedMessageConsumer : IConsumer<UserUpdatedMessage>
{
    private readonly IInnovationRepository _innovationRepository;
    private readonly ILogger<UserUpdatedMessageConsumer> _logger;

    public UserUpdatedMessageConsumer(
        IInnovationRepository innovationRepository,
        ILogger<UserUpdatedMessageConsumer> logger)
    {
        _innovationRepository = innovationRepository;
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

            // 更新该用户创建的所有创新项目的冗余字段
            var updatedInnovationsCount = await _innovationRepository.UpdateCreatorInfoAsync(
                userId,
                message.Name,
                message.AvatarUrl);

            // 更新该用户的所有评论的冗余字段
            var updatedCommentsCount = await _innovationRepository.UpdateCommentUserInfoAsync(
                userId,
                message.Name,
                message.AvatarUrl);

            _logger.LogInformation(
                "✅ 已更新 {InnovationsCount} 个创新项目和 {CommentsCount} 条评论的用户信息: UserId={UserId}",
                updatedInnovationsCount, updatedCommentsCount, message.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 处理用户更新消息失败: UserId={UserId}", message.UserId);
            throw; // 让 MassTransit 处理重试
        }
    }
}
