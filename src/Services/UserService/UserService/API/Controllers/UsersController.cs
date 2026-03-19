using System.ComponentModel.DataAnnotations;
using GoNomads.Shared.Communication;
using GoNomads.Shared.Middleware;
using GoNomads.Shared.Models;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs;
using UserService.Application.Services;

namespace UserService.API.Controllers;

/// <summary>
///     Users API - RESTful endpoints for user management
/// </summary>
[ApiController]
[Route("api/v1/users")]
public class UsersController : ControllerBase
{
    private readonly ILogger<UsersController> _logger;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ServiceInvocationClient _serviceInvocationClient;
    private readonly IUserService _userService;

    public UsersController(
        IUserService userService,
        ServiceInvocationClient serviceInvocationClient,
        IPublishEndpoint publishEndpoint,
        ILogger<UsersController> logger)
    {
        _userService = userService;
        _serviceInvocationClient = serviceInvocationClient;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    /// <summary>
    ///     获取用户列表（分页）
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<UserDto>>>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        // 获取用户上下文（可选，用于日志记录）
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated == true)
            _logger.LogInformation(
                "📋 GetUsers 请求 - 认证用户: UserId={UserId}, Role={Role}, Page={Page}, PageSize={PageSize}",
                userContext.UserId, userContext.Role, page, pageSize);

        // 验证并规范化分页参数
        page = Math.Max(1, page);
        pageSize = Math.Max(1, Math.Min(100, pageSize));

        try
        {
            var (users, total) = await _userService.GetUsersAsync(page, pageSize, cancellationToken);

            return Ok(new ApiResponse<PaginatedResponse<UserDto>>
            {
                Success = true,
                Message = "Users retrieved successfully",
                Data = new PaginatedResponse<UserDto>
                {
                    Items = users,
                    TotalCount = total,
                    Page = page,
                    PageSize = pageSize
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取用户列表失败");
            return StatusCode(500, new ApiResponse<PaginatedResponse<UserDto>>
            {
                Success = false,
                Message = "获取用户列表失败"
            });
        }
    }

    /// <summary>
    ///     搜索用户（按名称或邮箱，可筛选角色）
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<UserDto>>>> SearchUsers(
        [FromQuery] string? q = null,
        [FromQuery] string? role = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        // 获取用户上下文
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated == true)
            _logger.LogInformation(
                "🔍 SearchUsers 请求 - 认证用户: UserId={UserId}, Role={Role}, Query={Query}, FilterRole={FilterRole}",
                userContext.UserId, userContext.Role, q, role);

        // 验证并规范化分页参数
        page = Math.Max(1, page);
        pageSize = Math.Max(1, Math.Min(100, pageSize));

        try
        {
            var (users, total) = await _userService.SearchUsersAsync(q, role, page, pageSize, cancellationToken);

            return Ok(new ApiResponse<PaginatedResponse<UserDto>>
            {
                Success = true,
                Message = "Users searched successfully",
                Data = new PaginatedResponse<UserDto>
                {
                    Items = users,
                    TotalCount = total,
                    Page = page,
                    PageSize = pageSize
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 搜索用户失败 - Query: {Query}", q);
            return StatusCode(500, new ApiResponse<PaginatedResponse<UserDto>>
            {
                Success = false,
                Message = "搜索用户失败"
            });
        }
    }

    /// <summary>
    ///     获取版主候选人列表（Pro及以上会员或Admin用户）
    ///     仅返回符合版主资格的用户（会员等级 >= Pro 或者是 Admin）
    /// </summary>
    [HttpGet("moderator-candidates")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<ModeratorCandidateDto>>>> GetModeratorCandidates(
        [FromQuery] string? q = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // 获取用户上下文
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated == true)
            _logger.LogInformation(
                "👥 GetModeratorCandidates 请求 - 认证用户: UserId={UserId}, Role={Role}, Query={Query}",
                userContext.UserId, userContext.Role, q);

        // 验证并规范化分页参数
        page = Math.Max(1, page);
        pageSize = Math.Max(1, Math.Min(100, pageSize));

        try
        {
            var (users, total) = await _userService.GetModeratorCandidatesAsync(q, page, pageSize, cancellationToken);

            return Ok(new ApiResponse<PaginatedResponse<ModeratorCandidateDto>>
            {
                Success = true,
                Message = "Moderator candidates retrieved successfully",
                Data = new PaginatedResponse<ModeratorCandidateDto>
                {
                    Items = users,
                    TotalCount = total,
                    Page = page,
                    PageSize = pageSize
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取版主候选人列表失败 - Query: {Query}", q);
            return StatusCode(500, new ApiResponse<PaginatedResponse<ModeratorCandidateDto>>
            {
                Success = false,
                Message = "获取版主候选人列表失败"
            });
        }
    }

    /// <summary>
    ///     根据 ID 获取用户
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(
        string id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 获取用户: {UserId}", id);

        try
        {
            var user = await _userService.GetUserByIdAsync(id, cancellationToken);

            if (user == null)
                return NotFound(new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = "User not found"
                });

            return Ok(new ApiResponse<UserDto>
            {
                Success = true,
                Message = "User retrieved successfully",
                Data = user
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取用户失败: {UserId}", id);
            return StatusCode(500, new ApiResponse<UserDto>
            {
                Success = false,
                Message = "获取用户失败"
            });
        }
    }

    /// <summary>
    ///     根据 ID 获取用户基本信息（简化版，用于跨服务调用）
    /// </summary>
    [HttpGet("{id}/basic")]
    [AllowAnonymous]
    public async Task<ActionResult<UserBasicDto>> GetUserBasic(
        string id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 获取用户基本信息: {UserId}", id);

        try
        {
            var user = await _userService.GetUserByIdAsync(id, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("⚠️ 用户不存在: {UserId}", id);
                return NotFound();
            }

            // 返回简化的用户信息（直接返回 DTO，不包装 ApiResponse，便于服务间调用）
            return Ok(new UserBasicDto
            {
                Id = user.Id,
                Name = user.Name,
                Avatar = user.AvatarUrl,
                Email = user.Email
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取用户基本信息失败: {UserId}", id);
            return StatusCode(500);
        }
    }

    /// <summary>
    ///     批量根据 ID 获取用户
    /// </summary>
    [HttpPost("batch")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<UserDto>>>> GetUsersByIds(
        [FromBody] BatchUserIdsRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 批量获取用户: Count={Count}", request.UserIds?.Count ?? 0);

        if (request.UserIds == null || request.UserIds.Count == 0)
            return BadRequest(new ApiResponse<List<UserDto>>
            {
                Success = false,
                Message = "用户ID列表不能为空"
            });

        // 限制批量请求数量
        if (request.UserIds.Count > 100)
            return BadRequest(new ApiResponse<List<UserDto>>
            {
                Success = false,
                Message = "单次最多批量获取100个用户"
            });

        try
        {
            var users = await _userService.GetUsersByIdsAsync(request.UserIds, cancellationToken);

            return Ok(new ApiResponse<List<UserDto>>
            {
                Success = true,
                Message = $"成功获取 {users.Count} 个用户",
                Data = users
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 批量获取用户失败");
            return StatusCode(500, new ApiResponse<List<UserDto>>
            {
                Success = false,
                Message = "批量获取用户失败"
            });
        }
    }

    /// <summary>
    ///     获取当前用户信息（使用 UserContext）
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUser(CancellationToken cancellationToken = default)
    {
        // 从 UserContext 获取当前用户 ID
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
            return Unauthorized(new ApiResponse<UserDto>
            {
                Success = false,
                Message = "未认证用户"
            });

        _logger.LogInformation("🔍 获取当前用户: {UserId}", userContext.UserId);

        try
        {
            var user = await _userService.GetUserByIdAsync(userContext.UserId!, cancellationToken);

            if (user == null)
                return NotFound(new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = "User not found"
                });

            return Ok(new ApiResponse<UserDto>
            {
                Success = true,
                Message = "User retrieved successfully",
                Data = user
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取当前用户失败: {UserId}", userContext.UserId);
            return StatusCode(500, new ApiResponse<UserDto>
            {
                Success = false,
                Message = "获取用户失败"
            });
        }
    }

    /// <summary>
    ///     获取所有管理员的用户ID列表
    /// </summary>
    [HttpGet("admins")]
    public async Task<ActionResult<List<Guid>>> GetAdminUserIds(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 获取所有管理员用户ID");

        try
        {
            var adminIds = await _userService.GetAdminUserIdsAsync(cancellationToken);

            _logger.LogInformation("✅ 成功获取 {Count} 个管理员", adminIds.Count);
            return Ok(adminIds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取管理员列表失败");
            return StatusCode(500, new List<Guid>());
        }
    }

    /// <summary>
    ///     创建用户（不带密码 - 通常由管理员使用）
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 创建用户: {Email}", request.Email);

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(new ApiResponse<UserDto>
            {
                Success = false,
                Message = "验证失败",
                Errors = errors
            });
        }

        try
        {
            var user = await _userService.CreateUserAsync(
                request.Name,
                request.Email,
                request.Phone,
                cancellationToken);

            // 发布用户创建事件到消息总线
            try
            {
                var userCreatedEvent = new UserCreatedEvent
                {
                    UserId = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    CreatedAt = user.CreatedAt
                };

                await _publishEndpoint.Publish(userCreatedEvent, cancellationToken);

                _logger.LogInformation("📤 Published user-created event for user {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Failed to publish user-created event for user {UserId}", user.Id);
                // 不影响主流程
            }

            return CreatedAtAction(
                nameof(GetUser),
                new { id = user.Id },
                new ApiResponse<UserDto>
                {
                    Success = true,
                    Message = "User created successfully",
                    Data = user
                });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "⚠️ 创建用户失败: {Email}", request.Email);
            return BadRequest(new ApiResponse<UserDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建用户失败: {Email}", request.Email);
            return StatusCode(500, new ApiResponse<UserDto>
            {
                Success = false,
                Message = "创建用户失败"
            });
        }
    }

    /// <summary>
    ///     更新用户信息
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(
        string id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 更新用户: {UserId}", id);

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(new ApiResponse<UserDto>
            {
                Success = false,
                Message = "验证失败",
                Errors = errors
            });
        }

        try
        {
            var user = await _userService.UpdateUserAsync(
                id,
                request.Name,
                request.Email,
                request.Phone,
                request.AvatarUrl,
                request.Bio,
                cancellationToken);

            return Ok(new ApiResponse<UserDto>
            {
                Success = true,
                Message = "User updated successfully",
                Data = user
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<UserDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<UserDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新用户失败: {UserId}", id);
            return StatusCode(500, new ApiResponse<UserDto>
            {
                Success = false,
                Message = "更新用户失败"
            });
        }
    }

    /// <summary>
    ///     更新当前用户信息（使用 UserContext）
    /// </summary>
    [HttpPut("me")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateCurrentUser(
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        // 从 UserContext 获取当前用户 ID
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
            return Unauthorized(new ApiResponse<UserDto>
            {
                Success = false,
                Message = "未认证用户"
            });

        _logger.LogInformation("📝 更新当前用户: {UserId}", userContext.UserId);

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(new ApiResponse<UserDto>
            {
                Success = false,
                Message = "验证失败",
                Errors = errors
            });
        }

        try
        {
            var user = await _userService.UpdateUserAsync(
                userContext.UserId!,
                request.Name,
                request.Email,
                request.Phone,
                request.AvatarUrl,
                request.Bio,
                cancellationToken);

            return Ok(new ApiResponse<UserDto>
            {
                Success = true,
                Message = "User updated successfully",
                Data = user
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<UserDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<UserDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新当前用户失败: {UserId}", userContext.UserId);
            return StatusCode(500, new ApiResponse<UserDto>
            {
                Success = false,
                Message = "更新用户失败"
            });
        }
    }

    /// <summary>
    ///     删除用户
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteUser(
        string id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🗑️ 删除用户: {UserId}", id);

        try
        {
            var result = await _userService.DeleteUserAsync(id, cancellationToken);

            if (!result)
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "User not found"
                });

            // 发布用户删除事件到消息总线
            try
            {
                var userDeletedEvent = new UserDeletedEvent
                {
                    UserId = id,
                    DeletedAt = DateTime.UtcNow
                };

                await _publishEndpoint.Publish(userDeletedEvent, cancellationToken);

                _logger.LogInformation("📤 Published user-deleted event for user {UserId}", id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Failed to publish user-deleted event for user {UserId}", id);
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "User deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除用户失败: {UserId}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "删除用户失败"
            });
        }
    }

    /// <summary>
    ///     更改用户角色
    /// </summary>
    [HttpPatch("{id}/role")]
    public async Task<ActionResult<ApiResponse<UserDto>>> ChangeUserRole(
        string id,
        [FromBody] ChangeUserRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        // Gateway 已完成 token 验证，这里只获取用户信息用于日志
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);

        _logger.LogInformation(
            "🔄 更改用户角色: UserId={UserId}, RoleId={RoleId}, OperatorId={OperatorId}, OperatorRole={OperatorRole}",
            id, request.RoleId, userContext?.UserId, userContext?.Role);

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(new ApiResponse<UserDto>
            {
                Success = false,
                Message = "验证失败",
                Errors = errors
            });
        }

        try
        {
            var user = await _userService.ChangeUserRoleAsync(id, request.RoleId, cancellationToken);

            return Ok(new ApiResponse<UserDto>
            {
                Success = true,
                Message = "User role changed successfully",
                Data = user
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<UserDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更改用户角色失败: UserId={UserId}", id);
            return StatusCode(500, new ApiResponse<UserDto>
            {
                Success = false,
                Message = "更改用户角色失败"
            });
        }
    }

    /// <summary>
    ///     批量更改用户角色
    /// </summary>
    [HttpPatch("batch/role")]
    public async Task<ActionResult<ApiResponse<BatchChangeRoleResult>>> BatchChangeUserRole(
        [FromBody] BatchChangeUserRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        // Gateway 已完成 token 验证，这里只获取用户信息用于日志
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);

        _logger.LogInformation(
            "🔄 批量更改用户角色: UserCount={Count}, RoleId={RoleId}, OperatorId={OperatorId}, OperatorRole={OperatorRole}",
            request.UserIds?.Count ?? 0, request.RoleId, userContext?.UserId, userContext?.Role);

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(new ApiResponse<BatchChangeRoleResult>
            {
                Success = false,
                Message = "验证失败",
                Errors = errors
            });
        }

        if (request.UserIds == null || request.UserIds.Count == 0)
            return BadRequest(new ApiResponse<BatchChangeRoleResult>
            {
                Success = false,
                Message = "用户ID列表不能为空"
            });

        // 限制批量操作数量
        if (request.UserIds.Count > 100)
            return BadRequest(new ApiResponse<BatchChangeRoleResult>
            {
                Success = false,
                Message = "单次最多批量更改100个用户角色"
            });

        try
        {
            var successCount = 0;
            var failedCount = 0;
            var updatedUsers = new List<UserDto>();
            var errors = new List<string>();

            foreach (var userId in request.UserIds)
                try
                {
                    var user = await _userService.ChangeUserRoleAsync(userId, request.RoleId, cancellationToken);
                    updatedUsers.Add(user);
                    successCount++;
                }
                catch (Exception ex)
                {
                    failedCount++;
                    errors.Add($"用户 {userId}: {ex.Message}");
                    _logger.LogWarning(ex, "⚠️ 更改用户 {UserId} 角色失败", userId);
                }

            var result = new BatchChangeRoleResult
            {
                SuccessCount = successCount,
                FailedCount = failedCount,
                UpdatedUsers = updatedUsers,
                Errors = errors
            };

            if (failedCount > 0)
                return Ok(new ApiResponse<BatchChangeRoleResult>
                {
                    Success = false,
                    Message = $"批量更改完成，成功: {successCount}，失败: {failedCount}",
                    Data = result,
                    Errors = errors
                });

            return Ok(new ApiResponse<BatchChangeRoleResult>
            {
                Success = true,
                Message = $"成功更改 {successCount} 个用户的角色",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 批量更改用户角色失败");
            return StatusCode(500, new ApiResponse<BatchChangeRoleResult>
            {
                Success = false,
                Message = "批量更改用户角色失败"
            });
        }
    }

    /// <summary>
    ///     健康检查端点
    /// </summary>
    [HttpGet("health")]
    public ActionResult<object> HealthCheck()
    {
        return Ok(new { status = "healthy", service = "UserService", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    ///     获取用户的产品列表（通过服务调用 ProductService）
    /// </summary>
    [HttpGet("{userId}/products")]
    public async Task<ActionResult<ApiResponse<object>>> GetUserProducts(
        string userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📦 Getting products for user {UserId} via service invocation", userId);

        try
        {
            // 验证用户是否存在
            var exists = await _userService.UserExistsAsync(userId, cancellationToken);
            if (!exists)
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "User not found"
                });

            var products = await _serviceInvocationClient.InvokeAsync<object>(
                HttpMethod.Get,
                "product-service",
                $"/api/products/user/{userId}",
                cancellationToken);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "User products retrieved successfully",
                Data = products
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error getting products for user {UserId}", userId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "Failed to retrieve user products",
                Errors = new List<string> { ex.Message }
            });
        }
    }

    /// <summary>
    ///     使用内存缓存缓存用户数据
    /// </summary>
    [HttpGet("{id}/cached")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetCachedUser(
        string id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("💾 Getting cached user: {UserId}", id);

        try
        {
            var cachedUser = await _serviceInvocationClient.GetCachedStateAsync<UserDto>(
                "statestore",
                $"user:{id}",
                cancellationToken);

            if (cachedUser != null)
            {
                _logger.LogInformation("✅ User {UserId} found in cache", id);
                return Ok(new ApiResponse<UserDto>
                {
                    Success = true,
                    Message = "User retrieved from cache",
                    Data = cachedUser
                });
            }

            // 缓存未命中，从数据库获取
            _logger.LogInformation("🔍 User {UserId} not in cache, fetching from database", id);
            var user = await _userService.GetUserByIdAsync(id, cancellationToken);

            if (user == null)
                return NotFound(new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = "User not found"
                });

            await _serviceInvocationClient.SaveCachedStateAsync(
                "statestore",
                $"user:{id}",
                user,
                metadata: new Dictionary<string, string>
                {
                    { "ttlInSeconds", "300" }
                },
                cancellationToken);

            _logger.LogInformation("✅ User {UserId} cached successfully", id);

            return Ok(new ApiResponse<UserDto>
            {
                Success = true,
                Message = "User retrieved from database and cached",
                Data = user
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error getting cached user {UserId}", id);
            return StatusCode(500, new ApiResponse<UserDto>
            {
                Success = false,
                Message = "Failed to retrieve user",
                Errors = new List<string> { ex.Message }
            });
        }
    }
}

#region Request DTOs

/// <summary>
///     创建用户请求 DTO
/// </summary>
public class CreateUserRequest
{
    [Required(ErrorMessage = "姓名不能为空")] public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "邮箱不能为空")]
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "手机号不能为空")] public string Phone { get; set; } = string.Empty;
}

/// <summary>
///     更新用户请求 DTO
/// </summary>
public class UpdateUserRequest
{
    /// <summary>
    ///     用户姓名（可选，不传则不更新）
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///     用户邮箱（可选，不传则不更新）
    /// </summary>
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    public string? Email { get; set; }

    /// <summary>
    ///     手机号（可选，不传则不更新）
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    ///     头像 URL（可选）
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    ///     个人简介（可选）
    /// </summary>
    public string? Bio { get; set; }
}

/// <summary>
///     批量获取用户请求 DTO
/// </summary>
public class BatchUserIdsRequest
{
    [Required(ErrorMessage = "用户ID列表不能为空")]
    public List<string> UserIds { get; set; } = new();
}

/// <summary>
///     更改用户角色请求 DTO
/// </summary>
public class ChangeUserRoleRequest
{
    [Required(ErrorMessage = "角色ID不能为空")] public string RoleId { get; set; } = string.Empty;
}

/// <summary>
///     批量更改用户角色请求 DTO
/// </summary>
public class BatchChangeUserRoleRequest
{
    [Required(ErrorMessage = "用户ID列表不能为空")]
    public List<string> UserIds { get; set; } = new();

    [Required(ErrorMessage = "角色ID不能为空")] public string RoleId { get; set; } = string.Empty;
}

/// <summary>
///     批量更改用户角色结果 DTO
/// </summary>
public class BatchChangeRoleResult
{
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<UserDto> UpdatedUsers { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

#endregion

#region Event DTOs

/// <summary>
///     用户创建事件 DTO
/// </summary>
public class UserCreatedEvent
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
///     用户删除事件 DTO
/// </summary>
public class UserDeletedEvent
{
    public string UserId { get; set; } = string.Empty;
    public DateTime DeletedAt { get; set; }
}

#endregion