using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MessageService.API.Hubs;

/// <summary>
/// AI 进度推送 Hub
/// </summary>
[Authorize]
public class AIProgressHub : Hub
{
    private readonly ILogger<AIProgressHub> _logger;

    public AIProgressHub(ILogger<AIProgressHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("sub")?.Value 
                     ?? Context.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
            _logger.LogInformation("用户 {UserId} 连接到 AIProgressHub, ConnectionId: {ConnectionId}", 
                userId, Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// 订阅特定 AI 任务进度
    /// </summary>
    public async Task SubscribeToTask(string taskId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"task-{taskId}");
        _logger.LogInformation("ConnectionId {ConnectionId} 订阅任务: {TaskId}", 
            Context.ConnectionId, taskId);
    }

    /// <summary>
    /// 取消订阅任务
    /// </summary>
    public async Task UnsubscribeFromTask(string taskId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"task-{taskId}");
        _logger.LogInformation("ConnectionId {ConnectionId} 取消订阅任务: {TaskId}", 
            Context.ConnectionId, taskId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst("sub")?.Value 
                     ?? Context.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        
        if (!string.IsNullOrEmpty(userId))
        {
            _logger.LogInformation("用户 {UserId} 断开连接, ConnectionId: {ConnectionId}", 
                userId, Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
