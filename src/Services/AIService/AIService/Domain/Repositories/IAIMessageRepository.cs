using AIService.Domain.Entities;

namespace AIService.Domain.Repositories;

/// <summary>
///     AI 消息仓储接口
/// </summary>
public interface IAIMessageRepository
{
    /// <summary>
    ///     创建消息
    /// </summary>
    Task<AIMessage> CreateAsync(AIMessage message);

    /// <summary>
    ///     批量创建消息
    /// </summary>
    Task<List<AIMessage>> CreateBatchAsync(List<AIMessage> messages);

    /// <summary>
    ///     根据ID获取消息
    /// </summary>
    Task<AIMessage?> GetByIdAsync(Guid id);

    /// <summary>
    ///     根据对话ID获取消息列表
    /// </summary>
    Task<List<AIMessage>> GetByConversationIdAsync(
        Guid conversationId,
        int page = 1,
        int pageSize = 50,
        bool includeSystem = false);

    /// <summary>
    ///     获取对话的最新消息
    /// </summary>
    Task<AIMessage?> GetLatestMessageAsync(Guid conversationId);

    /// <summary>
    ///     获取对话的消息历史（用于AI上下文）
    /// </summary>
    Task<List<AIMessage>> GetContextMessagesAsync(
        Guid conversationId,
        int maxMessages = 20);

    /// <summary>
    ///     更新消息
    /// </summary>
    Task<AIMessage> UpdateAsync(AIMessage message);

    /// <summary>
    ///     删除消息
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    ///     删除对话的所有消息
    /// </summary>
    Task DeleteByConversationIdAsync(Guid conversationId);

    /// <summary>
    ///     获取对话的消息统计
    /// </summary>
    Task<(int TotalMessages, int TotalTokens)> GetConversationStatsAsync(Guid conversationId);

    /// <summary>
    ///     检查消息是否存在
    /// </summary>
    Task<bool> ExistsAsync(Guid id);
}