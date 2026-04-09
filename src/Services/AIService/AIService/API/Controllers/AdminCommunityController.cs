using System.Text.Json;
using AIService.Domain.Entities;
using AIService.Domain.Repositories;
using AIService.Infrastructure.GrpcClients;
using GoNomads.Shared.DTOs;
using GoNomads.Shared.Services;
using Microsoft.AspNetCore.Mvc;
using Postgrest;
using Client = Supabase.Client;

namespace AIService.API.Controllers;

[ApiController]
[Route("api/v1/admin/community")]
public class AdminCommunityController : ControllerBase
{
    private readonly ICommunityAnswerRepository _answerRepository;
    private readonly ICityGrpcClient _cityGrpcClient;
    private readonly ICommunityQuestionRepository _questionRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly Client _supabase;
    private readonly IUserGrpcClient _userGrpcClient;
    private readonly ILogger<AdminCommunityController> _logger;

    public AdminCommunityController(
        ICommunityAnswerRepository answerRepository,
        ICityGrpcClient cityGrpcClient,
        ICommunityQuestionRepository questionRepository,
        ICurrentUserService currentUser,
        Client supabase,
        IUserGrpcClient userGrpcClient,
        ILogger<AdminCommunityController> logger)
    {
        _answerRepository = answerRepository;
        _cityGrpcClient = cityGrpcClient;
        _questionRepository = questionRepository;
        _currentUser = currentUser;
        _supabase = supabase;
        _userGrpcClient = userGrpcClient;
        _logger = logger;
    }

    [HttpGet("posts")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<AdminCommunityPostDto>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? type = null)
    {
        try
        {
            if (!_currentUser.IsAdmin()) return Forbid();

            var response = await _supabase.From<CommunityQuestion>()
                .Order("created_at", Constants.Ordering.Descending)
                .Get();

            var userMap = await _userGrpcClient.GetUsersInfoByIdsAsync(
                response.Models.Select(model => model.UserId).Distinct().ToList());
            var cityMap = await ResolveCityDisplayMapAsync(response.Models.Select(model => model.City));

            var filteredItems = response.Models
                .Select(model => MapToDto(
                    model,
                    ResolveUserDisplayName(model.UserId, userMap.GetValueOrDefault(model.UserId)),
                    cityMap.GetValueOrDefault(model.City) ?? ResolveCityFallback(model.City)))
                .Where(item => MatchesSearch(item, search))
                .Where(item => MatchesType(item, type))
                .ToList();

            var totalCount = filteredItems.Count;
            var items = filteredItems
                .Skip(Math.Max(0, (page - 1) * pageSize))
                .Take(pageSize)
                .ToList();

            return Ok(new ApiResponse<PaginatedResponse<AdminCommunityPostDto>>
            {
                Success = true,
                Message = "获取社区帖子列表成功",
                Data = new PaginatedResponse<AdminCommunityPostDto>
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
            _logger.LogError(ex, "获取社区帖子列表失败");
            return StatusCode(500, ApiResponse<PaginatedResponse<AdminCommunityPostDto>>.ErrorResponse("获取社区帖子列表失败"));
        }
    }

    [HttpGet("posts/{id:guid}")]
    public async Task<ActionResult<ApiResponse<AdminCommunityPostDetailDto>>> GetById(Guid id)
    {
        try
        {
            if (!_currentUser.IsAdmin()) return Forbid();

            var question = await _questionRepository.GetByIdAsync(id);
            if (question == null)
                return NotFound(ApiResponse<AdminCommunityPostDetailDto>.ErrorResponse("帖子不存在"));

            var userInfo = await _userGrpcClient.GetUserInfoAsync(question.UserId);
            var cityDisplayName = await ResolveCityDisplayAsync(question.City);
            var acceptedAnswerSummary = await ResolveAcceptedAnswerSummaryAsync(question.AcceptedAnswerId);
            var payload = MapToDetailDto(
                question,
                ResolveUserDisplayName(question.UserId, userInfo),
                cityDisplayName,
                acceptedAnswerSummary);

            return Ok(ApiResponse<AdminCommunityPostDetailDto>.SuccessResponse(payload, "获取社区帖子详情成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取社区帖子详情失败: Id={Id}", id);
            return StatusCode(500, ApiResponse<AdminCommunityPostDetailDto>.ErrorResponse("获取社区帖子详情失败"));
        }
    }

    [HttpPut("posts/{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<AdminCommunityPostDetailDto>>> UpdateStatus(
        Guid id,
        [FromBody] AdminCommunityPostStatusRequest request)
    {
        try
        {
            if (!_currentUser.IsAdmin()) return Forbid();

            var question = await _questionRepository.GetByIdAsync(id);
            if (question == null)
                return NotFound(ApiResponse<AdminCommunityPostDetailDto>.ErrorResponse("帖子不存在"));

            var normalizedStatus = (request.Status ?? string.Empty).Trim().ToLowerInvariant();
            if (normalizedStatus != "active" && normalizedStatus != "hidden")
                return BadRequest(ApiResponse<AdminCommunityPostDetailDto>.ErrorResponse("仅支持 active 或 hidden"));

            question.DeletedAt = normalizedStatus == "hidden" ? DateTime.UtcNow : null;
            question.UpdatedAt = DateTime.UtcNow;

            var updated = await _questionRepository.UpdateAsync(question);
            if (updated == null)
                return StatusCode(500, ApiResponse<AdminCommunityPostDetailDto>.ErrorResponse("更新社区帖子状态失败"));

            var userInfo = await _userGrpcClient.GetUserInfoAsync(updated.UserId);
            var cityDisplayName = await ResolveCityDisplayAsync(updated.City);
            var acceptedAnswerSummary = await ResolveAcceptedAnswerSummaryAsync(updated.AcceptedAnswerId);
            var payload = MapToDetailDto(
                updated,
                ResolveUserDisplayName(updated.UserId, userInfo),
                cityDisplayName,
                acceptedAnswerSummary);

            return Ok(ApiResponse<AdminCommunityPostDetailDto>.SuccessResponse(payload, "更新社区帖子状态成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新社区帖子状态失败: Id={Id}", id);
            return StatusCode(500, ApiResponse<AdminCommunityPostDetailDto>.ErrorResponse("更新社区帖子状态失败"));
        }
    }

    [HttpDelete("posts/{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        try
        {
            if (!_currentUser.IsAdmin()) return Forbid();

            var existing = await _supabase.From<CommunityQuestion>()
                .Where(q => q.Id == id)
                .Single();

            if (existing == null)
                return NotFound(ApiResponse<bool>.ErrorResponse("帖子不存在"));

            existing.DeletedAt = DateTime.UtcNow;
            existing.UpdatedAt = DateTime.UtcNow;

            await _supabase.From<CommunityQuestion>()
                .Where(q => q.Id == id)
                .Update(existing);

            _logger.LogInformation("管理员删除社区帖子: Id={Id}", id);
            return Ok(ApiResponse<bool>.SuccessResponse(true, "删除成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除社区帖子失败: Id={Id}", id);
            return StatusCode(500, ApiResponse<bool>.ErrorResponse("删除失败"));
        }
    }

    private static AdminCommunityPostDto MapToDto(CommunityQuestion question, string authorName, string cityDisplayName)
    {
        var cityId = TryNormalizeGuid(question.City);

        return new AdminCommunityPostDto
        {
            Id = question.Id,
            Type = "post",
            AuthorId = question.UserId.ToString(),
            AuthorName = authorName,
            Content = string.IsNullOrWhiteSpace(question.Content) ? question.Title : question.Content,
            LikeCount = question.Upvotes,
            CommentCount = question.AnswerCount,
            CityId = cityId ?? string.Empty,
            CityName = cityDisplayName,
            Status = question.DeletedAt.HasValue ? "hidden" : "active",
            CreatedAt = question.CreatedAt
        };
    }

    private static AdminCommunityPostDetailDto MapToDetailDto(
        CommunityQuestion question,
        string authorName,
        string cityDisplayName,
        string? acceptedAnswerSummary)
    {
        var cityId = TryNormalizeGuid(question.City);

        return new AdminCommunityPostDetailDto
        {
            Id = question.Id,
            Type = "post",
            AuthorId = question.UserId.ToString(),
            AuthorName = authorName,
            Title = question.Title,
            Content = question.Content,
            LikeCount = question.Upvotes,
            CommentCount = question.AnswerCount,
            CityId = cityId ?? string.Empty,
            CityName = cityDisplayName,
            Status = question.DeletedAt.HasValue ? "hidden" : "active",
            CreatedAt = question.CreatedAt,
            UpdatedAt = question.UpdatedAt,
            Tags = ParseTags(question.TagsJson),
            AcceptedAnswerId = question.AcceptedAnswerId?.ToString(),
            AcceptedAnswerSummary = acceptedAnswerSummary
        };
    }

    private async Task<Dictionary<string, string>> ResolveCityDisplayMapAsync(IEnumerable<string?> rawCities)
    {
        var normalized = rawCities
            .Where(city => !string.IsNullOrWhiteSpace(city))
            .Select(city => city!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var tasks = normalized.Select(async city => new KeyValuePair<string, string>(city, await ResolveCityDisplayAsync(city)));
        var resolved = await Task.WhenAll(tasks);

        return resolved.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<string> ResolveCityDisplayAsync(string? rawCity)
    {
        var normalized = rawCity?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return string.Empty;

        if (!Guid.TryParse(normalized, out var cityId))
            return normalized;

        var cityInfo = await _cityGrpcClient.GetCityInfoAsync(cityId);
        if (!string.IsNullOrWhiteSpace(cityInfo?.Name))
            return cityInfo.Name.Trim();

        if (!string.IsNullOrWhiteSpace(cityInfo?.NameEn))
            return cityInfo.NameEn.Trim();

        return normalized;
    }

    private async Task<string?> ResolveAcceptedAnswerSummaryAsync(Guid? acceptedAnswerId)
    {
        if (!acceptedAnswerId.HasValue)
            return null;

        var answer = await _answerRepository.GetByIdAsync(acceptedAnswerId.Value);
        if (answer == null)
            return "已删除回答";

        var userInfo = await _userGrpcClient.GetUserInfoAsync(answer.UserId);
        var authorName = ResolveUserDisplayName(answer.UserId, userInfo);
        return $"{authorName}: {BuildTextPreview(answer.Content, 72)}";
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

    private static List<string> ParseTags(string? tagsJson)
    {
        if (string.IsNullOrWhiteSpace(tagsJson))
            return new List<string>();

        try
        {
            return JsonSerializer.Deserialize<List<string>>(tagsJson) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static bool MatchesSearch(AdminCommunityPostDto item, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return true;

        var keyword = search.Trim();
        return Contains(item.AuthorName, keyword)
               || Contains(item.Content, keyword)
               || Contains(item.CityName, keyword)
               || Contains(item.Status, keyword);
    }

    private static bool MatchesType(AdminCommunityPostDto item, string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
            return true;

        return string.Equals(item.Type, type.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool Contains(string? source, string keyword)
    {
        return !string.IsNullOrWhiteSpace(source)
               && source.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveCityFallback(string? rawCity)
    {
        return string.IsNullOrWhiteSpace(rawCity) ? string.Empty : rawCity.Trim();
    }

    private static string? TryNormalizeGuid(string? value)
    {
        var normalized = value?.Trim();
        return Guid.TryParse(normalized, out _) ? normalized : null;
    }

    private static string BuildTextPreview(string? value, int maxLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return "内容不可用";

        return normalized.Length <= maxLength ? normalized : $"{normalized[..maxLength]}...";
    }
}

public class AdminCommunityPostDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = "post";
    public string AuthorId { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public string CityId { get; set; } = string.Empty;
    public string CityName { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
    public DateTime CreatedAt { get; set; }
}

public class AdminCommunityPostDetailDto : AdminCommunityPostDto
{
    public string Title { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
    public List<string> Tags { get; set; } = new();
    public string? AcceptedAnswerId { get; set; }
    public string? AcceptedAnswerSummary { get; set; }
}

public class AdminCommunityPostStatusRequest
{
    public string Status { get; set; } = string.Empty;
}
