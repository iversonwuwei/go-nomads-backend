using System.Text.Json;
using GoNomads.Shared.Communication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MessageService.Application.Services;

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
///     UserService 客户端接口
/// </summary>
public interface IUserServiceClient
{
    Task<UserInfoDto?> GetUserInfoAsync(string userId, CancellationToken cancellationToken = default);

    Task<Dictionary<string, UserInfoDto>> GetUsersInfoAsync(IEnumerable<string> userIds,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     UserService 客户端实现
/// </summary>
public class UserServiceClient : IUserServiceClient
{
    private readonly ServiceInvocationClient _serviceClient;
    private readonly ILogger<UserServiceClient> _logger;
    private readonly string _userServiceName;

    public UserServiceClient(
        ServiceInvocationClient serviceClient,
        IConfiguration configuration,
        ILogger<UserServiceClient> logger)
    {
        _serviceClient = serviceClient;
        _logger = logger;
        _userServiceName = configuration["ServiceNames:UserService"] ?? "user-service";
    }

    /// <summary>
    ///     获取单个用户信息
    /// </summary>
    public async Task<UserInfoDto?> GetUserInfoAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("📞 调用 UserService - GET /api/v1/users/{UserId}", userId);

            var response = await _serviceClient.InvokeAsync<JsonElement>(
                HttpMethod.Get,
                _userServiceName,
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

                    _logger.LogDebug("🔍 解析后的用户数据: Id={Id}, Name={Name}, Email={Email}",
                        userInfo.Id, userInfo.Name, userInfo.Email);

                    return userInfo;
                }
            }

            _logger.LogWarning("⚠️ 获取用户信息失败 - UserId: {UserId}", userId);
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
            _logger.LogDebug("📞 批量调用 UserService - 用户数量: {Count}", userIdList.Count);

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

            _logger.LogDebug("✅ 成功获取 {Count}/{Total} 个用户信息", result.Count, userIdList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 批量调用 UserService 失败");
        }

        return result;
    }
}
