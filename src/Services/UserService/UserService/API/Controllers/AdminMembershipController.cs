using GoNomads.Shared.DTOs;
using GoNomads.Shared.Services;
using Microsoft.AspNetCore.Mvc;
using Postgrest;
using UserService.Application.Services;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;
using Client = Supabase.Client;

namespace UserService.API.Controllers;

[ApiController]
[Route("api/v1/admin/membership")]
public class AdminMembershipController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly IMembershipRepository _membershipRepository;
    private readonly IMembershipPlanRepository _planRepository;
    private readonly IUserService _userService;
    private readonly Client _supabase;
    private readonly ILogger<AdminMembershipController> _logger;

    public AdminMembershipController(
        ICurrentUserService currentUser,
        IMembershipRepository membershipRepository,
        IMembershipPlanRepository planRepository,
        IUserService userService,
        Client supabase,
        ILogger<AdminMembershipController> logger)
    {
        _currentUser = currentUser;
        _membershipRepository = membershipRepository;
        _planRepository = planRepository;
        _userService = userService;
        _supabase = supabase;
        _logger = logger;
    }

    [HttpGet("plans")]
    public async Task<ActionResult<ApiResponse<List<AdminMembershipPlanDto>>>> GetPlans()
    {
        try
        {
            if (!_currentUser.IsAdmin()) return Forbid();

            var plans = await _planRepository.GetAllAsync();
            var memberships = await _supabase.From<Membership>().Get();
            var subscriberCounts = memberships.Models
                .Where(membership => membership.Level > 0 && membership.IsActive)
                .GroupBy(membership => membership.Level)
                .ToDictionary(group => group.Key, group => group.Count());

            return Ok(new ApiResponse<List<AdminMembershipPlanDto>>
            {
                Success = true,
                Message = "获取会员计划成功",
                Data = plans
                    .OrderBy(p => p.SortOrder)
                    .Select(plan => MapToDto(plan, subscriberCounts.GetValueOrDefault(plan.Level)))
                    .ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取会员计划失败");
            return StatusCode(500, ApiResponse<List<AdminMembershipPlanDto>>.ErrorResponse("获取会员计划失败"));
        }
    }

    [HttpGet("plans/{id}")]
    public async Task<ActionResult<ApiResponse<AdminMembershipPlanDto>>> GetPlanById(string id)
    {
        try
        {
            if (!_currentUser.IsAdmin()) return Forbid();

            var existing = await _supabase.From<MembershipPlan>()
                .Where(p => p.Id == id)
                .Single();

            if (existing == null)
                return NotFound(ApiResponse<AdminMembershipPlanDto>.ErrorResponse("会员计划不存在"));

            var subscribers = await _supabase.From<Membership>()
                .Where(m => m.Level == existing.Level)
                .Get();

            return Ok(ApiResponse<AdminMembershipPlanDto>.SuccessResponse(
                MapToDto(existing, subscribers.Models.Count(m => m.IsActive)),
                "获取会员计划详情成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取会员计划详情失败: Id={Id}", id);
            return StatusCode(500, ApiResponse<AdminMembershipPlanDto>.ErrorResponse("获取会员计划详情失败"));
        }
    }

    [HttpGet("plans/{id}/subscribers")]
    public async Task<ActionResult<ApiResponse<List<AdminMembershipSubscriberDto>>>> GetPlanSubscribers(string id)
    {
        try
        {
            if (!_currentUser.IsAdmin()) return Forbid();

            var plan = await _supabase.From<MembershipPlan>()
                .Where(p => p.Id == id)
                .Single();

            if (plan == null)
                return NotFound(ApiResponse<List<AdminMembershipSubscriberDto>>.ErrorResponse("会员计划不存在"));

            var memberships = await _supabase.From<Membership>()
                .Where(m => m.Level == plan.Level)
                .Get();

            var userIds = memberships.Models
                .Select(membership => membership.UserId)
                .Where(userId => !string.IsNullOrWhiteSpace(userId))
                .Distinct()
                .ToList();

            var users = await _userService.GetUsersByIdsAsync(userIds);
            var userMap = users.ToDictionary(user => user.Id, user => user);

            var items = memberships.Models
                .OrderByDescending(membership => membership.UpdatedAt)
                .Select(membership =>
                {
                    userMap.TryGetValue(membership.UserId, out var user);
                    return new AdminMembershipSubscriberDto
                    {
                        UserId = membership.UserId,
                        UserName = ResolveUserDisplayName(user),
                        Email = user?.Email ?? string.Empty,
                        StartDate = membership.StartDate,
                        ExpiryDate = membership.ExpiryDate,
                        IsActive = membership.IsActive,
                        AutoRenew = membership.AutoRenew,
                        RemainingDays = membership.RemainingDays
                    };
                })
                .ToList();

            return Ok(ApiResponse<List<AdminMembershipSubscriberDto>>.SuccessResponse(items, "获取会员订阅者成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取会员计划订阅者失败: Id={Id}", id);
            return StatusCode(500, ApiResponse<List<AdminMembershipSubscriberDto>>.ErrorResponse("获取会员订阅者失败"));
        }
    }

    [HttpPost("plans")]
    public async Task<ActionResult<ApiResponse<AdminMembershipPlanDto>>> CreatePlan(
        [FromBody] AdminMembershipPlanRequest request)
    {
        try
        {
            if (!_currentUser.IsAdmin()) return Forbid();

            var plan = new MembershipPlan
            {
                Id = Guid.NewGuid().ToString(),
                Currency = request.Currency ?? "CNY",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            ApplyPlanMutation(plan, request, isNew: true);

            var response = await _supabase.From<MembershipPlan>().Insert(plan);
            var created = response.Models.FirstOrDefault();
            var payload = created ?? plan;

            _logger.LogInformation("管理员创建会员计划: Name={Name}, Level={Level}", plan.Name, plan.Level);
            return Ok(ApiResponse<AdminMembershipPlanDto>.SuccessResponse(MapToDto(payload, 0), "会员计划创建成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建会员计划失败");
            return StatusCode(500, ApiResponse<AdminMembershipPlanDto>.ErrorResponse("创建会员计划失败"));
        }
    }

    [HttpPut("plans/{id}")]
    public async Task<ActionResult<ApiResponse<AdminMembershipPlanDto>>> UpdatePlan(
        string id,
        [FromBody] AdminMembershipPlanRequest request)
    {
        try
        {
            if (!_currentUser.IsAdmin()) return Forbid();

            var existing = await _supabase.From<MembershipPlan>()
                .Where(p => p.Id == id)
                .Single();

            if (existing == null)
                return NotFound(ApiResponse<AdminMembershipPlanDto>.ErrorResponse("会员计划不存在"));

            ApplyPlanMutation(existing, request, isNew: false);
            existing.UpdatedAt = DateTime.UtcNow;

            await _supabase.From<MembershipPlan>()
                .Where(p => p.Id == id)
                .Update(existing);

            _logger.LogInformation("管理员更新会员计划: Id={Id}, Name={Name}", id, existing.Name);
            var subscribers = await _supabase.From<Membership>()
                .Where(m => m.Level == existing.Level)
                .Get();

            return Ok(ApiResponse<AdminMembershipPlanDto>.SuccessResponse(
                MapToDto(existing, subscribers.Models.Count(m => m.IsActive)),
                "更新成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新会员计划失败: Id={Id}", id);
            return StatusCode(500, ApiResponse<AdminMembershipPlanDto>.ErrorResponse("更新会员计划失败"));
        }
    }

    [HttpDelete("plans/{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeletePlan(string id)
    {
        try
        {
            if (!_currentUser.IsAdmin()) return Forbid();

            await _supabase.From<MembershipPlan>()
                .Where(p => p.Id == id)
                .Delete();

            _logger.LogInformation("管理员删除会员计划: Id={Id}", id);
            return Ok(ApiResponse<bool>.SuccessResponse(true, "删除成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除会员计划失败: Id={Id}", id);
            return StatusCode(500, ApiResponse<bool>.ErrorResponse("删除会员计划失败"));
        }
    }

    private static void ApplyPlanMutation(MembershipPlan plan, AdminMembershipPlanRequest request, bool isNew)
    {
        var compactPayload = request.Price.HasValue || !string.IsNullOrWhiteSpace(request.Duration);
        var normalizedDuration = (request.Duration ?? string.Empty).Trim().ToLowerInvariant();
        var normalizedPrice = request.Price ?? (normalizedDuration == "yearly" ? request.PriceYearly : request.PriceMonthly);

        plan.Level = request.Level > 0 ? request.Level : (isNew ? 1 : plan.Level);
        plan.Name = string.IsNullOrWhiteSpace(request.Name) ? plan.Name : request.Name.Trim();
        plan.Description = request.Description ?? plan.Description;
        plan.Currency = request.Currency ?? plan.Currency;
        plan.Icon = request.Icon ?? plan.Icon;
        plan.Color = request.Color ?? plan.Color;
        plan.Features = request.Features ?? plan.Features;
        plan.AiUsageLimit = request.AiUsageLimit > 0 ? request.AiUsageLimit : (isNew ? 30 : plan.AiUsageLimit);
        plan.CanUseAI = compactPayload ? true : request.CanUseAI;
        plan.CanApplyModerator = request.CanApplyModerator;
        plan.ModeratorDeposit = request.ModeratorDeposit > 0 ? request.ModeratorDeposit : plan.ModeratorDeposit;
        plan.SortOrder = request.SortOrder > 0 ? request.SortOrder : plan.SortOrder;

        if (compactPayload && normalizedPrice > 0)
        {
            if (normalizedDuration == "yearly")
            {
                plan.PriceYearly = normalizedPrice;
            }
            else
            {
                plan.PriceMonthly = normalizedPrice;
            }
        }

        if (request.PriceMonthly > 0)
            plan.PriceMonthly = request.PriceMonthly;

        if (request.PriceYearly > 0)
            plan.PriceYearly = request.PriceYearly;
    }

    private static AdminMembershipPlanDto MapToDto(MembershipPlan plan, int subscriberCount)
    {
        var hasMonthlyPrice = plan.PriceMonthly > 0;

        return new AdminMembershipPlanDto
        {
            Id = plan.Id,
            Level = plan.Level,
            Name = plan.Name,
            Description = plan.Description,
            Price = hasMonthlyPrice ? plan.PriceMonthly : plan.PriceYearly,
            Duration = hasMonthlyPrice ? "monthly" : "yearly",
            PriceMonthly = plan.PriceMonthly,
            PriceYearly = plan.PriceYearly,
            Currency = plan.Currency,
            Icon = plan.Icon,
            Color = plan.Color,
            Features = plan.Features,
            SubscriberCount = subscriberCount,
            Status = plan.IsActive ? "active" : "inactive",
            CreatedAt = plan.CreatedAt,
            UpdatedAt = plan.UpdatedAt,
            AiUsageLimit = plan.AiUsageLimit,
            CanUseAI = plan.CanUseAI,
            CanApplyModerator = plan.CanApplyModerator,
            ModeratorDeposit = plan.ModeratorDeposit,
            SortOrder = plan.SortOrder
        };
    }

    private static string ResolveUserDisplayName(Application.DTOs.UserDto? user)
    {
        if (!string.IsNullOrWhiteSpace(user?.Name))
            return user.Name.Trim();

        var email = user?.Email?.Trim();
        if (!string.IsNullOrWhiteSpace(email))
        {
            var atIndex = email.IndexOf('@');
            return atIndex > 0 ? email[..atIndex] : email;
        }

        return "未命名用户";
    }
}

public class AdminMembershipPlanRequest
{
    public int Level { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public string? Duration { get; set; }
    public decimal PriceYearly { get; set; }
    public decimal PriceMonthly { get; set; }
    public string? Currency { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public List<string>? Features { get; set; }
    public int AiUsageLimit { get; set; }
    public bool CanUseAI { get; set; }
    public bool CanApplyModerator { get; set; }
    public decimal ModeratorDeposit { get; set; }
    public int SortOrder { get; set; }
}

public class AdminMembershipPlanDto
{
    public string Id { get; set; } = string.Empty;
    public int Level { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Duration { get; set; } = "monthly";
    public decimal PriceMonthly { get; set; }
    public decimal PriceYearly { get; set; }
    public string Currency { get; set; } = "CNY";
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public List<string> Features { get; set; } = new();
    public int SubscriberCount { get; set; }
    public string Status { get; set; } = "active";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int AiUsageLimit { get; set; }
    public bool CanUseAI { get; set; }
    public bool CanApplyModerator { get; set; }
    public decimal ModeratorDeposit { get; set; }
    public int SortOrder { get; set; }
}

public class AdminMembershipSubscriberDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; }
    public bool AutoRenew { get; set; }
    public int RemainingDays { get; set; }
}
