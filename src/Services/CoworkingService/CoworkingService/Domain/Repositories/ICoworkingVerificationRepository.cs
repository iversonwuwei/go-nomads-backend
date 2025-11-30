using CoworkingService.Domain.Entities;

namespace CoworkingService.Domain.Repositories;

/// <summary>
///     Coworking 认证投票仓储接口
/// </summary>
public interface ICoworkingVerificationRepository
{
    Task<CoworkingVerification> AddAsync(CoworkingVerification verification);

    /// <summary>
    ///     添加验证记录并返回当前投票总数（原子操作，避免 N+1）
    /// </summary>
    Task<int> AddAndGetCountAsync(Guid coworkingId, Guid userId);

    Task<bool> HasUserVerifiedAsync(Guid coworkingId, Guid userId);

    Task<int> GetVerificationCountAsync(Guid coworkingId);

    Task<Dictionary<Guid, int>> GetCountsByCoworkingIdsAsync(IEnumerable<Guid> coworkingIds);
}
