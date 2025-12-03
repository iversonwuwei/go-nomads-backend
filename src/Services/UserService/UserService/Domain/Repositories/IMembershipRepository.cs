using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

/// <summary>
///     会员仓储接口 - 定义在领域层
/// </summary>
public interface IMembershipRepository
{
    /// <summary>
    ///     根据用户ID获取会员信息
    /// </summary>
    Task<Membership?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     根据ID获取会员信息
    /// </summary>
    Task<Membership?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     创建会员记录
    /// </summary>
    Task<Membership> CreateAsync(Membership membership, CancellationToken cancellationToken = default);

    /// <summary>
    ///     更新会员记录
    /// </summary>
    Task<Membership> UpdateAsync(Membership membership, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取即将过期的会员列表（用于提醒）
    /// </summary>
    Task<List<Membership>> GetExpiringMembershipsAsync(int daysBeforeExpiry = 7, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取需要自动续费的会员列表
    /// </summary>
    Task<List<Membership>> GetAutoRenewMembershipsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取已过期的会员列表
    /// </summary>
    Task<List<Membership>> GetExpiredMembershipsAsync(CancellationToken cancellationToken = default);
}
