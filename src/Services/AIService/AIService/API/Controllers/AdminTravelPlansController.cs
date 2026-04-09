using AIService.Domain.Entities;
using AIService.Domain.Repositories;
using AIService.Infrastructure.GrpcClients;
using GoNomads.Shared.DTOs;
using GoNomads.Shared.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Postgrest;
using Client = Supabase.Client;

namespace AIService.API.Controllers;

[ApiController]
[Route("api/v1/admin/travel-plans")]
public class AdminTravelPlansController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly ITravelPlanRepository _travelPlanRepository;
    private readonly Client _supabase;
    private readonly IUserGrpcClient _userGrpcClient;
    private readonly ILogger<AdminTravelPlansController> _logger;

    public AdminTravelPlansController(
        ICurrentUserService currentUser,
        ITravelPlanRepository travelPlanRepository,
        Client supabase,
        IUserGrpcClient userGrpcClient,
        ILogger<AdminTravelPlansController> logger)
    {
        _currentUser = currentUser;
        _travelPlanRepository = travelPlanRepository;
        _supabase = supabase;
        _userGrpcClient = userGrpcClient;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<AdminTravelPlanListDto>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null)
    {
        try
        {
            if (!_currentUser.IsAdmin()) return Forbid();

            var response = await _supabase.From<AiTravelPlan>()
                .Order("created_at", Constants.Ordering.Descending)
                .Get();

            var userMap = await _userGrpcClient.GetUsersInfoByIdsAsync(
                response.Models.Select(model => model.UserId).Distinct().ToList());

            var filteredItems = response.Models
                .Select(model => MapToListDto(model, ResolveUserDisplayName(model.UserId, userMap.GetValueOrDefault(model.UserId))))
                .Where(item => MatchesSearch(item, search))
                .Where(item => MatchesStatus(item, status))
                .ToList();

            var totalCount = filteredItems.Count;
            var items = filteredItems
                .Skip(Math.Max(0, (page - 1) * pageSize))
                .Take(pageSize)
                .ToList();

            return Ok(new ApiResponse<PaginatedResponse<AdminTravelPlanListDto>>
            {
                Success = true,
                Message = "获取旅行计划列表成功",
                Data = new PaginatedResponse<AdminTravelPlanListDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取旅行计划列表失败");
            return StatusCode(500, ApiResponse<PaginatedResponse<AdminTravelPlanListDto>>.ErrorResponse("获取旅行计划列表失败"));
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AdminTravelPlanDetailDto>>> GetById(Guid id)
    {
        try
        {
            if (!_currentUser.IsAdmin()) return Forbid();

            var plan = await _travelPlanRepository.GetByIdAsync(id);
            if (plan == null)
                return NotFound(ApiResponse<AdminTravelPlanDetailDto>.ErrorResponse("旅行计划不存在"));

            var userInfo = await _userGrpcClient.GetUserInfoAsync(plan.UserId);

            return Ok(ApiResponse<AdminTravelPlanDetailDto>.SuccessResponse(
                MapToDetailDto(plan, ResolveUserDisplayName(plan.UserId, userInfo)),
                "获取成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取旅行计划失败: Id={Id}", id);
            return StatusCode(500, ApiResponse<AdminTravelPlanDetailDto>.ErrorResponse("获取旅行计划失败"));
        }
    }

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<AdminTravelPlanDetailDto>>> UpdateStatus(
        Guid id,
        [FromBody] UpdateAdminTravelPlanStatusRequest request)
    {
        try
        {
            if (!_currentUser.IsAdmin()) return Forbid();

            if (!TryMapStoredStatus(request.Status, out var storedStatus))
                return BadRequest(ApiResponse<AdminTravelPlanDetailDto>.ErrorResponse("不支持的旅行计划状态"));

            var plan = await _travelPlanRepository.GetByIdAsync(id);
            if (plan == null)
                return NotFound(ApiResponse<AdminTravelPlanDetailDto>.ErrorResponse("旅行计划不存在"));

            plan.Status = storedStatus;
            plan.IsPublic = storedStatus == "published";
            plan.UpdatedAt = DateTime.UtcNow;

            var updated = await _travelPlanRepository.UpdateAsync(plan);
            if (updated == null)
                return NotFound(ApiResponse<AdminTravelPlanDetailDto>.ErrorResponse("旅行计划不存在"));

            var userInfo = await _userGrpcClient.GetUserInfoAsync(updated.UserId);

            _logger.LogInformation("管理员更新旅行计划状态: Id={Id}, Status={Status}", id, storedStatus);
            return Ok(ApiResponse<AdminTravelPlanDetailDto>.SuccessResponse(
                MapToDetailDto(updated, ResolveUserDisplayName(updated.UserId, userInfo)),
                "旅行计划状态更新成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新旅行计划状态失败: Id={Id}", id);
            return StatusCode(500, ApiResponse<AdminTravelPlanDetailDto>.ErrorResponse("更新旅行计划状态失败"));
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        try
        {
            if (!_currentUser.IsAdmin()) return Forbid();

            await _travelPlanRepository.DeleteAsync(id);

            _logger.LogInformation("管理员删除旅行计划: Id={Id}", id);
            return Ok(ApiResponse<bool>.SuccessResponse(true, "删除成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除旅行计划失败: Id={Id}", id);
            return StatusCode(500, ApiResponse<bool>.ErrorResponse("删除旅行计划失败"));
        }
    }

    private static AdminTravelPlanListDto MapToListDto(AiTravelPlan plan, string userName)
    {
        var status = MapPlanStatus(plan.Status);
        return new AdminTravelPlanListDto
        {
            Id = plan.Id,
            UserId = plan.UserId.ToString(),
            UserName = userName,
            Destination = plan.CityName,
            CityName = plan.CityName,
            Days = plan.Duration,
            BudgetLevel = plan.BudgetLevel,
            TravelStyle = plan.TravelStyle,
            CompletionRate = CalculateCompletionRate(plan),
            Status = status,
            CreatedAt = plan.CreatedAt,
            UpdatedAt = plan.UpdatedAt
        };
    }

    private static AdminTravelPlanDetailDto MapToDetailDto(AiTravelPlan plan, string userName)
    {
        var data = plan.PlanDataJson as JObject;
        var detail = new AdminTravelPlanDetailDto
        {
            Id = plan.Id,
            UserId = plan.UserId.ToString(),
            UserName = userName,
            Destination = plan.CityName,
            CityName = plan.CityName,
            Days = plan.Duration,
            BudgetLevel = plan.BudgetLevel,
            TravelStyle = plan.TravelStyle,
            CompletionRate = CalculateCompletionRate(plan),
            Status = MapPlanStatus(plan.Status),
            CreatedAt = plan.CreatedAt,
            UpdatedAt = plan.UpdatedAt,
            Interests = plan.Interests?.ToList() ?? ReadStringArray(data, "interests"),
            DepartureCity = plan.DepartureLocation,
            DepartureDate = plan.DepartureDate,
            DailyItinerary = ReadToken(data, "dailyItinerary", "daily_itinerary"),
            Attractions = ReadToken(data, "attractions"),
            Restaurants = ReadToken(data, "restaurants"),
            Budget = ReadToken(data, "budget"),
            Tips = ReadStringArray(data, "tips")
        };

        return detail;
    }

    private static string ResolveUserDisplayName(Guid userId, UserInfo? userInfo)
    {
        if (!string.IsNullOrWhiteSpace(userInfo?.Name))
            return userInfo.Name.Trim();

        var email = userInfo?.Email?.Trim();
        if (!string.IsNullOrWhiteSpace(email))
            return ExtractEmailDisplayName(email);

        return BuildUserFallbackLabel(userId);
    }

    private static string BuildUserFallbackLabel(Guid userId)
    {
        return $"孤儿用户{userId.ToString("N")[..8]}";
    }

    private static string ExtractEmailDisplayName(string email)
    {
        var atIndex = email.IndexOf('@');
        return atIndex > 0 ? email[..atIndex] : email;
    }

    private static int CalculateCompletionRate(AiTravelPlan plan)
    {
        return MapPlanStatus(plan.Status) switch
        {
            "completed" => 100,
            "confirmed" => 80,
            _ => 40
        };
    }

    private static string MapPlanStatus(string status)
    {
        return status.ToLowerInvariant() switch
        {
            "confirmed" => "confirmed",
            "completed" => "completed",
            "planning" => "planning",
            "published" => "confirmed",
            "archived" => "completed",
            _ => "planning"
        };
    }

    private static bool TryMapStoredStatus(string? status, out string storedStatus)
    {
        storedStatus = (status ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "planning" or "draft" => "draft",
            "confirmed" or "published" => "published",
            "completed" or "archived" => "archived",
            _ => string.Empty
        };

        return !string.IsNullOrWhiteSpace(storedStatus);
    }

    private static bool MatchesSearch(AdminTravelPlanListDto item, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return true;

        var keyword = search.Trim();
        return Contains(item.UserName, keyword)
               || Contains(item.CityName, keyword)
               || Contains(item.Destination, keyword)
               || Contains(item.BudgetLevel, keyword)
               || Contains(item.TravelStyle, keyword);
    }

    private static bool MatchesStatus(AdminTravelPlanListDto item, string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return true;

        return string.Equals(MapPlanStatus(status), item.Status, StringComparison.OrdinalIgnoreCase)
               || string.Equals(status, item.Status, StringComparison.OrdinalIgnoreCase);
    }

    private static bool Contains(string? source, string value)
    {
        return !string.IsNullOrWhiteSpace(source)
               && source.Contains(value, StringComparison.OrdinalIgnoreCase);
    }

    private static object? ReadToken(JObject? data, params string[] keys)
    {
        if (data == null)
            return null;

        foreach (var key in keys)
        {
            if (data.TryGetValue(key, out var token))
                return token?.ToObject<object>();
        }

        return null;
    }

    private static List<string> ReadStringArray(JObject? data, params string[] keys)
    {
        if (data == null)
            return new List<string>();

        foreach (var key in keys)
        {
            if (!data.TryGetValue(key, out var token) || token is not JArray array)
                continue;

            return array.Values<string>()
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToList()!;
        }

        return new List<string>();
    }
}

public class AdminTravelPlanListDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string CityName { get; set; } = string.Empty;
    public int Days { get; set; }
    public string BudgetLevel { get; set; } = string.Empty;
    public string TravelStyle { get; set; } = string.Empty;
    public int CompletionRate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class AdminTravelPlanDetailDto : AdminTravelPlanListDto
{
    public List<string> Interests { get; set; } = new();
    public string? DepartureCity { get; set; }
    public DateTime? DepartureDate { get; set; }
    public object? DailyItinerary { get; set; }
    public object? Attractions { get; set; }
    public object? Restaurants { get; set; }
    public object? Budget { get; set; }
    public List<string> Tips { get; set; } = new();
}

public class UpdateAdminTravelPlanStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
