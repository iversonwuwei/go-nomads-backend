namespace AIService.Application.DTOs;

/// <summary>
/// 对话响应
/// </summary>
public class ConversationResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string? SystemPrompt { get; set; }
    public int TotalMessages { get; set; }
    public int TotalTokens { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// 消息响应
/// </summary>
public class MessageResponse
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int TokenCount { get; set; }
    public string? ModelName { get; set; }
    public int? PromptTokens { get; set; }
    public int? CompletionTokens { get; set; }
    public int? TotalTokens { get; set; }
    public int? ResponseTimeMs { get; set; }
    public string? Metadata { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsError { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// AI 聊天响应
/// </summary>
public class ChatResponse
{
    public string Content { get; set; } = string.Empty;
    public string Role { get; set; } = "assistant";
    public string? ModelName { get; set; }
    public int? PromptTokens { get; set; }
    public int? CompletionTokens { get; set; }
    public int? TotalTokens { get; set; }
    public int ResponseTimeMs { get; set; }
    public string? FinishReason { get; set; }
    public bool IsError { get; set; }
    public string? ErrorMessage { get; set; }
    public MessageResponse? UserMessage { get; set; }
    public MessageResponse? AssistantMessage { get; set; }
}

/// <summary>
/// 分页响应
/// </summary>
public class PagedResponse<T>
{
    public List<T> Data { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrevious => Page > 1;
}

/// <summary>
/// 用户统计响应
/// </summary>
public class UserStatsResponse
{
    public int TotalConversations { get; set; }
    public int ActiveConversations { get; set; }
    public int TotalMessages { get; set; }
    public int TotalTokens { get; set; }
    public DateTime? LastActivityAt { get; set; }
}

/// <summary>
/// 流式响应块
/// </summary>
public class StreamResponse
{
    public string Delta { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
    public string? FinishReason { get; set; }
    public int? TokenCount { get; set; }
    public string? Error { get; set; }
}