using System.Text.Json;
using GoNomads.Shared.Communication;
using GoNomads.Shared.Models;

namespace CoworkingService.Services;

/// <summary>
///     用户信息响应 DTO (匹配 UserService API 响应)
/// </summary>
public class UserInfoDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty; // ✅ UserService 返回的字段名是 "name"
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // 便捷属性:用于兼容,返回 name 或 email 前缀
    public string Username => !string.IsNullOrWhiteSpace(Name) ? Name : Email.Split('@')[0];
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

            var response = await _serviceInvocationClient.InvokeAsync<JsonElement>(
                HttpMethod.Get,
                "user-service",
                $"api/v1/users/{userId}",
                cancellationToken);

            // 手动解析 JSON 响应
            if (response.ValueKind == JsonValueKind.Object)
            {
                var success = response.GetProperty("success").GetBoolean();
                
                if (success && response.TryGetProperty("data", out var dataElement))
                {
                    // 从 data 中提取用户信息 (支持 PascalCase 和 camelCase)
                    var userInfo = new UserInfoDto
                    {
                        Id = GetStringProperty(dataElement, "id", "Id") ?? string.Empty,
                        Name = GetStringProperty(dataElement, "name", "Name") ?? string.Empty,
                        Email = GetStringProperty(dataElement, "email", "Email") ?? string.Empty,
                        Phone = GetStringProperty(dataElement, "phone", "Phone") ?? string.Empty,
                        AvatarUrl = GetStringProperty(dataElement, "avatarUrl", "AvatarUrl")
                    };

                    _logger.LogInformation("🔍 解析后的用户数据: Id={Id}, Name={Name}, Email={Email}, Username={Username}", 
                        userInfo.Id, userInfo.Name, userInfo.Email, userInfo.Username);
                    
                    return userInfo;
                }
            }

            _logger.LogWarning("⚠️ 获取用户信息失败");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 调用 UserService 失败 - UserId: {UserId}", userId);
            return null;
        }
    }

    /// <summary>
    ///     从 JsonElement 中获取字符串属性 (支持 camelCase 和 PascalCase)
    /// </summary>
    private static string? GetStringProperty(JsonElement element, string camelCaseName, string pascalCaseName)
    {
        if (element.TryGetProperty(camelCaseName, out var camelValue))
            return camelValue.GetString();
        
        if (element.TryGetProperty(pascalCaseName, out var pascalValue))
            return pascalValue.GetString();
        
        return null;
    }

    /// <summary>
    ///     批量获取用户信息 (使用批量 API)
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

            var requestBody = new { UserIds = userIdList };
            var response = await _serviceInvocationClient.InvokeAsync<object, JsonElement>(
                HttpMethod.Post,
                "user-service",
                "api/v1/users/batch",
                requestBody,
                cancellationToken);

            // 解析响应
            if (response.ValueKind == JsonValueKind.Object)
            {
                var success = response.TryGetProperty("success", out var successProp) && successProp.GetBoolean();

                if (success && response.TryGetProperty("data", out var dataElement) &&
                    dataElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var userElement in dataElement.EnumerateArray())
                    {
                        var userInfo = new UserInfoDto
                        {
                            Id = GetStringProperty(userElement, "id", "Id") ?? string.Empty,
                            Name = GetStringProperty(userElement, "name", "Name") ?? string.Empty,
                            Email = GetStringProperty(userElement, "email", "Email") ?? string.Empty,
                            Phone = GetStringProperty(userElement, "phone", "Phone") ?? string.Empty,
                            AvatarUrl = GetStringProperty(userElement, "avatarUrl", "AvatarUrl")
                        };

                        if (!string.IsNullOrEmpty(userInfo.Id))
                        {
                            result[userInfo.Id] = userInfo;
                        }
                    }
                }
            }

            _logger.LogInformation("✅ 批量 API 成功获取 {Count}/{Total} 个用户信息", result.Count, userIdList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 批量 API 失败，降级为并发单独调用");

            // 降级：并发调用单个 API
            var tasks = userIdList.Select(async userId =>
            {
                var userInfo = await GetUserInfoAsync(userId, cancellationToken);
                return (userId, userInfo);
            });

            var results = await Task.WhenAll(tasks);

            foreach (var (userId, userInfo) in results)
                if (userInfo != null)
                    result[userId] = userInfo;
        }

        return result;
    }
}
