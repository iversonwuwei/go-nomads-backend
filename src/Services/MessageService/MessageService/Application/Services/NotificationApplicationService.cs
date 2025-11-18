using System.Text.Json;
using MessageService.Application.DTOs;
using MessageService.Domain.Entities;
using MessageService.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace MessageService.Application.Services;

/// <summary>
///     通知应用服务实现
/// </summary>
public class NotificationApplicationService : INotificationService
{
    private readonly ILogger<NotificationApplicationService> _logger;
    private readonly INotificationRepository _repository;

    public NotificationApplicationService(
        INotificationRepository repository,
        ILogger<NotificationApplicationService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<(List<NotificationDto> Notifications, int TotalCount)> GetUserNotificationsAsync(
        string userId,
        bool? isRead = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var (notifications, totalCount) = await _repository.GetUserNotificationsAsync(
            userId, isRead, page, pageSize, cancellationToken);

        var dtos = notifications.Select(MapToDto).ToList();

        return (dtos, totalCount);
    }

    public async Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _repository.GetUnreadCountAsync(userId, cancellationToken);
    }

    public async Task<NotificationDto> CreateNotificationAsync(
        CreateNotificationDto dto,
        CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            UserId = dto.UserId,
            Title = dto.Title,
            Message = dto.Message,
            Type = dto.Type,
            RelatedId = dto.RelatedId,
            Metadata = dto.Metadata != null ? JsonSerializer.Serialize(dto.Metadata) : null
        };

        var created = await _repository.CreateAsync(notification, cancellationToken);

        return MapToDto(created);
    }

    public async Task<List<NotificationDto>> SendToAdminsAsync(
        SendToAdminsDto dto,
        CancellationToken cancellationToken = default)
    {
        // 获取所有管理员用户ID
        var adminUserIds = await _repository.GetAdminUserIdsAsync(cancellationToken);

        if (adminUserIds == null || adminUserIds.Count == 0)
        {
            _logger.LogWarning("⚠️ 没有找到管理员用户");
            return new List<NotificationDto>();
        }

        // 为每个管理员创建通知
        var notifications = adminUserIds.Select(adminUserId => new Notification
        {
            UserId = adminUserId,
            Title = dto.Title,
            Message = dto.Message,
            Type = dto.Type,
            RelatedId = dto.RelatedId,
            Metadata = dto.Metadata != null ? JsonSerializer.Serialize(dto.Metadata) : null
        }).ToList();

        var created = await _repository.CreateBatchAsync(notifications, cancellationToken);

        _logger.LogInformation("✅ 发送通知给 {Count} 位管理员", created.Count);

        return created.Select(MapToDto).ToList();
    }

    public async Task<bool> MarkAsReadAsync(string notificationId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(notificationId, out var id)) return false;

        return await _repository.MarkAsReadAsync(id, cancellationToken);
    }

    public async Task<int> MarkMultipleAsReadAsync(
        string userId,
        List<string> notificationIds,
        CancellationToken cancellationToken = default)
    {
        var ids = notificationIds
            .Select(id => Guid.TryParse(id, out var guid) ? guid : (Guid?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();

        return await _repository.MarkMultipleAsReadAsync(ids, userId, cancellationToken);
    }

    public async Task<int> MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _repository.MarkAllAsReadAsync(userId, cancellationToken);
    }

    public async Task<bool> DeleteNotificationAsync(string notificationId,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(notificationId, out var id)) return false;

        return await _repository.DeleteAsync(id, cancellationToken);
    }

    private static NotificationDto MapToDto(Notification notification)
    {
        Dictionary<string, object>? metadata = null;
        if (!string.IsNullOrEmpty(notification.Metadata))
            try
            {
                metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(notification.Metadata);
            }
            catch
            {
                // 忽略 JSON 解析错误
            }

        return new NotificationDto
        {
            Id = notification.Id.ToString(),
            UserId = notification.UserId,
            Title = notification.Title,
            Message = notification.Message,
            Type = notification.Type,
            RelatedId = notification.RelatedId,
            Metadata = metadata,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt,
            ReadAt = notification.ReadAt
        };
    }
}