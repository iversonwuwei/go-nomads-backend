using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

/// <summary>
///     会员计划仓储接口
/// </summary>
public interface IMembershipPlanRepository
{
    /// <summary>
    ///     获取所有激活的会员计划
    /// </summary>
    Task<List<MembershipPlan>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     根据等级获取会员计划
    /// </summary>
    Task<MembershipPlan?> GetByLevelAsync(int level, CancellationToken cancellationToken = default);
}
