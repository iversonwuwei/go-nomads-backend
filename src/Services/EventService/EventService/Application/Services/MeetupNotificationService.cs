using EventService.API.Hubs;
using EventService.Application.DTOs;
using Microsoft.AspNetCore.SignalR;

namespace EventService.Application.Services;

/// <summary>
/// Meetup 通知服务接口
/// 用于向客户端推送 Meetup 相关的实时更新
/// 推送完整的 Meetup 数据以支持单点更新
/// </summary>
public interface IMeetupNotificationService
{
    /// <summary>
    /// 通知 Meetup 已创建 - 推送完整的 Meetup 数据
    /// </summary>
    Task NotifyMeetupCreatedAsync(EventResponse meetup);

    /// <summary>
    /// 通知 Meetup 已更新 - 推送完整的 Meetup 数据
    /// </summary>
    Task NotifyMeetupUpdatedAsync(EventResponse meetup);

    /// <summary>
    /// 通知 Meetup 已删除
    /// </summary>
    Task NotifyMeetupDeletedAsync(string meetupId, string? cityId);

    /// <summary>
    /// 通知 Meetup 已取消 - 推送完整的 Meetup 数据
    /// </summary>
    Task NotifyMeetupCancelledAsync(EventResponse meetup);

    /// <summary>
    /// 通知参与者加入 - 推送 meetupId 和新的参与人数
    /// </summary>
    Task NotifyParticipantJoinedAsync(string meetupId, string? cityId, string userId, int newParticipantCount);

    /// <summary>
    /// 通知参与者离开 - 推送 meetupId 和新的参与人数
    /// </summary>
    Task NotifyParticipantLeftAsync(string meetupId, string? cityId, string userId, int newParticipantCount);

    /// <summary>
    /// 通知特定用户（如被邀请加入 Meetup）
    /// </summary>
    Task NotifyUserAsync(string userId, string eventName, object data);
}

/// <summary>
/// Meetup 通知服务实现
/// </summary>
public class MeetupNotificationService : IMeetupNotificationService
{
    private readonly IHubContext<MeetupHub> _hubContext;
    private readonly ILogger<MeetupNotificationService> _logger;

    public MeetupNotificationService(
        IHubContext<MeetupHub> hubContext,
        ILogger<MeetupNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task NotifyMeetupCreatedAsync(EventResponse meetup)
    {
        _logger.LogInformation("📤 [MeetupNotification] Sending MeetupCreated: {MeetupId} in city {CityId}",
            meetup.Id, meetup.CityId);

        var tasks = new List<Task>();
        var cityId = meetup.CityId?.ToString();

        // 通知订阅该城市的所有用户
        if (!string.IsNullOrEmpty(cityId))
        {
            var cityGroup = MeetupHub.GetCityGroupName(cityId);
            tasks.Add(_hubContext.Clients.Group(cityGroup)
                .SendAsync("MeetupCreated", meetup));
        }

        // 也发送到全局，让未指定城市筛选的用户也能收到
        tasks.Add(_hubContext.Clients.All.SendAsync("MeetupCreated", meetup));

        await Task.WhenAll(tasks);
    }

    /// <inheritdoc/>
    public async Task NotifyMeetupUpdatedAsync(EventResponse meetup)
    {
        _logger.LogInformation("📤 [MeetupNotification] Sending MeetupUpdated: {MeetupId}", meetup.Id);

        var tasks = new List<Task>();
        var meetupId = meetup.Id.ToString();
        var cityId = meetup.CityId?.ToString();

        // 通知订阅该 Meetup 的用户（详情页）
        var meetupGroup = MeetupHub.GetMeetupGroupName(meetupId);
        tasks.Add(_hubContext.Clients.Group(meetupGroup)
            .SendAsync("MeetupUpdated", meetup));

        // 通知订阅该城市的用户（列表页）
        if (!string.IsNullOrEmpty(cityId))
        {
            var cityGroup = MeetupHub.GetCityGroupName(cityId);
            tasks.Add(_hubContext.Clients.Group(cityGroup)
                .SendAsync("MeetupUpdated", meetup));
        }

        // 也发送到全局
        tasks.Add(_hubContext.Clients.All.SendAsync("MeetupUpdated", meetup));

        await Task.WhenAll(tasks);
    }

    /// <inheritdoc/>
    public async Task NotifyMeetupDeletedAsync(string meetupId, string? cityId)
    {
        _logger.LogInformation("📤 [MeetupNotification] Sending MeetupDeleted: {MeetupId}", meetupId);

        var tasks = new List<Task>();

        // 通知订阅该 Meetup 的用户
        var meetupGroup = MeetupHub.GetMeetupGroupName(meetupId);
        tasks.Add(_hubContext.Clients.Group(meetupGroup)
            .SendAsync("MeetupDeleted", meetupId));

        // 通知订阅该城市的用户
        if (!string.IsNullOrEmpty(cityId))
        {
            var cityGroup = MeetupHub.GetCityGroupName(cityId);
            tasks.Add(_hubContext.Clients.Group(cityGroup)
                .SendAsync("MeetupDeleted", meetupId));
        }

        // 也发送到全局
        tasks.Add(_hubContext.Clients.All.SendAsync("MeetupDeleted", meetupId));

        await Task.WhenAll(tasks);
    }

    /// <inheritdoc/>
    public async Task NotifyMeetupCancelledAsync(EventResponse meetup)
    {
        _logger.LogInformation("📤 [MeetupNotification] Sending MeetupCancelled: {MeetupId}", meetup.Id);

        var tasks = new List<Task>();
        var meetupId = meetup.Id.ToString();
        var cityId = meetup.CityId?.ToString();

        // 通知订阅该 Meetup 的用户
        var meetupGroup = MeetupHub.GetMeetupGroupName(meetupId);
        tasks.Add(_hubContext.Clients.Group(meetupGroup)
            .SendAsync("MeetupCancelled", meetup));

        // 通知订阅该城市的用户
        if (!string.IsNullOrEmpty(cityId))
        {
            var cityGroup = MeetupHub.GetCityGroupName(cityId);
            tasks.Add(_hubContext.Clients.Group(cityGroup)
                .SendAsync("MeetupCancelled", meetup));
        }

        // 也发送到全局
        tasks.Add(_hubContext.Clients.All.SendAsync("MeetupCancelled", meetup));

        await Task.WhenAll(tasks);
    }

    /// <inheritdoc/>
    public async Task NotifyParticipantJoinedAsync(string meetupId, string? cityId, string userId, int newParticipantCount)
    {
        _logger.LogInformation("📤 [MeetupNotification] Sending ParticipantJoined: User {UserId} joined {MeetupId}, new count: {Count}",
            userId, meetupId, newParticipantCount);

        var tasks = new List<Task>();

        // 通知订阅该 Meetup 的用户
        var meetupGroup = MeetupHub.GetMeetupGroupName(meetupId);
        tasks.Add(_hubContext.Clients.Group(meetupGroup)
            .SendAsync("ParticipantJoined", meetupId, userId, newParticipantCount));

        // 通知订阅该城市的用户（更新参与人数显示）
        if (!string.IsNullOrEmpty(cityId))
        {
            var cityGroup = MeetupHub.GetCityGroupName(cityId);
            tasks.Add(_hubContext.Clients.Group(cityGroup)
                .SendAsync("ParticipantJoined", meetupId, userId, newParticipantCount));
        }

        await Task.WhenAll(tasks);
    }

    /// <inheritdoc/>
    public async Task NotifyParticipantLeftAsync(string meetupId, string? cityId, string userId, int newParticipantCount)
    {
        _logger.LogInformation("📤 [MeetupNotification] Sending ParticipantLeft: User {UserId} left {MeetupId}, new count: {Count}",
            userId, meetupId, newParticipantCount);

        var tasks = new List<Task>();

        // 通知订阅该 Meetup 的用户
        var meetupGroup = MeetupHub.GetMeetupGroupName(meetupId);
        tasks.Add(_hubContext.Clients.Group(meetupGroup)
            .SendAsync("ParticipantLeft", meetupId, userId, newParticipantCount));

        // 通知订阅该城市的用户
        if (!string.IsNullOrEmpty(cityId))
        {
            var cityGroup = MeetupHub.GetCityGroupName(cityId);
            tasks.Add(_hubContext.Clients.Group(cityGroup)
                .SendAsync("ParticipantLeft", meetupId, userId, newParticipantCount));
        }

        await Task.WhenAll(tasks);
    }

    /// <inheritdoc/>
    public async Task NotifyUserAsync(string userId, string eventName, object data)
    {
        _logger.LogInformation("📤 [MeetupNotification] Sending {Event} to user {UserId}",
            eventName, userId);

        var userGroup = MeetupHub.GetUserGroupName(userId);
        await _hubContext.Clients.Group(userGroup).SendAsync(eventName, data);
    }
}
