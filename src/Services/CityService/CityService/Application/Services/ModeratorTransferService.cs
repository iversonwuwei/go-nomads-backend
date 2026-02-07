using System.Net.Http.Json;
using CityService.Application.DTOs;
using CityService.Domain.Entities;
using CityService.Domain.Repositories;

namespace CityService.Application.Services;

/// <summary>
///     版主转让服务实现
/// </summary>
public class ModeratorTransferService : IModeratorTransferService
{
    private readonly IModeratorTransferRepository _transferRepo;
    private readonly ICityModeratorRepository _moderatorRepo;
    private readonly ICityRepository _cityRepo;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ModeratorTransferService> _logger;

    public ModeratorTransferService(
        IModeratorTransferRepository transferRepo,
        ICityModeratorRepository moderatorRepo,
        ICityRepository cityRepo,
        IHttpClientFactory httpClientFactory,
        ILogger<ModeratorTransferService> logger)
    {
        _transferRepo = transferRepo;
        _moderatorRepo = moderatorRepo;
        _cityRepo = cityRepo;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<ModeratorTransferResponse> InitiateTransferAsync(Guid fromUserId, InitiateModeratorTransferRequest request)
    {
        _logger.LogInformation("🔄 用户 {FromUserId} 发起版主转让请求给 {ToUserId}，城市 {CityId}",
            fromUserId, request.ToUserId, request.CityId);

        // 1. 检查城市是否存在
        var city = await _cityRepo.GetByIdAsync(request.CityId);
        if (city == null)
        {
            throw new KeyNotFoundException($"城市 {request.CityId} 不存在");
        }

        // 2. 检查发起者是否是该城市的版主
        var isModerator = await _moderatorRepo.IsModeratorAsync(request.CityId, fromUserId);
        if (!isModerator)
        {
            throw new InvalidOperationException("您不是该城市的版主，无法发起转让");
        }

        // 3. 检查目标用户是否已经是版主
        var targetIsModerator = await _moderatorRepo.IsModeratorAsync(request.CityId, request.ToUserId);
        if (targetIsModerator)
        {
            throw new InvalidOperationException("目标用户已经是该城市的版主");
        }

        // 4. 检查是否有待处理的转让请求
        var hasPending = await _transferRepo.HasPendingTransferAsync(request.CityId, request.ToUserId);
        if (hasPending)
        {
            throw new InvalidOperationException("该用户已有待处理的转让请求");
        }

        // 5. 创建转让请求
        var transfer = ModeratorTransfer.Create(fromUserId, request.ToUserId, request.CityId, request.Message);
        var created = await _transferRepo.CreateAsync(transfer);

        // 6. 获取用户信息
        var fromUserInfo = await GetUserInfoAsync(fromUserId);

        // 7. 发送通知给目标用户
        await NotifyTargetUserAboutTransferAsync(created, fromUserInfo, city);

        _logger.LogInformation("✅ 版主转让请求创建成功: TransferId={Id}", created.Id);

        return await MapToResponseAsync(created);
    }

    public async Task<ModeratorTransferResponse> RespondToTransferAsync(Guid userId, RespondToTransferRequest request)
    {
        _logger.LogInformation("🔍 用户 {UserId} 响应转让请求 {TransferId}: {Action}",
            userId, request.TransferId, request.Action);

        // 1. 获取转让请求
        var transfer = await _transferRepo.GetByIdAsync(request.TransferId);
        if (transfer == null)
        {
            throw new KeyNotFoundException("转让请求不存在");
        }

        // 2. 验证是目标用户
        if (transfer.ToUserId != userId)
        {
            throw new InvalidOperationException("您无权响应此转让请求");
        }

        // 3. 检查是否可以响应
        if (!transfer.CanRespond)
        {
            if (transfer.IsExpired)
            {
                throw new InvalidOperationException("转让请求已过期");
            }
            throw new InvalidOperationException($"转让请求已被处理，当前状态: {transfer.Status}");
        }

        // 4. 获取城市信息
        var city = await _cityRepo.GetByIdAsync(transfer.CityId);

        // 5. 处理响应
        if (request.Action.ToLower() == "accept")
        {
            // 接受转让
            transfer.Accept(request.ResponseMessage);

            // 删除原版主记录
            await _moderatorRepo.RemoveAsync(transfer.CityId, transfer.FromUserId);
            _logger.LogInformation("✅ 已移除原版主: UserId={UserId}, CityId={CityId}",
                transfer.FromUserId, transfer.CityId);

            // 创建新版主记录
            var newModerator = new CityModerator
            {
                Id = Guid.NewGuid(),
                CityId = transfer.CityId,
                UserId = transfer.ToUserId,
                AssignedBy = transfer.FromUserId,
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

            await _moderatorRepo.AddAsync(newModerator);
            _logger.LogInformation("✅ 已创建新版主记录: UserId={UserId}, CityId={CityId}",
                transfer.ToUserId, transfer.CityId);

            // 通知原版主：转让已被接受
            await NotifyTransferAcceptedAsync(transfer, city);
        }
        else if (request.Action.ToLower() == "reject")
        {
            // 拒绝转让
            transfer.Reject(request.ResponseMessage);

            // 通知原版主：转让被拒绝
            await NotifyTransferRejectedAsync(transfer, city, request.ResponseMessage);
        }
        else
        {
            throw new InvalidOperationException($"无效的操作: {request.Action}，请使用 accept 或 reject");
        }

        // 6. 更新转让请求
        var updated = await _transferRepo.UpdateAsync(transfer);

        _logger.LogInformation("✅ 转让请求处理完成: Status={Status}", updated.Status);

        return await MapToResponseAsync(updated);
    }

    public async Task CancelTransferAsync(Guid userId, Guid transferId)
    {
        _logger.LogInformation("🔍 用户 {UserId} 取消转让请求 {TransferId}", userId, transferId);

        var transfer = await _transferRepo.GetByIdAsync(transferId);
        if (transfer == null)
        {
            throw new KeyNotFoundException("转让请求不存在");
        }

        if (transfer.FromUserId != userId)
        {
            throw new InvalidOperationException("您无权取消此转让请求");
        }

        if (transfer.Status != "pending")
        {
            throw new InvalidOperationException($"只能取消待处理的转让请求，当前状态: {transfer.Status}");
        }

        transfer.Cancel();
        await _transferRepo.UpdateAsync(transfer);

        // 通知目标用户：转让已取消
        await NotifyTransferCancelledAsync(transfer);

        _logger.LogInformation("✅ 转让请求已取消: TransferId={TransferId}", transferId);
    }

    public async Task<List<ModeratorTransferResponse>> GetInitiatedTransfersAsync(Guid userId)
    {
        var transfers = await _transferRepo.GetByFromUserIdAsync(userId);
        var responses = new List<ModeratorTransferResponse>();

        foreach (var transfer in transfers)
        {
            responses.Add(await MapToResponseAsync(transfer));
        }

        return responses;
    }

    public async Task<List<ModeratorTransferResponse>> GetReceivedTransfersAsync(Guid userId)
    {
        var transfers = await _transferRepo.GetByToUserIdAsync(userId);
        var responses = new List<ModeratorTransferResponse>();

        foreach (var transfer in transfers)
        {
            responses.Add(await MapToResponseAsync(transfer));
        }

        return responses;
    }

    public async Task<List<ModeratorTransferResponse>> GetPendingTransfersAsync(Guid userId)
    {
        var transfers = await _transferRepo.GetPendingTransfersForUserAsync(userId);
        var responses = new List<ModeratorTransferResponse>();

        foreach (var transfer in transfers)
        {
            responses.Add(await MapToResponseAsync(transfer));
        }

        return responses;
    }

    public async Task<ModeratorTransferResponse?> GetTransferByIdAsync(Guid transferId)
    {
        var transfer = await _transferRepo.GetByIdAsync(transferId);
        if (transfer == null) return null;

        return await MapToResponseAsync(transfer);
    }

    #region 私有方法

    /// <summary>
    ///     映射到响应 DTO
    /// </summary>
    private async Task<ModeratorTransferResponse> MapToResponseAsync(ModeratorTransfer transfer)
    {
        var city = await _cityRepo.GetByIdAsync(transfer.CityId);
        var fromUserInfo = await GetUserInfoAsync(transfer.FromUserId);
        var toUserInfo = await GetUserInfoAsync(transfer.ToUserId);

        return new ModeratorTransferResponse
        {
            Id = transfer.Id,
            CityId = transfer.CityId,
            CityName = city?.NameEn ?? "Unknown City",
            FromUserId = transfer.FromUserId,
            FromUserName = fromUserInfo?.Name ?? "Unknown User",
            FromUserAvatar = fromUserInfo?.Avatar ?? "",
            ToUserId = transfer.ToUserId,
            ToUserName = toUserInfo?.Name ?? "Unknown User",
            ToUserAvatar = toUserInfo?.Avatar ?? "",
            Status = transfer.Status,
            Message = transfer.Message,
            ResponseMessage = transfer.ResponseMessage,
            CreatedAt = transfer.CreatedAt,
            RespondedAt = transfer.RespondedAt,
            ExpiresAt = transfer.ExpiresAt
        };
    }

    /// <summary>
    ///     通过 UserService 获取用户信息
    /// </summary>
    private async Task<UserInfo?> GetUserInfoAsync(Guid userId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("user-service");
            var response = await client.GetFromJsonAsync<UserInfo>(
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
    ///     通知目标用户有新的转让请求
    /// </summary>
    private async Task NotifyTargetUserAboutTransferAsync(ModeratorTransfer transfer, UserInfo? fromUserInfo, City city)
    {
        try
        {
            var notification = new
            {
                UserId = transfer.ToUserId.ToString(),
                Title = "版主权限转让邀请",
                Message = $"{fromUserInfo?.Name ?? "某用户"} 邀请您成为 {city.NameEn} 的版主",
                Type = "moderator_transfer",
                RelatedId = transfer.Id.ToString(),
                Metadata = new Dictionary<string, object>
                {
                    ["transferId"] = transfer.Id.ToString(),
                    ["fromUserId"] = transfer.FromUserId.ToString(),
                    ["fromUserName"] = fromUserInfo?.Name ?? "Unknown User",
                    ["fromUserAvatar"] = fromUserInfo?.Avatar ?? "",
                    ["cityId"] = transfer.CityId.ToString(),
                    ["cityName"] = city.NameEn ?? "Unknown City",
                    ["message"] = transfer.Message ?? "",
                    ["expiresAt"] = transfer.ExpiresAt.ToString("o")
                }
            };

            await _httpClientFactory.CreateClient("message-service").PostAsJsonAsync("api/v1/notifications", notification);

            _logger.LogInformation("📬 已向目标用户 {ToUserId} 发送转让通知", transfer.ToUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 发送转让通知失败");
        }
    }

    /// <summary>
    ///     通知原版主：转让已被接受
    /// </summary>
    private async Task NotifyTransferAcceptedAsync(ModeratorTransfer transfer, City? city)
    {
        try
        {
            var toUserInfo = await GetUserInfoAsync(transfer.ToUserId);

            var notification = new
            {
                UserId = transfer.FromUserId.ToString(),
                Title = "版主转让已完成",
                Message = $"{toUserInfo?.Name ?? "对方"} 已接受成为 {city?.NameEn ?? "该城市"} 的版主",
                Type = "moderator_transfer_result",
                RelatedId = transfer.Id.ToString(),
                Metadata = new Dictionary<string, object>
                {
                    ["transferId"] = transfer.Id.ToString(),
                    ["result"] = "accepted",
                    ["toUserId"] = transfer.ToUserId.ToString(),
                    ["toUserName"] = toUserInfo?.Name ?? "Unknown User",
                    ["toUserAvatar"] = toUserInfo?.Avatar ?? "",
                    ["cityId"] = transfer.CityId.ToString(),
                    ["cityName"] = city?.NameEn ?? "Unknown City"
                }
            };

            await _httpClientFactory.CreateClient("message-service").PostAsJsonAsync("api/v1/notifications", notification);

            _logger.LogInformation("📬 已向原版主 {FromUserId} 发送转让接受通知", transfer.FromUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 发送转让接受通知失败");
        }
    }

    /// <summary>
    ///     通知原版主：转让被拒绝
    /// </summary>
    private async Task NotifyTransferRejectedAsync(ModeratorTransfer transfer, City? city, string? reason)
    {
        try
        {
            var toUserInfo = await GetUserInfoAsync(transfer.ToUserId);

            var notification = new
            {
                UserId = transfer.FromUserId.ToString(),
                Title = "版主转让被拒绝",
                Message = $"{toUserInfo?.Name ?? "对方"} 拒绝了成为 {city?.NameEn ?? "该城市"} 版主的邀请",
                Type = "moderator_transfer_result",
                RelatedId = transfer.Id.ToString(),
                Metadata = new Dictionary<string, object>
                {
                    ["transferId"] = transfer.Id.ToString(),
                    ["result"] = "rejected",
                    ["toUserId"] = transfer.ToUserId.ToString(),
                    ["toUserName"] = toUserInfo?.Name ?? "Unknown User",
                    ["toUserAvatar"] = toUserInfo?.Avatar ?? "",
                    ["cityId"] = transfer.CityId.ToString(),
                    ["cityName"] = city?.NameEn ?? "Unknown City",
                    ["responseMessage"] = reason ?? ""
                }
            };

            await _httpClientFactory.CreateClient("message-service").PostAsJsonAsync("api/v1/notifications", notification);

            _logger.LogInformation("📬 已向原版主 {FromUserId} 发送转让拒绝通知", transfer.FromUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 发送转让拒绝通知失败");
        }
    }

    /// <summary>
    ///     通知目标用户：转让已取消
    /// </summary>
    private async Task NotifyTransferCancelledAsync(ModeratorTransfer transfer)
    {
        try
        {
            var fromUserInfo = await GetUserInfoAsync(transfer.FromUserId);
            var city = await _cityRepo.GetByIdAsync(transfer.CityId);

            var notification = new
            {
                UserId = transfer.ToUserId.ToString(),
                Title = "版主转让已取消",
                Message = $"{fromUserInfo?.Name ?? "对方"} 取消了 {city?.NameEn ?? "该城市"} 的版主转让邀请",
                Type = "moderator_transfer_result",
                RelatedId = transfer.Id.ToString(),
                Metadata = new Dictionary<string, object>
                {
                    ["transferId"] = transfer.Id.ToString(),
                    ["result"] = "cancelled",
                    ["fromUserId"] = transfer.FromUserId.ToString(),
                    ["fromUserName"] = fromUserInfo?.Name ?? "Unknown User",
                    ["cityId"] = transfer.CityId.ToString(),
                    ["cityName"] = city?.NameEn ?? "Unknown City"
                }
            };

            await _httpClientFactory.CreateClient("message-service").PostAsJsonAsync("api/v1/notifications", notification);

            _logger.LogInformation("📬 已向目标用户 {ToUserId} 发送转让取消通知", transfer.ToUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 发送转让取消通知失败");
        }
    }

    #endregion

    /// <summary>
    ///     用户基本信息（从 UserService 获取）
    /// </summary>
    private class UserInfo
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
    }
}
