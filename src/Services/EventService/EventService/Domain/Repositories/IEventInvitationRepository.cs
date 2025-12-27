using EventService.Domain.Entities;

namespace EventService.Domain.Repositories;

/// <summary>
///     活动邀请仓储接口
/// </summary>
public interface IEventInvitationRepository
{
    /// <summary>
    ///     创建邀请
    /// </summary>
    Task<EventInvitation> CreateAsync(EventInvitation invitation);

    /// <summary>
    ///     根据ID获取邀请
    /// </summary>
    Task<EventInvitation?> GetByIdAsync(Guid id);

    /// <summary>
    ///     更新邀请
    /// </summary>
    Task<EventInvitation> UpdateAsync(EventInvitation invitation);

    /// <summary>
    ///     获取活动的所有邀请
    /// </summary>
    Task<List<EventInvitation>> GetByEventIdAsync(Guid eventId);

    /// <summary>
    ///     获取用户收到的所有邀请
    /// </summary>
    Task<List<EventInvitation>> GetReceivedInvitationsAsync(Guid inviteeId, string? status = null);

    /// <summary>
    ///     获取用户发出的所有邀请
    /// </summary>
    Task<List<EventInvitation>> GetSentInvitationsAsync(Guid inviterId, string? status = null);

    /// <summary>
    ///     检查是否已存在邀请
    /// </summary>
    Task<bool> ExistsAsync(Guid eventId, Guid inviteeId);

    /// <summary>
    ///     获取特定活动对特定用户的待处理邀请
    /// </summary>
    Task<EventInvitation?> GetPendingInvitationAsync(Guid eventId, Guid inviteeId);

    /// <summary>
    ///     删除邀请
    /// </summary>
    Task DeleteAsync(Guid id);
}
