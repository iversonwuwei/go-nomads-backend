using EventService.Application.DTOs;

namespace EventService.Infrastructure.GrpcClients;

/// <summary>
///     User Service gRPC 客户端接口
/// </summary>
public interface IUserGrpcClient
{
    /// <summary>
    ///     根据用户 ID 获取用户信息
    /// </summary>
    Task<OrganizerInfo?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     批量获取用户信息
    /// </summary>
    Task<Dictionary<Guid, OrganizerInfo>> GetUsersByIdsAsync(IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     批量获取完整用户信息（包含 Avatar 和 Phone）
    /// </summary>
    Task<Dictionary<Guid, UserInfo>> GetUsersInfoByIdsAsync(IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default);
}