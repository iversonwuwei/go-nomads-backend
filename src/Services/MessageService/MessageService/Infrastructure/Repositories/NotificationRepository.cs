using MessageService.Domain.Entities;
using MessageService.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Postgrest;
using Client = Supabase.Client;

namespace MessageService.Infrastructure.Repositories;

/// <summary>
///     通知仓储实现 - Supabase
/// </summary>
public class NotificationRepository : INotificationRepository
{
    private readonly ILogger<NotificationRepository> _logger;
    private readonly Client _supabaseClient;

    public NotificationRepository(Client supabaseClient, ILogger<NotificationRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<(List<Notification> Notifications, int TotalCount)> GetUserNotificationsAsync(
        string userId,
        bool? isRead = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _supabaseClient
                .From<Notification>()
                .Where(n => n.UserId == userId);

            // 筛选已读/未读
            if (isRead.HasValue) query = query.Where(n => n.IsRead == isRead.Value);

            // 获取总数
            var countQuery = _supabaseClient
                .From<Notification>()
                .Where(n => n.UserId == userId);

            if (isRead.HasValue) countQuery = countQuery.Where(n => n.IsRead == isRead.Value);

            // 获取总数（先获取数据再统计）
            var countResult = await countQuery.Get(cancellationToken);
            var totalCount = countResult.Models.Count;

            // 分页查询
            var skip = (page - 1) * pageSize;
            var response = await query
                .Order("created_at", Constants.Ordering.Descending)
                .Range(skip, skip + pageSize - 1)
                .Get(cancellationToken);

            var notifications = response.Models ?? new List<Notification>();

            _logger.LogInformation("✅ 获取用户通知: UserId={UserId}, IsRead={IsRead}, Total={Total}",
                userId, isRead, totalCount);

            return (notifications, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取用户通知失败: UserId={UserId}", userId);
            throw;
        }
    }

    public async Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _supabaseClient
                .From<Notification>()
                .Where(n => n.UserId == userId)
                .Where(n => n.IsRead == false)
                .Get(cancellationToken);

            var count = response.Models.Count;

            _logger.LogInformation("✅ 获取未读数量: UserId={UserId}, Count={Count}", userId, count);

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取未读数量失败: UserId={UserId}", userId);
            throw;
        }
    }

    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _supabaseClient
                .From<Notification>()
                .Where(n => n.Id == id)
                .Single(cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取通知失败: Id={Id}", id);
            return null;
        }
    }

    public async Task<Notification> CreateAsync(Notification notification,
        CancellationToken cancellationToken = default)
    {
        try
        {
            notification.Id = Guid.NewGuid();
            notification.CreatedAt = DateTime.UtcNow;
            notification.IsRead = false;

            var response = await _supabaseClient
                .From<Notification>()
                .Insert(notification, new QueryOptions { Returning = QueryOptions.ReturnType.Representation },
                    cancellationToken);

            var created = response.Models.FirstOrDefault();

            if (created == null)
                // Fallback: 查询刚创建的记录
                created = await GetByIdAsync(notification.Id, cancellationToken);

            if (created == null) throw new InvalidOperationException("创建通知失败 - 无法获取创建的记录");

            _logger.LogInformation("✅ 创建通知成功: Id={Id}, UserId={UserId}, Type={Type}",
                created.Id, created.UserId, created.Type);

            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建通知失败: UserId={UserId}", notification.UserId);
            throw;
        }
    }

    public async Task<List<Notification>> CreateBatchAsync(List<Notification> notifications,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTime.UtcNow;
            foreach (var notification in notifications)
            {
                notification.Id = Guid.NewGuid();
                notification.CreatedAt = now;
                notification.IsRead = false;
            }

            var response = await _supabaseClient
                .From<Notification>()
                .Insert(notifications, new QueryOptions { Returning = QueryOptions.ReturnType.Representation },
                    cancellationToken);

            var created = response.Models ?? new List<Notification>();

            _logger.LogInformation("✅ 批量创建通知成功: Count={Count}", created.Count);

            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 批量创建通知失败");
            throw;
        }
    }

    public async Task<bool> MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = await GetByIdAsync(id, cancellationToken);
            if (notification == null)
            {
                _logger.LogWarning("⚠️ 通知不存在: Id={Id}", id);
                return false;
            }

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;

            await _supabaseClient
                .From<Notification>()
                .Where(n => n.Id == id)
                .Update(notification);

            _logger.LogInformation("✅ 标记通知已读: Id={Id}", id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 标记通知已读失败: Id={Id}", id);
            throw;
        }
    }

    public async Task<int> MarkMultipleAsReadAsync(List<Guid> ids, string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (ids == null || ids.Count == 0) return 0;

            var now = DateTime.UtcNow;
            var updateModel = new Notification
            {
                IsRead = true,
                ReadAt = now
            };

            // Supabase 不支持 IN 查询，需要逐个更新
            var updatedCount = 0;
            foreach (var id in ids)
            {
                var notification = await GetByIdAsync(id, cancellationToken);
                if (notification != null && notification.UserId == userId)
                {
                    await MarkAsReadAsync(id, cancellationToken);
                    updatedCount++;
                }
            }

            _logger.LogInformation("✅ 批量标记已读: UserId={UserId}, Count={Count}", userId, updatedCount);

            return updatedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 批量标记已读失败: UserId={UserId}", userId);
            throw;
        }
    }

    public async Task<int> MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // 获取所有未读通知
            var (notifications, _) = await GetUserNotificationsAsync(userId, false, 1, 1000, cancellationToken);

            var now = DateTime.UtcNow;
            var updatedCount = 0;

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAt = now;

                await _supabaseClient
                    .From<Notification>()
                    .Where(n => n.Id == notification.Id)
                    .Update(notification);

                updatedCount++;
            }

            _logger.LogInformation("✅ 标记所有通知已读: UserId={UserId}, Count={Count}", userId, updatedCount);

            return updatedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 标记所有通知已读失败: UserId={UserId}", userId);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = await GetByIdAsync(id, cancellationToken);
            if (notification == null)
            {
                _logger.LogWarning("⚠️ 通知不存在: Id={Id}", id);
                return false;
            }

            await _supabaseClient
                .From<Notification>()
                .Where(n => n.Id == id)
                .Delete();

            _logger.LogInformation("✅ 删除通知: Id={Id}", id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除通知失败: Id={Id}", id);
            throw;
        }
    }

    public async Task<bool> UpdateMetadataAsync(Guid id, string metadata, CancellationToken cancellationToken = default)
    {
        try
        {
            var notification = await GetByIdAsync(id, cancellationToken);
            if (notification == null)
            {
                _logger.LogWarning("⚠️ 通知不存在: Id={Id}", id);
                return false;
            }

            notification.Metadata = metadata;

            await _supabaseClient
                .From<Notification>()
                .Where(n => n.Id == id)
                .Update(notification);

            _logger.LogInformation("✅ 更新通知元数据: Id={Id}", id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新通知元数据失败: Id={Id}", id);
            throw;
        }
    }

    public async Task<List<string>> GetAdminUserIdsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // 查询 role 为 admin 的用户
            // 使用 RPC 函数获取管理员用户ID列表
            var response = await _supabaseClient
                .Rpc<List<string>>("get_admin_user_ids", new Dictionary<string, object>());

            _logger.LogInformation("✅ 获取管理员用户ID: Count={Count}", response?.Count ?? 0);

            return response ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 获取管理员用户ID失败，返回空列表");
            // 如果 RPC 函数不存在，返回空列表
            return new List<string>();
        }
    }
}