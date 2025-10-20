using GoNomads.Shared.Models;
using UserService.Repositories;

namespace UserService.Services;

public class UserServiceImpl : IUserService
{
    private readonly SupabaseUserRepository _userRepository;
    private readonly ILogger<UserServiceImpl> _logger;

    public UserServiceImpl(SupabaseUserRepository userRepository, ILogger<UserServiceImpl> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<(IEnumerable<User> Users, int TotalCount)> GetUsersAsync(
        int page, 
        int pageSize, 
        CancellationToken cancellationToken = default)
    {
        return await _userRepository.GetUsersAsync(page, pageSize, cancellationToken);
    }

    public async Task<User?> GetUserByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _userRepository.GetUserByIdAsync(id, cancellationToken);
    }

    public async Task<User> CreateUserAsync(
        string name, 
        string email, 
        string? phone, 
        CancellationToken cancellationToken = default)
    {
        // Check if email already exists
        var existingUser = await _userRepository.GetUserByEmailAsync(email, cancellationToken);
        if (existingUser != null)
        {
            throw new InvalidOperationException($"User with email '{email}' already exists");
        }

        var user = new User
        {
            Name = name,
            Email = email,
            Phone = phone
        };

        return await _userRepository.CreateUserAsync(user, cancellationToken);
    }

    public async Task<User> UpdateUserAsync(
        string id, 
        string name, 
        string email, 
        string? phone, 
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetUserByIdAsync(id, cancellationToken);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID '{id}' not found");
        }

        // Check if email is being changed to an existing email
        if (user.Email != email)
        {
            var existingUser = await _userRepository.GetUserByEmailAsync(email, cancellationToken);
            if (existingUser != null && existingUser.Id != id)
            {
                throw new InvalidOperationException($"User with email '{email}' already exists");
            }
        }

        user.Name = name;
        user.Email = email;
        user.Phone = phone;

        return await _userRepository.UpdateUserAsync(user, cancellationToken);
    }

    public async Task<bool> DeleteUserAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _userRepository.DeleteUserAsync(id, cancellationToken);
    }

    public async Task<bool> UserExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _userRepository.UserExistsAsync(id, cancellationToken);
    }
}
