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
    private readonly ISignalRNotifier? _signalRNotifier;

    public NotificationApplicationService(
        INotificationRepository repository,
        ILogger<NotificationApplicationService> logger,
        ISignalRNotifier? signalRNotifier = null)
    {
        _repository = repository;
        _logger = logger;
        _signalRNotifier = signalRNotifier;
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

        // 通过 SignalR 实时推送通知
        if (_signalRNotifier != null)
        {
            try
            {
                var message = new NotificationMessage
                {
                    UserId = created.UserId,
                    Type = created.Type,
                    Title = created.Title,
                    Content = created.Message,
                    Data = new Dictionary<string, object>
                    {
                        { "notificationId", created.Id.ToString() },
                        { "relatedId", created.RelatedId ?? "" },
                        { "isRead", created.IsRead }
                    },
                    CreatedAt = created.CreatedAt
                };
                await _signalRNotifier.SendNotificationAsync(created.UserId, message);
                _logger.LogInformation("📡 SignalR 推送通知给用户: {UserId}", created.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ SignalR 推送通知失败: {UserId}", created.UserId);
            }
        }

        return MapToDto(created);
    }

    public async Task<BatchNotificationResponse> CreateBatchNotificationsAsync(
        CreateBatchNotificationDto dto,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📬 批量创建通知: UserCount={Count}, Type={Type}",
            dto.UserIds.Count, dto.Type);

        if (dto.UserIds == null || dto.UserIds.Count == 0)
        {
            _logger.LogWarning("⚠️ 用户ID列表为空");
            return new BatchNotificationResponse
            {
                CreatedCount = 0,
                NotificationIds = new List<string>()
            };
        }

        // 为每个用户创建通知
        var notifications = dto.UserIds.Select(userId => new Notification
        {
            UserId = userId,
            Title = dto.Title,
            Message = dto.Message,
            Type = dto.Type,
            RelatedId = dto.RelatedId,
            Metadata = dto.Metadata != null ? JsonSerializer.Serialize(dto.Metadata) : null
        }).ToList();

        var created = await _repository.CreateBatchAsync(notifications, cancellationToken);

        _logger.LogInformation("✅ 批量创建通知成功: {Count} 条", created.Count);

        // 通过 SignalR 实时推送通知给每个用户
        if (_signalRNotifier != null)
        {
            foreach (var notification in created)
            {
                try
                {
                    var message = new NotificationMessage
                    {
                        UserId = notification.UserId,
                        Type = notification.Type,
                        Title = notification.Title,
                        Content = notification.Message,
                        Data = new Dictionary<string, object>
                        {
                            { "notificationId", notification.Id.ToString() },
                            { "relatedId", notification.RelatedId ?? "" },
                            { "isRead", notification.IsRead }
                        },
                        CreatedAt = notification.CreatedAt
                    };
                    await _signalRNotifier.SendNotificationAsync(notification.UserId, message);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ SignalR 推送通知失败: {UserId}", notification.UserId);
                }
            }
        }

        return new BatchNotificationResponse
        {
            CreatedCount = created.Count,
            NotificationIds = created.Select(n => n.Id.ToString()).ToList()
        };
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

        // 通过 SignalR 实时推送通知给每个管理员
        if (_signalRNotifier != null)
        {
            foreach (var notification in created)
            {
                try
                {
                    var message = new NotificationMessage
                    {
                        UserId = notification.UserId,
                        Type = notification.Type,
                        Title = notification.Title,
                        Content = notification.Message,
                        Data = new Dictionary<string, object>
                        {
                            { "notificationId", notification.Id.ToString() },
                            { "relatedId", notification.RelatedId ?? "" },
                            { "isRead", notification.IsRead }
                        },
                        CreatedAt = notification.CreatedAt
                    };
                    await _signalRNotifier.SendNotificationAsync(notification.UserId, message);
                    _logger.LogInformation("📡 SignalR 推送通知给管理员: {UserId}", notification.UserId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ SignalR 推送通知失败: {UserId}", notification.UserId);
                    // 不抛出异常，通知已保存到数据库
                }
            }
        }

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