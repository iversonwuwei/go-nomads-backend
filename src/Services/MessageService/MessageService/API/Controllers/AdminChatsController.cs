using GoNomads.Shared.DTOs;
using GoNomads.Shared.Services;
using MessageService.Application.Services;
using MessageService.Domain.Entities;
using MessageService.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace MessageService.API.Controllers;

[ApiController]
[Route("api/v1/admin/chats")]
public class AdminChatsController : ControllerBase
{
    private readonly ICurrentUserService _currentUser;
    private readonly IChatService _chatService;
    private readonly IChatRoomRepository _chatRoomRepository;
    private readonly IUserServiceClient _userServiceClient;
    private readonly ILogger<AdminChatsController> _logger;

    public AdminChatsController(
        ICurrentUserService currentUser,
        IChatService chatService,
        IChatRoomRepository chatRoomRepository,
        IUserServiceClient userServiceClient,
        ILogger<AdminChatsController> logger)
    {
        _currentUser = currentUser;
        _chatService = chatService;
        _chatRoomRepository = chatRoomRepository;
        _userServiceClient = userServiceClient;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<AdminChatRoomDto>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null)
    {
        try
        {
            if (!_currentUser.IsAdmin()) return Forbid();

            List<ChatRoom> rooms;
            int totalCount;

            if (string.IsNullOrWhiteSpace(search))
            {
                (rooms, totalCount) = await _chatRoomRepository.GetAllRoomsPagedAsync(page, pageSize);
            }
            else
            {
                var (_, allCount) = await _chatRoomRepository.GetAllRoomsPagedAsync(1, 1);
                (var allRooms, _) = await _chatRoomRepository.GetAllRoomsPagedAsync(1, Math.Max(allCount, 1));
                rooms = allRooms;
                totalCount = allCount;
            }

            var creatorInfos = await _userServiceClient.GetUsersInfoAsync(
                rooms.Select(room => room.CreatedBy).Where(createdBy => !string.IsNullOrWhiteSpace(createdBy)));

            var filteredItems = rooms
                .Select(room => MapToListDto(room, creatorInfos.GetValueOrDefault(room.CreatedBy)))
                .Where(item => MatchesSearch(item, search))
                .ToList();

            if (!string.IsNullOrWhiteSpace(search))
            {
                totalCount = filteredItems.Count;
                filteredItems = filteredItems
                    .Skip(Math.Max(0, (page - 1) * pageSize))
                    .Take(pageSize)
                    .ToList();
            }

            return Ok(new ApiResponse<PaginatedResponse<AdminChatRoomDto>>
            {
                Success = true,
                Message = "获取聊天室列表成功",
                Data = new PaginatedResponse<AdminChatRoomDto>
                {
                    Items = filteredItems,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取聊天室列表失败");
            return StatusCode(500, ApiResponse<PaginatedResponse<AdminChatRoomDto>>.ErrorResponse("获取聊天室列表失败"));
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<AdminChatRoomDetailDto>>> GetById(string id)
    {
        try
        {
            if (!_currentUser.IsAdmin()) return Forbid();

            var room = await _chatRoomRepository.GetByIdAsync(id);
            if (room == null)
                return NotFound(ApiResponse<AdminChatRoomDetailDto>.ErrorResponse("聊天室不存在"));

            var roomOverview = await _chatService.GetRoomByIdAsync(id);
            var members = await _chatService.GetMembersAsync(id, 1, 100);
            var messages = await _chatService.GetMessagesAsync(id, 1, 100);
            var creator = string.IsNullOrWhiteSpace(room.CreatedBy)
                ? null
                : await _userServiceClient.GetUserInfoAsync(room.CreatedBy);

            var detail = new AdminChatRoomDetailDto
            {
                Id = room.Id.ToString(),
                Name = room.Name,
                RoomType = room.RoomType,
                City = room.City,
                Country = room.Country,
                Description = room.Description,
                ImageUrl = room.ImageUrl,
                TotalMembers = roomOverview?.TotalMembers ?? room.TotalMembers,
                CreatedBy = room.CreatedBy,
                CreatedByName = ResolveUserDisplayName(creator),
                IsPublic = room.IsPublic,
                LastMessage = roomOverview?.LastMessage?.Message,
                LastActiveAt = roomOverview?.LastMessage?.Timestamp ?? room.UpdatedAt,
                CreatedAt = room.CreatedAt,
                UpdatedAt = room.UpdatedAt,
                Members = members.Select(member => new AdminChatMemberDto
                {
                    UserId = member.UserId,
                    UserName = member.UserName,
                    UserAvatar = member.UserAvatar,
                    Role = member.Role,
                    IsOnline = member.IsOnline,
                    LastSeenAt = member.LastSeenAt
                }).ToList(),
                Messages = messages.Select(message => new AdminChatMessageDto
                {
                    Id = message.Id,
                    UserId = message.Author.UserId,
                    UserName = message.Author.UserName,
                    UserAvatar = message.Author.UserAvatar,
                    Message = message.Message,
                    MessageType = message.MessageType,
                    Timestamp = message.Timestamp
                }).ToList()
            };

            return Ok(ApiResponse<AdminChatRoomDetailDto>.SuccessResponse(detail, "获取聊天室详情成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取聊天室详情失败: Id={Id}", id);
            return StatusCode(500, ApiResponse<AdminChatRoomDetailDto>.ErrorResponse("获取聊天室详情失败"));
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(string id)
    {
        try
        {
            if (!_currentUser.IsAdmin()) return Forbid();

            await _chatRoomRepository.DeleteAsync(id);

            _logger.LogInformation("管理员删除聊天室: Id={Id}", id);
            return Ok(ApiResponse<bool>.SuccessResponse(true, "聊天室已删除"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除聊天室失败: Id={Id}", id);
            return StatusCode(500, ApiResponse<bool>.ErrorResponse("删除聊天室失败"));
        }
    }

    private static AdminChatRoomDto MapToListDto(ChatRoom room, UserInfoDto? creator)
    {
        return new AdminChatRoomDto
        {
            Id = room.Id.ToString(),
            Name = room.Name,
            RoomType = room.RoomType,
            City = room.City,
            Country = room.Country,
            Description = room.Description,
            ImageUrl = room.ImageUrl,
            TotalMembers = room.TotalMembers,
            CreatedBy = room.CreatedBy,
            CreatedByName = ResolveUserDisplayName(creator),
            LastActiveAt = room.UpdatedAt,
            CreatedAt = room.CreatedAt,
            UpdatedAt = room.UpdatedAt
        };
    }

    private static string ResolveUserDisplayName(UserInfoDto? creator)
    {
        if (!string.IsNullOrWhiteSpace(creator?.Name))
            return creator.Name.Trim();

        if (!string.IsNullOrWhiteSpace(creator?.Username))
            return creator.Username.Trim();

        var email = creator?.Email?.Trim();
        if (!string.IsNullOrWhiteSpace(email))
            return ExtractEmailDisplayName(email);

        return "未命名用户";
    }

    private static string ExtractEmailDisplayName(string email)
    {
        var atIndex = email.IndexOf('@');
        return atIndex > 0 ? email[..atIndex] : email;
    }

    private static bool MatchesSearch(AdminChatRoomDto item, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return true;

        var keyword = search.Trim();
        return Contains(item.Name, keyword)
               || Contains(item.RoomType, keyword)
               || Contains(item.City, keyword)
               || Contains(item.Country, keyword)
               || Contains(item.CreatedByName, keyword)
               || Contains(item.Description, keyword);
    }

    private static bool Contains(string? source, string keyword)
    {
        return !string.IsNullOrWhiteSpace(source)
               && source.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }
}

public class AdminChatRoomDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string RoomType { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int TotalMembers { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public string? LastMessage { get; set; }
    public DateTime? LastActiveAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class AdminChatRoomDetailDto : AdminChatRoomDto
{
    public bool IsPublic { get; set; }
    public List<AdminChatMemberDto> Members { get; set; } = new();
    public List<AdminChatMessageDto> Messages { get; set; } = new();
}

public class AdminChatMemberDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatar { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public DateTime? LastSeenAt { get; set; }
}

public class AdminChatMessageDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatar { get; set; }
    public string Message { get; set; } = string.Empty;
    public string MessageType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
