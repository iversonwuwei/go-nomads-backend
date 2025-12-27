using EventService.Application.DTOs;

namespace EventService.Application.Services;

/// <summary>
///     Event 应用服务接口
/// </summary>
public interface IEventService
{
    /// <summary>
    ///     创建 Event
    /// </summary>
    Task<EventResponse> CreateEventAsync(CreateEventRequest request, Guid organizerId);

    /// <summary>
    ///     获取 Event 详情
    /// </summary>
    Task<EventResponse> GetEventAsync(Guid id, Guid? userId = null);

    /// <summary>
    ///     更新 Event
    /// </summary>
    Task<EventResponse> UpdateEventAsync(Guid id, UpdateEventRequest request, Guid userId);

    /// <summary>
    ///     取消活动
    /// </summary>
    Task<EventResponse> CancelEventAsync(Guid id, Guid userId);

    /// <summary>
    ///     获取 Event 列表
    /// </summary>
    Task<(List<EventResponse> Events, int Total)> GetEventsAsync(
        Guid? cityId = null,
        string? category = null,
        string? status = null,
        int page = 1,
        int pageSize = 20,
        Guid? userId = null);

    /// <summary>
    ///     参加 Event
    /// </summary>
    Task<ParticipantResponse> JoinEventAsync(Guid eventId, Guid userId, JoinEventRequest request);

    /// <summary>
    ///     取消参加 Event
    /// </summary>
    Task LeaveEventAsync(Guid eventId, Guid userId);

    /// <summary>
    ///     关注 Event
    /// </summary>
    Task<FollowerResponse> FollowEventAsync(Guid eventId, Guid userId, FollowEventRequest request);

    /// <summary>
    ///     取消关注 Event
    /// </summary>
    Task UnfollowEventAsync(Guid eventId, Guid userId);

    /// <summary>
    ///     获取参与者列表
    /// </summary>
    Task<List<ParticipantResponse>> GetParticipantsAsync(Guid eventId);

    /// <summary>
    ///     获取关注者列表
    /// </summary>
    Task<List<FollowerResponse>> GetFollowersAsync(Guid eventId);

    /// <summary>
    ///     获取用户创建的 Event
    /// </summary>
    Task<List<EventResponse>> GetUserCreatedEventsAsync(Guid userId);

    /// <summary>
    ///     获取用户创建的 Event 数量
    /// </summary>
    Task<int> GetUserCreatedEventsCountAsync(Guid userId);

    /// <summary>
    ///     获取用户参加的 Event
    /// </summary>
    Task<List<EventResponse>> GetUserJoinedEventsAsync(Guid userId);

    /// <summary>
    ///     获取用户关注的 Event
    /// </summary>
    Task<List<EventResponse>> GetUserFollowingEventsAsync(Guid userId);

    /// <summary>
    ///     获取用户已加入的活动列表(分页)
    /// </summary>
    Task<(List<EventResponse> Events, int Total)> GetJoinedEventsAsync(
        Guid userId,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    ///     获取用户取消的活动列表(分页)
    /// </summary>
    Task<(List<EventResponse> Events, int Total)> GetCancelledEventsByUserAsync(
        Guid userId,
        int page = 1,
        int pageSize = 20);

    #region 邀请相关

    /// <summary>
    ///     邀请用户参加活动
    /// </summary>
    Task<EventInvitationResponse> InviteToEventAsync(Guid eventId, Guid inviterId, InviteToEventRequest request);

    /// <summary>
    ///     响应邀请（接受或拒绝）
    /// </summary>
    Task<EventInvitationResponse> RespondToInvitationAsync(Guid invitationId, Guid userId, string response);

    /// <summary>
    ///     获取用户收到的邀请列表
    /// </summary>
    Task<List<EventInvitationResponse>> GetReceivedInvitationsAsync(Guid userId, string? status = null);

    /// <summary>
    ///     获取用户发出的邀请列表
    /// </summary>
    Task<List<EventInvitationResponse>> GetSentInvitationsAsync(Guid userId, string? status = null);

    /// <summary>
    ///     获取邀请详情
    /// </summary>
    Task<EventInvitationResponse> GetInvitationAsync(Guid invitationId);

    #endregion
}