using GoNomads.Shared.Models;
using Supabase;
using Shared.Repositories;

namespace UserService.Repositories;

/// <summary>
/// 用户 Repository - 使用 Supabase API
/// </summary>
public class SupabaseUserRepository : SupabaseRepositoryBase<User>
{
    public SupabaseUserRepository(Client supabaseClient, ILogger<SupabaseUserRepository> logger)
        : base(supabaseClient, logger)
    {
    }

    public async Task<(IEnumerable<User> Users, int TotalCount)> GetUsersAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Fetching users via Supabase API - Page: {Page}, PageSize: {PageSize}", page, pageSize);

        try
        {
            var from = (page - 1) * pageSize;
            var to = from + pageSize - 1;

            // 获取总数
            var countResponse = await SupabaseClient
                .From<User>()
                .Get(cancellationToken);

            var totalCount = countResponse.Models.Count;

            // 获取分页数据
            var response = await SupabaseClient
                .From<User>()
                .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
                .Range(from, to)
                .Get(cancellationToken);

            var users = response.Models;

            Logger.LogInformation("Successfully fetched {Count} users from Supabase", users.Count);
            return (users, totalCount);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching users from Supabase");
            throw;
        }
    }

    public async Task<User?> GetUserByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await GetByIdAsync(id, "id", cancellationToken);
    }

    public async Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Fetching user by Email via Supabase API: {Email}", email);

        try
        {
            var response = await SupabaseClient
                .From<User>()
                .Where(u => u.Email == email)
                .Single(cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error fetching user by email from Supabase: {Email}", email);
            return null;
        }
    }

    public async Task<User> CreateUserAsync(User user, CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Creating user via Supabase API: {Email}", user.Email);

        try
        {
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            return await InsertAsync(user, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating user in Supabase: {Email}", user.Email);
            throw;
        }
    }

    public async Task<User> UpdateUserAsync(User user, CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Updating user via Supabase API: {UserId}", user.Id);

        try
        {
            user.UpdatedAt = DateTime.UtcNow;

            var response = await SupabaseClient
                .From<User>()
                .Where(u => u.Id == user.Id)
                .Set(u => u.Name, user.Name)
                .Set(u => u.Email, user.Email)
                .Set(u => u.Phone!, user.Phone)
                .Set(u => u.UpdatedAt, user.UpdatedAt)
                .Update();

            var updatedUser = response.Models.FirstOrDefault();
            if (updatedUser == null)
            {
                throw new InvalidOperationException($"User not found: {user.Id}");
            }

            Logger.LogInformation("Successfully updated user: {UserId}", user.Id);
            return updatedUser;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating user in Supabase: {UserId}", user.Id);
            throw;
        }
    }

    public async Task<bool> DeleteUserAsync(string id, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync(id, "id", cancellationToken);
    }

    public async Task<bool> UserExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        return await ExistsAsync(id, "id", cancellationToken);
    }
}

