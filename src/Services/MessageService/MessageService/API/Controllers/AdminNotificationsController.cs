using GoNomads.Shared.DTOs;
using GoNomads.Shared.Services;
using MessageService.Application.DTOs;
using MessageService.Application.Services;
using MessageService.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Postgrest;
using System.Text.Json;
using Client = Supabase.Client;

namespace MessageService.API.Controllers;

[ApiController]
[Route("api/v1/admin/notifications")]
public class AdminNotificationsController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notificationService;
    private readonly Client _supabase;
    private readonly IUserServiceClient _userServiceClient;
    private readonly ILogger<AdminNotificationsController> _logger;

    public AdminNotificationsController(
        ICurrentUserService currentUser,
        INotificationService notificationService,
        Client supabase,
        IUserServiceClient userServiceClient,
        ILogger<AdminNotificationsController> logger)
    {
        _currentUser = currentUser;
        _notificationService = notificationService;
        _supabase = supabase;
        _userServiceClient = userServiceClient;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<NotificationDto>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null)
    {
        try
        {
            if (!_currentUser.IsAdmin()) return Forbid();

            var response = await _supabase.From<Notification>()
                .Order("created_at", Constants.Ordering.Descending)
                .Get();

            var userInfos = await _userServiceClient.GetUsersInfoAsync(
                response.Models
                    .Select(model => model.UserId)
                    .Where(userId => !string.IsNullOrWhiteSpace(userId)));

            var filteredItems = response.Models
                .Select(model => MapToDto(model, userInfos.GetValueOrDefault(model.UserId)))
                .Where(item => MatchesStatus(item, status))
                .ToList();

            var totalCount = filteredItems.Count;
            var items = filteredItems
                .Skip(Math.Max(0, (page - 1) * pageSize))
                .Take(pageSize)
                .ToList();

            return Ok(new ApiResponse<PaginatedResponse<NotificationDto>>
            {
                Success = true,
                Message = "获取通知列表成功",
                Data = new PaginatedResponse<NotificationDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取通知列表失败");
            return StatusCode(500, ApiResponse<PaginatedResponse<NotificationDto>>.ErrorResponse("获取通知列表失败"));
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<NotificationDto>>> GetById(Guid id)
    {
        try
        {
            if (!_currentUser.IsAdmin()) return Forbid();

            var existing = await _supabase.From<Notification>()
                .Where(n => n.Id == id)
                .Single();

            if (existing == null)
                return NotFound(ApiResponse<NotificationDto>.ErrorResponse("通知不存在"));

            var userInfo = string.IsNullOrWhiteSpace(existing.UserId)
                ? null
                : await _userServiceClient.GetUserInfoAsync(existing.UserId);

            return Ok(ApiResponse<NotificationDto>.SuccessResponse(MapToDto(existing, userInfo), "获取通知详情成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取通知详情失败: Id={Id}", id);
            return StatusCode(500, ApiResponse<NotificationDto>.ErrorResponse("获取通知详情失败"));
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<bool>>> Create([FromBody] AdminCreateNotificationRequest request)
    {
        try
        {
            if (!_currentUser.IsAdmin()) return Forbid();

            await _notificationService.SendToAdminsAsync(new Application.DTOs.SendToAdminsDto
            {
                Title = request.Title,
                Message = request.Message,
                Type = request.Type ?? "system_announcement"
            });

            _logger.LogInformation("管理员创建通知: Title={Title}", request.Title);
            return Ok(ApiResponse<bool>.SuccessResponse(true, "通知已创建"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建通知失败");
            return StatusCode(500, ApiResponse<bool>.ErrorResponse("创建通知失败"));
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<NotificationDto>>> Update(
        Guid id,
        [FromBody] AdminUpdateNotificationRequest request)
    {
        try
        {
            if (!_currentUser.IsAdmin()) return Forbid();

            var existing = await _supabase.From<Notification>()
                .Where(n => n.Id == id)
                .Single();

            if (existing == null)
                return NotFound(ApiResponse<NotificationDto>.ErrorResponse("通知不存在"));

            if (!string.IsNullOrWhiteSpace(request.Title))
                existing.Title = request.Title.Trim();

            if (!string.IsNullOrWhiteSpace(request.Message))
                existing.Message = request.Message.Trim();

            if (!string.IsNullOrWhiteSpace(request.Type))
                existing.Type = request.Type.Trim();

            if (!string.IsNullOrWhiteSpace(request.Metadata))
                existing.Metadata = request.Metadata;

            await _supabase.From<Notification>()
                .Where(n => n.Id == id)
                .Update(existing);

            var userInfo = string.IsNullOrWhiteSpace(existing.UserId)
                ? null
                : await _userServiceClient.GetUserInfoAsync(existing.UserId);

            return Ok(ApiResponse<NotificationDto>.SuccessResponse(MapToDto(existing, userInfo), "更新通知成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新通知失败: Id={Id}", id);
            return StatusCode(500, ApiResponse<NotificationDto>.ErrorResponse("更新通知失败"));
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        try
        {
            if (!_currentUser.IsAdmin()) return Forbid();

            await _notificationService.DeleteNotificationAsync(id.ToString());

            _logger.LogInformation("管理员删除通知: Id={Id}", id);
            return Ok(ApiResponse<bool>.SuccessResponse(true, "删除成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除通知失败: Id={Id}", id);
            return StatusCode(500, ApiResponse<bool>.ErrorResponse("删除通知失败"));
        }
    }

    private static NotificationDto MapToDto(Notification notification, UserInfoDto? userInfo)
    {
        var metadata = ParseMetadata(notification.Metadata);
        var scope = ResolveScope(notification, metadata);
        var recipientUserName = ResolveUserDisplayName(userInfo);
        var recipientSummary = ResolveRecipientSummary(notification, userInfo, scope);

        return new NotificationDto
        {
            Id = notification.Id.ToString(),
            UserId = notification.UserId,
            RecipientUserName = recipientUserName,
            RecipientSummary = recipientSummary,
            Title = notification.Title,
            Message = notification.Message,
            Type = notification.Type,
            Scope = scope,
            ScopeDisplay = ResolveScopeDisplay(scope),
            RelatedId = notification.RelatedId,
            RelatedResourceName = ResolveRelatedResourceName(notification, metadata),
            Metadata = metadata,
            Status = notification.IsRead ? "read" : "unread",
            DeliveredCount = 1,
            ReadCount = notification.IsRead ? 1 : 0,
            ScheduledAt = TryReadDateTime(metadata, "scheduledAt", "scheduled_at"),
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt,
            ReadAt = notification.ReadAt
        };
    }

    private static Dictionary<string, object>? ParseMetadata(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata))
            return null;

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(metadata);
        }
        catch
        {
            return new Dictionary<string, object>
            {
                ["raw"] = metadata
            };
        }
    }

    private static bool MatchesStatus(NotificationDto notification, string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return true;

        return string.Equals(notification.Status, status.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveScope(Notification notification, Dictionary<string, object>? metadata)
    {
        var explicitScope = TryReadString(metadata, "scope", "Scope");
        if (!string.IsNullOrWhiteSpace(explicitScope))
            return explicitScope!;

        if (notification.Type.Contains("announcement", StringComparison.OrdinalIgnoreCase)
            || notification.Type.Contains("system", StringComparison.OrdinalIgnoreCase))
            return "admins";

        return string.IsNullOrWhiteSpace(notification.UserId) ? "admins" : "user";
    }

    private static string ResolveScopeDisplay(string? scope)
    {
        return (scope ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "admins" => "管理员广播",
            "user" => "定向用户",
            "segment" => "分群发送",
            _ => "通知范围未定义"
        };
    }

    private static string ResolveRecipientSummary(Notification notification, UserInfoDto? userInfo, string scope)
    {
        if (scope.Equals("admins", StringComparison.OrdinalIgnoreCase))
            return "管理员群体";

        var displayName = ResolveUserDisplayName(userInfo);
        if (!string.IsNullOrWhiteSpace(displayName))
            return displayName;

        return string.IsNullOrWhiteSpace(notification.UserId) ? "未指定接收人" : "定向用户";
    }

    private static string? ResolveRelatedResourceName(Notification notification, Dictionary<string, object>? metadata)
    {
        var explicitLabel = TryReadString(metadata,
            "relatedResourceName",
            "RelatedResourceName",
            "relatedResourceTitle",
            "RelatedResourceTitle",
            "resourceName",
            "ResourceName",
            "resourceTitle",
            "ResourceTitle",
            "resourceDisplayName",
            "ResourceDisplayName");
        if (!string.IsNullOrWhiteSpace(explicitLabel))
            return explicitLabel;

        var eventTitle = TryReadString(metadata, "eventTitle", "EventTitle");
        if (!string.IsNullOrWhiteSpace(eventTitle))
            return $"活动「{eventTitle}」";

        var genericResourceLabel = TryReadString(metadata,
            "projectTitle",
            "ProjectTitle",
            "innovationTitle",
            "InnovationTitle",
            "coworkingName",
            "CoworkingName",
            "cityName",
            "CityName",
            "cityNameEn",
            "CityNameEn",
            "name",
            "Name");
        if (!string.IsNullOrWhiteSpace(genericResourceLabel))
            return genericResourceLabel;

        var cityName = TryReadString(metadata, "cityName", "CityName", "cityNameEn", "CityNameEn");
        var applicantName = TryReadString(metadata, "applicantName", "ApplicantName");
        var fromUserName = TryReadString(metadata, "fromUserName", "FromUserName");
        var toUserName = TryReadString(metadata, "toUserName", "ToUserName");
        var result = TryReadString(metadata, "result", "Result");
        var taskType = TryReadString(metadata, "taskType", "TaskType");

        if (!string.IsNullOrWhiteSpace(taskType))
            return BuildAiTaskLabel(taskType);

        return (notification.Type ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "moderator_application" => BuildCompositeLabel(cityName, applicantName, "版主申请"),
            "moderator_approved" or "moderator_rejected" or "moderator_revoked" => BuildCompositeLabel(cityName, applicantName, "版主申请"),
            "moderator_transfer" => BuildTransferLabel(cityName, fromUserName, toUserName, "版主转让"),
            "moderator_transfer_result" => BuildTransferResultLabel(cityName, fromUserName, toUserName, result),
            _ => !string.IsNullOrWhiteSpace(cityName) ? cityName : null
        };
    }

    private static string? ResolveUserDisplayName(UserInfoDto? userInfo)
    {
        if (userInfo == null)
            return null;

        if (!string.IsNullOrWhiteSpace(userInfo.Name))
            return userInfo.Name.Trim();

        if (!string.IsNullOrWhiteSpace(userInfo.Username))
            return userInfo.Username.Trim();

        var email = userInfo.Email?.Trim();
        return !string.IsNullOrWhiteSpace(email) ? ExtractEmailDisplayName(email) : null;
    }

    private static string ExtractEmailDisplayName(string email)
    {
        var atIndex = email.IndexOf('@');
        return atIndex > 0 ? email[..atIndex] : email;
    }

    private static string BuildCompositeLabel(string? cityName, string? personName, string fallback)
    {
        if (!string.IsNullOrWhiteSpace(cityName) && !string.IsNullOrWhiteSpace(personName))
            return $"{cityName} / {personName}";

        if (!string.IsNullOrWhiteSpace(cityName))
            return $"{cityName} {fallback}";

        if (!string.IsNullOrWhiteSpace(personName))
            return personName;

        return fallback;
    }

    private static string BuildTransferLabel(string? cityName, string? fromUserName, string? toUserName, string fallback)
    {
        var actors = new[] { fromUserName, toUserName }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .ToList();

        if (!string.IsNullOrWhiteSpace(cityName) && actors.Count > 0)
            return $"{cityName} / {string.Join(" → ", actors)}";

        if (!string.IsNullOrWhiteSpace(cityName))
            return $"{cityName} {fallback}";

        if (actors.Count > 0)
            return string.Join(" → ", actors);

        return fallback;
    }

    private static string BuildTransferResultLabel(string? cityName, string? fromUserName, string? toUserName, string? result)
    {
        var baseLabel = BuildTransferLabel(cityName, fromUserName, toUserName, "版主转让结果");
        var resultLabel = result switch
        {
            "accepted" => "已接受",
            "rejected" => "已拒绝",
            "cancelled" => "已取消",
            _ => null
        };

        return string.IsNullOrWhiteSpace(resultLabel) ? baseLabel : $"{baseLabel} / {resultLabel}";
    }

    private static string BuildAiTaskLabel(string taskType)
    {
        return taskType.Trim().ToLowerInvariant() switch
        {
            "travel-plan" => "AI 旅行计划",
            "digital-nomad-guide" => "AI 数字游民指南",
            _ => "AI 任务"
        };
    }

    private static string? TryReadString(Dictionary<string, object>? metadata, params string[] keys)
    {
        if (metadata == null)
            return null;

        foreach (var key in keys)
        {
            if (!metadata.TryGetValue(key, out var value) || value == null)
                continue;

            if (value is string str)
                return str;

            if (value is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.String)
                    return element.GetString();

                return element.ToString();
            }

            return value.ToString();
        }

        return null;
    }

    private static DateTime? TryReadDateTime(Dictionary<string, object>? metadata, params string[] keys)
    {
        var raw = TryReadString(metadata, keys);
        return DateTime.TryParse(raw, out var parsed) ? parsed : null;
    }
}

public class AdminCreateNotificationRequest
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Type { get; set; }
}

public class AdminUpdateNotificationRequest
{
    public string? Title { get; set; }
    public string? Message { get; set; }
    public string? Type { get; set; }
    public string? Metadata { get; set; }
}
