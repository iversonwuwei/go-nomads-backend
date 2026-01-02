using System.Text.Json;
using Dapr.Client;
using EventService.Application.DTOs;
using EventService.Application.Services;
using GoNomads.Shared.Middleware;
using GoNomads.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventService.API.Controllers;

/// <summary>
///     Events API - RESTful endpoints for event management
/// </summary>
[ApiController]
[Route("api/v1/events")]
public class EventsController : ControllerBase
{
    private readonly DaprClient _daprClient;
    private readonly IEventService _eventService;
    private readonly ILogger<EventsController> _logger;

    public EventsController(IEventService eventService, ILogger<EventsController> logger, DaprClient daprClient)
    {
        _eventService = eventService;
        _logger = logger;
        _daprClient = daprClient;
    }

    /// <summary>
    ///     创建 Event
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<EventResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<EventResponse>>> CreateEvent([FromBody] CreateEventRequest request)
    {
        try
        {
            _logger.LogInformation("📥 收到创建 Event 请求: Title={Title}, CityId={CityId}, Location={Location}",
                request.Title, request.CityId, request.Location);

            // 从 UserContext 获取当前用户信息
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
                return Unauthorized(new ApiResponse<EventResponse>
                {
                    Success = false,
                    Message = "用户未认证",
                    Errors = new List<string> { "用户未认证" }
                });

            _logger.LogInformation("🔍 用户上下文: UserId={UserId}, Length={Length}",
                userContext.UserId, userContext.UserId.Length);

            var organizerId = Guid.Parse(userContext.UserId);
            var response = await _eventService.CreateEventAsync(request, organizerId);

            _logger.LogInformation("✅ 用户 {UserId} 成功创建 Event {EventId}", organizerId, response.Id);
            return CreatedAtAction(
                nameof(GetEvent),
                new { id = response.Id },
                new ApiResponse<EventResponse>
                {
                    Success = true,
                    Message = "Event 创建成功",
                    Data = response
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建 Event 失败");
            return StatusCode(500, new ApiResponse<EventResponse>
            {
                Success = false,
                Message = "创建 Event 失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     获取 Event 详情
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<EventResponse>>> GetEvent(Guid id)
    {
        try
        {
            // 从 UserContext 获取当前用户信息（可选，用于判断关注/参与状态）
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            Guid? userId = null;

            if (userContext?.IsAuthenticated == true && !string.IsNullOrEmpty(userContext.UserId))
                userId = Guid.Parse(userContext.UserId);

            var response = await _eventService.GetEventAsync(id, userId);
            return Ok(new ApiResponse<EventResponse>
            {
                Success = true,
                Message = "Event 获取成功",
                Data = response
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<EventResponse>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取 Event 失败");
            return StatusCode(500, new ApiResponse<EventResponse>
            {
                Success = false,
                Message = "获取 Event 失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     获取 Event 列表
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<EventResponse>>>> GetEvents(
        [FromQuery] Guid? cityId = null,
        [FromQuery] string? category = null,
        [FromQuery] string? status = "upcoming",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            // 从 UserContext 获取当前用户信息（用于判断参与状态）
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            Guid? userId = null;

            _logger.LogInformation("🔍 GetEvents - UserContext 详情:");
            _logger.LogInformation("   userContext == null: {IsNull}", userContext == null);
            if (userContext != null)
            {
                _logger.LogInformation("   IsAuthenticated: {IsAuth}", userContext.IsAuthenticated);
                _logger.LogInformation("   UserId: {UserId}", userContext.UserId);
                _logger.LogInformation("   Email: {Email}", userContext.Email);
                _logger.LogInformation("   Role: {Role}", userContext.Role);
            }

            if (userContext?.IsAuthenticated == true && !string.IsNullOrEmpty(userContext.UserId))
            {
                userId = Guid.Parse(userContext.UserId);
                _logger.LogInformation("✅ GetEvents: 已认证用户 ID = {UserId}", userId);
            }
            else
            {
                _logger.LogWarning("⚠️ GetEvents: 未认证用户或 UserId 为空。IsAuthenticated={IsAuth}, UserId={UserId}",
                    userContext?.IsAuthenticated, userContext?.UserId);
            }

            var (events, total) = await _eventService.GetEventsAsync(cityId, category, status, page, pageSize, userId);
            return Ok(new ApiResponse<PaginatedResponse<EventResponse>>
            {
                Success = true,
                Message = "Event 列表获取成功",
                Data = new PaginatedResponse<EventResponse>
                {
                    Items = events.ToList(),
                    TotalCount = total,
                    Page = page,
                    PageSize = pageSize
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取 Event 列表失败");
            return StatusCode(500, new ApiResponse<PaginatedResponse<EventResponse>>
            {
                Success = false,
                Message = "获取 Event 列表失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     更新 Event
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<EventResponse>>> UpdateEvent(Guid id,
        [FromBody] UpdateEventRequest request)
    {
        try
        {
            // 从 UserContext 获取当前用户信息
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
                return Unauthorized(new ApiResponse<EventResponse>
                {
                    Success = false,
                    Message = "用户未认证",
                    Errors = new List<string> { "用户未认证" }
                });

            var userId = Guid.Parse(userContext.UserId);
            var response = await _eventService.UpdateEventAsync(id, request, userId);

            _logger.LogInformation("✅ 用户 {UserId} 成功更新 Event {EventId}", userId, id);
            return Ok(new ApiResponse<EventResponse>
            {
                Success = true,
                Message = "Event 更新成功",
                Data = response
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<EventResponse>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new ApiResponse<EventResponse>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新 Event 失败");
            return StatusCode(500, new ApiResponse<EventResponse>
            {
                Success = false,
                Message = "更新 Event 失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     取消活动
    /// </summary>
    [HttpPost("{id}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<EventResponse>>> CancelEvent(Guid id)
    {
        try
        {
            // 从 UserContext 获取当前用户信息
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
                return Unauthorized(new ApiResponse<EventResponse>
                {
                    Success = false,
                    Message = "用户未认证",
                    Errors = new List<string> { "用户未认证" }
                });

            var userId = Guid.Parse(userContext.UserId);
            var response = await _eventService.CancelEventAsync(id, userId);

            _logger.LogInformation("✅ 用户 {UserId} 成功取消活动 {EventId}", userId, id);
            return Ok(new ApiResponse<EventResponse>
            {
                Success = true,
                Message = "活动已取消",
                Data = response
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<EventResponse>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new ApiResponse<EventResponse>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<EventResponse>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消活动失败");
            return StatusCode(500, new ApiResponse<EventResponse>
            {
                Success = false,
                Message = "取消活动失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     获取用户已加入的活动列表
    /// </summary>
    [HttpGet("joined")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<EventResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<EventResponse>>>> GetJoinedEvents(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            // 从 UserContext 获取当前用户信息
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
                return Unauthorized(new ApiResponse<PaginatedResponse<EventResponse>>
                {
                    Success = false,
                    Message = "用户未认证",
                    Errors = new List<string> { "用户未认证" }
                });

            var userId = Guid.Parse(userContext.UserId);
            var (events, total) = await _eventService.GetJoinedEventsAsync(userId, page, pageSize);

            return Ok(new ApiResponse<PaginatedResponse<EventResponse>>
            {
                Success = true,
                Message = "已加入的活动列表获取成功",
                Data = new PaginatedResponse<EventResponse>
                {
                    Items = events.ToList(),
                    TotalCount = total,
                    Page = page,
                    PageSize = pageSize
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取已加入的活动列表失败");
            return StatusCode(500, new ApiResponse<PaginatedResponse<EventResponse>>
            {
                Success = false,
                Message = "获取已加入的活动列表失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     获取用户取消的活动列表
    /// </summary>
    [HttpGet("cancelled")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<EventResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<EventResponse>>>> GetCancelledEventsByUser(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            // 从 UserContext 获取当前用户信息
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
                return Unauthorized(new ApiResponse<PaginatedResponse<EventResponse>>
                {
                    Success = false,
                    Message = "用户未认证",
                    Errors = new List<string> { "用户未认证" }
                });

            var userId = Guid.Parse(userContext.UserId);

            var (events, total) = await _eventService.GetCancelledEventsByUserAsync(userId, page, pageSize);

            return Ok(new ApiResponse<PaginatedResponse<EventResponse>>
            {
                Success = true,
                Message = "取消的活动列表获取成功",
                Data = new PaginatedResponse<EventResponse>
                {
                    Items = events.ToList(),
                    TotalCount = total,
                    Page = page,
                    PageSize = pageSize
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取取消的活动列表失败");
            return StatusCode(500, new ApiResponse<PaginatedResponse<EventResponse>>
            {
                Success = false,
                Message = "获取取消的活动列表失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     参加 Event
    /// </summary>
    [HttpPost("{id}/join")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ParticipantResponse>>> JoinEvent(Guid id,
        [FromBody] JoinEventRequest request)
    {
        try
        {
            // 从 UserContext 获取当前用户信息
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
                return Unauthorized(new ApiResponse<ParticipantResponse>
                {
                    Success = false,
                    Message = "用户未认证",
                    Errors = new List<string> { "用户未认证" }
                });

            var userId = Guid.Parse(userContext.UserId);
            var response = await _eventService.JoinEventAsync(id, userId, request);

            _logger.LogInformation("✅ 用户 {UserId} 成功参加 Event {EventId}", userId, id);
            return Ok(new ApiResponse<ParticipantResponse>
            {
                Success = true,
                Message = "成功加入 Event",
                Data = response
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<ParticipantResponse>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<ParticipantResponse>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "参加 Event 失败");
            return StatusCode(500, new ApiResponse<ParticipantResponse>
            {
                Success = false,
                Message = "参加 Event 失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     取消参加 Event
    /// </summary>
    [HttpDelete("{id}/join")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> LeaveEvent(Guid id)
    {
        try
        {
            // 从 UserContext 获取当前用户信息
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
                return Unauthorized(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "用户未认证",
                    Errors = new List<string> { "用户未认证" }
                });

            var userId = Guid.Parse(userContext.UserId);
            await _eventService.LeaveEventAsync(id, userId);

            _logger.LogInformation("✅ 用户 {UserId} 成功取消参加 Event {EventId}", userId, id);
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Message = "已取消参加",
                Data = true
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<bool>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消参加 Event 失败");
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "取消参加 Event 失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     关注 Event
    /// </summary>
    [HttpPost("{id}/follow")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<FollowerResponse>>> FollowEvent(Guid id,
        [FromBody] FollowEventRequest request)
    {
        try
        {
            // 从 UserContext 获取当前用户信息
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
                return Unauthorized(new ApiResponse<FollowerResponse>
                {
                    Success = false,
                    Message = "用户未认证",
                    Errors = new List<string> { "用户未认证" }
                });

            var userId = Guid.Parse(userContext.UserId);
            var response = await _eventService.FollowEventAsync(id, userId, request);

            _logger.LogInformation("✅ 用户 {UserId} 成功关注 Event {EventId}", userId, id);
            return Ok(new ApiResponse<FollowerResponse>
            {
                Success = true,
                Message = "成功关注 Event",
                Data = response
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<FollowerResponse>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<FollowerResponse>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "关注 Event 失败");
            return StatusCode(500, new ApiResponse<FollowerResponse>
            {
                Success = false,
                Message = "关注 Event 失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     取消关注 Event
    /// </summary>
    [HttpDelete("{id}/follow")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> UnfollowEvent(Guid id)
    {
        try
        {
            // 从 UserContext 获取当前用户信息
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
                return Unauthorized(new ApiResponse<bool>
                {
                    Success = false,
                    Message = "用户未认证",
                    Errors = new List<string> { "用户未认证" }
                });

            var userId = Guid.Parse(userContext.UserId);
            await _eventService.UnfollowEventAsync(id, userId);

            _logger.LogInformation("✅ 用户 {UserId} 成功取消关注 Event {EventId}", userId, id);
            return Ok(new ApiResponse<bool>
            {
                Success = true,
                Message = "已取消关注",
                Data = true
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<bool>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消关注 Event 失败");
            return StatusCode(500, new ApiResponse<bool>
            {
                Success = false,
                Message = "取消关注 Event 失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     获取 Event 参与者列表
    /// </summary>
    [HttpGet("{id}/participants")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<ParticipantResponse>>>> GetEventParticipants(Guid id)
    {
        try
        {
            var participants = await _eventService.GetParticipantsAsync(id);

            if (!participants.Any())
                return Ok(new ApiResponse<IEnumerable<ParticipantResponse>>
                {
                    Success = true,
                    Message = "参与者列表为空",
                    Data = participants
                });

            // 提取所有 userId
            var userIds = participants.Select(p => p.UserId.ToString()).ToList();

            _logger.LogInformation("📋 获取 {Count} 个参与者的用户详细信息", userIds.Count);

            try
            {
                // 通过 Dapr 调用 UserService 批量获取用户信息
                var userServiceRequest = new { UserIds = userIds };
                var userServiceResponse = await _daprClient.InvokeMethodAsync<object, JsonElement>(
                    HttpMethod.Post,
                    "user-service",
                    "api/v1/users/batch",
                    userServiceRequest
                );

                // 解析响应
                if (userServiceResponse.TryGetProperty("success", out var successProp) &&
                    successProp.GetBoolean() &&
                    userServiceResponse.TryGetProperty("data", out var dataProp))
                {
                    var users = dataProp.EnumerateArray();
                    var userMap = new Dictionary<string, JsonElement>();

                    foreach (var user in users)
                        if (user.TryGetProperty("id", out var idProp))
                            userMap[idProp.GetString() ?? ""] = user;

                    // 填充用户详细信息
                    foreach (var participant in participants)
                    {
                        var userIdStr = participant.UserId.ToString();
                        if (userMap.TryGetValue(userIdStr, out var user))
                            participant.User = new UserInfo
                            {
                                Id = user.TryGetProperty("id", out var userId) ? userId.GetString() ?? "" : "",
                                Name = user.TryGetProperty("name", out var name) ? name.GetString() : null,
                                Email = user.TryGetProperty("email", out var email) ? email.GetString() : null,
                                Avatar = user.TryGetProperty("avatar", out var avatar) ? avatar.GetString() : null,
                                Phone = user.TryGetProperty("phone", out var phone) ? phone.GetString() : null
                            };
                    }

                    _logger.LogInformation("✅ 成功填充 {Count}/{Total} 个用户详细信息",
                        userMap.Count, participants.Count());
                }
                else
                {
                    _logger.LogWarning("⚠️ 批量获取用户信息失败或返回空数据");
                }
            }
            catch (Exception userEx)
            {
                _logger.LogWarning(userEx, "⚠️ 调用 UserService 失败，返回不含用户详细信息的参与者列表");
                // 即使获取用户信息失败，仍然返回参与者列表（只是缺少用户详细信息）
            }

            return Ok(new ApiResponse<IEnumerable<ParticipantResponse>>
            {
                Success = true,
                Message = "参与者列表获取成功",
                Data = participants
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取参与者列表失败");
            return StatusCode(500, new ApiResponse<IEnumerable<ParticipantResponse>>
            {
                Success = false,
                Message = "获取参与者列表失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     获取 Event 关注者列表
    /// </summary>
    [HttpGet("{id}/followers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<FollowerResponse>>>> GetEventFollowers(Guid id)
    {
        try
        {
            var followers = await _eventService.GetFollowersAsync(id);
            return Ok(new ApiResponse<IEnumerable<FollowerResponse>>
            {
                Success = true,
                Message = "关注者列表获取成功",
                Data = followers
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取关注者列表失败");
            return StatusCode(500, new ApiResponse<IEnumerable<FollowerResponse>>
            {
                Success = false,
                Message = "获取关注者列表失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     获取当前用户创建的 Event 列表
    /// </summary>
    [HttpGet("me/created")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IEnumerable<EventResponse>>>> GetMyCreatedEvents()
    {
        try
        {
            // 从 UserContext 获取当前用户信息
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
                return Unauthorized(new ApiResponse<IEnumerable<EventResponse>>
                {
                    Success = false,
                    Message = "用户未认证",
                    Errors = new List<string> { "用户未认证" }
                });

            var userId = Guid.Parse(userContext.UserId);
            var events = await _eventService.GetUserCreatedEventsAsync(userId);

            _logger.LogInformation("✅ 获取用户 {UserId} 创建的 Event 列表，共 {Count} 个", userId, events.Count);
            return Ok(new ApiResponse<IEnumerable<EventResponse>>
            {
                Success = true,
                Message = "获取创建的 Event 列表成功",
                Data = events
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户创建的 Event 失败");
            return StatusCode(500, new ApiResponse<IEnumerable<EventResponse>>
            {
                Success = false,
                Message = "获取用户创建的 Event 失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     获取指定用户创建的 Event 数量（供其他服务调用）
    /// </summary>
    [HttpGet("user/{userId}/created/count")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> GetUserCreatedEventsCount(string userId)
    {
        try
        {
            if (!Guid.TryParse(userId, out var userGuid))
            {
                return BadRequest(0);
            }

            var count = await _eventService.GetUserCreatedEventsCountAsync(userGuid);
            _logger.LogInformation("✅ 获取用户 {UserId} 创建的 Event 数量: {Count}", userId, count);
            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户创建的 Event 数量失败: UserId={UserId}", userId);
            return Ok(0);
        }
    }

    /// <summary>
    ///     获取当前用户参加的 Event 列表
    /// </summary>
    [HttpGet("me/joined")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IEnumerable<EventResponse>>>> GetMyJoinedEvents()
    {
        try
        {
            // 从 UserContext 获取当前用户信息
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
                return Unauthorized(new ApiResponse<IEnumerable<EventResponse>>
                {
                    Success = false,
                    Message = "用户未认证",
                    Errors = new List<string> { "用户未认证" }
                });

            var userId = Guid.Parse(userContext.UserId);
            var events = await _eventService.GetUserJoinedEventsAsync(userId);

            _logger.LogInformation("✅ 获取用户 {UserId} 参加的 Event 列表，共 {Count} 个", userId, events.Count);
            return Ok(new ApiResponse<IEnumerable<EventResponse>>
            {
                Success = true,
                Message = "获取参加的 Event 列表成功",
                Data = events
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户参加的 Event 失败");
            return StatusCode(500, new ApiResponse<IEnumerable<EventResponse>>
            {
                Success = false,
                Message = "获取用户参加的 Event 失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     获取当前用户关注的 Event 列表
    /// </summary>
    [HttpGet("me/following")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IEnumerable<EventResponse>>>> GetMyFollowingEvents()
    {
        try
        {
            // 从 UserContext 获取当前用户信息
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
                return Unauthorized(new ApiResponse<IEnumerable<EventResponse>>
                {
                    Success = false,
                    Message = "用户未认证",
                    Errors = new List<string> { "用户未认证" }
                });

            var userId = Guid.Parse(userContext.UserId);
            var events = await _eventService.GetUserFollowingEventsAsync(userId);

            _logger.LogInformation("✅ 获取用户 {UserId} 关注的 Event 列表，共 {Count} 个", userId, events.Count);
            return Ok(new ApiResponse<IEnumerable<EventResponse>>
            {
                Success = true,
                Message = "获取关注的 Event 列表成功",
                Data = events
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取用户关注的 Event 失败");
            return StatusCode(500, new ApiResponse<IEnumerable<EventResponse>>
            {
                Success = false,
                Message = "获取用户关注的 Event 失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    #region 邀请 API

    /// <summary>
    ///     邀请用户参加活动
    /// </summary>
    [HttpPost("{eventId}/invitations")]
    [ProducesResponseType(typeof(ApiResponse<EventInvitationResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<EventInvitationResponse>>> InviteToEvent(
        Guid eventId,
        [FromBody] InviteToEventRequest request)
    {
        try
        {
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
                return Unauthorized(new ApiResponse<EventInvitationResponse>
                {
                    Success = false,
                    Message = "用户未认证",
                    Errors = new List<string> { "用户未认证" }
                });

            var inviterId = Guid.Parse(userContext.UserId);

            _logger.LogInformation("📨 用户 {InviterId} 邀请用户 {InviteeId} 参加活动 {EventId}",
                inviterId, request.InviteeId, eventId);

            var invitation = await _eventService.InviteToEventAsync(eventId, inviterId, request);

            // 发送通知给被邀请人
            try
            {
                await SendInvitationNotificationAsync(invitation);
            }
            catch (Exception notifyEx)
            {
                _logger.LogWarning(notifyEx, "发送邀请通知失败，但邀请已创建");
            }

            return CreatedAtAction(
                nameof(GetInvitation),
                new { invitationId = invitation.Id },
                new ApiResponse<EventInvitationResponse>
                {
                    Success = true,
                    Message = "邀请发送成功",
                    Data = invitation
                });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<EventInvitationResponse>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new ApiResponse<EventInvitationResponse>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<EventInvitationResponse>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "邀请用户参加活动失败");
            return StatusCode(500, new ApiResponse<EventInvitationResponse>
            {
                Success = false,
                Message = "邀请发送失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     响应邀请（接受或拒绝）
    /// </summary>
    [HttpPost("invitations/{invitationId}/respond")]
    [ProducesResponseType(typeof(ApiResponse<EventInvitationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<EventInvitationResponse>>> RespondToInvitation(
        Guid invitationId,
        [FromBody] RespondToInvitationRequest request)
    {
        try
        {
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
                return Unauthorized(new ApiResponse<EventInvitationResponse>
                {
                    Success = false,
                    Message = "用户未认证",
                    Errors = new List<string> { "用户未认证" }
                });

            var userId = Guid.Parse(userContext.UserId);

            _logger.LogInformation("📩 用户 {UserId} 响应邀请 {InvitationId}: {Response}",
                userId, invitationId, request.Response);

            var invitation = await _eventService.RespondToInvitationAsync(invitationId, userId, request.Response);

            // 发送响应通知给邀请人
            try
            {
                await SendInvitationResponseNotificationAsync(invitation, request.Response);
            }
            catch (Exception notifyEx)
            {
                _logger.LogWarning(notifyEx, "发送邀请响应通知失败");
            }

            return Ok(new ApiResponse<EventInvitationResponse>
            {
                Success = true,
                Message = request.Response.ToLower() == "accept" ? "已接受邀请并加入活动" : "已拒绝邀请",
                Data = invitation
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<EventInvitationResponse>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new ApiResponse<EventInvitationResponse>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<EventInvitationResponse>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "响应邀请失败");
            return StatusCode(500, new ApiResponse<EventInvitationResponse>
            {
                Success = false,
                Message = "响应邀请失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     获取邀请详情
    /// </summary>
    [HttpGet("invitations/{invitationId}")]
    [ProducesResponseType(typeof(ApiResponse<EventInvitationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<EventInvitationResponse>>> GetInvitation(Guid invitationId)
    {
        try
        {
            var invitation = await _eventService.GetInvitationAsync(invitationId);

            return Ok(new ApiResponse<EventInvitationResponse>
            {
                Success = true,
                Message = "获取邀请详情成功",
                Data = invitation
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<EventInvitationResponse>
            {
                Success = false,
                Message = ex.Message,
                Errors = new List<string> { ex.Message }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取邀请详情失败");
            return StatusCode(500, new ApiResponse<EventInvitationResponse>
            {
                Success = false,
                Message = "获取邀请详情失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     获取当前用户收到的邀请列表
    /// </summary>
    [HttpGet("invitations/received")]
    [ProducesResponseType(typeof(ApiResponse<List<EventInvitationResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<EventInvitationResponse>>>> GetReceivedInvitations(
        [FromQuery] string? status = null)
    {
        try
        {
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
                return Unauthorized(new ApiResponse<List<EventInvitationResponse>>
                {
                    Success = false,
                    Message = "用户未认证",
                    Errors = new List<string> { "用户未认证" }
                });

            var userId = Guid.Parse(userContext.UserId);
            var invitations = await _eventService.GetReceivedInvitationsAsync(userId, status);

            return Ok(new ApiResponse<List<EventInvitationResponse>>
            {
                Success = true,
                Message = "获取收到的邀请列表成功",
                Data = invitations
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取收到的邀请列表失败");
            return StatusCode(500, new ApiResponse<List<EventInvitationResponse>>
            {
                Success = false,
                Message = "获取收到的邀请列表失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     获取当前用户发出的邀请列表
    /// </summary>
    [HttpGet("invitations/sent")]
    [ProducesResponseType(typeof(ApiResponse<List<EventInvitationResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<List<EventInvitationResponse>>>> GetSentInvitations(
        [FromQuery] string? status = null)
    {
        try
        {
            var userContext = UserContextMiddleware.GetUserContext(HttpContext);
            if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
                return Unauthorized(new ApiResponse<List<EventInvitationResponse>>
                {
                    Success = false,
                    Message = "用户未认证",
                    Errors = new List<string> { "用户未认证" }
                });

            var userId = Guid.Parse(userContext.UserId);
            var invitations = await _eventService.GetSentInvitationsAsync(userId, status);

            return Ok(new ApiResponse<List<EventInvitationResponse>>
            {
                Success = true,
                Message = "获取发出的邀请列表成功",
                Data = invitations
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取发出的邀请列表失败");
            return StatusCode(500, new ApiResponse<List<EventInvitationResponse>>
            {
                Success = false,
                Message = "获取发出的邀请列表失败",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     发送邀请通知给被邀请人
    /// </summary>
    private async Task SendInvitationNotificationAsync(EventInvitationResponse invitation)
    {
        try
        {
            var notificationPayload = new
            {
                userId = invitation.InviteeId.ToString(),
                title = "您收到了活动邀请",
                message = $"{invitation.Inviter?.Name ?? "某位用户"} 邀请您参加活动「{invitation.Event?.Title}」",
                type = "event_invitation",
                relatedId = invitation.Id.ToString(),
                metadata = new Dictionary<string, object>
                {
                    { "eventId", invitation.EventId.ToString() },
                    { "inviterId", invitation.InviterId.ToString() },
                    { "invitationId", invitation.Id.ToString() },
                    { "eventTitle", invitation.Event?.Title ?? "" },
                    { "inviterName", invitation.Inviter?.Name ?? "" }
                }
            };

            // 使用 Dapr 发布通知事件
            await _daprClient.PublishEventAsync("pubsub", "notification-created", notificationPayload);
            _logger.LogInformation("✅ 邀请通知已发送给用户 {InviteeId}", invitation.InviteeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送邀请通知失败");
            throw;
        }
    }

    /// <summary>
    ///     发送邀请响应通知给邀请人
    /// </summary>
    private async Task SendInvitationResponseNotificationAsync(EventInvitationResponse invitation, string response)
    {
        try
        {
            var action = response.ToLower() == "accept" ? "接受" : "拒绝";
            var notificationPayload = new
            {
                userId = invitation.InviterId.ToString(),
                title = "活动邀请已被响应",
                message = $"{invitation.Invitee?.Name ?? "被邀请用户"} {action}了您的活动「{invitation.Event?.Title}」邀请",
                type = "event_invitation_response",
                relatedId = invitation.Id.ToString(),
                metadata = new Dictionary<string, object>
                {
                    { "eventId", invitation.EventId.ToString() },
                    { "inviteeId", invitation.InviteeId.ToString() },
                    { "invitationId", invitation.Id.ToString() },
                    { "response", response.ToLower() },
                    { "eventTitle", invitation.Event?.Title ?? "" },
                    { "inviteeName", invitation.Invitee?.Name ?? "" }
                }
            };

            // 使用 Dapr 发布通知事件
            await _daprClient.PublishEventAsync("pubsub", "notification-created", notificationPayload);
            _logger.LogInformation("✅ 邀请响应通知已发送给用户 {InviterId}", invitation.InviterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送邀请响应通知失败");
            throw;
        }
    }

    #endregion
    
    #region 城市活动统计 API

    /// <summary>
    ///     批量获取城市活动数量（供 CityService 调用）
    /// </summary>
    [HttpPost("cities/counts")]
    [ProducesResponseType(typeof(BatchCityCountResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<BatchCityCountResponse>> GetCitiesEventCounts([FromBody] List<string> cityIds)
    {
        try
        {
            _logger.LogInformation("📊 批量获取城市活动数量: {Count} 个城市", cityIds.Count);

            var counts = await _eventService.GetCitiesEventCountsAsync(cityIds);

            return Ok(new BatchCityCountResponse
            {
                Counts = counts.Select(kvp => new CityCountItem
                {
                    CityId = kvp.Key,
                    Count = kvp.Value
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量获取城市活动数量失败");
            return StatusCode(500, new BatchCityCountResponse { Counts = new List<CityCountItem>() });
        }
    }

    #endregion
}

/// <summary>
/// 批量城市数量响应
/// </summary>
public class BatchCityCountResponse
{
    public List<CityCountItem> Counts { get; set; } = new();
}

/// <summary>
/// 城市数量项
/// </summary>
public class CityCountItem
{
    public string CityId { get; set; } = string.Empty;
    public int Count { get; set; }
}