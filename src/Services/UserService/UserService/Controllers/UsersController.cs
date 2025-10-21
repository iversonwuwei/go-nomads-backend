using Microsoft.AspNetCore.Mvc;
using GoNomads.Shared.Models;
using GoNomads.Shared.Middleware;
using Dapr.Client;
using UserService.DTOs;

namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ILogger<UsersController> _logger;
    private readonly DaprClient _daprClient;
    private readonly Services.IUserService _userService;
    private readonly Services.IAuthService _authService;

    public UsersController(
        ILogger<UsersController> logger,
        DaprClient daprClient,
        Services.IUserService userService,
        Services.IAuthService authService)
    {
        _logger = logger;
        _daprClient = daprClient;
        _userService = userService;
        _authService = authService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<UserDto>>>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        // 获取用户上下文信息（从 Gateway 传递过来的）
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        
        if (userContext?.IsAuthenticated == true)
        {
            _logger.LogInformation(
                "📋 GetUsers 请求 - 认证用户信息: UserId={UserId}, Email={Email}, Role={Role}, Page={Page}, PageSize={PageSize}",
                userContext.UserId,
                userContext.Email,
                userContext.Role,
                page,
                pageSize
            );
        }
        else
        {
            _logger.LogInformation("📋 GetUsers 请求 - 未认证用户, Page: {Page}, PageSize: {PageSize}", page, pageSize);
        }
        
        page = Math.Max(1, page);
        pageSize = Math.Max(1, Math.Min(100, pageSize));

        var (users, totalCount) = await _userService.GetUsersAsync(page, pageSize, cancellationToken);

        // 转换为 DTO
        var userDtos = users.Select(u => new UserDto
        {
            Id = u.Id,
            Name = u.Name,
            Email = u.Email,
            Phone = u.Phone,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt
        }).ToList();

        var response = new ApiResponse<PaginatedResponse<UserDto>>
        {
            Success = true,
            Message = "Users retrieved successfully",
            Data = new PaginatedResponse<UserDto>
            {
                Items = userDtos,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            }
        };

        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUser(
        string id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting user with ID: {UserId}", id);

        var user = await _userService.GetUserByIdAsync(id, cancellationToken);

        if (user == null)
        {
            return NotFound(new ApiResponse<UserDto>
            {
                Success = false,
                Message = "User not found"
            });
        }

        // 转换为 DTO
        var userDto = new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };

        return Ok(new ApiResponse<UserDto>
        {
            Success = true,
            Message = "User retrieved successfully",
            Data = userDto
        });
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    /// <param name="loginDto">登录信息</param>
    /// <returns>认证令牌和用户信息</returns>
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login(
        [FromBody] LoginDto loginDto)
    {
        _logger.LogInformation("用户尝试登录: {Email}", loginDto.Email);

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(new ApiResponse<AuthResponseDto>
            {
                Success = false,
                Message = "验证失败",
                Errors = errors
            });
        }

        try
        {
            var authResponse = await _authService.LoginAsync(loginDto.Email, loginDto.Password);

            return Ok(new ApiResponse<AuthResponseDto>
            {
                Success = true,
                Message = "登录成功",
                Data = authResponse
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "用户 {Email} 登录失败: 未授权", loginDto.Email);
            return Unauthorized(new ApiResponse<AuthResponseDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用户 {Email} 登录时发生错误", loginDto.Email);
            return StatusCode(500, new ApiResponse<AuthResponseDto>
            {
                Success = false,
                Message = "登录失败,请稍后重试"
            });
        }
    }

    /// <summary>
    /// 刷新访问令牌
    /// </summary>
    /// <param name="dto">刷新令牌请求</param>
    /// <returns>新的认证令牌</returns>
    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> RefreshToken(
        [FromBody] RefreshTokenDto dto)
    {
        _logger.LogInformation("尝试刷新访问令牌");

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(new ApiResponse<AuthResponseDto>
            {
                Success = false,
                Message = "验证失败",
                Errors = errors
            });
        }

        try
        {
            var authResponse = await _authService.RefreshTokenAsync(dto.RefreshToken);

            return Ok(new ApiResponse<AuthResponseDto>
            {
                Success = true,
                Message = "令牌刷新成功",
                Data = authResponse
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "刷新令牌失败: 未授权");
            return Unauthorized(new ApiResponse<AuthResponseDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新令牌时发生错误");
            return StatusCode(500, new ApiResponse<AuthResponseDto>
            {
                Success = false,
                Message = "刷新令牌失败,请稍后重试"
            });
        }
    }

    /// <summary>
    /// 用户注册
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register(
        [FromBody] RegisterDto dto)
    {
        _logger.LogInformation("用户尝试注册: {Email}", dto.Email);

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(new ApiResponse<AuthResponseDto>
            {
                Success = false,
                Message = "验证失败",
                Errors = errors
            });
        }

        try
        {
            // 检查邮箱是否已存在
            var existingUser = await _userService.GetUserByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                return BadRequest(new ApiResponse<AuthResponseDto>
                {
                    Success = false,
                    Message = "该邮箱已被注册"
                });
            }

            // 创建用户（包含密码哈希）
            var user = await _userService.CreateUserWithPasswordAsync(
                dto.Name,
                dto.Email,
                dto.Password,
                dto.Phone ?? string.Empty
            );

            // 自动登录并返回 token
            var authResponse = await _authService.LoginAsync(dto.Email, dto.Password);

            return Ok(new ApiResponse<AuthResponseDto>
            {
                Success = true,
                Message = "注册成功",
                Data = authResponse
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用户 {Email} 注册时发生错误", dto.Email);
            return StatusCode(500, new ApiResponse<AuthResponseDto>
            {
                Success = false,
                Message = "注册失败,请稍后重试"
            });
        }
    }

    /// <summary>
    /// 用户登出
    /// </summary>
    /// <returns></returns>
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<object>>> Logout()
    {
        _logger.LogInformation("用户登出");

        try
        {
            await _authService.SignOutAsync();

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "登出成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登出时发生错误");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "登出失败,请稍后重试"
            });
        }
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser(
        [FromBody] CreateUserDto dto,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating user: {UserName}", dto.Name);
        
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(new ApiResponse<UserDto>
            {
                Success = false,
                Message = "Validation failed",
                Errors = errors
            });
        }

        try
        {
            var user = await _userService.CreateUserAsync(dto.Name, dto.Email, dto.Phone, cancellationToken);

            // 使用 Dapr 发布用户创建事件到其他服务
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

                _logger.LogInformation("Published user-created event for user {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish user-created event for user {UserId}", user.Id);
                // 不影响主流程，继续返回成功
            }

            // 转换为 DTO
            var userDto = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new ApiResponse<UserDto>
            {
                Success = true,
                Message = "User created successfully",
                Data = userDto
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
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(
        string id,
        [FromBody] UpdateUserDto dto,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating user with ID: {UserId}", id);
        
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(new ApiResponse<UserDto>
            {
                Success = false,
                Message = "Validation failed",
                Errors = errors
            });
        }

        try
        {
            var user = await _userService.UpdateUserAsync(id, dto.Name, dto.Email, dto.Phone, cancellationToken);

            // 转换为 DTO
            var userDto = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };

            return Ok(new ApiResponse<UserDto>
            {
                Success = true,
                Message = "User updated successfully",
                Data = userDto
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
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteUser(
        string id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting user with ID: {UserId}", id);

        var result = await _userService.DeleteUserAsync(id, cancellationToken);

        if (!result)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "User not found"
            });
        }

        // 使用 Dapr 发布用户删除事件
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

            _logger.LogInformation("Published user-deleted event for user {UserId}", id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish user-deleted event for user {UserId}", id);
        }

        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = "User deleted successfully"
        });
    }

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
        _logger.LogInformation("Getting products for user {UserId} via Dapr", userId);

        try
        {
            // 先验证用户是否存在
            var user = await _userService.GetUserByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "User not found"
                });
            }

            // 使用 Dapr gRPC 服务调用 ProductService（性能更优）
            // 使用 HttpMethod.Get 指定 GET 请求
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
            _logger.LogError(ex, "Error getting products for user {UserId}", userId);
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
        _logger.LogInformation("Getting cached user with ID: {UserId}", id);

        try
        {
            // 尝试从 Dapr State Store 获取缓存
            var cachedUserDto = await _daprClient.GetStateAsync<UserDto>(
                storeName: "statestore",
                key: $"user:{id}",
                cancellationToken: cancellationToken);

            if (cachedUserDto != null)
            {
                _logger.LogInformation("User {UserId} found in cache", id);
                return Ok(new ApiResponse<UserDto>
                {
                    Success = true,
                    Message = "User retrieved from cache",
                    Data = cachedUserDto
                });
            }

            // 缓存未命中，从数据库获取
            _logger.LogInformation("User {UserId} not in cache, fetching from database", id);
            var user = await _userService.GetUserByIdAsync(id, cancellationToken);

            if (user == null)
            {
                return NotFound(new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = "User not found"
                });
            }

            // 转换为 DTO
            var userDto = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };

            // 保存到缓存（5分钟过期）
            await _daprClient.SaveStateAsync(
                storeName: "statestore",
                key: $"user:{id}",
                value: userDto,
                metadata: new Dictionary<string, string>
                {
                    { "ttlInSeconds", "300" } // 5分钟过期
                },
                cancellationToken: cancellationToken);

            _logger.LogInformation("User {UserId} cached successfully", id);

            return Ok(new ApiResponse<UserDto>
            {
                Success = true,
                Message = "User retrieved from database and cached",
                Data = userDto
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached user {UserId}", id);
            return StatusCode(500, new ApiResponse<UserDto>
            {
                Success = false,
                Message = "Failed to retrieve user",
                Errors = new List<string> { ex.Message }
            });
        }
    }
}

public class CreateUserDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}

public class UpdateUserDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}

// Dapr 事件模型
public class UserCreatedEvent
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class UserDeletedEvent
{
    public string UserId { get; set; } = string.Empty;
    public DateTime DeletedAt { get; set; }
}