using UserService.Application.DTOs;

namespace UserService.Application.Services;

/// <summary>
///     会员服务接口
/// </summary>
public interface IMembershipService
{
    /// <summary>
    ///     获取用户会员信息
    /// </summary>
    Task<MembershipResponse?> GetMembershipAsync(string userId);

    /// <summary>
    ///     升级会员
    /// </summary>
    Task<MembershipResponse> UpgradeMembershipAsync(string userId, int level, int durationDays);

    /// <summary>
    ///     缴纳保证金
    /// </summary>
    Task<MembershipResponse> PayDepositAsync(string userId, decimal amount);

    /// <summary>
    ///     设置自动续费
    /// </summary>
    Task<MembershipResponse> SetAutoRenewAsync(string userId, bool enabled);

    /// <summary>
    ///     记录 AI 使用次数
    /// </summary>
    Task<bool> RecordAiUsageAsync(string userId);

    /// <summary>
    ///     检查用户 AI 使用配额
    /// </summary>
    Task<AiUsageCheckResponse> CheckAiUsageAsync(string userId);

    /// <summary>
    ///     获取即将过期的会员列表（用于发送提醒）
    /// </summary>
    Task<IEnumerable<MembershipResponse>> GetExpiringMembershipsAsync(int daysBeforeExpiry = 7);

    /// <summary>
    ///     处理自动续费
    /// </summary>
    Task ProcessAutoRenewalsAsync();

    /// <summary>
    ///     处理过期会员（降级为免费）
    /// </summary>
    Task ProcessExpiredMembershipsAsync();
}
