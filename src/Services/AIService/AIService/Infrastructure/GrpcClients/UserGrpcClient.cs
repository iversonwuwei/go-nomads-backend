using System.Net.Http.Json;

namespace AIService.Infrastructure.GrpcClients;

/// <summary>
///     用户服务 gRPC 客户端实现 (通过 HttpClient)
/// </summary>
public class UserGrpcClient : IUserGrpcClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UserGrpcClient> _logger;

    public UserGrpcClient(HttpClient httpClient, ILogger<UserGrpcClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<UserInfo?> GetUserInfoAsync(Guid userId)
    {
        try
        {
            _logger.LogInformation("获取用户信息，用户ID: {UserId}", userId);

            var response = await _httpClient.GetFromJsonAsync<ApiResponse<UserDto>>(
                $"api/v1/users/{userId}"
            );

            if (response?.Success == true && response.Data != null)
            {
                var userInfo = new UserInfo
                {
                    Id = response.Data.Id,
                    Name = response.Data.Name,
                    Email = response.Data.Email,
                    Avatar = response.Data.Avatar,
                    Phone = response.Data.Phone
                };

                _logger.LogInformation("✅ 成功获取用户信息，用户ID: {UserId}", userId);
                return userInfo;
            }

            _logger.LogWarning("⚠️ 用户不存在或获取失败，用户ID: {UserId}", userId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取用户信息失败，用户ID: {UserId}", userId);
            return null;
        }
    }

    public async Task<Dictionary<Guid, UserInfo>> GetUsersInfoByIdsAsync(List<Guid> userIds)
    {
        var result = new Dictionary<Guid, UserInfo>();

        if (!userIds.Any()) return result;

        try
        {
            _logger.LogInformation("批量获取用户信息，用户数量: {Count}", userIds.Count);

            // 并行获取用户信息
            var tasks = userIds.Select(async userId =>
            {
                var userInfo = await GetUserInfoAsync(userId);
                return new { UserId = userId, UserInfo = userInfo };
            });

            var results = await Task.WhenAll(tasks);

            foreach (var item in results)
                if (item.UserInfo != null)
                    result[item.UserId] = item.UserInfo;

            _logger.LogInformation("✅ 成功获取 {SuccessCount}/{TotalCount} 个用户信息",
                result.Count, userIds.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 批量获取用户信息失败");
            return result;
        }
    }

    public async Task<bool> IsUserExistsAsync(Guid userId)
    {
        try
        {
            var userInfo = await GetUserInfoAsync(userId);
            return userInfo != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 验证用户存在性失败，用户ID: {UserId}", userId);
            return false;
        }
    }

    // 内部 DTO 类
    private class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    private class UserDto
    {
        public Guid Id { get; set; }
        public string Name { get; } = string.Empty;
        public string Email { get; } = string.Empty;
        public string? Avatar { get; set; }
        public string? Phone { get; set; }
    }
}