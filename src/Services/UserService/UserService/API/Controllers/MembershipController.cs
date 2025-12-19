using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GoNomads.Shared.Middleware;
using GoNomads.Shared.Services;
using UserService.Application.DTOs;
using UserService.Application.Services;
using UserService.Domain.Repositories;

namespace UserService.API.Controllers;

/// <summary>
///     会员管理控制器
///     Token 验证在 Gateway 完成，通过 UserContext 获取用户信息
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class MembershipController : ControllerBase
{
    private readonly ILogger<MembershipController> _logger;
    private readonly IMembershipService _membershipService;
    private readonly IMembershipPlanRepository _membershipPlanRepository;
    private readonly ICurrentUserService _currentUser;

    public MembershipController(
        IMembershipService membershipService,
        IMembershipPlanRepository membershipPlanRepository,
        ICurrentUserService currentUser,
        ILogger<MembershipController> logger)
    {
        _membershipService = membershipService;
        _membershipPlanRepository = membershipPlanRepository;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    ///     获取所有会员计划（公开接口）
    /// </summary>
    [HttpGet("plans")]
    public async Task<ActionResult<IEnumerable<MembershipPlanResponse>>> GetPlans()
    {
        _logger.LogInformation("📋 获取所有会员计划");
        var plans = await _membershipPlanRepository.GetAllAsync();
        var response = plans.Select(MembershipPlanResponse.FromEntity);
        return Ok(response);
    }

    /// <summary>
    ///     获取指定等级的会员计划（公开接口）
    /// </summary>
    [HttpGet("plans/{level:int}")]
    public async Task<ActionResult<MembershipPlanResponse>> GetPlanByLevel(int level)
    {
        _logger.LogInformation("📋 获取会员计划: Level={Level}", level);
        var plan = await _membershipPlanRepository.GetByLevelAsync(level);
        if (plan == null)
            return NotFound(new { message = $"会员计划等级 {level} 不存在" });

        return Ok(MembershipPlanResponse.FromEntity(plan));
    }

    /// <summary>
    ///     获取当前用户会员信息
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<MembershipResponse>> GetMembership()
    {
        var userId = _currentUser.GetUserIdString();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "用户未登录" });

        _logger.LogInformation("👤 获取会员信息: UserId={UserId}", userId);
        var membership = await _membershipService.GetMembershipAsync(userId);
        return Ok(membership);
    }

    /// <summary>
    ///     升级会员
    /// </summary>
    [HttpPost("upgrade")]
    public async Task<ActionResult<MembershipResponse>> Upgrade([FromBody] UpgradeMembershipRequest request)
    {
        var userId = _currentUser.GetUserIdString();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "用户未登录" });

        _logger.LogInformation("⬆️ 升级会员: UserId={UserId}, Level={Level}", userId, request.Level);
        var membership = await _membershipService.UpgradeMembershipAsync(
            userId, request.Level, request.DurationDays);
        return Ok(membership);
    }

    /// <summary>
    ///     缴纳保证金
    /// </summary>
    [HttpPost("deposit")]
    public async Task<ActionResult<MembershipResponse>> PayDeposit([FromBody] PayDepositRequest request)
    {
        var userId = _currentUser.GetUserIdString();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "用户未登录" });

        _logger.LogInformation("💰 缴纳保证金: UserId={UserId}, Amount={Amount}", userId, request.Amount);
        var membership = await _membershipService.PayDepositAsync(userId, request.Amount);
        return Ok(membership);
    }

    /// <summary>
    ///     设置自动续费
    /// </summary>
    [HttpPost("auto-renew")]
    public async Task<ActionResult<MembershipResponse>> SetAutoRenew([FromBody] SetAutoRenewRequest request)
    {
        var userId = _currentUser.GetUserIdString();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "用户未登录" });

        _logger.LogInformation("🔄 设置自动续费: UserId={UserId}, Enabled={Enabled}", userId, request.Enabled);
        var membership = await _membershipService.SetAutoRenewAsync(userId, request.Enabled);
        return Ok(membership);
    }

    /// <summary>
    ///     记录 AI 使用
    /// </summary>
    [HttpPost("ai-usage")]
    public async Task<ActionResult<bool>> RecordAiUsage()
    {
        var userId = _currentUser.GetUserIdString();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "用户未登录" });

        _logger.LogInformation("🤖 记录 AI 使用: UserId={UserId}", userId);
        var result = await _membershipService.RecordAiUsageAsync(userId);
        return Ok(new { success = result });
    }

    /// <summary>
    ///     获取即将过期的会员列表（管理员接口）
    /// </summary>
    [HttpGet("expiring")]
    public async Task<ActionResult<IEnumerable<MembershipResponse>>> GetExpiringMemberships(
        [FromQuery] int daysBeforeExpiry = 7)
    {
        if (!_currentUser.IsAdmin())
            return Forbid();

        _logger.LogInformation("📅 获取即将过期的会员: DaysBeforeExpiry={Days}", daysBeforeExpiry);
        var memberships = await _membershipService.GetExpiringMembershipsAsync(daysBeforeExpiry);
        return Ok(memberships);
    }

    /// <summary>
    ///     手动触发自动续费处理（管理员接口）
    /// </summary>
    [HttpPost("process-renewals")]
    public async Task<IActionResult> ProcessAutoRenewals()
    {
        if (!_currentUser.IsAdmin())
            return Forbid();

        _logger.LogInformation("🔄 手动触发自动续费处理");
        await _membershipService.ProcessAutoRenewalsAsync();
        return Ok(new { message = "Auto renewals processed" });
    }

    /// <summary>
    ///     手动触发过期会员处理（管理员接口）
    /// </summary>
    [HttpPost("process-expired")]
    public async Task<IActionResult> ProcessExpiredMemberships()
    {
        if (!_currentUser.IsAdmin())
            return Forbid();

        _logger.LogInformation("⏰ 手动触发过期会员处理");
        await _membershipService.ProcessExpiredMembershipsAsync();
        return Ok(new { message = "Expired memberships processed" });
    }
}
