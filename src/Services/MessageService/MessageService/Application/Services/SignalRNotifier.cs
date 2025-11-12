using MessageService.Application.DTOs;

namespace MessageService.Application.Services;

/// <summary>
/// SignalR 推送服务接口（Application 层定义，API 层实现）
/// </summary>
public interface ISignalRNotifier
{
    Task SendAIProgressAsync(string userId, AIProgressMessage progress);
    Task SendTaskUpdateAsync(string taskId, object update);
    Task SendNotificationAsync(string userId, NotificationMessage notification);
    Task BroadcastNotificationAsync(NotificationMessage notification);
}
