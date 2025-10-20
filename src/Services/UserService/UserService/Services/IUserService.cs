using GoNomads.Shared.Models;

namespace UserService.Services;

public interface IUserService
{
    Task<(IEnumerable<User> Users, int TotalCount)> GetUsersAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<User?> GetUserByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<User> CreateUserAsync(string name, string email, string phone, CancellationToken cancellationToken = default);
    Task<User> UpdateUserAsync(string id, string name, string email, string phone, CancellationToken cancellationToken = default);
    Task<bool> DeleteUserAsync(string id, CancellationToken cancellationToken = default);
    Task<bool> UserExistsAsync(string id, CancellationToken cancellationToken = default);
}
