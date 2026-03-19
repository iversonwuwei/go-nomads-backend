using GoNomads.Shared.Communication;
using GoNomads.Shared.Models;

namespace InnovationService.Services;

/// <summary>
///     用户信息响应 DTO (匹配 UserService API 响应)
/// </summary>
public class UserInfoDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
///     UserService 客户端
/// </summary>
public interface IUserServiceClient
{
    Task<UserInfoDto?> GetUserInfoAsync(string userId, CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, UserInfoDto>> GetUsersInfoBatchAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);
}

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
            _logger.LogInformation("📞 调用 UserService - GET /api/v1/users/{UserId}", userId);

            var response = await _serviceClient.InvokeAsync<ApiResponse<UserInfoDto>>(
                HttpMethod.Get,
                _userServiceName,
                $"api/v1/users/{userId}",
                cancellationToken);

            if (response?.Success == true && response.Data != null)
            {
                _logger.LogInformation("✅ 成功获取用户信息: {Name}", response.Data.Name);
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
    ///     批量获取用户信息 (并发调用)
    /// </summary>
    public async Task<Dictionary<Guid, UserInfoDto>> GetUsersInfoBatchAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<Guid, UserInfoDto>();
        var userIdList = userIds.Distinct().ToList();

        if (userIdList.Count == 0) return result;

        try
        {
            _logger.LogInformation("📞 批量调用 UserService - 用户数量: {Count}", userIdList.Count);

            // 并发调用多个用户信息
            var tasks = userIdList.Select(async userId =>
            {
                var userInfo = await GetUserInfoAsync(userId.ToString(), cancellationToken);
                return (userId, userInfo);
            });

            var results = await Task.WhenAll(tasks);

            foreach (var (userId, userInfo) in results)
            {
                if (userInfo != null)
                {
                    result[userId] = userInfo;
                }
            }

            _logger.LogInformation("✅ 成功获取 {Count}/{Total} 个用户信息", result.Count, userIdList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 批量调用 UserService 失败");
        }

        return result;
    }
}
