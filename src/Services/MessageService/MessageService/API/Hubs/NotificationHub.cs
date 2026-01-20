using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MessageService.API.Hubs;

/// <summary>
///     通用通知 Hub
///     注意：暂时允许匿名访问，通过 JoinUserGroup 方法加入用户组
/// </summary>
[AllowAnonymous]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
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
            _logger.LogInformation("用户 {UserId} 连接到 NotificationHub, ConnectionId: {ConnectionId}",
                userId, Context.ConnectionId);
        }
        else
        {
            _logger.LogWarning("匿名用户连接到 NotificationHub, ConnectionId: {ConnectionId}，需要调用 JoinUserGroup 加入用户组",
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
        _logger.LogInformation("ConnectionId {ConnectionId} 加入 NotificationHub 用户组: user-{UserId}",
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
        _logger.LogInformation("ConnectionId {ConnectionId} 离开 NotificationHub 用户组: user-{UserId}",
            Context.ConnectionId, userId);
    }

    /// <summary>
    ///     订阅城市更新（用于接收城市评分等实时更新）
    /// </summary>
    /// <param name="cityId">城市 ID</param>
    public async Task SubscribeCity(string cityId)
    {
        if (string.IsNullOrEmpty(cityId))
        {
            _logger.LogWarning("SubscribeCity 调用失败：cityId 为空, ConnectionId: {ConnectionId}",
                Context.ConnectionId);
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"city-{cityId}");
        _logger.LogInformation("ConnectionId {ConnectionId} 订阅城市: city-{CityId}",
            Context.ConnectionId, cityId);
    }

    /// <summary>
    ///     取消订阅城市更新
    /// </summary>
    /// <param name="cityId">城市 ID</param>
    public async Task UnsubscribeCity(string cityId)
    {
        if (string.IsNullOrEmpty(cityId))
        {
            return;
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"city-{cityId}");
        _logger.LogInformation("ConnectionId {ConnectionId} 取消订阅城市: city-{CityId}",
            Context.ConnectionId, cityId);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst("sub")?.Value
                     ?? Context.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
                         ?.Value;

        if (!string.IsNullOrEmpty(userId))
            _logger.LogInformation("用户 {UserId} 断开 NotificationHub, ConnectionId: {ConnectionId}",
                userId, Context.ConnectionId);

        await base.OnDisconnectedAsync(exception);
    }
}