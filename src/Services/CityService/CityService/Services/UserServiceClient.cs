using Dapr.Client;
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
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // 便捷属性:用于兼容,返回 name 或 email 前缀
    public string Username => !string.IsNullOrWhiteSpace(Name) ? Name : Email.Split('@')[0];
}

/// <summary>
///     UserService 客户端 - 通过 Dapr Service Invocation 调用
/// </summary>
public interface IUserServiceClient
{
    Task<UserInfoDto?> GetUserInfoAsync(string userId, CancellationToken cancellationToken = default);

    Task<Dictionary<string, UserInfoDto>> GetUsersInfoAsync(IEnumerable<string> userIds,
        CancellationToken cancellationToken = default);
}

public class UserServiceClient : IUserServiceClient
{
    private readonly DaprClient _daprClient;
    private readonly ILogger<UserServiceClient> _logger;
    private readonly string _userServiceAppId;

    public UserServiceClient(
        DaprClient daprClient,
        IConfiguration configuration,
        ILogger<UserServiceClient> logger)
    {
        _daprClient = daprClient;
        _logger = logger;
        // Dapr app-id 从配置读取,默认为 "user-service"
        _userServiceAppId = configuration["Dapr:UserServiceAppId"] ?? "user-service";
    }

    /// <summary>
    ///     获取单个用户信息
    /// </summary>
    public async Task<UserInfoDto?> GetUserInfoAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("📞 通过 Dapr 调用 UserService - GET /api/v1/users/{UserId}", userId);

            // 使用 Dapr Service Invocation 调用 UserService 的 REST API
            var response = await _daprClient.InvokeMethodAsync<ApiResponse<UserInfoDto>>(
                HttpMethod.Get,
                _userServiceAppId,
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
            _logger.LogError(ex, "❌ Dapr 调用 UserService 失败 - UserId: {UserId}", userId);
            return null;
        }
    }

    /// <summary>
    ///     批量获取用户信息 (并发调用)
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
            _logger.LogInformation("📞 通过 Dapr 批量调用 UserService - 用户数量: {Count}", userIdList.Count);

            // 并发调用多个用户信息
            var tasks = userIdList.Select(async userId =>
            {
                var userInfo = await GetUserInfoAsync(userId, cancellationToken);
                return (userId, userInfo);
            });

            var results = await Task.WhenAll(tasks);

            foreach (var (userId, userInfo) in results)
                if (userInfo != null)
                    result[userId] = userInfo;

            _logger.LogInformation("✅ 成功获取 {Count}/{Total} 个用户信息", result.Count, userIdList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Dapr 批量调用 UserService 失败");
        }

        return result;
    }
}