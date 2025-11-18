using Dapr.Client;
using EventService.Application.DTOs;
using GoNomads.Shared.Models;

namespace EventService.Infrastructure.GrpcClients;

/// <summary>
///     User Service gRPC 客户端实现（通过 Dapr）
/// </summary>
public class UserGrpcClient : IUserGrpcClient
{
    private const string UserServiceAppId = "user-service";
    private readonly DaprClient _daprClient;
    private readonly ILogger<UserGrpcClient> _logger;

    public UserGrpcClient(DaprClient daprClient, ILogger<UserGrpcClient> logger)
    {
        _daprClient = daprClient;
        _logger = logger;
    }

    public async Task<OrganizerInfo?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("👤 通过 Dapr 调用 UserService 获取用户信息: UserId={UserId}", userId);

            // UserService 使用 string ID，需要转换
            var userIdString = userId.ToString();

            // 使用 Dapr Service Invocation 调用 UserService
            var response = await _daprClient.InvokeMethodAsync<ApiResponse<UserDto>>(
                HttpMethod.Get,
                UserServiceAppId,
                $"api/v1/users/{userIdString}",
                cancellationToken);

            if (response?.Success == true && response.Data != null)
            {
                var userDto = response.Data;
                return new OrganizerInfo
                {
                    Id = userDto.Id,
                    Name = userDto.Name,
                    Email = userDto.Email
                };
            }

            _logger.LogWarning("⚠️ UserService 返回空数据或失败: UserId={UserId}", userId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 调用 UserService 失败: UserId={UserId}", userId);
            return null;
        }
    }

    public async Task<Dictionary<Guid, OrganizerInfo>> GetUsersByIdsAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<Guid, OrganizerInfo>();
        var uniqueUserIds = userIds.Distinct().Where(id => id != Guid.Empty).ToList();

        if (!uniqueUserIds.Any()) return result;

        _logger.LogInformation("👥 批量获取用户信息: Count={Count}", uniqueUserIds.Count);

        // 并行获取用户信息
        var tasks = uniqueUserIds.Select(async userId =>
        {
            var userInfo = await GetUserByIdAsync(userId, cancellationToken);
            return (userId, userInfo);
        });

        var users = await Task.WhenAll(tasks);

        foreach (var (userId, userInfo) in users)
            if (userInfo != null)
                result[userId] = userInfo;

        _logger.LogInformation("✅ 批量获取用户信息完成: 请求={Requested}, 成功={Success}",
            uniqueUserIds.Count, result.Count);

        return result;
    }

    public async Task<Dictionary<Guid, UserInfo>> GetUsersInfoByIdsAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<Guid, UserInfo>();
        var uniqueUserIds = userIds.Distinct().Where(id => id != Guid.Empty).ToList();

        if (!uniqueUserIds.Any()) return result;

        _logger.LogInformation("👥 批量获取完整用户信息（含 Avatar 和 Phone）: Count={Count}", uniqueUserIds.Count);

        // 并行获取用户信息
        var tasks = uniqueUserIds.Select(async userId =>
        {
            try
            {
                var userIdString = userId.ToString();

                // 使用 Dapr Service Invocation 调用 UserService
                var response = await _daprClient.InvokeMethodAsync<ApiResponse<UserDto>>(
                    HttpMethod.Get,
                    UserServiceAppId,
                    $"api/v1/users/{userIdString}",
                    cancellationToken);

                if (response?.Success == true && response.Data != null)
                {
                    var userDto = response.Data;
                    var userInfo = new UserInfo
                    {
                        Id = userDto.Id,
                        Name = userDto.Name,
                        Email = userDto.Email,
                        Avatar = userDto.Avatar,
                        Phone = userDto.Phone
                    };
                    return (userId, userInfo);
                }

                _logger.LogWarning("⚠️ UserService 返回空数据或失败: UserId={UserId}", userId);
                return (userId, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 调用 UserService 失败: UserId={UserId}", userId);
                return (userId, (UserInfo?)null);
            }
        });

        var users = await Task.WhenAll(tasks);

        foreach (var (userId, userInfo) in users)
            if (userInfo != null)
                result[userId] = userInfo;

        _logger.LogInformation("✅ 批量获取完整用户信息完成: 请求={Requested}, 成功={Success}",
            uniqueUserIds.Count, result.Count);

        return result;
    }
}

/// <summary>
///     UserService 返回的 DTO(映射)
/// </summary>
internal class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Avatar { get; set; }
}