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

    public async Task<MembershipResponse> UpgradeMembershipAsync(string userId, int level, int durationDays, string billingCycle = "yearly")
    {
        var membership = await _membershipRepository.GetByUserIdAsync(userId);
        var cycle = billingCycle.ToLower() == "monthly"
            ? BillingCycle.Monthly
            : BillingCycle.Yearly;

        if (membership == null)
        {
            membership = Membership.Create(userId, (MembershipLevel)level, durationDays, cycle);
            await _membershipRepository.CreateAsync(membership);
        }
        else
        {
            // 业务规则：年付用户在会员有效期内不能切换到月付
            if (membership.IsActive && membership.IsYearly && cycle == BillingCycle.Monthly
                && membership.Level == level)
            {
                throw new InvalidOperationException("年付会员在有效期内不能切换为月付，请等会员到期后再操作");
            }

            membership.Upgrade((MembershipLevel)level, durationDays, cycle);
            await _membershipRepository.UpdateAsync(membership);
        }

        _logger.LogInformation("User {UserId} upgraded to level {Level} for {Days} days ({Cycle})",
            userId, level, durationDays, cycle);

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

    public async Task<bool> RecordAiUsageAsync(string userId, bool isAdmin = false)
    {
        // Admin 用户无限制，直接返回成功
        if (isAdmin)
        {
            _logger.LogInformation("🤖 Admin 用户 {UserId} 使用 AI，跳过配额检查", userId);
            return true;
        }

        var membership = await _membershipRepository.GetByUserIdAsync(userId);
        if (membership == null)
        {
            // 如果用户没有会员记录，创建免费会员
            membership = Membership.CreateFree(userId);
            await _membershipRepository.CreateAsync(membership);
        }

        if (!membership.CanUseAI)
            return false;

        membership.IncrementAiUsage();
        await _membershipRepository.UpdateAsync(membership);
        return true;
    }

    /// <summary>
    /// 检查用户是否可以使用 AI 服务
    /// </summary>
    public async Task<AiUsageCheckResponse> CheckAiUsageAsync(string userId, bool isAdmin = false)
    {
        // Admin 用户直接返回无限制配额
        if (isAdmin)
        {
            _logger.LogInformation("🤖 Admin 用户 {UserId} 检查 AI 配额，返回无限制", userId);
            return new AiUsageCheckResponse
            {
                CanUse = true,
                Level = 999,
                LevelName = "Admin",
                Limit = -1,
                Used = 0,
                Remaining = -1,
                IsUnlimited = true,
                ResetDate = null
            };
        }

        var membership = await _membershipRepository.GetByUserIdAsync(userId);
        if (membership == null)
        {
            // 如果用户没有会员记录，创建免费会员
            membership = Membership.CreateFree(userId);
            await _membershipRepository.CreateAsync(membership);
        }

        // 重置月度使用量（如果需要）
        membership.ResetAiUsageIfNeeded();
        await _membershipRepository.UpdateAsync(membership);

        var limit = membership.AiUsageLimit;
        var used = membership.AiUsageThisMonth;
        var remaining = limit < 0 ? -1 : Math.Max(0, limit - used);

        return new AiUsageCheckResponse
        {
            CanUse = membership.CanUseAI,
            Level = membership.Level,
            LevelName = membership.MembershipLevel.ToString(),
            Limit = limit,
            Used = used,
            Remaining = remaining,
            IsUnlimited = limit < 0,
            ResetDate = membership.AiUsageResetDate
        };
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
