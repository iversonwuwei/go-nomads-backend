using MessageService.API.Hubs;
using MessageService.Application.DTOs;
using MessageService.Application.Services;
using Microsoft.AspNetCore.SignalR;

namespace MessageService.API.Services;

/// <summary>
/// SignalR 推送服务实现
/// </summary>
public class SignalRNotifierImpl : ISignalRNotifier
{
    private readonly IHubContext<AIProgressHub> _aiProgressHub;
    private readonly IHubContext<NotificationHub> _notificationHub;
    private readonly ILogger<SignalRNotifierImpl> _logger;

    public SignalRNotifierImpl(
        IHubContext<AIProgressHub> aiProgressHub,
        IHubContext<NotificationHub> notificationHub,
        ILogger<SignalRNotifierImpl> logger)
    {
        _aiProgressHub = aiProgressHub;
        _notificationHub = notificationHub;
        _logger = logger;
    }

    public async Task SendAIProgressAsync(string userId, AIProgressMessage progress)
    {
        try
        {
            await _aiProgressHub.Clients
                .Group($"user-{userId}")
                .SendAsync("ReceiveProgress", progress);

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
            await _aiProgressHub.Clients
                .Group($"task-{taskId}")
                .SendAsync("ReceiveTaskUpdate", update);

            _logger.LogInformation("推送任务更新: TaskId={TaskId}", taskId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "推送任务更新失败: TaskId={TaskId}", taskId);
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
}
