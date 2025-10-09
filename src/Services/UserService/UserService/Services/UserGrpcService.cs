using Grpc.Core;
using GoNomads.Shared.Grpc;
using GoNomads.Shared.Models;

// Type aliases to resolve naming conflicts
using DomainUser = GoNomads.Shared.Models.User;
using GrpcUser = GoNomads.Shared.Grpc.User;

namespace UserService.Services;

public class UserGrpcService : GoNomads.Shared.Grpc.UserService.UserServiceBase
{
    private static readonly List<DomainUser> Users = new();
    private readonly ILogger<UserGrpcService> _logger;

    public UserGrpcService(ILogger<UserGrpcService> logger)
    {
        _logger = logger;
        
        // Initialize with sample data
        if (!Users.Any())
        {
            Users.AddRange(new[]
            {
                new DomainUser { Id = "1", Name = "John Doe", Email = "john@example.com", Phone = "123-456-7890" },
                new DomainUser { Id = "2", Name = "Jane Smith", Email = "jane@example.com", Phone = "098-765-4321" }
            });
        }
    }

    public override Task<UserResponse> GetUser(GetUserRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Getting user with ID: {UserId}", request.Id);
        
        var user = Users.FirstOrDefault(u => u.Id == request.Id);
        
        if (user == null)
        {
            return Task.FromResult(new UserResponse
            {
                Success = false,
                Message = "User not found"
            });
        }

        return Task.FromResult(new UserResponse
        {
            User = new GrpcUser
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone ?? "",
                CreatedAt = user.CreatedAt.Ticks,
                UpdatedAt = user.UpdatedAt.Ticks
            },
            Success = true,
            Message = "User retrieved successfully"
        });
    }

    public override Task<UserResponse> CreateUser(CreateUserRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Creating user: {UserName}", request.Name);
        
        var user = new DomainUser
        {
            Id = Guid.NewGuid().ToString(),
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone
        };

        Users.Add(user);

        return Task.FromResult(new UserResponse
        {
            User = new GrpcUser
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone ?? "",
                CreatedAt = user.CreatedAt.Ticks,
                UpdatedAt = user.UpdatedAt.Ticks
            },
            Success = true,
            Message = "User created successfully"
        });
    }

    public override Task<UserResponse> UpdateUser(UpdateUserRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Updating user with ID: {UserId}", request.Id);
        
        var user = Users.FirstOrDefault(u => u.Id == request.Id);
        
        if (user == null)
        {
            return Task.FromResult(new UserResponse
            {
                Success = false,
                Message = "User not found"
            });
        }

        user.Name = request.Name;
        user.Email = request.Email;
        user.Phone = request.Phone;
        user.UpdatedAt = DateTime.UtcNow;

        return Task.FromResult(new UserResponse
        {
            User = new GrpcUser
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone ?? "",
                CreatedAt = user.CreatedAt.Ticks,
                UpdatedAt = user.UpdatedAt.Ticks
            },
            Success = true,
            Message = "User updated successfully"
        });
    }

    public override Task<DeleteUserResponse> DeleteUser(DeleteUserRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Deleting user with ID: {UserId}", request.Id);
        
        var user = Users.FirstOrDefault(u => u.Id == request.Id);
        
        if (user == null)
        {
            return Task.FromResult(new DeleteUserResponse
            {
                Success = false,
                Message = "User not found"
            });
        }

        Users.Remove(user);

        return Task.FromResult(new DeleteUserResponse
        {
            Success = true,
            Message = "User deleted successfully"
        });
    }

    public override Task<ListUsersResponse> ListUsers(ListUsersRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Listing users - Page: {Page}, PageSize: {PageSize}", request.Page, request.PageSize);
        
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Max(1, Math.Min(100, request.PageSize));
        
        var skip = (page - 1) * pageSize;
        var pagedUsers = Users.Skip(skip).Take(pageSize).ToList();

        var response = new ListUsersResponse
        {
            TotalCount = Users.Count,
            Page = page,
            PageSize = pageSize
        };

        foreach (var user in pagedUsers)
        {
            response.Users.Add(new GrpcUser
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone ?? "",
                CreatedAt = user.CreatedAt.Ticks,
                UpdatedAt = user.UpdatedAt.Ticks
            });
        }

        return Task.FromResult(response);
    }
}