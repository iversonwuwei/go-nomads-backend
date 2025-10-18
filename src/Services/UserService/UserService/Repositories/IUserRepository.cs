using GoNomads.Shared.Models;

namespace UserService.Repositories;

public interface IUserRepository
{
    Task<(IEnumerable<User> Users, int TotalCount)> GetUsersAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<User?> GetUserByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User> CreateUserAsync(User user, CancellationToken cancellationToken = default);
    Task<User> UpdateUserAsync(User user, CancellationToken cancellationToken = default);
    Task<bool> DeleteUserAsync(string id, CancellationToken cancellationToken = default);
    Task<bool> UserExistsAsync(string id, CancellationToken cancellationToken = default);
}
