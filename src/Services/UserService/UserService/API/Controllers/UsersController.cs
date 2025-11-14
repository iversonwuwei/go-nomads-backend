using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GoNomads.Shared.Models;
using GoNomads.Shared.Middleware;
using Dapr.Client;
using UserService.Application.DTOs;
using UserService.Application.Services;
using System.ComponentModel.DataAnnotations;

namespace UserService.API.Controllers;

/// <summary>
/// Users API - RESTful endpoints for user management
/// </summary>
[ApiController]
[Route("api/v1/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly DaprClient _daprClient;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserService userService,
        DaprClient daprClient,
        ILogger<UsersController> logger)
    {
        _userService = userService;
        _daprClient = daprClient;
        _logger = logger;
    }

    /// <summary>
    /// 获取用户列表（分页）
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
        {
            _logger.LogInformation(
                "📋 GetUsers 请求 - 认证用户: UserId={UserId}, Role={Role}, Page={Page}, PageSize={PageSize}",
                userContext.UserId, userContext.Role, page, pageSize);
        }

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
    /// 搜索用户（按名称或邮箱，可筛选角色）
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
        {
            _logger.LogInformation(
                "🔍 SearchUsers 请求 - 认证用户: UserId={UserId}, Role={Role}, Query={Query}, FilterRole={FilterRole}",
                userContext.UserId, userContext.Role, q, role);
        }

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
    /// 根据 ID 获取用户
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
            {
                return NotFound(new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = "User not found"
                });
            }

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
    /// 批量根据 ID 获取用户
    /// </summary>
    [HttpPost("batch")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<UserDto>>>> GetUsersByIds(
        [FromBody] BatchUserIdsRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 批量获取用户: Count={Count}", request.UserIds?.Count ?? 0);

        if (request.UserIds == null || request.UserIds.Count == 0)
        {
            return BadRequest(new ApiResponse<List<UserDto>>
            {
                Success = false,
                Message = "用户ID列表不能为空"
            });
        }

        // 限制批量请求数量
        if (request.UserIds.Count > 100)
        {
            return BadRequest(new ApiResponse<List<UserDto>>
            {
                Success = false,
                Message = "单次最多批量获取100个用户"
            });
        }

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
    /// 获取当前用户信息（使用 UserContext）
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetCurrentUser(CancellationToken cancellationToken = default)
    {
        // 从 UserContext 获取当前用户 ID
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
        {
            return Unauthorized(new ApiResponse<UserDto>
            {
                Success = false,
                Message = "未认证用户"
            });
        }

        _logger.LogInformation("🔍 获取当前用户: {UserId}", userContext.UserId);

        try
        {
            var user = await _userService.GetUserByIdAsync(userContext.UserId!, cancellationToken);

            if (user == null)
            {
                return NotFound(new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = "User not found"
                });
            }

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
    /// 创建用户（不带密码 - 通常由管理员使用）
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

            // 发布用户创建事件到 Dapr Pub/Sub
            try
            {
                var userCreatedEvent = new UserCreatedEvent
                {
                    UserId = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    CreatedAt = user.CreatedAt
                };

                await _daprClient.PublishEventAsync(
                    pubsubName: "pubsub",
                    topicName: "user-created",
                    data: userCreatedEvent,
                    cancellationToken: cancellationToken);

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
    /// 更新用户信息
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
    /// 更新当前用户信息（使用 UserContext）
    /// </summary>
    [HttpPut("me")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateCurrentUser(
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        // 从 UserContext 获取当前用户 ID
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
        {
            return Unauthorized(new ApiResponse<UserDto>
            {
                Success = false,
                Message = "未认证用户"
            });
        }

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
    /// 删除用户
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
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "User not found"
                });
            }

            // 发布用户删除事件到 Dapr Pub/Sub
            try
            {
                var userDeletedEvent = new UserDeletedEvent
                {
                    UserId = id,
                    DeletedAt = DateTime.UtcNow
                };

                await _daprClient.PublishEventAsync(
                    pubsubName: "pubsub",
                    topicName: "user-deleted",
                    data: userDeletedEvent,
                    cancellationToken: cancellationToken);

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
    /// 更改用户角色（仅管理员）
    /// </summary>
    [HttpPatch("{id}/role")]
    public async Task<ActionResult<ApiResponse<UserDto>>> ChangeUserRole(
        string id,
        [FromBody] ChangeUserRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        // 验证用户是否为管理员
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.Role != "admin")
        {
            return StatusCode(403, new ApiResponse<UserDto>
            {
                Success = false,
                Message = "只有管理员可以更改用户角色"
            });
        }

        _logger.LogInformation("🔄 更改用户角色: UserId={UserId}, RoleId={RoleId}", id, request.RoleId);

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
    /// 健康检查端点
    /// </summary>
    [HttpGet("health")]
    public ActionResult<object> HealthCheck()
    {
        return Ok(new { status = "healthy", service = "UserService", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// 获取用户的产品列表（通过 Dapr 调用 ProductService）
    /// </summary>
    [HttpGet("{userId}/products")]
    public async Task<ActionResult<ApiResponse<object>>> GetUserProducts(
        string userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📦 Getting products for user {UserId} via Dapr", userId);

        try
        {
            // 验证用户是否存在
            var exists = await _userService.UserExistsAsync(userId, cancellationToken);
            if (!exists)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "User not found"
                });
            }

            // 使用 Dapr 服务调用 ProductService
            var products = await _daprClient.InvokeMethodAsync<object>(
                httpMethod: HttpMethod.Get,
                appId: "product-service",
                methodName: $"/api/products/user/{userId}",
                cancellationToken: cancellationToken);

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
    /// 使用 Dapr State Store 缓存用户数据
    /// </summary>
    [HttpGet("{id}/cached")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetCachedUser(
        string id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("💾 Getting cached user: {UserId}", id);

        try
        {
            // 尝试从 Dapr State Store 获取缓存
            var cachedUser = await _daprClient.GetStateAsync<UserDto>(
                storeName: "statestore",
                key: $"user:{id}",
                cancellationToken: cancellationToken);

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
            {
                return NotFound(new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = "User not found"
                });
            }

            // 保存到缓存（5分钟过期）
            await _daprClient.SaveStateAsync(
                storeName: "statestore",
                key: $"user:{id}",
                value: user,
                metadata: new Dictionary<string, string>
                {
                    { "ttlInSeconds", "300" }
                },
                cancellationToken: cancellationToken);

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
/// 创建用户请求 DTO
/// </summary>
public class CreateUserRequest
{
    [Required(ErrorMessage = "姓名不能为空")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "邮箱不能为空")]
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "手机号不能为空")]
    public string Phone { get; set; } = string.Empty;
}

/// <summary>
/// 更新用户请求 DTO
/// </summary>
public class UpdateUserRequest
{
    [Required(ErrorMessage = "姓名不能为空")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "邮箱不能为空")]
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "手机号不能为空")]
    public string Phone { get; set; } = string.Empty;
}

/// <summary>
/// 批量获取用户请求 DTO
/// </summary>
public class BatchUserIdsRequest
{
    [Required(ErrorMessage = "用户ID列表不能为空")]
    public List<string> UserIds { get; set; } = new();
}

/// <summary>
/// 更改用户角色请求 DTO
/// </summary>
public class ChangeUserRoleRequest
{
    [Required(ErrorMessage = "角色ID不能为空")]
    public string RoleId { get; set; } = string.Empty;
}

#endregion

#region Event DTOs

/// <summary>
/// 用户创建事件 DTO
/// </summary>
public class UserCreatedEvent
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 用户删除事件 DTO
/// </summary>
public class UserDeletedEvent
{
    public string UserId { get; set; } = string.Empty;
    public DateTime DeletedAt { get; set; }
}

#endregion
