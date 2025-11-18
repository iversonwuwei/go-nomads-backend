using CoworkingService.Domain.Entities;

namespace CoworkingService.Domain.Repositories;

/// <summary>
///     Coworking 认证投票仓储接口
/// </summary>
public interface ICoworkingVerificationRepository
{
    Task<CoworkingVerification> AddAsync(CoworkingVerification verification);

    Task<bool> HasUserVerifiedAsync(Guid coworkingId, Guid userId);

    Task<int> GetVerificationCountAsync(Guid coworkingId);

    Task<Dictionary<Guid, int>> GetCountsByCoworkingIdsAsync(IEnumerable<Guid> coworkingIds);
}
