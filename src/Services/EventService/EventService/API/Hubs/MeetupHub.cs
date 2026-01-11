using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace EventService.API.Hubs;

/// <summary>
/// Meetup/Event 实时通信 Hub
/// 用于推送活动创建、更新、删除、参与者变化等实时事件
/// </summary>
[AllowAnonymous] // 暂时允许匿名访问，后续可改为 [Authorize]
public class MeetupHub : Hub
{
    private readonly ILogger<MeetupHub> _logger;

    public MeetupHub(ILogger<MeetupHub> logger)
    {
        _logger = logger;
    }

    #region 连接生命周期

    /// <summary>
    /// 客户端连接时
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("📡 [MeetupHub] User {UserId} connected, ConnectionId: {ConnectionId}",
            userId ?? "Anonymous", Context.ConnectionId);

        // 自动加入用户个人组，用于接收与自己相关的通知
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroupName(userId));
            _logger.LogInformation("📡 [MeetupHub] User {UserId} joined personal group", userId);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// 客户端断开时
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        _logger.LogInformation("📡 [MeetupHub] User {UserId} disconnected, ConnectionId: {ConnectionId}, Error: {Error}",
            userId ?? "Anonymous", Context.ConnectionId, exception?.Message);

        await base.OnDisconnectedAsync(exception);
    }

    #endregion

    #region 订阅方法

    /// <summary>
    /// 订阅特定城市的 Meetup 更新
    /// </summary>
    /// <param name="cityId">城市ID</param>
    public async Task SubscribeToCityMeetups(string cityId)
    {
        if (string.IsNullOrEmpty(cityId))
        {
            _logger.LogWarning("📡 [MeetupHub] SubscribeToCityMeetups called with empty cityId");
            return;
        }

        var groupName = GetCityGroupName(cityId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation("📡 [MeetupHub] ConnectionId {ConnectionId} subscribed to city: {CityId}",
            Context.ConnectionId, cityId);
    }

    /// <summary>
    /// 取消订阅特定城市的 Meetup 更新
    /// </summary>
    /// <param name="cityId">城市ID</param>
    public async Task UnsubscribeFromCityMeetups(string cityId)
    {
        if (string.IsNullOrEmpty(cityId))
        {
            return;
        }

        var groupName = GetCityGroupName(cityId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation("📡 [MeetupHub] ConnectionId {ConnectionId} unsubscribed from city: {CityId}",
            Context.ConnectionId, cityId);
    }

    /// <summary>
    /// 订阅特定 Meetup 的更新（用于详情页）
    /// </summary>
    /// <param name="meetupId">Meetup ID</param>
    public async Task SubscribeToMeetup(string meetupId)
    {
        if (string.IsNullOrEmpty(meetupId))
        {
            return;
        }

        var groupName = GetMeetupGroupName(meetupId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation("📡 [MeetupHub] ConnectionId {ConnectionId} subscribed to meetup: {MeetupId}",
            Context.ConnectionId, meetupId);
    }

    /// <summary>
    /// 取消订阅特定 Meetup 的更新
    /// </summary>
    /// <param name="meetupId">Meetup ID</param>
    public async Task UnsubscribeFromMeetup(string meetupId)
    {
        if (string.IsNullOrEmpty(meetupId))
        {
            return;
        }

        var groupName = GetMeetupGroupName(meetupId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation("📡 [MeetupHub] ConnectionId {ConnectionId} unsubscribed from meetup: {MeetupId}",
            Context.ConnectionId, meetupId);
    }

    #endregion

    #region 静态辅助方法

    /// <summary>
    /// 获取城市组名
    /// </summary>
    public static string GetCityGroupName(string cityId) => $"city_{cityId}";

    /// <summary>
    /// 获取 Meetup 组名
    /// </summary>
    public static string GetMeetupGroupName(string meetupId) => $"meetup_{meetupId}";

    /// <summary>
    /// 获取用户组名
    /// </summary>
    public static string GetUserGroupName(string userId) => $"user_{userId}";

    #endregion
}
