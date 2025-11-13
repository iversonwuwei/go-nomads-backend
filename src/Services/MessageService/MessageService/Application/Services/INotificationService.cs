using MessageService.Application.DTOs;

namespace MessageService.Application.Services;

/// <summary>
/// 通知服务接口
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// 获取用户通知列表（支持筛选）
    /// </summary>
    Task<(List<NotificationDto> Notifications, int TotalCount)> GetUserNotificationsAsync(
        string userId,
        bool? isRead = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取未读通知数量
    /// </summary>
    Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建通知
    /// </summary>
    Task<NotificationDto> CreateNotificationAsync(CreateNotificationDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送通知给所有管理员
    /// </summary>
    Task<List<NotificationDto>> SendToAdminsAsync(SendToAdminsDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记通知为已读
    /// </summary>
    Task<bool> MarkAsReadAsync(string notificationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量标记通知为已读
    /// </summary>
    Task<int> MarkMultipleAsReadAsync(string userId, List<string> notificationIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记所有通知为已读
    /// </summary>
    Task<int> MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除通知
    /// </summary>
    Task<bool> DeleteNotificationAsync(string notificationId, CancellationToken cancellationToken = default);
}
