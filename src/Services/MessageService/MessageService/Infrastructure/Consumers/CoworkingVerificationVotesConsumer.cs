using MassTransit;
using MessageService.Application.Services;
using Microsoft.Extensions.Logging;
using Shared.Messages;

namespace MessageService.Infrastructure.Consumers;

/// <summary>
///     Coworking 验证人数变化消息消费者
/// </summary>
public class CoworkingVerificationVotesConsumer : IConsumer<CoworkingVerificationVotesMessage>
{
    private readonly ILogger<CoworkingVerificationVotesConsumer> _logger;
    private readonly ISignalRNotifier _notifier;

    public CoworkingVerificationVotesConsumer(
        ISignalRNotifier notifier,
        ILogger<CoworkingVerificationVotesConsumer> logger)
    {
        _notifier = notifier;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CoworkingVerificationVotesMessage> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "收到 Coworking 验证人数变化消息: CoworkingId={CoworkingId}, Votes={Votes}, IsVerified={IsVerified}",
            message.CoworkingId, message.VerificationVotes, message.IsVerified);

        try
        {
            // 通过 SignalR 推送到订阅该 Coworking 的客户端
            await _notifier.SendCoworkingVerificationVotesAsync(message.CoworkingId, new
            {
                message.CoworkingId,
                message.VerificationVotes,
                message.IsVerified,
                message.Timestamp
            });

            _logger.LogInformation("Coworking 验证人数消息处理成功: CoworkingId={CoworkingId}", message.CoworkingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理 Coworking 验证人数消息失败: CoworkingId={CoworkingId}", message.CoworkingId);
            throw; // 重新抛出异常触发 RabbitMQ 重试
        }
    }
}
