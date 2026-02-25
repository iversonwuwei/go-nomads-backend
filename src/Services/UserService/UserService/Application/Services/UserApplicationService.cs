using System.Collections.Concurrent;
using MassTransit;
using Shared.Messages;
using UserService.Application.DTOs;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Application.Services;

/// <summary>
///     User 应用服务实现 - 协调领域对象和仓储
/// </summary>
public class UserApplicationService : IUserService
{
    private readonly IInterestService _interestService;
    private readonly ILogger<UserApplicationService> _logger;
    private readonly IMembershipService _membershipService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IRoleRepository _roleRepository;
    private readonly ISkillService _skillService;
    private readonly ITravelHistoryService _travelHistoryService;
    private readonly IUserRepository _userRepository;

    // 角色缓存（角色数据几乎不变，缓存 10 分钟）
    private static readonly ConcurrentDictionary<string, (string RoleName, DateTime CachedAt)> _roleCache = new();
    private static readonly TimeSpan _roleCacheDuration = TimeSpan.FromMinutes(10);

    public UserApplicationService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        ISkillService skillService,
        IInterestService interestService,
        IMembershipService membershipService,
        ITravelHistoryService travelHistoryService,
        IPublishEndpoint publishEndpoint,
        ILogger<UserApplicationService> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _skillService = skillService;
        _interestService = interestService;
        _membershipService = membershipService;
        _travelHistoryService = travelHistoryService;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task<(List<UserDto> Users, int Total)> GetUsersAsync(
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📋 获取用户列表 - Page: {Page}, PageSize: {PageSize}", page, pageSize);

        var (users, total) = await _userRepository.GetListAsync(page, pageSize, cancellationToken);

        var userDtos = new List<UserDto>();
        foreach (var user in users) userDtos.Add(await MapToDtoAsync(user, cancellationToken));

        return (userDtos, total);
    }

    public async Task<(List<UserDto> Users, int Total)> SearchUsersAsync(
        string? searchTerm = null,
        string? role = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 搜索用户 - SearchTerm: {SearchTerm}, Role: {Role}, Page: {Page}, PageSize: {PageSize}",
            searchTerm, role, page, pageSize);

        var (users, total) = await _userRepository.SearchAsync(searchTerm, role, page, pageSize, cancellationToken);

        var userDtos = new List<UserDto>();
        foreach (var user in users) userDtos.Add(await MapToDtoAsync(user, cancellationToken));

        _logger.LogInformation("✅ 搜索结果: {Count}/{Total} 个用户", userDtos.Count, total);
        return (userDtos, total);
    }

    public async Task<UserDto?> GetUserByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user == null) return null;

        var userDto = await MapToDtoAsync(user, cancellationToken);

        // 并行加载所有附加数据（技能、兴趣、旅行数据）
        var skillsTask = _skillService.GetUserSkillsAsync(id, cancellationToken);
        var interestsTask = _interestService.GetUserInterestsAsync(id, cancellationToken);
        var latestTravelTask = _travelHistoryService.GetLatestTravelHistoryAsync(id, cancellationToken);
        var confirmedTravelTask = _travelHistoryService.GetConfirmedTravelHistoryAsync(id, cancellationToken);
        var travelStatsTask = _travelHistoryService.GetUserTravelStatsAsync(id, cancellationToken);

        await Task.WhenAll(skillsTask, interestsTask, latestTravelTask, confirmedTravelTask, travelStatsTask);

        // 技能和兴趣
        try
        {
            userDto.Skills = await skillsTask;
            userDto.Interests = await interestsTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 加载用户技能/兴趣失败: UserId={UserId}", id);
            userDto.Skills = new List<UserSkillDto>();
            userDto.Interests = new List<UserInterestDto>();
        }

        // 最新旅行历史
        try
        {
            userDto.LatestTravelHistory = await latestTravelTask;
            _logger.LogInformation("📍 用户最新旅行历史: UserId={UserId}, HasData={HasData}, City={City}",
                id,
                userDto.LatestTravelHistory != null,
                userDto.LatestTravelHistory?.City ?? "null");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 加载用户最新旅行历史失败: UserId={UserId}", id);
            userDto.LatestTravelHistory = null;
        }

        // 旅行历史列表
        try
        {
            userDto.TravelHistory = await confirmedTravelTask;
            _logger.LogInformation("📜 用户旅行历史列表: UserId={UserId}, Count={Count}",
                id, userDto.TravelHistory?.Count ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 加载用户旅行历史列表失败: UserId={UserId}", id);
            userDto.TravelHistory = new List<DTOs.TravelHistoryDto>();
        }

        // 旅行统计
        try
        {
            var travelStats = await travelStatsTask;
            userDto.Stats = new UserTravelStatsDto
            {
                CountriesVisited = travelStats.CountriesVisited,
                CitiesVisited = travelStats.CitiesVisited,
                TotalDays = travelStats.TotalDays,
                TotalTrips = travelStats.ConfirmedTrips
            };
            _logger.LogInformation("📊 用户旅行统计: UserId={UserId}, Countries={Countries}, Cities={Cities}",
                id, travelStats.CountriesVisited, travelStats.CitiesVisited);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 加载用户旅行统计失败: UserId={UserId}", id);
            userDto.Stats = new UserTravelStatsDto
            {
                CountriesVisited = 0,
                CitiesVisited = 0,
                TotalDays = 0,
                TotalTrips = 0
            };
        }

        return userDto;
    }

    public async Task<List<UserDto>> GetUsersByIdsAsync(List<string> ids, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📋 批量获取用户 - Count: {Count}", ids.Count);

        if (ids == null || ids.Count == 0) return new List<UserDto>();

        // 单次 IN 查询获取所有用户
        var userEntities = await _userRepository.GetByIdsAsync(ids.Distinct().ToList(), cancellationToken);

        // 并行映射为 DTO
        var mapTasks = userEntities.Select(u => MapToDtoAsync(u, cancellationToken));
        var users = (await Task.WhenAll(mapTasks)).ToList();

        _logger.LogInformation("✅ 成功获取 {Count}/{Total} 个用户", users.Count, ids.Count);
        return users;
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        return user == null ? null : await MapToDtoAsync(user, cancellationToken);
    }

    public async Task<UserDto> CreateUserAsync(
        string name,
        string email,
        string phone,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 创建用户（无密码）: {Email}", email);

        // 检查邮箱是否已存在
        var existingUser = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (existingUser != null) throw new InvalidOperationException($"邮箱 '{email}' 已被注册");

        // 获取默认角色
        var defaultRole = await _roleRepository.GetByNameAsync(Role.RoleNames.User, cancellationToken);
        if (defaultRole == null)
        {
            _logger.LogError("❌ 默认角色 'user' 不存在");
            throw new InvalidOperationException("系统配置错误: 默认用户角色不存在");
        }

        // 使用领域工厂方法创建用户
        var user = User.Create(name, email, phone, defaultRole.Id);

        // 持久化
        var createdUser = await _userRepository.CreateAsync(user, cancellationToken);

        _logger.LogInformation("✅ 成功创建用户: {UserId}", createdUser.Id);
        return await MapToDtoAsync(createdUser, cancellationToken);
    }

    public async Task<UserDto> CreateUserWithPasswordAsync(
        string name,
        string email,
        string password,
        string phone,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 创建用户（带密码）: {Email}", email);

        // 检查邮箱是否已存在
        var existingUser = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (existingUser != null) throw new InvalidOperationException($"邮箱 '{email}' 已被注册");

        // 获取默认角色
        var defaultRole = await _roleRepository.GetByNameAsync(Role.RoleNames.User, cancellationToken);
        if (defaultRole == null)
        {
            _logger.LogError("❌ 默认角色 'user' 不存在");
            throw new InvalidOperationException("系统配置错误: 默认用户角色不存在");
        }

        // 使用领域工厂方法创建用户（带密码）
        var user = User.CreateWithPassword(name, email, password, phone, defaultRole.Id);

        // 持久化
        var createdUser = await _userRepository.CreateAsync(user, cancellationToken);

        _logger.LogInformation("✅ 成功创建用户: {UserId}", createdUser.Id);
        return await MapToDtoAsync(createdUser, cancellationToken);
    }

    public async Task<UserDto> UpdateUserAsync(
        string id,
        string? name = null,
        string? email = null,
        string? phone = null,
        string? avatarUrl = null,
        string? bio = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 更新用户: {UserId}", id);

        // 获取用户
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user == null) throw new KeyNotFoundException($"用户不存在: {id}");

        // 如果要更新邮箱，检查是否被其他用户使用
        if (email != null && user.Email != email)
        {
            var existingUser = await _userRepository.GetByEmailAsync(email, cancellationToken);
            if (existingUser != null && existingUser.Id != id)
                throw new InvalidOperationException($"邮箱 '{email}' 已被其他用户使用");
        }

        // 记录更新的字段（用于事件通知）
        var updatedFields = new List<string>();
        if (name != null && name != user.Name) updatedFields.Add("name");
        if (email != null && email != user.Email) updatedFields.Add("email");
        if (avatarUrl != null && avatarUrl != user.AvatarUrl) updatedFields.Add("avatarUrl");

        // 使用领域方法进行部分更新（只更新非null字段）
        user.PartialUpdate(name, email, phone, avatarUrl, bio);

        // 持久化
        var updatedUser = await _userRepository.UpdateAsync(user, cancellationToken);

        // 如果更新了 name 或 avatarUrl，发布 UserUpdatedMessage 事件
        if (updatedFields.Contains("name") || updatedFields.Contains("avatarUrl"))
        {
            try
            {
                var message = new UserUpdatedMessage
                {
                    UserId = id,
                    Name = updatedUser.Name,
                    AvatarUrl = updatedUser.AvatarUrl,
                    Email = updatedUser.Email,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedFields = updatedFields
                };

                await _publishEndpoint.Publish(message, cancellationToken);
                _logger.LogInformation("📤 已发布用户更新事件: UserId={UserId}, UpdatedFields=[{Fields}]",
                    id, string.Join(", ", updatedFields));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ 发布用户更新事件失败: UserId={UserId}", id);
                // 不抛出异常，用户更新已成功，事件发布失败不影响主流程
            }
        }

        _logger.LogInformation("✅ 成功更新用户: {UserId}", updatedUser.Id);
        return await MapToDtoAsync(updatedUser, cancellationToken);
    }

    public async Task<bool> DeleteUserAsync(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🗑️ 删除用户: {UserId}", id);

        var result = await _userRepository.DeleteAsync(id, cancellationToken);

        if (result) _logger.LogInformation("✅ 成功删除用户: {UserId}", id);

        return result;
    }

    public async Task<bool> UserExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _userRepository.ExistsAsync(id, cancellationToken);
    }

    // ============================================================================
    // 角色管理相关方法
    // ============================================================================

    public async Task<List<RoleDto>> GetAllRolesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📋 获取所有角色");

        var roles = await _roleRepository.GetAllAsync(cancellationToken);
        return roles.Select(MapRoleToDto).ToList();
    }

    public async Task<RoleDto?> GetRoleByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetByIdAsync(id, cancellationToken);
        return role == null ? null : MapRoleToDto(role);
    }

    public async Task<RoleDto?> GetRoleByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetByNameAsync(name, cancellationToken);
        return role == null ? null : MapRoleToDto(role);
    }

    public async Task<RoleDto> CreateRoleAsync(
        string name,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 创建角色: {RoleName}", name);

        // 检查角色名称是否已存在
        var existingRole = await _roleRepository.GetByNameAsync(name, cancellationToken);
        if (existingRole != null) throw new InvalidOperationException($"角色名称 '{name}' 已存在");

        // 使用领域工厂方法创建角色
        var role = Role.Create(name, description);

        // 持久化
        var createdRole = await _roleRepository.CreateAsync(role, cancellationToken);

        _logger.LogInformation("✅ 成功创建角色: {RoleId}", createdRole.Id);
        return MapRoleToDto(createdRole);
    }

    public async Task<RoleDto> UpdateRoleAsync(
        string id,
        string name,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 更新角色: {RoleId}", id);

        // 获取角色
        var role = await _roleRepository.GetByIdAsync(id, cancellationToken);
        if (role == null) throw new KeyNotFoundException($"角色不存在: {id}");

        // 检查角色名称是否被其他角色使用
        if (role.Name != name)
        {
            var existingRole = await _roleRepository.GetByNameAsync(name, cancellationToken);
            if (existingRole != null && existingRole.Id != id)
                throw new InvalidOperationException($"角色名称 '{name}' 已被其他角色使用");
        }

        // 使用领域方法更新
        role.Update(name, description);

        // 持久化
        var updatedRole = await _roleRepository.UpdateAsync(role, cancellationToken);

        _logger.LogInformation("✅ 成功更新角色: {RoleId}", updatedRole.Id);
        return MapRoleToDto(updatedRole);
    }

    public async Task<bool> DeleteRoleAsync(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🗑️ 删除角色: {RoleId}", id);

        // 检查是否有用户在使用此角色
        var usersWithRole = await GetUsersByRoleAsync(id, cancellationToken);
        if (usersWithRole.Any()) throw new InvalidOperationException($"无法删除角色: 仍有 {usersWithRole.Count} 个用户使用此角色");

        var result = await _roleRepository.DeleteAsync(id, cancellationToken);

        if (result) _logger.LogInformation("✅ 成功删除角色: {RoleId}", id);

        return result;
    }

    public async Task<UserDto> ChangeUserRoleAsync(
        string userId,
        string roleId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔄 更改用户角色: UserId={UserId}, RoleId={RoleId}", userId, roleId);

        // 获取用户
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null) throw new KeyNotFoundException($"用户不存在: {userId}");

        // 验证角色是否存在
        var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
        if (role == null) throw new KeyNotFoundException($"角色不存在: {roleId}");

        // 更改用户角色
        user.ChangeRole(roleId);

        // 持久化
        var updatedUser = await _userRepository.UpdateAsync(user, cancellationToken);

        _logger.LogInformation("✅ 成功更改用户角色: UserId={UserId}, NewRole={RoleName}", userId, role.Name);
        return await MapToDtoAsync(updatedUser, cancellationToken);
    }

    public async Task<List<UserDto>> GetUsersByRoleAsync(string roleId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📋 获取角色用户: RoleId={RoleId}", roleId);

        // 验证角色是否存在
        var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
        if (role == null) throw new KeyNotFoundException($"角色不存在: {roleId}");

        // 这里需要在 IUserRepository 中添加 GetByRoleIdAsync 方法
        // 暂时使用获取所有用户然后过滤的方式（性能较低，仅用于演示）
        var (allUsers, _) = await _userRepository.GetListAsync(1, 10000, cancellationToken);
        var usersWithRole = allUsers.Where(u => u.RoleId == roleId).ToList();

        var userDtos = new List<UserDto>();
        foreach (var user in usersWithRole) userDtos.Add(await MapToDtoAsync(user, cancellationToken));

        _logger.LogInformation("✅ 找到 {Count} 个用户使用角色 {RoleName}", userDtos.Count, role.Name);
        return userDtos;
    }

    public async Task<List<Guid>> GetAdminUserIdsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 获取所有管理员用户ID");

        try
        {
            // 1. 获取 admin 角色
            var adminRole = await _roleRepository.GetByNameAsync("admin", cancellationToken);
            if (adminRole == null)
            {
                _logger.LogWarning("⚠️ 未找到 admin 角色");
                return new List<Guid>();
            }

            // 2. 获取所有 admin 用户
            var adminUsers = await _userRepository.GetUsersByRoleIdAsync(adminRole.Id, cancellationToken);

            // 3. 提取用户ID
            var adminIds = adminUsers.Select(u => Guid.Parse(u.Id)).ToList();

            _logger.LogInformation("✅ 找到 {Count} 个管理员", adminIds.Count);
            return adminIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取管理员列表失败");
            return new List<Guid>();
        }
    }

    #region 私有映射方法

    private async Task<UserDto> MapToDtoAsync(User user, CancellationToken cancellationToken = default)
    {
        // 获取用户角色名称（使用内存缓存）
        var roleName = await GetCachedRoleNameAsync(user.RoleId, cancellationToken);

        var userDto = new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            AvatarUrl = user.AvatarUrl,
            Bio = user.Bio,
            Role = roleName,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };

        // 加载会员信息
        try
        {
            var membership = await _membershipService.GetMembershipAsync(user.Id);
            if (membership != null)
            {
                userDto.Membership = new UserMembershipDto
                {
                    Level = membership.Level,
                    LevelName = membership.LevelName,
                    StartDate = membership.StartDate,
                    ExpiryDate = membership.ExpiryDate,
                    AutoRenew = membership.AutoRenew,
                    AiUsageThisMonth = membership.AiUsageThisMonth,
                    AiUsageLimit = membership.AiUsageLimit,
                    ModeratorDeposit = membership.ModeratorDeposit,
                    IsActive = membership.IsActive,
                    IsExpired = membership.IsExpired,
                    RemainingDays = membership.RemainingDays,
                    IsExpiringSoon = membership.IsExpiringSoon,
                    CanUseAI = membership.CanUseAI,
                    CanApplyModerator = membership.CanApplyModerator
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 加载用户会员信息失败: UserId={UserId}", user.Id);
            // 即使加载失败也返回用户基本信息
        }

        return userDto;
    }

    private RoleDto MapRoleToDto(Role role)
    {
        return new RoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            CreatedAt = role.CreatedAt,
            UpdatedAt = role.UpdatedAt
        };
    }

    /// <summary>
    ///     获取缓存的角色名称（角色数据几乎不变，避免每次都查库）
    /// </summary>
    private async Task<string> GetCachedRoleNameAsync(string roleId, CancellationToken cancellationToken)
    {
        if (_roleCache.TryGetValue(roleId, out var cached) &&
            DateTime.UtcNow - cached.CachedAt < _roleCacheDuration)
        {
            return cached.RoleName;
        }

        var role = await _roleRepository.GetByIdAsync(roleId, cancellationToken);
        var roleName = role?.Name ?? "user";
        _roleCache[roleId] = (roleName, DateTime.UtcNow);
        return roleName;
    }

    #endregion

    public async Task<(List<ModeratorCandidateDto> Users, int Total)> GetModeratorCandidatesAsync(
        string? searchTerm = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "👥 获取版主候选人列表 - SearchTerm: {SearchTerm}, Page: {Page}, PageSize: {PageSize}",
            searchTerm, page, pageSize);

        var (users, total) = await _userRepository.GetModeratorCandidatesAsync(
            searchTerm, page, pageSize, cancellationToken);

        var dtos = users.Select(ModeratorCandidateDto.FromEntity).ToList();

        _logger.LogInformation("✅ 获取到 {Count}/{Total} 个版主候选人", dtos.Count, total);
        return (dtos, total);
    }
}