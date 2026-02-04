namespace Shared.Messages;

/// <summary>
///     AI Chat 发送消息请求（通过 RabbitMQ）
/// </summary>
public class AIChatSendMessageRequest
{
    /// <summary>
    ///     对话 ID
    /// </summary>
    public required Guid ConversationId { get; set; }

    /// <summary>
    ///     用户 ID
    /// </summary>
    public required string UserId { get; set; }

    /// <summary>
    ///     用户消息内容
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    ///     请求 ID（用于关联响应）
    /// </summary>
    public required string RequestId { get; set; }

    /// <summary>
    ///     时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
///     AI Chat 流式响应块（通过 RabbitMQ → SignalR 推送）
/// </summary>
public class AIChatStreamChunk
{
    /// <summary>
    ///     对话 ID
    /// </summary>
    public required Guid ConversationId { get; set; }

    /// <summary>
    ///     消息 ID（AI 回复的消息 ID）
    /// </summary>
    public Guid? MessageId { get; set; }

    /// <summary>
    ///     用户 ID
    /// </summary>
    public required string UserId { get; set; }

    /// <summary>
    ///     请求 ID（关联原始请求）
    /// </summary>
    public required string RequestId { get; set; }

    /// <summary>
    ///     增量文本内容
    /// </summary>
    public required string Delta { get; set; }

    /// <summary>
    ///     是否完成
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    ///     完成原因：stop, length, error
    /// </summary>
    public string? FinishReason { get; set; }

    /// <summary>
    ///     Token 数量（完成时返回）
    /// </summary>
    public int? TokenCount { get; set; }

    /// <summary>
    ///     错误信息（如果有）
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    ///     序列号（用于排序）
    /// </summary>
    public int SequenceNumber { get; set; }

    /// <summary>
    ///     时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
