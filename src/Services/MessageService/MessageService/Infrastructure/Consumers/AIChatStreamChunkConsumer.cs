using MassTransit;
using MessageService.Application.Services;
using Microsoft.Extensions.Logging;
using Shared.Messages;

namespace MessageService.Infrastructure.Consumers;

/// <summary>
///     AI Chat 流式响应 Chunk 消费者
///     接收 AIService 发布的流式响应块，通过 SignalR 推送给客户端
/// </summary>
public class AIChatStreamChunkConsumer : IConsumer<AIChatStreamChunk>
{
    private readonly ILogger<AIChatStreamChunkConsumer> _logger;
    private readonly ISignalRNotifier _notifier;

    public AIChatStreamChunkConsumer(
        ISignalRNotifier notifier,
        ILogger<AIChatStreamChunkConsumer> logger)
    {
        _notifier = notifier;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AIChatStreamChunk> context)
    {
        var chunk = context.Message;

        try
        {
            if (chunk.IsComplete)
            {
                if (!string.IsNullOrEmpty(chunk.Error))
                {
                    _logger.LogWarning(
                        "⚠️ AI Chat 流式响应错误: ConversationId={ConversationId}, RequestId={RequestId}, Error={Error}",
                        chunk.ConversationId, chunk.RequestId, chunk.Error);
                }
                else
                {
                    _logger.LogInformation(
                        "✅ AI Chat 流式响应完成: ConversationId={ConversationId}, RequestId={RequestId}, MessageId={MessageId}",
                        chunk.ConversationId, chunk.RequestId, chunk.MessageId);
                }
            }
            else
            {
                _logger.LogDebug(
                    "📤 AI Chat Chunk: ConversationId={ConversationId}, Seq={Seq}, DeltaLen={DeltaLen}",
                    chunk.ConversationId, chunk.SequenceNumber, chunk.Delta.Length);
            }

            // 通过 SignalR 推送到用户
            await _notifier.SendAIChatChunkAsync(chunk.UserId, new
            {
                conversationId = chunk.ConversationId.ToString(),
                messageId = chunk.MessageId?.ToString(),
                requestId = chunk.RequestId,
                delta = chunk.Delta,
                isComplete = chunk.IsComplete,
                finishReason = chunk.FinishReason,
                tokenCount = chunk.TokenCount,
                error = chunk.Error,
                sequenceNumber = chunk.SequenceNumber,
                timestamp = chunk.Timestamp.ToString("o")
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ 推送 AI Chat Chunk 失败: ConversationId={ConversationId}, RequestId={RequestId}",
                chunk.ConversationId, chunk.RequestId);
            throw;
        }
    }
}
