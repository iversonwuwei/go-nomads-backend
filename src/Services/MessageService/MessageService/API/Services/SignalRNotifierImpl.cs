using MessageService.API.Hubs;
using MessageService.Application.DTOs;
using MessageService.Application.Services;
using Microsoft.AspNetCore.SignalR;

namespace MessageService.API.Services;

/// <summary>
///     SignalR 推送服务实现
/// </summary>
public class SignalRNotifierImpl : ISignalRNotifier
{
    private readonly IHubContext<AIProgressHub> _aiProgressHub;
    private readonly IHubContext<ChatHub> _chatHub;
    private readonly ILogger<SignalRNotifierImpl> _logger;
    private readonly IHubContext<NotificationHub> _notificationHub;

    public SignalRNotifierImpl(
        IHubContext<AIProgressHub> aiProgressHub,
        IHubContext<NotificationHub> notificationHub,
        IHubContext<ChatHub> chatHub,
        ILogger<SignalRNotifierImpl> logger)
    {
        _aiProgressHub = aiProgressHub;
        _notificationHub = notificationHub;
        _chatHub = chatHub;
        _logger = logger;
    }

    public async Task SendAIProgressAsync(string userId, AIProgressMessage progress)
    {
        try
        {
            // 发送到用户组
            await _aiProgressHub.Clients
                .Group($"user-{userId}")
                .SendAsync("TaskProgress", progress); // 修改为 TaskProgress

            _logger.LogInformation("推送 AI 进度到用户 {UserId}: TaskId={TaskId}, Progress={Progress}%",
                userId, progress.TaskId, progress.Progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "推送 AI 进度失败: UserId={UserId}, TaskId={TaskId}",
                userId, progress.TaskId);
            throw;
        }
    }

    public async Task SendTaskUpdateAsync(string taskId, object update)
    {
        try
        {
            // 发送到任务组
            await _aiProgressHub.Clients
                .Group($"task-{taskId}")
                .SendAsync("TaskProgress", update); // 修改为 TaskProgress

            _logger.LogInformation("推送任务更新: TaskId={TaskId}", taskId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "推送任务更新失败: TaskId={TaskId}", taskId);
            throw;
        }
    }

    public async Task SendTaskCompletedAsync(string taskId, string userId, object result)
    {
        try
        {
            // 发送到任务组和用户组
            await _aiProgressHub.Clients
                .Group($"task-{taskId}")
                .SendAsync("TaskCompleted", result);

            await _aiProgressHub.Clients
                .Group($"user-{userId}")
                .SendAsync("TaskCompleted", result);

            _logger.LogInformation("推送任务完成事件: TaskId={TaskId}, UserId={UserId}", taskId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "推送任务完成事件失败: TaskId={TaskId}", taskId);
            throw;
        }
    }

    public async Task SendTaskFailedAsync(string taskId, string userId, string error)
    {
        try
        {
            var failureData = new { TaskId = taskId, Error = error, Timestamp = DateTime.UtcNow };

            // 发送到任务组和用户组
            await _aiProgressHub.Clients
                .Group($"task-{taskId}")
                .SendAsync("TaskFailed", failureData);

            await _aiProgressHub.Clients
                .Group($"user-{userId}")
                .SendAsync("TaskFailed", failureData);

            _logger.LogInformation("推送任务失败事件: TaskId={TaskId}, UserId={UserId}", taskId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "推送任务失败事件失败: TaskId={TaskId}", taskId);
            throw;
        }
    }

    public async Task SendNotificationAsync(string userId, NotificationMessage notification)
    {
        try
        {
            await _notificationHub.Clients
                .Group($"user-{userId}")
                .SendAsync("ReceiveNotification", notification);

            _logger.LogInformation("推送通知到用户 {UserId}: Type={Type}, Title={Title}",
                userId, notification.Type, notification.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "推送通知失败: UserId={UserId}", userId);
            throw;
        }
    }

    public async Task BroadcastNotificationAsync(NotificationMessage notification)
    {
        try
        {
            await _notificationHub.Clients.All
                .SendAsync("ReceiveNotification", notification);

            _logger.LogInformation("广播通知: Type={Type}, Title={Title}",
                notification.Type, notification.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "广播通知失败");
            throw;
        }
    }

    public async Task SendCityImageUpdatedAsync(string cityId, string userId, object imageData)
    {
        try
        {
            // 发送到用户组 - 城市图片更新事件
            await _aiProgressHub.Clients
                .Group($"user-{userId}")
                .SendAsync("CityImageUpdated", imageData);

            _logger.LogInformation("推送城市图片更新通知: CityId={CityId}, UserId={UserId}",
                cityId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "推送城市图片更新通知失败: CityId={CityId}, UserId={UserId}",
                cityId, userId);
            throw;
        }
    }

    public async Task SendChatRoomOnlineStatusAsync(string roomId, object onlineStatusData)
    {
        try
        {
            // 发送到聊天室组 - 在线状态更新事件
            await _chatHub.Clients
                .Group(roomId)
                .SendAsync("OnlineStatusUpdated", onlineStatusData);

            _logger.LogInformation("推送聊天室在线状态更新: RoomId={RoomId}", roomId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "推送聊天室在线状态更新失败: RoomId={RoomId}", roomId);
            throw;
        }
    }
}