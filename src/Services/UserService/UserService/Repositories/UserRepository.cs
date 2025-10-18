using Microsoft.EntityFrameworkCore;
using GoNomads.Shared.Models;
using UserService.Data;

namespace UserService.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(ApplicationDbContext context, ILogger<UserRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(IEnumerable<User> Users, int TotalCount)> GetUsersAsync(
        int page, 
        int pageSize, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching users - Page: {Page}, PageSize: {PageSize}", page, pageSize);

        var totalCount = await _context.Users.CountAsync(cancellationToken);
        
        var skip = (page - 1) * pageSize;
        var users = await _context.Users
            .OrderByDescending(u => u.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (users, totalCount);
    }

    public async Task<User?> GetUserByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching user by ID: {UserId}", id);
        return await _context.Users.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching user by Email: {Email}", email);
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User> CreateUserAsync(User user, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating user: {UserName}", user.Name);
        
        user.Id = Guid.NewGuid().ToString();
        user.CreatedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        return user;
    }

    public async Task<User> UpdateUserAsync(User user, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating user: {UserId}", user.Id);
        
        user.UpdatedAt = DateTime.UtcNow;
        
        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);

        return user;
    }

    public async Task<bool> DeleteUserAsync(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting user: {UserId}", id);
        
        var user = await GetUserByIdAsync(id, cancellationToken);
        if (user == null)
        {
            return false;
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> UserExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _context.Users.AnyAsync(u => u.Id == id, cancellationToken);
    }
}
