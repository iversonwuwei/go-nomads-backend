using CityService.Application.DTOs;
using CityService.Domain.Entities;
using CityService.Domain.Repositories;
using Dapr.Client;

namespace CityService.Application.Services;

/// <summary>
///     版主申请服务实现
/// </summary>
public class ModeratorApplicationService : IModeratorApplicationService
{
    private readonly IModeratorApplicationRepository _applicationRepo;
    private readonly ICityModeratorRepository _moderatorRepo;
    private readonly ICityRepository _cityRepo;
    private readonly DaprClient _daprClient;
    private readonly ILogger<ModeratorApplicationService> _logger;

    public ModeratorApplicationService(
        IModeratorApplicationRepository applicationRepo,
        ICityModeratorRepository moderatorRepo,
        ICityRepository cityRepo,
        DaprClient daprClient,
        ILogger<ModeratorApplicationService> logger)
    {
        _applicationRepo = applicationRepo;
        _moderatorRepo = moderatorRepo;
        _cityRepo = cityRepo;
        _daprClient = daprClient;
        _logger = logger;
    }

    public async Task<ModeratorApplicationResponse> ApplyAsync(Guid userId, ApplyModeratorRequest request)
    {
        _logger.LogInformation("📝 用户 {UserId} 申请成为城市 {CityId} 的版主", userId, request.CityId);

        // 1. 检查城市是否存在
        var city = await _cityRepo.GetByIdAsync(request.CityId);
        if (city == null)
        {
            throw new KeyNotFoundException($"城市 {request.CityId} 不存在");
        }

        // 2. 检查用户是否已经是该城市的版主
        var isModerator = await _moderatorRepo.IsModeratorAsync(request.CityId, userId);
        if (isModerator)
        {
            throw new InvalidOperationException("您已经是该城市的版主");
        }

        // 3. 检查是否有待处理的申请
        var hasPending = await _applicationRepo.HasPendingApplicationAsync(userId, request.CityId);
        if (hasPending)
        {
            throw new InvalidOperationException("您已有待处理的申请，请等待审核");
        }

        // 4. 创建申请记录
        var application = ModeratorApplication.Create(userId, request.CityId, request.Reason);
        var created = await _applicationRepo.CreateAsync(application);

        // 5. 获取用户信息（用于通知）
        var userInfo = await GetUserInfoAsync(userId);

        // 6. 通知所有管理员
        await NotifyAdminsAboutNewApplicationAsync(created, userInfo, city);

        _logger.LogInformation("✅ 版主申请创建成功: ApplicationId={Id}", created.Id);

        return await MapToResponseAsync(created);
    }

    public async Task<ModeratorApplicationResponse> HandleApplicationAsync(
        Guid adminId,
        HandleModeratorApplicationRequest request)
    {
        _logger.LogInformation("🔍 管理员 {AdminId} 处理申请 {ApplicationId}: {Action}",
            adminId, request.ApplicationId, request.Action);

        // 1. 获取申请记录
        var application = await _applicationRepo.GetByIdAsync(request.ApplicationId);
        if (application == null)
        {
            throw new KeyNotFoundException("申请记录不存在");
        }

        // 2. 检查申请状态
        if (application.Status != "pending")
        {
            throw new InvalidOperationException($"申请已被处理，当前状态: {application.Status}");
        }

        // 3. 处理申请
        if (request.Action.ToLower() == "approve")
        {
            // 批准申请
            application.Approve(adminId);

            // 创建版主记录
            var moderator = new CityModerator
            {
                Id = Guid.NewGuid(),
                CityId = application.CityId,
                UserId = application.UserId,
                AssignedBy = adminId,
                AssignedAt = DateTime.UtcNow,
                IsActive = true,
                CanEditCity = true,
                CanManageCoworks = true,
                CanManageCosts = true,
                CanManageVisas = true,
                CanModerateChats = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _moderatorRepo.AddAsync(moderator);
            _logger.LogInformation("✅ 已为用户 {UserId} 创建版主记录", application.UserId);

            // 通知申请人：批准
            await NotifyApplicantApprovedAsync(application);
        }
        else if (request.Action.ToLower() == "reject")
        {
            // 拒绝申请
            var reason = request.RejectionReason ?? "未通过审核";
            application.Reject(adminId, reason);

            // 通知申请人：拒绝
            await NotifyApplicantRejectedAsync(application, reason);
        }
        else
        {
            throw new InvalidOperationException($"无效的操作: {request.Action}，请使用 approve 或 reject");
        }

        // 4. 更新申请记录
        var updated = await _applicationRepo.UpdateAsync(application);

        _logger.LogInformation("✅ 申请处理完成: Status={Status}", updated.Status);

        return await MapToResponseAsync(updated);
    }

    public async Task<List<ModeratorApplicationResponse>> GetPendingApplicationsAsync(int page = 1, int pageSize = 20)
    {
        var applications = await _applicationRepo.GetPendingApplicationsAsync(page, pageSize);
        var responses = new List<ModeratorApplicationResponse>();

        foreach (var app in applications)
        {
            responses.Add(await MapToResponseAsync(app));
        }

        return responses;
    }

    public async Task<List<ModeratorApplicationResponse>> GetUserApplicationsAsync(Guid userId)
    {
        var applications = await _applicationRepo.GetByUserIdAsync(userId);
        var responses = new List<ModeratorApplicationResponse>();

        foreach (var app in applications)
        {
            responses.Add(await MapToResponseAsync(app));
        }

        return responses;
    }

    public async Task<ModeratorApplicationResponse?> GetApplicationByIdAsync(Guid applicationId)
    {
        var application = await _applicationRepo.GetByIdAsync(applicationId);
        if (application == null) return null;

        return await MapToResponseAsync(application);
    }

    public async Task<(int Total, int Pending, int Approved, int Rejected)> GetStatisticsAsync()
    {
        return await _applicationRepo.GetStatisticsAsync();
    }

    public async Task RevokeModeratorAsync(Guid adminId, Guid applicationId)
    {
        _logger.LogInformation("🔍 管理员 {AdminId} 撤销版主资格，申请ID: {ApplicationId}",
            adminId, applicationId);

        // 1. 获取申请记录
        var application = await _applicationRepo.GetByIdAsync(applicationId);
        if (application == null)
        {
            throw new KeyNotFoundException("申请记录不存在");
        }

        // 2. 检查申请状态必须是已批准
        if (application.Status != "approved")
        {
            throw new InvalidOperationException($"只能撤销已批准的申请，当前状态: {application.Status}");
        }

        // 3. 删除版主记录
        var isModerator = await _moderatorRepo.IsModeratorAsync(application.CityId, application.UserId);
        if (isModerator)
        {
            await _moderatorRepo.RemoveAsync(application.CityId, application.UserId);
            _logger.LogInformation("✅ 已删除用户 {UserId} 的版主记录", application.UserId);
        }

        // 4. 更新申请状态为已撤销
        application.Status = "revoked";
        application.ProcessedBy = adminId;
        application.ProcessedAt = DateTime.UtcNow;
        application.UpdatedAt = DateTime.UtcNow;
        
        await _applicationRepo.UpdateAsync(application);

        // 5. 通知用户
        await NotifyModeratorRevokedAsync(application);

        _logger.LogInformation("✅ 版主资格已撤销: UserId={UserId}, CityId={CityId}", 
            application.UserId, application.CityId);
    }

    #region 私有方法

    /// <summary>
    ///     映射到响应 DTO
    /// </summary>
    private async Task<ModeratorApplicationResponse> MapToResponseAsync(ModeratorApplication application)
    {
        var city = await _cityRepo.GetByIdAsync(application.CityId);
        var userInfo = await GetUserInfoAsync(application.UserId);
        
        var response = new ModeratorApplicationResponse
        {
            Id = application.Id,
            UserId = application.UserId,
            UserName = userInfo?.Name ?? "Unknown User",
            UserAvatar = userInfo?.Avatar ?? "",
            CityId = application.CityId,
            CityName = city?.NameEn ?? "Unknown City",
            Reason = application.Reason,
            Status = application.Status,
            ProcessedBy = application.ProcessedBy,
            ProcessedAt = application.ProcessedAt,
            RejectionReason = application.RejectionReason,
            CreatedAt = application.CreatedAt
        };

        if (application.ProcessedBy.HasValue)
        {
            var processorInfo = await GetUserInfoAsync(application.ProcessedBy.Value);
            response.ProcessedByName = processorInfo?.Name ?? "Unknown Admin";
        }

        return response;
    }

    /// <summary>
    ///     通过 Dapr 调用 UserService 获取用户信息
    /// </summary>
    private async Task<UserInfo?> GetUserInfoAsync(Guid userId)
    {
        try
        {
            var response = await _daprClient.InvokeMethodAsync<UserInfo>(
                HttpMethod.Get,
                "user-service",
                $"api/v1/users/{userId}/basic"
            );
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取用户信息失败: UserId={UserId}", userId);
            return null;
        }
    }

    /// <summary>
    ///     通知所有管理员有新的申请
    /// </summary>
    private async Task NotifyAdminsAboutNewApplicationAsync(
        ModeratorApplication application,
        UserInfo? applicantInfo,
        City city)
    {
        try
        {
            // 获取所有管理员用户ID
            var adminIds = await GetAdminUserIdsAsync();

            if (!adminIds.Any())
            {
                _logger.LogWarning("⚠️ 没有找到管理员用户，无法发送通知");
                return;
            }

            // 使用批量接口一次性为所有管理员创建通知
            var batchNotification = new
            {
                UserIds = adminIds.Select(id => id.ToString()).ToList(),
                Title = "新的版主申请",
                Message = $"用户 {applicantInfo?.Name ?? "未知用户"} 申请成为 {city.NameEn} 的版主",
                Type = "moderator_application",
                RelatedId = application.Id.ToString(),
                Metadata = new Dictionary<string, object>
                {
                    ["applicationId"] = application.Id.ToString(),
                    ["applicantId"] = application.UserId.ToString(),
                    ["applicantName"] = applicantInfo?.Name ?? "未知用户",
                    ["applicantAvatar"] = applicantInfo?.Avatar ?? "",
                    ["cityId"] = application.CityId,
                    ["cityName"] = city.NameEn ?? "Unknown City",
                    ["reason"] = application.Reason
                }
            };

            // 通过 Dapr 调用 MessageService 批量发送通知
            await _daprClient.InvokeMethodAsync(
                HttpMethod.Post,
                "message-service",
                "api/v1/notifications/batch",
                batchNotification
            );

            _logger.LogInformation("📬 已向 {Count} 位管理员批量发送通知", adminIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 发送管理员通知失败");
            // 不抛出异常，避免影响主流程
        }
    }

    /// <summary>
    ///     通知申请人：申请已批准
    /// </summary>
    private async Task NotifyApplicantApprovedAsync(ModeratorApplication application)
    {
        try
        {
            var city = await _cityRepo.GetByIdAsync(application.CityId);

            var notification = new
            {
                UserId = application.UserId.ToString(),
                Title = "版主申请已通过",
                Message = $"恭喜！您已成为 {city?.NameEn ?? "该城市"} 的版主",
                Type = "moderator_approved",
                RelatedId = application.Id.ToString(),
                Metadata = new Dictionary<string, object>
                {
                    { "applicationId", application.Id.ToString() },
                    { "cityId", application.CityId.ToString() },
                    { "cityName", city?.NameEn ?? "Unknown City" }
                }
            };

            await _daprClient.InvokeMethodAsync(
                HttpMethod.Post,
                "message-service",
                "api/v1/notifications",
                notification
            );

            _logger.LogInformation("📬 已向申请人 {UserId} 发送批准通知", application.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 发送批准通知失败");
        }
    }

    /// <summary>
    ///     通知申请人：申请已拒绝
    /// </summary>
    private async Task NotifyApplicantRejectedAsync(ModeratorApplication application, string reason)
    {
        try
        {
            var city = await _cityRepo.GetByIdAsync(application.CityId);

            var notification = new
            {
                UserId = application.UserId.ToString(),
                Title = "版主申请未通过",
                Message = $"很抱歉，您申请成为 {city?.NameEn ?? "该城市"} 版主的请求未通过。原因：{reason}",
                Type = "moderator_rejected",
                RelatedId = application.Id.ToString(),
                Metadata = new Dictionary<string, object>
                {
                    { "applicationId", application.Id.ToString() },
                    { "cityId", application.CityId.ToString() },
                    { "cityName", city?.NameEn ?? "Unknown City" },
                    { "rejectionReason", reason }
                }
            };

            await _daprClient.InvokeMethodAsync(
                HttpMethod.Post,
                "message-service",
                "api/v1/notifications",
                notification
            );

            _logger.LogInformation("📬 已向申请人 {UserId} 发送拒绝通知", application.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 发送拒绝通知失败");
        }
    }

    /// <summary>
    ///     通知用户：版主资格已被撤销
    /// </summary>
    private async Task NotifyModeratorRevokedAsync(ModeratorApplication application)
    {
        try
        {
            var city = await _cityRepo.GetByIdAsync(application.CityId);

            var notification = new
            {
                UserId = application.UserId.ToString(),
                Title = "版主资格已被撤销",
                Message = $"您在 {city?.NameEn ?? "该城市"} 的版主资格已被管理员撤销",
                Type = "moderator_revoked",
                RelatedId = application.Id.ToString(),
                Metadata = new Dictionary<string, object>
                {
                    { "applicationId", application.Id.ToString() },
                    { "cityId", application.CityId.ToString() },
                    { "cityName", city?.NameEn ?? "Unknown City" }
                }
            };

            await _daprClient.InvokeMethodAsync(
                HttpMethod.Post,
                "message-service",
                "api/v1/notifications",
                notification
            );

            _logger.LogInformation("📬 已向用户 {UserId} 发送版主撤销通知", application.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 发送版主撤销通知失败");
        }
    }

    /// <summary>
    ///     获取所有管理员用户ID（通过 UserService）
    /// </summary>
    private async Task<List<Guid>> GetAdminUserIdsAsync()
    {
        try
        {
            var response = await _daprClient.InvokeMethodAsync<List<Guid>>(
                HttpMethod.Get,
                "user-service",
                "api/v1/users/admins"
            );
            return response ?? new List<Guid>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取管理员列表失败");
            return new List<Guid>();
        }
    }

    #endregion

    /// <summary>
    ///     用户信息 DTO（从 UserService 获取）
    /// </summary>
    private class UserInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
