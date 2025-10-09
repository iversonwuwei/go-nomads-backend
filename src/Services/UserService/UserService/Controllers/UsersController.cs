using Microsoft.AspNetCore.Mvc;
using GoNomads.Shared.Models;
using Dapr.Client;

namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private static readonly List<User> Users = new();
    private readonly ILogger<UsersController> _logger;
    private readonly DaprClient _daprClient;

    public UsersController(ILogger<UsersController> logger, DaprClient daprClient)
    {
        _logger = logger;
        _daprClient = daprClient;
        
        // Initialize with sample data
        if (!Users.Any())
        {
            Users.AddRange(new[]
            {
                new User { Id = "1", Name = "John Doe", Email = "john@example.com", Phone = "123-456-7890" },
                new User { Id = "2", Name = "Jane Smith", Email = "jane@example.com", Phone = "098-765-4321" }
            });
        }
    }

    [HttpGet]
    public ActionResult<ApiResponse<PaginatedResponse<User>>> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        _logger.LogInformation("Getting users - Page: {Page}, PageSize: {PageSize}", page, pageSize);
        
        page = Math.Max(1, page);
        pageSize = Math.Max(1, Math.Min(100, pageSize));
        
        var skip = (page - 1) * pageSize;
        var pagedUsers = Users.Skip(skip).Take(pageSize).ToList();

        var response = new ApiResponse<PaginatedResponse<User>>
        {
            Success = true,
            Message = "Users retrieved successfully",
            Data = new PaginatedResponse<User>
            {
                Items = pagedUsers,
                TotalCount = Users.Count,
                Page = page,
                PageSize = pageSize
            }
        };

        return Ok(response);
    }

    [HttpGet("{id}")]
    public ActionResult<ApiResponse<User>> GetUser(string id)
    {
        _logger.LogInformation("Getting user with ID: {UserId}", id);
        
        var user = Users.FirstOrDefault(u => u.Id == id);
        
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
    public ActionResult<ApiResponse<User>> CreateUser([FromBody] CreateUserDto dto)
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

        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Name = dto.Name,
            Email = dto.Email,
            Phone = dto.Phone
        };

        Users.Add(user);

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new ApiResponse<User>
        {
            Success = true,
            Message = "User created successfully",
            Data = user
        });
    }

    [HttpPut("{id}")]
    public ActionResult<ApiResponse<User>> UpdateUser(string id, [FromBody] UpdateUserDto dto)
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

        var user = Users.FirstOrDefault(u => u.Id == id);
        
        if (user == null)
        {
            return NotFound(new ApiResponse<User>
            {
                Success = false,
                Message = "User not found"
            });
        }

        user.Name = dto.Name;
        user.Email = dto.Email;
        user.Phone = dto.Phone;
        user.UpdatedAt = DateTime.UtcNow;

        return Ok(new ApiResponse<User>
        {
            Success = true,
            Message = "User updated successfully",
            Data = user
        });
    }

    [HttpDelete("{id}")]
    public ActionResult<ApiResponse<object>> DeleteUser(string id)
    {
        _logger.LogInformation("Deleting user with ID: {UserId}", id);
        
        var user = Users.FirstOrDefault(u => u.Id == id);
        
        if (user == null)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = "User not found"
            });
        }

        Users.Remove(user);

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