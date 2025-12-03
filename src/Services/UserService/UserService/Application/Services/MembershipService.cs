using Microsoft.Extensions.Logging;
using UserService.Application.DTOs;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Application.Services;

/// <summary>
///     会员服务实现
/// </summary>
public class MembershipService : IMembershipService
{
    private readonly ILogger<MembershipService> _logger;
    private readonly IMembershipRepository _membershipRepository;

    public MembershipService(
        IMembershipRepository membershipRepository,
        ILogger<MembershipService> logger)
    {
        _membershipRepository = membershipRepository;
        _logger = logger;
    }

    public async Task<MembershipResponse?> GetMembershipAsync(string userId)
    {
        var membership = await _membershipRepository.GetByUserIdAsync(userId);
        if (membership == null)
        {
            // 如果用户没有会员记录，创建免费会员
            membership = Membership.CreateFree(userId);
            await _membershipRepository.CreateAsync(membership);
        }

        return MembershipResponse.FromEntity(membership);
    }

    public async Task<MembershipResponse> UpgradeMembershipAsync(string userId, int level, int durationDays)
    {
        var membership = await _membershipRepository.GetByUserIdAsync(userId);

        if (membership == null)
        {
            membership = Membership.Create(userId, (MembershipLevel)level, durationDays);
            await _membershipRepository.CreateAsync(membership);
        }
        else
        {
            membership.Upgrade((MembershipLevel)level, durationDays);
            await _membershipRepository.UpdateAsync(membership);
        }

        _logger.LogInformation("User {UserId} upgraded to level {Level} for {Days} days",
            userId, level, durationDays);

        return MembershipResponse.FromEntity(membership);
    }

    public async Task<MembershipResponse> PayDepositAsync(string userId, decimal amount)
    {
        var membership = await _membershipRepository.GetByUserIdAsync(userId);
        if (membership == null)
            throw new InvalidOperationException("会员记录不存在");

        membership.PayDeposit(amount);
        await _membershipRepository.UpdateAsync(membership);

        _logger.LogInformation("User {UserId} paid deposit {Amount}", userId, amount);
        return MembershipResponse.FromEntity(membership);
    }

    public async Task<MembershipResponse> SetAutoRenewAsync(string userId, bool enabled)
    {
        var membership = await _membershipRepository.GetByUserIdAsync(userId);
        if (membership == null)
            throw new InvalidOperationException("会员记录不存在");

        membership.SetAutoRenew(enabled);
        await _membershipRepository.UpdateAsync(membership);

        return MembershipResponse.FromEntity(membership);
    }

    public async Task<bool> RecordAiUsageAsync(string userId)
    {
        var membership = await _membershipRepository.GetByUserIdAsync(userId);
        if (membership == null || !membership.CanUseAI)
            return false;

        membership.IncrementAiUsage();
        await _membershipRepository.UpdateAsync(membership);
        return true;
    }

    public async Task<IEnumerable<MembershipResponse>> GetExpiringMembershipsAsync(int daysBeforeExpiry = 7)
    {
        var memberships = await _membershipRepository.GetExpiringMembershipsAsync(daysBeforeExpiry);
        return memberships.Select(MembershipResponse.FromEntity);
    }

    public async Task ProcessAutoRenewalsAsync()
    {
        var memberships = await _membershipRepository.GetAutoRenewMembershipsAsync();

        foreach (var membership in memberships)
            try
            {
                // TODO: 调用支付服务进行自动扣款
                // 这里仅处理续费逻辑，实际扣款需集成支付系统

                membership.Renew(365); // 默认续费一年
                await _membershipRepository.UpdateAsync(membership);

                _logger.LogInformation("Auto-renewed membership for user {UserId}", membership.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-renew membership for user {UserId}", membership.UserId);
            }
    }

    public async Task ProcessExpiredMembershipsAsync()
    {
        var memberships = await _membershipRepository.GetExpiredMembershipsAsync();

        foreach (var membership in memberships)
            try
            {
                membership.Downgrade();
                await _membershipRepository.UpdateAsync(membership);

                _logger.LogInformation("Downgraded expired membership for user {UserId} to Free",
                    membership.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process expired membership for user {UserId}",
                    membership.UserId);
            }
    }
}
