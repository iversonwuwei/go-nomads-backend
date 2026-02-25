using System.Collections.Concurrent;
using System.Net.Http.Json;
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
///     UserService 客户端 - 通过 HttpClient 调用
/// </summary>
public interface IUserServiceClient
{
    Task<UserInfoDto?> GetUserInfoAsync(string userId, CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, UserInfoDto>> GetUsersInfoBatchAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);
}

public class UserServiceClient : IUserServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UserServiceClient> _logger;

    // 用户信息内存缓存（用户名/头像极少变化，缓存 10 分钟）
    private static readonly ConcurrentDictionary<string, (UserInfoDto Info, DateTime CachedAt)> _userInfoCache = new();
    private static readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(10);

    public UserServiceClient(
        HttpClient httpClient,
        ILogger<UserServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    ///     获取单个用户信息（带内存缓存）
    /// </summary>
    public async Task<UserInfoDto?> GetUserInfoAsync(string userId, CancellationToken cancellationToken = default)
    {
        // 检查缓存
        if (_userInfoCache.TryGetValue(userId, out var cached) &&
            DateTime.UtcNow - cached.CachedAt < _cacheDuration)
        {
            return cached.Info;
        }

        try
        {
            _logger.LogInformation("📞 调用 UserService - GET /api/v1/users/{UserId}", userId);

            var response = await _httpClient.GetFromJsonAsync<ApiResponse<UserInfoDto>>(
                $"api/v1/users/{userId}",
                cancellationToken);

            if (response?.Success == true && response.Data != null)
            {
                _logger.LogInformation("✅ 成功获取用户信息: {Name}", response.Data.Name);
                // 写入缓存
                _userInfoCache[userId] = (response.Data, DateTime.UtcNow);
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
    ///     批量获取用户信息 (并发调用，带缓存)
    /// </summary>
    public async Task<Dictionary<Guid, UserInfoDto>> GetUsersInfoBatchAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<Guid, UserInfoDto>();
        var userIdList = userIds.Distinct().ToList();

        if (userIdList.Count == 0) return result;

        // 分离缓存命中和缓存未命中的 ID
        var uncachedIds = new List<Guid>();
        foreach (var userId in userIdList)
        {
            var key = userId.ToString();
            if (_userInfoCache.TryGetValue(key, out var cached) &&
                DateTime.UtcNow - cached.CachedAt < _cacheDuration)
            {
                result[userId] = cached.Info;
            }
            else
            {
                uncachedIds.Add(userId);
            }
        }

        if (uncachedIds.Count == 0)
        {
            _logger.LogInformation("✅ 全部 {Count} 个用户信息命中缓存", userIdList.Count);
            return result;
        }

        try
        {
            _logger.LogInformation("📞 批量调用 UserService - 缓存命中: {CachedCount}, 需查询: {UncachedCount}",
                result.Count, uncachedIds.Count);

            // 并发调用未命中缓存的用户
            var tasks = uncachedIds.Select(async userId =>
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
