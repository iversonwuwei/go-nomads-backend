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
[Route("api/v1/admin/ai/conversations")]
public class AdminAIConversationsController : ControllerBase
{
    private readonly IAIConversationRepository _conversationRepository;
    private readonly IAIMessageRepository _messageRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly Client _supabase;
    private readonly IUserGrpcClient _userGrpcClient;
    private readonly ILogger<AdminAIConversationsController> _logger;

    public AdminAIConversationsController(
        IAIConversationRepository conversationRepository,
        IAIMessageRepository messageRepository,
        ICurrentUserService currentUser,
        Client supabase,
        IUserGrpcClient userGrpcClient,
        ILogger<AdminAIConversationsController> logger)
    {
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _currentUser = currentUser;
        _supabase = supabase;
        _userGrpcClient = userGrpcClient;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<AdminAiConversationDto>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        try
        {
            if (!_currentUser.IsAdmin()) return Forbid();

            var response = await _supabase.From<AIConversation>()
                .Filter("deleted_at", Constants.Operator.Is, "null")
                .Order("created_at", Constants.Ordering.Descending)
                .Get();

            var userMap = await _userGrpcClient.GetUsersInfoByIdsAsync(
                response.Models.Select(model => model.UserId).Distinct().ToList());

            var filteredItems = response.Models
                .Select(model => MapToDto(model, ResolveUserDisplayName(model.UserId, userMap.GetValueOrDefault(model.UserId))))
                .Where(item => MatchesSearch(item, search))
                .ToList();

            var totalCount = filteredItems.Count;
            var items = filteredItems
                .Skip(Math.Max(0, (page - 1) * pageSize))
                .Take(pageSize)
                .ToList();

            return Ok(new ApiResponse<PaginatedResponse<AdminAiConversationDto>>
            {
                Success = true,
                Message = "获取 AI 会话列表成功",
                Data = new PaginatedResponse<AdminAiConversationDto>
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
            _logger.LogError(ex, "获取 AI 会话列表失败");
            return StatusCode(500, ApiResponse<PaginatedResponse<AdminAiConversationDto>>.ErrorResponse("获取 AI 会话列表失败"));
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AdminAiConversationDetailDto>>> GetById(Guid id)
    {
        try
        {
            if (!_currentUser.IsAdmin()) return Forbid();

            var conversation = await _conversationRepository.GetByIdAsync(id);
            if (conversation == null)
                return NotFound(ApiResponse<AdminAiConversationDetailDto>.ErrorResponse("会话不存在"));

            var userInfo = await _userGrpcClient.GetUserInfoAsync(conversation.UserId);
            var messages = await _messageRepository.GetByConversationIdAsync(id, 1, 100, true);

            var detail = new AdminAiConversationDetailDto
            {
                Id = conversation.Id,
                UserId = conversation.UserId.ToString(),
                UserName = ResolveUserDisplayName(conversation.UserId, userInfo),
                Title = conversation.Title,
                Status = conversation.Status,
                ModelName = conversation.ModelName,
                TotalTokens = conversation.TotalTokens,
                LastMessageAt = conversation.LastMessageAt,
                CreatedAt = conversation.CreatedAt,
                UpdatedAt = conversation.UpdatedAt,
                Messages = messages.Select(message => new AdminAiConversationMessageDto
                {
                    Id = message.Id,
                    Role = message.Role,
                    Content = message.Content,
                    ModelName = message.ModelName,
                    TokenCount = message.TokenCount,
                    TotalTokens = message.TotalTokens,
                    IsError = message.IsError,
                    ErrorMessage = message.ErrorMessage,
                    CreatedAt = message.CreatedAt
                }).ToList()
            };

            return Ok(ApiResponse<AdminAiConversationDetailDto>.SuccessResponse(detail, "获取 AI 会话详情成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取 AI 会话详情失败: Id={Id}", id);
            return StatusCode(500, ApiResponse<AdminAiConversationDetailDto>.ErrorResponse("获取 AI 会话详情失败"));
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        try
        {
            if (!_currentUser.IsAdmin()) return Forbid();

            var existing = await _supabase.From<AIConversation>()
                .Where(c => c.Id == id)
                .Single();

            if (existing == null)
                return NotFound(ApiResponse<bool>.ErrorResponse("会话不存在"));

            existing.DeletedAt = DateTime.UtcNow;
            existing.UpdatedAt = DateTime.UtcNow;

            await _supabase.From<AIConversation>()
                .Where(c => c.Id == id)
                .Update(existing);

            _logger.LogInformation("管理员删除 AI 会话: Id={Id}", id);
            return Ok(ApiResponse<bool>.SuccessResponse(true, "删除成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除 AI 会话失败: Id={Id}", id);
            return StatusCode(500, ApiResponse<bool>.ErrorResponse("删除失败"));
        }
    }

    private static AdminAiConversationDto MapToDto(AIConversation conversation, string userName)
    {
        return new AdminAiConversationDto
        {
            Id = conversation.Id,
            UserId = conversation.UserId.ToString(),
            UserName = userName,
            Title = conversation.Title,
            Status = conversation.Status,
            ModelName = conversation.ModelName,
            TotalTokens = conversation.TotalTokens,
            LastMessageAt = conversation.LastMessageAt,
            CreatedAt = conversation.CreatedAt
        };
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

    private static bool MatchesSearch(AdminAiConversationDto item, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return true;

        var keyword = search.Trim();
        return Contains(item.UserName, keyword)
               || Contains(item.Title, keyword)
               || Contains(item.Status, keyword)
               || Contains(item.ModelName, keyword);
    }

    private static bool Contains(string? source, string keyword)
    {
        return !string.IsNullOrWhiteSpace(source)
               && source.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }
}

public class AdminAiConversationDto
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public int TotalTokens { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminAiConversationDetailDto : AdminAiConversationDto
{
    public DateTime? UpdatedAt { get; set; }
    public List<AdminAiConversationMessageDto> Messages { get; set; } = new();
}

public class AdminAiConversationMessageDto
{
    public Guid Id { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? ModelName { get; set; }
    public int TokenCount { get; set; }
    public int? TotalTokens { get; set; }
    public bool IsError { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}
