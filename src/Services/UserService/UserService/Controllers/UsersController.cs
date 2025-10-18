using Microsoft.AspNetCore.Mvc;
using GoNomads.Shared.Models;
using Dapr.Client;

namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ILogger<UsersController> _logger;
    private readonly DaprClient _daprClient;
    private readonly Services.IUserService _userService;

    public UsersController(
        ILogger<UsersController> logger,
        DaprClient daprClient,
        Services.IUserService userService)
    {
        _logger = logger;
        _daprClient = daprClient;
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<User>>>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting users - Page: {Page}, PageSize: {PageSize}", page, pageSize);
        
        page = Math.Max(1, page);
        pageSize = Math.Max(1, Math.Min(100, pageSize));

        var (users, totalCount) = await _userService.GetUsersAsync(page, pageSize, cancellationToken);

        var response = new ApiResponse<PaginatedResponse<User>>
        {
            Success = true,
            Message = "Users retrieved successfully",
            Data = new PaginatedResponse<User>
            {
                Items = users.ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            }
        };

        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<User>>> GetUser(
        string id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting user with ID: {UserId}", id);

        var user = await _userService.GetUserByIdAsync(id, cancellationToken);

        if (user == null)
        {
            return NotFound(new ApiResponse<User>
            {
                Success = false,
                Message = "User not found"
            });
        }

        return Ok(new ApiResponse<User>
        {
            Success = true,
            Message = "User retrieved successfully",
            Data = user
        });
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<User>>> CreateUser(
        [FromBody] CreateUserDto dto,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating user: {UserName}", dto.Name);
        
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(new ApiResponse<User>
            {
                Success = false,
                Message = "Validation failed",
                Errors = errors
            });
        }

        try
        {
            var user = await _userService.CreateUserAsync(dto.Name, dto.Email, dto.Phone, cancellationToken);

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new ApiResponse<User>
            {
                Success = true,
                Message = "User created successfully",
                Data = user
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<User>
            {
                Success = false,
                Message = ex.Message
            });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<User>>> UpdateUser(
        string id,
        [FromBody] UpdateUserDto dto,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating user with ID: {UserId}", id);
        
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(new ApiResponse<User>
            {
                Success = false,
                Message = "Validation failed",
                Errors = errors
            });
        }

        try
        {
            var user = await _userService.UpdateUserAsync(id, dto.Name, dto.Email, dto.Phone, cancellationToken);

            return Ok(new ApiResponse<User>
            {
                Success = true,
                Message = "User updated successfully",
                Data = user
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<User>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<User>
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
}

public class CreateUserDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

public class UpdateUserDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
}