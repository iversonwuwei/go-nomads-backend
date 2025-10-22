using GoNomads.Shared.Models;
using Supabase;
using Shared.Repositories;

namespace UserService.Repositories;

/// <summary>
/// 角色 Repository - 使用 Supabase API
/// </summary>
public class RoleRepository : SupabaseRepositoryBase<Role>, IRoleRepository
{
    public RoleRepository(Client supabaseClient, ILogger<RoleRepository> logger)
        : base(supabaseClient, logger)
    {
    }

    public async Task<List<Role>> GetAllRolesAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Fetching all roles via Supabase API");

        try
        {
            var roles = await GetAllAsync(cancellationToken);
            return roles.ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting all roles");
            throw;
        }
    }

    public async Task<Role?> GetRoleByIdAsync(string roleId, CancellationToken cancellationToken = default)
    {
        return await GetByIdAsync(roleId, "id", cancellationToken);
    }

    public async Task<Role?> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Fetching role by name via Supabase API: {RoleName}", roleName);

        try
        {
            var response = await SupabaseClient
                .From<Role>()
                .Where(r => r.Name == roleName)
                .Single(cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting role by name: {RoleName}", roleName);
            return null;
        }
    }

    public async Task<Role> CreateRoleAsync(Role role, CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Creating role via Supabase API: {RoleName}", role.Name);

        try
        {
            role.CreatedAt = DateTime.UtcNow;
            role.UpdatedAt = DateTime.UtcNow;

            return await InsertAsync(role, cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error creating role: {RoleName}", role.Name);
            throw;
        }
    }

    public async Task<Role> UpdateRoleAsync(Role role, CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Updating role via Supabase API: {RoleId}", role.Id);

        try
        {
            role.UpdatedAt = DateTime.UtcNow;

            var response = await SupabaseClient
                .From<Role>()
                .Where(r => r.Id == role.Id)
                .Set(r => r.Name, role.Name)
                .Set(r => r.Description!, role.Description)
                .Set(r => r.UpdatedAt, role.UpdatedAt)
                .Update();

            var updatedRole = response.Models.FirstOrDefault();
            if (updatedRole == null)
            {
                throw new InvalidOperationException($"Role not found: {role.Id}");
            }

            Logger.LogInformation("Successfully updated role: {RoleId}", role.Id);
            return updatedRole;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating role: {RoleId}", role.Id);
            throw;
        }
    }

    public async Task<bool> DeleteRoleAsync(string roleId, CancellationToken cancellationToken = default)
    {
        return await DeleteAsync(roleId, "id", cancellationToken);
    }

    public async Task<bool> RoleExistsAsync(string roleId, CancellationToken cancellationToken = default)
    {
        return await ExistsAsync(roleId, "id", cancellationToken);
    }
}
