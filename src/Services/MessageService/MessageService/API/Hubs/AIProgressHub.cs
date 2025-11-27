using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MessageService.API.Hubs;

/// <summary>
///     AI 进度推送 Hub
///     注意：暂时允许匿名访问，后续可以添加认证
/// </summary>
[AllowAnonymous]
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
                     ?? Context.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
                         ?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
            _logger.LogInformation("用户 {UserId} 连接到 AIProgressHub, ConnectionId: {ConnectionId}",
                userId, Context.ConnectionId);
        }
        else
        {
            _logger.LogWarning("匿名用户连接到 AIProgressHub, ConnectionId: {ConnectionId}，需要调用 JoinUserGroup 加入用户组",
                Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    ///     加入用户组（用于接收用户相关通知）
    /// </summary>
    /// <param name="userId">用户 ID</param>
    public async Task JoinUserGroup(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("JoinUserGroup 调用失败：userId 为空, ConnectionId: {ConnectionId}",
                Context.ConnectionId);
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        _logger.LogInformation("ConnectionId {ConnectionId} 加入用户组: user-{UserId}",
            Context.ConnectionId, userId);
    }

    /// <summary>
    ///     离开用户组
    /// </summary>
    /// <param name="userId">用户 ID</param>
    public async Task LeaveUserGroup(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");
        _logger.LogInformation("ConnectionId {ConnectionId} 离开用户组: user-{UserId}",
            Context.ConnectionId, userId);
    }

    /// <summary>
    ///     订阅特定 AI 任务进度
    /// </summary>
    public async Task SubscribeToTask(string taskId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"task-{taskId}");
        _logger.LogInformation("ConnectionId {ConnectionId} 订阅任务: {TaskId}",
            Context.ConnectionId, taskId);
    }

    /// <summary>
    ///     取消订阅任务
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
                     ?? Context.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
                         ?.Value;

        if (!string.IsNullOrEmpty(userId))
            _logger.LogInformation("用户 {UserId} 断开连接, ConnectionId: {ConnectionId}",
                userId, Context.ConnectionId);

        await base.OnDisconnectedAsync(exception);
    }
}