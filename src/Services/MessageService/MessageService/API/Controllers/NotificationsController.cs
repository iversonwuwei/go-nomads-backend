using System.ComponentModel.DataAnnotations;
using GoNomads.Shared.DTOs;
using MessageService.Application.DTOs;
using MessageService.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace MessageService.API.Controllers;

/// <summary>
///     通知 API - RESTful endpoints for notification management
/// </summary>
[ApiController]
[Route("api/v1/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly ILogger<NotificationsController> _logger;
    private readonly INotificationService _notificationService;

    public NotificationsController(
        INotificationService notificationService,
        ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    ///     获取用户通知列表（支持筛选）
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedNotificationsResponse>>> GetNotifications(
        [FromQuery] [Required] string userId,
        [FromQuery] bool? isRead = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📋 获取通知列表: UserId={UserId}, IsRead={IsRead}, Page={Page}",
            userId, isRead, page);

        try
        {
            var (notifications, totalCount) = await _notificationService.GetUserNotificationsAsync(
                userId, isRead, page, pageSize, cancellationToken);

            // 同时获取未读数量
            var unreadCount = await _notificationService.GetUnreadCountAsync(userId, cancellationToken);

            return Ok(new ApiResponse<PaginatedNotificationsResponse>
            {
                Success = true,
                Message = "通知列表获取成功",
                Data = new PaginatedNotificationsResponse
                {
                    Notifications = notifications,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    UnreadCount = unreadCount
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取通知列表失败: UserId={UserId}", userId);
            return StatusCode(500, new ApiResponse<PaginatedNotificationsResponse>
            {
                Success = false,
                Message = "获取通知列表失败"
            });
        }
    }

    /// <summary>
    ///     获取未读通知数量
    /// </summary>
    [HttpGet("unread/count")]
    public async Task<ActionResult<ApiResponse<NotificationStatsDto>>> GetUnreadCount(
        [FromQuery] [Required] string userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔢 获取未读数量: UserId={UserId}", userId);

        try
        {
            var count = await _notificationService.GetUnreadCountAsync(userId, cancellationToken);

            return Ok(new ApiResponse<NotificationStatsDto>
            {
                Success = true,
                Message = "未读数量获取成功",
                Data = new NotificationStatsDto
                {
                    UnreadCount = count
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取未读数量失败: UserId={UserId}", userId);
            return StatusCode(500, new ApiResponse<NotificationStatsDto>
            {
                Success = false,
                Message = "获取未读数量失败"
            });
        }
    }

    /// <summary>
    ///     创建通知
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<NotificationDto>>> CreateNotification(
        [FromBody] CreateNotificationDto request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 创建通知: UserId={UserId}, Type={Type}",
            request.UserId, request.Type);

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(new ApiResponse<NotificationDto>
            {
                Success = false,
                Message = "验证失败",
                Errors = errors
            });
        }

        try
        {
            var notification = await _notificationService.CreateNotificationAsync(request, cancellationToken);

            return Ok(new ApiResponse<NotificationDto>
            {
                Success = true,
                Message = "通知创建成功",
                Data = notification
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建通知失败: UserId={UserId}", request.UserId);
            return StatusCode(500, new ApiResponse<NotificationDto>
            {
                Success = false,
                Message = "创建通知失败"
            });
        }
    }

    /// <summary>
    ///     批量创建通知
    /// </summary>
    [HttpPost("batch")]
    public async Task<ActionResult<ApiResponse<BatchNotificationResponse>>> CreateBatchNotifications(
        [FromBody] CreateBatchNotificationDto request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📬 批量创建通知: UserCount={Count}, Type={Type}",
            request.UserIds?.Count ?? 0, request.Type);

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(new ApiResponse<BatchNotificationResponse>
            {
                Success = false,
                Message = "验证失败",
                Errors = errors
            });
        }

        try
        {
            var response = await _notificationService.CreateBatchNotificationsAsync(request, cancellationToken);

            return Ok(new ApiResponse<BatchNotificationResponse>
            {
                Success = true,
                Message = $"成功创建 {response.CreatedCount} 条通知",
                Data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 批量创建通知失败");
            return StatusCode(500, new ApiResponse<BatchNotificationResponse>
            {
                Success = false,
                Message = "批量创建通知失败"
            });
        }
    }

    /// <summary>
    ///     发送通知给所有管理员
    /// </summary>
    [HttpPost("admins")]
    public async Task<ActionResult<ApiResponse<List<NotificationDto>>>> SendToAdmins(
        [FromBody] SendToAdminsDto request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📢 发送通知给管理员: Type={Type}, Title={Title}",
            request.Type, request.Title);

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(new ApiResponse<List<NotificationDto>>
            {
                Success = false,
                Message = "验证失败",
                Errors = errors
            });
        }

        try
        {
            var notifications = await _notificationService.SendToAdminsAsync(request, cancellationToken);

            return Ok(new ApiResponse<List<NotificationDto>>
            {
                Success = true,
                Message = $"成功发送通知给 {notifications.Count} 位管理员",
                Data = notifications
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 发送通知给管理员失败");
            return StatusCode(500, new ApiResponse<List<NotificationDto>>
            {
                Success = false,
                Message = "发送通知失败"
            });
        }
    }

    /// <summary>
    ///     发送通知给指定城市的版主
    /// </summary>
    [HttpPost("city-moderators")]
    public async Task<ActionResult<ApiResponse<List<NotificationDto>>>> SendToCityModerators(
        [FromBody] SendToCityModeratorsDto request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📢 发送通知给城市版主: CityId={CityId}, Type={Type}, Title={Title}",
            request.CityId, request.Type, request.Title);

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(new ApiResponse<List<NotificationDto>>
            {
                Success = false,
                Message = "验证失败",
                Errors = errors
            });
        }

        try
        {
            var notifications = await _notificationService.SendToCityModeratorsAsync(request, cancellationToken);

            return Ok(new ApiResponse<List<NotificationDto>>
            {
                Success = true,
                Message = $"成功发送通知给 {notifications.Count} 位城市版主",
                Data = notifications
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 发送通知给城市版主失败: CityId={CityId}", request.CityId);
            return StatusCode(500, new ApiResponse<List<NotificationDto>>
            {
                Success = false,
                Message = "发送通知失败"
            });
        }
    }

    /// <summary>
    ///     标记通知为已读
    /// </summary>
    [HttpPut("{id}/read")]
    public async Task<ActionResult<ApiResponse<object>>> MarkAsRead(
        string id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("✅ 标记通知已读: Id={Id}", id);

        try
        {
            var result = await _notificationService.MarkAsReadAsync(id, cancellationToken);

            if (!result)
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "通知不存在"
                });

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "通知已标记为已读"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 标记通知已读失败: Id={Id}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "标记已读失败"
            });
        }
    }

    /// <summary>
    ///     更新通知元数据
    /// </summary>
    [HttpPatch("{id}/metadata")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateMetadata(
        string id,
        [FromBody] UpdateNotificationMetadataDto request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 更新通知元数据: Id={Id}", id);

        try
        {
            var result = await _notificationService.UpdateMetadataAsync(id, request.Metadata, cancellationToken);

            if (!result)
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "通知不存在"
                });

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "通知元数据已更新"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新通知元数据失败: Id={Id}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "更新元数据失败"
            });
        }
    }

    /// <summary>
    ///     批量标记通知为已读
    /// </summary>
    [HttpPut("read/batch")]
    public async Task<ActionResult<ApiResponse<object>>> MarkMultipleAsRead(
        [FromQuery] [Required] string userId,
        [FromBody] MarkMultipleAsReadDto request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("✅ 批量标记已读: UserId={UserId}, Count={Count}",
            userId, request.NotificationIds?.Count ?? 0);

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "验证失败",
                Errors = errors
            });
        }

        try
        {
            if (request.NotificationIds == null || request.NotificationIds.Count == 0)
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "通知ID列表不能为空"
                });

            var count = await _notificationService.MarkMultipleAsReadAsync(
                userId, request.NotificationIds, cancellationToken);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = $"成功标记 {count} 条通知为已读"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 批量标记已读失败: UserId={UserId}", userId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "批量标记已读失败"
            });
        }
    }

    /// <summary>
    ///     标记所有通知为已读
    /// </summary>
    [HttpPut("read/all")]
    public async Task<ActionResult<ApiResponse<object>>> MarkAllAsRead(
        [FromQuery] [Required] string userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("✅ 标记所有通知已读: UserId={UserId}", userId);

        try
        {
            var count = await _notificationService.MarkAllAsReadAsync(userId, cancellationToken);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = $"成功标记 {count} 条通知为已读"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 标记所有通知已读失败: UserId={UserId}", userId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "标记所有通知已读失败"
            });
        }
    }

    /// <summary>
    ///     删除通知
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteNotification(
        string id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🗑️ 删除通知: Id={Id}", id);

        try
        {
            var result = await _notificationService.DeleteNotificationAsync(id, cancellationToken);

            if (!result)
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "通知不存在"
                });

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "通知删除成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除通知失败: Id={Id}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "删除通知失败"
            });
        }
    }
}

#region Response DTOs

/// <summary>
///     分页通知响应 DTO
/// </summary>
public class PaginatedNotificationsResponse
{
    public List<NotificationDto> Notifications { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int UnreadCount { get; set; }
}

#endregion