using Microsoft.AspNetCore.SignalR;

namespace AIService.API.Hubs;

/// <summary>
/// SignalR 通知中心
/// </summary>
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;
        _logger.LogInformation("🔌 SignalR 客户端已连接: {ConnectionId}", connectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        if (exception != null)
        {
            _logger.LogWarning(exception, "⚠️ SignalR 客户端异常断开: {ConnectionId}", connectionId);
        }
        else
        {
            _logger.LogInformation("🔌 SignalR 客户端已断开: {ConnectionId}", connectionId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// 订阅任务通知
    /// </summary>
    public async Task SubscribeToTask(string taskId)
    {
        var groupName = $"task_{taskId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("📢 客户端 {ConnectionId} 订阅任务: {TaskId}", Context.ConnectionId, taskId);
    }

    /// <summary>
    /// 取消订阅任务通知
    /// </summary>
    public async Task UnsubscribeFromTask(string taskId)
    {
        var groupName = $"task_{taskId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("🔕 客户端 {ConnectionId} 取消订阅任务: {TaskId}", Context.ConnectionId, taskId);
    }
}

/// <summary>
/// SignalR 通知服务
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// 发送任务进度更新
    /// </summary>
    Task SendTaskProgressAsync(string taskId, int progress, string? message = null);

    /// <summary>
    /// 发送任务完成通知
    /// </summary>
    Task SendTaskCompletedAsync(string taskId, string planId);

    /// <summary>
    /// 发送任务失败通知
    /// </summary>
    Task SendTaskFailedAsync(string taskId, string error);
}

/// <summary>
/// SignalR 通知服务实现
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IHubContext<NotificationHub> hubContext,
        ILogger<NotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendTaskProgressAsync(string taskId, int progress, string? message = null)
    {
        var groupName = $"task_{taskId}";
        await _hubContext.Clients.Group(groupName).SendAsync("TaskProgress", new
        {
            TaskId = taskId,
            Progress = progress,
            Message = message,
            Timestamp = DateTime.UtcNow
        });
        _logger.LogInformation("📊 任务进度通知已发送: {TaskId} - {Progress}%", taskId, progress);
    }

    public async Task SendTaskCompletedAsync(string taskId, string planId)
    {
        var groupName = $"task_{taskId}";
        await _hubContext.Clients.Group(groupName).SendAsync("TaskCompleted", new
        {
            TaskId = taskId,
            PlanId = planId,
            Timestamp = DateTime.UtcNow
        });
        _logger.LogInformation("✅ 任务完成通知已发送: {TaskId} - PlanId: {PlanId}", taskId, planId);
    }

    public async Task SendTaskFailedAsync(string taskId, string error)
    {
        var groupName = $"task_{taskId}";
        await _hubContext.Clients.Group(groupName).SendAsync("TaskFailed", new
        {
            TaskId = taskId,
            Error = error,
            Timestamp = DateTime.UtcNow
        });
        _logger.LogError("❌ 任务失败通知已发送: {TaskId} - Error: {Error}", taskId, error);
    }
}
