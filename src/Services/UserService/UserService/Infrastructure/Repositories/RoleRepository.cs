using Supabase;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Infrastructure.Repositories;

/// <summary>
///     Role 仓储 Supabase 实现
/// </summary>
public class RoleRepository : IRoleRepository
{
    private readonly ILogger<RoleRepository> _logger;
    private readonly Client _supabaseClient;

    public RoleRepository(Client supabaseClient, ILogger<RoleRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<Role> CreateAsync(Role role, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 创建角色: {Name}", role.Name);

        try
        {
            var result = await _supabaseClient
                .From<Role>()
                .Insert(role, cancellationToken: cancellationToken);

            var createdRole = result.Models.FirstOrDefault();
            if (createdRole == null) throw new InvalidOperationException("创建角色失败");

            _logger.LogInformation("✅ 成功创建角色: {RoleId}", createdRole.Id);
            return createdRole;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建角色失败: {Name}", role.Name);
            throw;
        }
    }

    public async Task<Role?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 根据 ID 查询角色: {RoleId}", id);

        try
        {
            var response = await _supabaseClient
                .From<Role>()
                .Where(r => r.Id == id)
                .Single(cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 未找到角色: {RoleId}", id);
            return null;
        }
    }

    public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 根据名称查询角色: {Name}", name);

        try
        {
            var response = await _supabaseClient
                .From<Role>()
                .Where(r => r.Name == name)
                .Single(cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 未找到角色: {Name}", name);
            return null;
        }
    }

    public async Task<List<Role>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📋 查询所有角色");

        try
        {
            var response = await _supabaseClient
                .From<Role>()
                .Get();

            _logger.LogInformation("✅ 成功查询 {Count} 个角色", response.Models.Count);
            return response.Models.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 查询角色列表失败");
            throw;
        }
    }

    public async Task<Role> UpdateAsync(Role role, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 更新角色: {RoleId}", role.Id);

        try
        {
            var response = await _supabaseClient
                .From<Role>()
                .Where(r => r.Id == role.Id)
                .Update(role, cancellationToken: cancellationToken);

            var updatedRole = response.Models.FirstOrDefault();
            if (updatedRole == null) throw new KeyNotFoundException($"角色不存在: {role.Id}");

            _logger.LogInformation("✅ 成功更新角色: {RoleId}", role.Id);
            return updatedRole;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新角色失败: {RoleId}", role.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🗑️ 删除角色: {RoleId}", id);

        try
        {
            await _supabaseClient
                .From<Role>()
                .Where(r => r.Id == id)
                .Delete();

            _logger.LogInformation("✅ 成功删除角色: {RoleId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除角色失败: {RoleId}", id);
            return false;
        }
    }

    public async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        var role = await GetByIdAsync(id, cancellationToken);
        return role != null;
    }
}