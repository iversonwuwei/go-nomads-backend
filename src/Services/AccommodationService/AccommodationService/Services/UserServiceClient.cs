using System.Text.Json;
using Dapr.Client;

namespace AccommodationService.Services;

/// <summary>
///     用户信息响应 DTO (匹配 UserService API 响应)
/// </summary>
public class UserInfoDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
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

            var response = await _daprClient.InvokeMethodAsync<JsonElement>(
                HttpMethod.Get,
                _userServiceAppId,
                $"api/v1/users/{userId}",
                cancellationToken);

            if (response.ValueKind == JsonValueKind.Object)
            {
                var success = response.GetProperty("success").GetBoolean();
                
                if (success && response.TryGetProperty("data", out var dataElement))
                {
                    var userInfo = new UserInfoDto
                    {
                        Id = GetStringProperty(dataElement, "id", "Id") ?? string.Empty,
                        Name = GetStringProperty(dataElement, "name", "Name") ?? string.Empty,
                        Email = GetStringProperty(dataElement, "email", "Email") ?? string.Empty,
                        Phone = GetStringProperty(dataElement, "phone", "Phone") ?? string.Empty,
                        AvatarUrl = GetStringProperty(dataElement, "avatarUrl", "AvatarUrl")
                    };

                    _logger.LogInformation("🔍 解析后的用户数据: Id={Id}, Name={Name}, Username={Username}", 
                        userInfo.Id, userInfo.Name, userInfo.Username);
                    
                    return userInfo;
                }
            }

            _logger.LogWarning("⚠️ 获取用户信息失败");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Dapr 调用 UserService 失败 - UserId: {UserId}", userId);
            return null;
        }
    }

    private static string? GetStringProperty(JsonElement element, string camelCaseName, string pascalCaseName)
    {
        if (element.TryGetProperty(camelCaseName, out var camelValue))
            return camelValue.GetString();
        
        if (element.TryGetProperty(pascalCaseName, out var pascalValue))
            return pascalValue.GetString();
        
        return null;
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
