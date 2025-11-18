using System.ComponentModel.DataAnnotations;
using AIService.Models;
using Postgrest.Attributes;

namespace AIService.Domain.Entities;

/// <summary>
///     AI 消息实体
/// </summary>
[Table("ai_messages")]
public class AIMessage : BaseAIModel
{
    [Required] [Column("conversation_id")] public Guid ConversationId { get; private set; }

    [Required]
    [MaxLength(20)]
    [Column("role")]
    public string Role { get; private set; } = string.Empty; // user, assistant, system

    [Required] [Column("content")] public string Content { get; private set; } = string.Empty;

    [Column("token_count")] public int TokenCount { get; private set; }

    [Column("model_name")] public string? ModelName { get; private set; }

    [Column("prompt_tokens")] public int? PromptTokens { get; private set; }

    [Column("completion_tokens")] public int? CompletionTokens { get; private set; }

    [Column("total_tokens")] public int? TotalTokens { get; private set; }

    [Column("response_time_ms")] public int? ResponseTimeMs { get; private set; }

    [Column("metadata")] public string? Metadata { get; private set; } // JSON格式存储额外信息

    [Column("error_message")] public string? ErrorMessage { get; private set; }

    [Column("is_error")] public bool IsError { get; private set; }

    // 领域行为方法

    /// <summary>
    ///     工厂方法 - 创建用户消息
    /// </summary>
    public static AIMessage CreateUserMessage(Guid conversationId, string content)
    {
        ValidateConversationId(conversationId);
        ValidateContent(content);

        return new AIMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Role = "user",
            Content = content.Trim(),
            TokenCount = EstimateTokens(content),
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    ///     工厂方法 - 创建助手回复消息
    /// </summary>
    public static AIMessage CreateAssistantMessage(
        Guid conversationId,
        string content,
        string? modelName = null,
        int? promptTokens = null,
        int? completionTokens = null,
        int? responseTimeMs = null)
    {
        ValidateConversationId(conversationId);
        ValidateContent(content);

        return new AIMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Role = "assistant",
            Content = content.Trim(),
            TokenCount = EstimateTokens(content),
            ModelName = modelName,
            PromptTokens = promptTokens,
            CompletionTokens = completionTokens,
            TotalTokens = (promptTokens ?? 0) + (completionTokens ?? 0),
            ResponseTimeMs = responseTimeMs,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    ///     工厂方法 - 创建系统消息
    /// </summary>
    public static AIMessage CreateSystemMessage(Guid conversationId, string content)
    {
        ValidateConversationId(conversationId);
        ValidateContent(content);

        return new AIMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Role = "system",
            Content = content.Trim(),
            TokenCount = EstimateTokens(content),
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    ///     工厂方法 - 创建错误消息
    /// </summary>
    public static AIMessage CreateErrorMessage(Guid conversationId, string errorMessage, string? userContent = null)
    {
        ValidateConversationId(conversationId);

        if (string.IsNullOrWhiteSpace(errorMessage))
            throw new ArgumentException("错误消息不能为空", nameof(errorMessage));

        return new AIMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Role = "assistant",
            Content = userContent ?? "处理请求时发生错误",
            ErrorMessage = errorMessage.Trim(),
            IsError = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    ///     设置元数据
    /// </summary>
    public void SetMetadata(string metadata)
    {
        Metadata = metadata;
        Touch();
    }

    /// <summary>
    ///     检查是否为用户消息
    /// </summary>
    public bool IsUserMessage()
    {
        return Role == "user";
    }

    /// <summary>
    ///     检查是否为助手消息
    /// </summary>
    public bool IsAssistantMessage()
    {
        return Role == "assistant";
    }

    /// <summary>
    ///     检查是否为系统消息
    /// </summary>
    public bool IsSystemMessage()
    {
        return Role == "system";
    }

    // 私有验证方法

    private static void ValidateConversationId(Guid conversationId)
    {
        if (conversationId == Guid.Empty)
            throw new ArgumentException("对话ID不能为空", nameof(conversationId));
    }

    private static void ValidateContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("消息内容不能为空", nameof(content));

        if (content.Length > 50000) // 限制消息长度
            throw new ArgumentException("消息内容不能超过50000个字符", nameof(content));
    }

    /// <summary>
    ///     简单的Token估算（实际应该使用具体模型的tokenizer）
    /// </summary>
    private static int EstimateTokens(string content)
    {
        if (string.IsNullOrEmpty(content))
            return 0;

        // 简单估算：中文按字符数，英文按单词数 * 1.3
        var chineseChars = content.Count(c => c >= 0x4e00 && c <= 0x9fff);
        var englishWords = content.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;

        return (int)(chineseChars * 1.5 + englishWords * 1.3);
    }

    // 无参构造函数 (ORM 需要)
}