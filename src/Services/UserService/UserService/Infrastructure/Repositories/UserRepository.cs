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

    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _supabaseClient
                .From<User>()
                .Count(Constants.CountType.Exact);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取用户总数失败");
            throw;
        }
    }

    public async Task<int> GetCreatedSinceCountAsync(DateTime createdSinceUtc, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _supabaseClient
                .From<User>()
                .Filter("created_at", Constants.Operator.GreaterThanOrEqual, createdSinceUtc.ToString("O"))
                .Count(Constants.CountType.Exact);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取新增用户数失败: CreatedSince={CreatedSince}", createdSinceUtc);
            throw;
        }
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

    public async Task<User?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 根据手机号查询用户: {Phone}", MaskPhoneNumber(phone));

        try
        {
            var response = await _supabaseClient
                .From<User>()
                .Where(u => u.Phone == phone)
                .Single(cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 未找到用户: {Phone}", MaskPhoneNumber(phone));
            return null;
        }
    }

    /// <summary>
    ///     脱敏手机号
    /// </summary>
    private static string MaskPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 7)
            return "***";
        return phoneNumber[..3] + "****" + phoneNumber[^4..];
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

            var total = await _supabaseClient
                .From<User>()
                .Count(Constants.CountType.Exact);

            var response = await _supabaseClient
                .From<User>()
                .Order(u => u.CreatedAt, Constants.Ordering.Descending)
                .Range(from, to)
                .Get(cancellationToken);

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

            var countQuery = (Supabase.Interfaces.ISupabaseTable<User, Supabase.Realtime.RealtimeChannel>)_supabaseClient.From<User>();
            var dataQuery = (Supabase.Interfaces.ISupabaseTable<User, Supabase.Realtime.RealtimeChannel>)_supabaseClient.From<User>();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                countQuery = (Supabase.Interfaces.ISupabaseTable<User, Supabase.Realtime.RealtimeChannel>)countQuery.Filter("name", Constants.Operator.ILike, $"%{searchTerm}%");
                dataQuery = (Supabase.Interfaces.ISupabaseTable<User, Supabase.Realtime.RealtimeChannel>)dataQuery.Filter("name", Constants.Operator.ILike, $"%{searchTerm}%");
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                countQuery = (Supabase.Interfaces.ISupabaseTable<User, Supabase.Realtime.RealtimeChannel>)countQuery.Filter("role", Constants.Operator.Equals, role);
                dataQuery = (Supabase.Interfaces.ISupabaseTable<User, Supabase.Realtime.RealtimeChannel>)dataQuery.Filter("role", Constants.Operator.Equals, role);
            }

            var total = await countQuery.Count(Constants.CountType.Exact);

            var response = await dataQuery
                .Order(u => u.CreatedAt, Constants.Ordering.Descending)
                .Range(from, to)
                .Get(cancellationToken);

            _logger.LogInformation("✅ 搜索到 {Count}/{Total} 个用户", response.Models.Count, total);
            return (response.Models.ToList(), total);
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

    public async Task<(List<UserWithMembership> Users, int Total)> GetModeratorCandidatesAsync(
        string? searchTerm = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "👥 获取版主候选人列表 - SearchTerm: {SearchTerm}, Page: {Page}, PageSize: {PageSize}",
            searchTerm, page, pageSize);

        try
        {
            var from = (page - 1) * pageSize;
            var to = from + pageSize - 1;

            // 第一步：获取所有用户
            var userQuery = _supabaseClient
                .From<User>()
                .Order(u => u.CreatedAt, Constants.Ordering.Descending);

            // 如果有搜索词，添加名称过滤
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                userQuery = userQuery.Filter("name", Constants.Operator.ILike, $"%{searchTerm}%");
            }

            var usersResponse = await userQuery.Get(cancellationToken);
            var users = usersResponse.Models.ToList();

            _logger.LogInformation("👥 查询到 {Count} 个用户", users.Count);

            if (users.Count == 0)
            {
                return (new List<UserWithMembership>(), 0);
            }

            // 第二步：批量获取这些用户的会员信息
            var userIds = users.Select(u => u.Id).ToList();
            var membershipsResponse = await _supabaseClient
                .From<Membership>()
                .Filter("user_id", Constants.Operator.In, userIds)
                .Get(cancellationToken);

            var memberships = membershipsResponse.Models.ToList();
            _logger.LogInformation("👥 查询到 {Count} 条会员记录", memberships.Count);

            // 第三步：批量获取角色信息
            var roleIds = users.Select(u => u.RoleId).Distinct().ToList();
            var rolesResponse = await _supabaseClient
                .From<Role>()
                .Filter("id", Constants.Operator.In, roleIds)
                .Get(cancellationToken);

            var rolesById = rolesResponse.Models.ToDictionary(r => r.Id, r => r);
            _logger.LogInformation("👥 查询到 {Count} 个角色", rolesById.Count);

            // 按用户ID分组会员信息
            var membershipsByUserId = memberships
                .GroupBy(m => m.UserId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // 第四步：组装 UserWithMembership 对象并过滤
            var candidates = new List<UserWithMembership>();
            foreach (var user in users)
            {
                var userMemberships = membershipsByUserId.GetValueOrDefault(user.Id);
                var role = rolesById.GetValueOrDefault(user.RoleId);

                var userWithMembership = new UserWithMembership
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    AvatarUrl = user.AvatarUrl,
                    RoleId = user.RoleId,
                    CreatedAt = user.CreatedAt,
                    Role = role,
                    Memberships = userMemberships
                };

                // 只保留 Pro 及以上会员或 Admin 用户
                if (userWithMembership.CanBeModeratorCandidate)
                {
                    candidates.Add(userWithMembership);
                }
            }

            _logger.LogInformation("✅ 版主候选人总数: {Total}", candidates.Count);

            // 手动分页
            var pagedCandidates = candidates
                .Skip(from)
                .Take(pageSize)
                .ToList();

            return (pagedCandidates, candidates.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取版主候选人列表失败");
            throw;
        }
    }

    public async Task<User?> GetBySocialLoginAsync(string provider, string openId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 根据社交登录查询用户: Provider={Provider}, OpenId={OpenId}",
            provider, MaskSocialOpenId(openId));

        try
        {
            var normalizedProvider = provider.ToLower();
            var response = await _supabaseClient
                .From<User>()
                .Where(u => u.SocialProvider == normalizedProvider && u.SocialOpenId == openId)
                .Single(cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 未找到社交登录用户: Provider={Provider}, OpenId={OpenId}",
                provider, MaskSocialOpenId(openId));
            return null;
        }
    }

    /// <summary>
    ///     脱敏社交平台 OpenId
    /// </summary>
    private static string MaskSocialOpenId(string openId)
    {
        if (string.IsNullOrEmpty(openId) || openId.Length < 8)
            return "***";
        return openId[..4] + "****" + openId[^4..];
    }
}