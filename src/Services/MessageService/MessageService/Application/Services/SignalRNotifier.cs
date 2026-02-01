using MessageService.Application.DTOs;

namespace MessageService.Application.Services;

/// <summary>
///     SignalR 推送服务接口（Application 层定义，API 层实现）
/// </summary>
public interface ISignalRNotifier
{
    Task SendAIProgressAsync(string userId, AIProgressMessage progress);
    Task SendTaskUpdateAsync(string taskId, object update);
    Task SendTaskCompletedAsync(string taskId, string userId, object result);
    Task SendTaskFailedAsync(string taskId, string userId, string error);
    Task SendNotificationAsync(string userId, NotificationMessage notification);
    Task BroadcastNotificationAsync(NotificationMessage notification);

    /// <summary>
    ///     发送 AI Chat 流式响应块
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="chunk">响应块数据</param>
    Task SendAIChatChunkAsync(string userId, object chunk);

    /// <summary>
    ///     发送城市图片更新通知
    /// </summary>
    /// <param name="cityId">城市ID</param>
    /// <param name="userId">用户ID</param>
    /// <param name="imageData">图片数据</param>
    Task SendCityImageUpdatedAsync(string cityId, string userId, object imageData);

    /// <summary>
    ///     发送聊天室在线状态更新
    /// </summary>
    /// <param name="roomId">聊天室ID</param>
    /// <param name="onlineStatusData">在线状态数据</param>
    Task SendChatRoomOnlineStatusAsync(string roomId, object onlineStatusData);

    /// <summary>
    ///     发送 Coworking 验证人数更新
    /// </summary>
    /// <param name="coworkingId">Coworking ID</param>
    /// <param name="votesData">验证人数数据</param>
    Task SendCoworkingVerificationVotesAsync(string coworkingId, object votesData);

    /// <summary>
    ///     发送城市评分更新通知（广播给所有订阅该城市的用户）
    /// </summary>
    /// <param name="cityId">城市ID</param>
    /// <param name="ratingData">评分数据 (包含 overallScore, reviewCount 等)</param>
    Task BroadcastCityRatingUpdatedAsync(string cityId, object ratingData);

    /// <summary>
    ///     发送城市评论更新通知（广播给所有订阅该城市的用户）
    /// </summary>
    /// <param name="cityId">城市ID</param>
    /// <param name="reviewData">评论数据 (包含 changeType, reviewCount 等)</param>
    Task BroadcastCityReviewUpdatedAsync(string cityId, object reviewData);
}