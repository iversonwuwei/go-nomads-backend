using MessageService.Domain.Entities;

namespace MessageService.Domain.Repositories;

/// <summary>
/// 通知仓储接口
/// </summary>
public interface INotificationRepository
{
    /// <summary>
    /// 获取用户通知列表（支持筛选）
    /// </summary>
    Task<(List<Notification> Notifications, int TotalCount)> GetUserNotificationsAsync(
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
    /// 根据ID获取通知
    /// </summary>
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建通知
    /// </summary>
    Task<Notification> CreateAsync(Notification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量创建通知
    /// </summary>
    Task<List<Notification>> CreateBatchAsync(List<Notification> notifications, CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记通知为已读
    /// </summary>
    Task<bool> MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量标记通知为已读
    /// </summary>
    Task<int> MarkMultipleAsReadAsync(List<Guid> ids, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记所有通知为已读
    /// </summary>
    Task<int> MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除通知
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有管理员用户ID
    /// </summary>
    Task<List<string>> GetAdminUserIdsAsync(CancellationToken cancellationToken = default);
}
