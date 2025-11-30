using Postgrest;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;
using Client = Supabase.Client;

namespace UserService.Infrastructure.Repositories;

/// <summary>
///     User 仓储 Supabase 实现
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly ILogger<UserRepository> _logger;
    private readonly Client _supabaseClient;

    public UserRepository(Client supabaseClient, ILogger<UserRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 创建用户: {Email}", user.Email);

        try
        {
            var result = await _supabaseClient
                .From<User>()
                .Insert(user, cancellationToken: cancellationToken);

            var createdUser = result.Models.FirstOrDefault();
            if (createdUser == null) throw new InvalidOperationException("创建用户失败");

            _logger.LogInformation("✅ 成功创建用户: {UserId}", createdUser.Id);
            return createdUser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建用户失败: {Email}", user.Email);
            throw;
        }
    }

    public async Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 根据 ID 查询用户: {UserId}", id);

        try
        {
            var response = await _supabaseClient
                .From<User>()
                .Where(u => u.Id == id)
                .Single(cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 未找到用户: {UserId}", id);
            return null;
        }
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 根据邮箱查询用户: {Email}", email);

        try
        {
            var response = await _supabaseClient
                .From<User>()
                .Where(u => u.Email == email)
                .Single(cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 未找到用户: {Email}", email);
            return null;
        }
    }

    public async Task<UserWithRole?> GetByEmailWithRoleAsync(string email, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 根据邮箱查询用户(含角色): {Email}", email);

        try
        {
            // 使用 Select 显式声明 JOIN 查询，一次性获取用户和角色
            var response = await _supabaseClient
                .From<UserWithRole>()
                .Select("*, role:roles(*)")  // 关联查询 roles 表
                .Where(u => u.Email == email)
                .Single(cancellationToken);

            if (response != null)
            {
                _logger.LogInformation("✅ 找到用户: {Email}, 角色: {Role}", email, response.RoleName);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 未找到用户: {Email}", email);
            return null;
        }
    }

    public async Task<UserWithRole?> GetByIdWithRoleAsync(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 根据ID查询用户(含角色): {UserId}", id);

        try
        {
            // 使用 Select 显式声明 JOIN 查询，一次性获取用户和角色
            var response = await _supabaseClient
                .From<UserWithRole>()
                .Select("*, role:roles(*)")  // 关联查询 roles 表
                .Where(u => u.Id == id)
                .Single(cancellationToken);

            if (response != null)
            {
                _logger.LogInformation("✅ 找到用户: {UserId}, 角色: {Role}", id, response.RoleName);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 未找到用户: {UserId}", id);
            return null;
        }
    }

    public async Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 更新用户: {UserId}", user.Id);

        try
        {
            var response = await _supabaseClient
                .From<User>()
                .Where(u => u.Id == user.Id)
                .Update(user, cancellationToken: cancellationToken);

            var updatedUser = response.Models.FirstOrDefault();
            if (updatedUser == null) throw new KeyNotFoundException($"用户不存在: {user.Id}");

            _logger.LogInformation("✅ 成功更新用户: {UserId}", user.Id);
            return updatedUser;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新用户失败: {UserId}", user.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🗑️ 删除用户: {UserId}", id);

        try
        {
            await _supabaseClient
                .From<User>()
                .Where(u => u.Id == id)
                .Delete();

            _logger.LogInformation("✅ 成功删除用户: {UserId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 删除用户失败: {UserId}", id);
            return false;
        }
    }

    public async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        var user = await GetByIdAsync(id, cancellationToken);
        return user != null;
    }

    public async Task<(List<User> Users, int Total)> GetListAsync(
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📋 查询用户列表 - Page: {Page}, PageSize: {PageSize}", page, pageSize);

        try
        {
            var from = (page - 1) * pageSize;
            var to = from + pageSize - 1;

            // 获取分页数据（Supabase 会在响应头中返回总数）
            var response = await _supabaseClient
                .From<User>()
                .Order(u => u.CreatedAt, Constants.Ordering.Descending)
                .Range(from, to)
                .Get();

            // 从响应中获取总数（如果可用），否则使用当前页的数量
            var total = response.Models.Count;

            _logger.LogInformation("✅ 成功查询 {Count} 个用户", response.Models.Count);
            return (response.Models.ToList(), total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 查询用户列表失败");
            throw;
        }
    }

    public async Task<(List<User> Users, int Total)> SearchAsync(
        string? searchTerm = null,
        string? role = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 搜索用户 - SearchTerm: {SearchTerm}, Role: {Role}, Page: {Page}, PageSize: {PageSize}",
            searchTerm, role, page, pageSize);

        try
        {
            var from = (page - 1) * pageSize;
            var to = from + pageSize - 1;

            // 根据不同条件组合进行查询
            if (!string.IsNullOrWhiteSpace(searchTerm) && !string.IsNullOrWhiteSpace(role))
            {
                // 同时有搜索词和角色筛选
                var response = await _supabaseClient
                    .From<User>()
                    .Filter("name", Constants.Operator.ILike, $"%{searchTerm}%")
                    .Filter("role", Constants.Operator.Equals, role)
                    .Order(u => u.CreatedAt, Constants.Ordering.Descending)
                    .Range(from, to)
                    .Get(cancellationToken);

                _logger.LogInformation("✅ 搜索到 {Count} 个用户", response.Models.Count);
                return (response.Models.ToList(), response.Models.Count);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                // 只有搜索词（搜索名称）
                var response = await _supabaseClient
                    .From<User>()
                    .Filter("name", Constants.Operator.ILike, $"%{searchTerm}%")
                    .Order(u => u.CreatedAt, Constants.Ordering.Descending)
                    .Range(from, to)
                    .Get(cancellationToken);

                _logger.LogInformation("✅ 搜索到 {Count} 个用户", response.Models.Count);
                return (response.Models.ToList(), response.Models.Count);
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                // 只有角色筛选
                var response = await _supabaseClient
                    .From<User>()
                    .Filter("role", Constants.Operator.Equals, role)
                    .Order(u => u.CreatedAt, Constants.Ordering.Descending)
                    .Range(from, to)
                    .Get(cancellationToken);

                _logger.LogInformation("✅ 搜索到 {Count} 个用户", response.Models.Count);
                return (response.Models.ToList(), response.Models.Count);
            }
            else
            {
                // 无筛选条件，返回所有用户
                var response = await _supabaseClient
                    .From<User>()
                    .Order(u => u.CreatedAt, Constants.Ordering.Descending)
                    .Range(from, to)
                    .Get(cancellationToken);

                _logger.LogInformation("✅ 搜索到 {Count} 个用户", response.Models.Count);
                return (response.Models.ToList(), response.Models.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 搜索用户失败 - SearchTerm: {SearchTerm}", searchTerm);
            throw;
        }
    }

    public async Task<List<User>> GetUsersByRoleIdAsync(string roleId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 根据角色ID查询用户: {RoleId}", roleId);

        try
        {
            var response = await _supabaseClient
                .From<User>()
                .Filter("role_id", Constants.Operator.Equals, roleId)
                .Get(cancellationToken);

            _logger.LogInformation("✅ 找到 {Count} 个用户", response.Models.Count);
            return response.Models.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 根据角色ID查询用户失败: {RoleId}", roleId);
            throw;
        }
    }
}