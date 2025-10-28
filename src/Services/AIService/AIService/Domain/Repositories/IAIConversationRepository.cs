using AIService.Domain.Entities;

namespace AIService.Domain.Repositories;

/// <summary>
/// AI 对话仓储接口
/// </summary>
public interface IAIConversationRepository
{
    /// <summary>
    /// 创建对话
    /// </summary>
    Task<AIConversation> CreateAsync(AIConversation conversation);

    /// <summary>
    /// 根据ID获取对话
    /// </summary>
    Task<AIConversation?> GetByIdAsync(Guid id);

    /// <summary>
    /// 根据用户ID获取对话列表
    /// </summary>
    Task<(List<AIConversation> Conversations, int Total)> GetByUserIdAsync(
        Guid userId, 
        string? status = null,
        int page = 1, 
        int pageSize = 20);

    /// <summary>
    /// 更新对话
    /// </summary>
    Task<AIConversation> UpdateAsync(AIConversation conversation);

    /// <summary>
    /// 删除对话
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// 检查对话是否存在
    /// </summary>
    Task<bool> ExistsAsync(Guid id);

    /// <summary>
    /// 检查用户是否有权限访问对话
    /// </summary>
    Task<bool> HasPermissionAsync(Guid conversationId, Guid userId);

    /// <summary>
    /// 获取用户的对话统计
    /// </summary>
    Task<(int TotalConversations, int ActiveConversations, int TotalMessages)> GetUserStatsAsync(Guid userId);
}