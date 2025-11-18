using Microsoft.AspNetCore.SignalR;

namespace AIService.API.Hubs;

/// <summary>
///     SignalR 通知中心
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
            _logger.LogWarning(exception, "⚠️ SignalR 客户端异常断开: {ConnectionId}", connectionId);
        else
            _logger.LogInformation("🔌 SignalR 客户端已断开: {ConnectionId}", connectionId);
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    ///     订阅任务通知
    /// </summary>
    public async Task SubscribeToTask(string taskId)
    {
        var groupName = $"task_{taskId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("📢 客户端 {ConnectionId} 订阅任务: {TaskId}", Context.ConnectionId, taskId);
    }

    /// <summary>
    ///     取消订阅任务通知
    /// </summary>
    public async Task UnsubscribeFromTask(string taskId)
    {
        var groupName = $"task_{taskId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("🔕 客户端 {ConnectionId} 取消订阅任务: {TaskId}", Context.ConnectionId, taskId);
    }
}

/// <summary>
///     SignalR 通知服务
/// </summary>
public interface INotificationService
{
    /// <summary>
    ///     发送任务进度更新
    /// </summary>
    Task SendTaskProgressAsync(string taskId, int progress, string? message = null);

    /// <summary>
    ///     发送任务完成通知
    /// </summary>
    Task SendTaskCompletedAsync(string taskId, string? planId = null, string? guideId = null, object? result = null);

    /// <summary>
    ///     发送任务失败通知
    /// </summary>
    Task SendTaskFailedAsync(string taskId, string error);
}

/// <summary>
///     SignalR 通知服务实现
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
        var now = DateTime.UtcNow;
        await _hubContext.Clients.Group(groupName).SendAsync("TaskProgress", new
        {
            taskId,
            status = "processing",
            progress,
            progressMessage = message,
            createdAt = now.ToString("o"),
            updatedAt = now.ToString("o")
        });
        _logger.LogInformation("📊 任务进度通知已发送: {TaskId} - {Progress}%", taskId, progress);
    }

    public async Task SendTaskCompletedAsync(string taskId, string? planId = null, string? guideId = null,
        object? result = null)
    {
        var groupName = $"task_{taskId}";
        var now = DateTime.UtcNow;

        var payload = new Dictionary<string, object?>
        {
            ["taskId"] = taskId,
            ["status"] = "completed",
            ["planId"] = planId,
            ["guideId"] = guideId,
            ["progress"] = 100,
            ["progressMessage"] = "任务已完成",
            ["createdAt"] = now.ToString("o"),
            ["updatedAt"] = now.ToString("o"),
            ["completedAt"] = now.ToString("o")
        };

        // 如果有 result 数据，添加到 payload 中
        if (result != null) payload["result"] = result;

        await _hubContext.Clients.Group(groupName).SendAsync("TaskCompleted", payload);
        _logger.LogInformation("✅ 任务完成通知已发送: {TaskId} - PlanId: {PlanId}, GuideId: {GuideId}", taskId, planId, guideId);
    }

    public async Task SendTaskFailedAsync(string taskId, string error)
    {
        var groupName = $"task_{taskId}";
        var now = DateTime.UtcNow;
        await _hubContext.Clients.Group(groupName).SendAsync("TaskFailed", new
        {
            taskId,
            status = "failed",
            error,
            progress = 0,
            progressMessage = "任务失败",
            createdAt = now.ToString("o"),
            updatedAt = now.ToString("o"),
            completedAt = now.ToString("o")
        });
        _logger.LogError("❌ 任务失败通知已发送: {TaskId} - Error: {Error}", taskId, error);
    }
}