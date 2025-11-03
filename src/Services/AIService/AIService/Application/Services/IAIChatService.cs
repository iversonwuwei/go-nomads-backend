using AIService.Application.DTOs;

namespace AIService.Application.Services;

/// <summary>
/// AI 聊天应用服务接口
/// </summary>
public interface IAIChatService
{
    /// <summary>
    /// 创建新对话
    /// </summary>
    Task<ConversationResponse> CreateConversationAsync(CreateConversationRequest request, Guid userId);

    /// <summary>
    /// 获取用户的对话列表
    /// </summary>
    Task<PagedResponse<ConversationResponse>> GetConversationsAsync(GetConversationsRequest request, Guid userId);

    /// <summary>
    /// 根据ID获取对话
    /// </summary>
    Task<ConversationResponse> GetConversationAsync(Guid conversationId, Guid userId);

    /// <summary>
    /// 更新对话
    /// </summary>
    Task<ConversationResponse> UpdateConversationAsync(Guid conversationId, UpdateConversationRequest request, Guid userId);

    /// <summary>
    /// 删除对话
    /// </summary>
    Task DeleteConversationAsync(Guid conversationId, Guid userId);

    /// <summary>
    /// 归档对话
    /// </summary>
    Task<ConversationResponse> ArchiveConversationAsync(Guid conversationId, Guid userId);

    /// <summary>
    /// 激活对话
    /// </summary>
    Task<ConversationResponse> ActivateConversationAsync(Guid conversationId, Guid userId);

    /// <summary>
    /// 发送消息并获取AI回复
    /// </summary>
    Task<ChatResponse> SendMessageAsync(Guid conversationId, SendMessageRequest request, Guid userId);

    /// <summary>
    /// 发送消息并获取流式AI回复
    /// </summary>
    IAsyncEnumerable<StreamResponse> SendMessageStreamAsync(Guid conversationId, SendMessageRequest request, Guid userId);

    /// <summary>
    /// 获取对话的消息历史
    /// </summary>
    Task<PagedResponse<MessageResponse>> GetMessagesAsync(Guid conversationId, GetMessagesRequest request, Guid userId);

    /// <summary>
    /// 获取用户统计信息
    /// </summary>
    Task<UserStatsResponse> GetUserStatsAsync(Guid userId);

    /// <summary>
    /// 健康检查 - 测试AI服务连接
    /// </summary>
    Task<bool> HealthCheckAsync();

    /// <summary>
    /// 生成旅行计划
    /// </summary>
    Task<TravelPlanResponse> GenerateTravelPlanAsync(
        GenerateTravelPlanRequest request, 
        Guid userId,
        Func<int, string, Task>? onProgress = null);

    /// <summary>
    /// 生成数字游民旅游指南
    /// </summary>
    Task<TravelGuideResponse> GenerateTravelGuideAsync(
        GenerateTravelGuideRequest request, 
        Guid userId,
        Func<int, string, Task>? onProgress = null);
}