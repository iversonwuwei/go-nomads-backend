using GoNomads.Shared.Communication;
using GoNomads.Shared.Models;

namespace CityService.Services;

/// <summary>
///     用户信息响应 DTO (匹配 UserService API 响应)
/// </summary>
public class UserInfoDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty; // ✅ UserService 返回的字段名是 "name"
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; } // ✅ 用户头像 URL
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // 便捷属性:用于兼容,返回 name 或 email 前缀
    public string Username => !string.IsNullOrWhiteSpace(Name) ? Name : Email.Split('@')[0];
}

/// <summary>
///     批量获取用户请求
/// </summary>
public class BatchUserIdsRequest
{
    public List<string> UserIds { get; set; } = new();
}

/// <summary>
///     UserService 客户端
/// </summary>
public interface IUserServiceClient
{
    Task<UserInfoDto?> GetUserInfoAsync(string userId, CancellationToken cancellationToken = default);

    Task<Dictionary<string, UserInfoDto>> GetUsersInfoAsync(IEnumerable<string> userIds,
        CancellationToken cancellationToken = default);
}

public class UserServiceClient : IUserServiceClient
{
    private readonly ILogger<UserServiceClient> _logger;
    private readonly ServiceInvocationClient _serviceInvocationClient;

    public UserServiceClient(
        ServiceInvocationClient serviceInvocationClient,
        ILogger<UserServiceClient> logger)
    {
        _serviceInvocationClient = serviceInvocationClient;
        _logger = logger;
    }

    /// <summary>
    ///     获取单个用户信息
    /// </summary>
    public async Task<UserInfoDto?> GetUserInfoAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("📞 调用 UserService - GET /api/v1/users/{UserId}", userId);

            var response = await _serviceInvocationClient.InvokeAsync<ApiResponse<UserInfoDto>>(
                HttpMethod.Get,
                "user-service",
                $"api/v1/users/{userId}",
                cancellationToken);

            if (response?.Success == true && response.Data != null)
            {
                _logger.LogInformation("✅ 成功获取用户信息: {Username}", response.Data.Username);
                return response.Data;
            }

            _logger.LogWarning("⚠️ 获取用户信息失败: {Message}", response?.Message ?? "Unknown error");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 调用 UserService 失败 - UserId: {UserId}", userId);
            return null;
        }
    }

    /// <summary>
    ///     批量获取用户信息 (使用批量接口，一次请求获取所有用户)
    /// </summary>
    public async Task<Dictionary<string, UserInfoDto>> GetUsersInfoAsync(
        IEnumerable<string> userIds,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, UserInfoDto>();
        var userIdList = userIds.Distinct().ToList();

        if (userIdList.Count == 0) return result;

        try
        {
            _logger.LogInformation("📞 批量调用 UserService - POST /api/v1/users/batch - 用户数量: {Count}", userIdList.Count);

            var request = new BatchUserIdsRequest { UserIds = userIdList };
            var response = await _serviceInvocationClient.InvokeAsync<BatchUserIdsRequest, ApiResponse<List<UserInfoDto>>>(
                HttpMethod.Post,
                "user-service",
                "api/v1/users/batch",
                request,
                cancellationToken);

            if (response?.Success == true && response.Data != null)
            {
                foreach (var userInfo in response.Data)
                {
                    result[userInfo.Id] = userInfo;
                }
                _logger.LogInformation("✅ 批量获取成功 {Count}/{Total} 个用户信息", result.Count, userIdList.Count);
            }
            else
            {
                _logger.LogWarning("⚠️ 批量获取用户信息失败: {Message}", response?.Message ?? "Unknown error");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 批量调用 UserService 失败");
        }

        return result;
    }
}