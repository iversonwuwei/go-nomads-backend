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
    private const string UserBatchEndpoint = "api/v1/users/batch";
    private const int BatchSize = 50;
    private readonly DaprClient _daprClient;
    private readonly ILogger<UserGrpcClient> _logger;

    public UserGrpcClient(DaprClient daprClient, ILogger<UserGrpcClient> logger)
    {
        _daprClient = daprClient;
        _logger = logger;
    }

    public async Task<OrganizerInfo?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            _logger.LogWarning("⚠️ 无效的用户 ID");
            return null;
        }

        var users = await GetUsersByIdsAsync(new[] { userId }, cancellationToken);
        if (users.TryGetValue(userId, out var userInfo))
            return userInfo;

        _logger.LogWarning("⚠️ UserService 返回空数据或失败: UserId={UserId}", userId);
        return null;
    }

    public async Task<Dictionary<Guid, OrganizerInfo>> GetUsersByIdsAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<Guid, OrganizerInfo>();
        var uniqueUserIds = userIds.Distinct().Where(id => id != Guid.Empty).ToList();

        if (!uniqueUserIds.Any()) return result;

        _logger.LogInformation("👥 批量获取用户信息: Count={Count}", uniqueUserIds.Count);

        // 🚀 性能优化：使用批量接口代替 N+1 单独查询
        foreach (var batch in uniqueUserIds.Chunk(BatchSize))
        {
            var payload = new BatchUserIdsRequest(batch.Select(id => id.ToString()).ToList());
            try
            {
                var response = await _daprClient.InvokeMethodAsync<BatchUserIdsRequest, ApiResponse<List<UserDto>>>(
                    HttpMethod.Post,
                    UserServiceAppId,
                    UserBatchEndpoint,
                    payload,
                    cancellationToken);

                if (response?.Success == true && response.Data != null)
                {
                    foreach (var userDto in response.Data)
                    {
                        if (Guid.TryParse(userDto.Id, out var userId))
                        {
                            result[userId] = new OrganizerInfo
                            {
                                Id = userDto.Id,
                                Name = userDto.Name,
                                Email = userDto.Email,
                                AvatarUrl = userDto.AvatarUrl
                            };
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("⚠️ User batch lookup failed for batch size {BatchSize}", batch.Length);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ User batch lookup request failed for batch size {BatchSize}", batch.Length);
            }
        }

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

        // 🚀 性能优化：使用批量接口代替 N+1 单独查询
        foreach (var batch in uniqueUserIds.Chunk(BatchSize))
        {
            var payload = new BatchUserIdsRequest(batch.Select(id => id.ToString()).ToList());
            try
            {
                var response = await _daprClient.InvokeMethodAsync<BatchUserIdsRequest, ApiResponse<List<UserDto>>>(
                    HttpMethod.Post,
                    UserServiceAppId,
                    UserBatchEndpoint,
                    payload,
                    cancellationToken);

                if (response?.Success == true && response.Data != null)
                {
                    foreach (var userDto in response.Data)
                    {
                        if (Guid.TryParse(userDto.Id, out var userId))
                        {
                            result[userId] = new UserInfo
                            {
                                Id = userDto.Id,
                                Name = userDto.Name,
                                Email = userDto.Email,
                                Avatar = userDto.AvatarUrl,
                                Phone = userDto.Phone
                            };
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("⚠️ User batch lookup failed for batch size {BatchSize}", batch.Length);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ User batch lookup request failed for batch size {BatchSize}", batch.Length);
            }
        }

        _logger.LogInformation("✅ 批量获取完整用户信息完成: 请求={Requested}, 成功={Success}",
            uniqueUserIds.Count, result.Count);

        return result;
    }
}

/// <summary>
///     批量用户ID请求
/// </summary>
internal sealed record BatchUserIdsRequest(List<string> UserIds);

/// <summary>
///     UserService 返回的 DTO(映射)
/// </summary>
internal class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
}