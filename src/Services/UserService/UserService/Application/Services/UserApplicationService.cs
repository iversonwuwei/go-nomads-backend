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
    private readonly IRoleRepository _roleRepository;
    private readonly ISkillService _skillService;
    private readonly IUserRepository _userRepository;

    public UserApplicationService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        ISkillService skillService,
        IInterestService interestService,
        ILogger<UserApplicationService> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _skillService = skillService;
        _interestService = interestService;
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

        // 加载用户的技能和兴趣
        try
        {
            userDto.Skills = await _skillService.GetUserSkillsAsync(id, cancellationToken);
            userDto.Interests = await _interestService.GetUserInterestsAsync(id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 加载用户技能/兴趣失败: UserId={UserId}", id);
            // 即使加载失败也返回用户基本信息
            userDto.Skills = new List<UserSkillDto>();
            userDto.Interests = new List<UserInterestDto>();
        }

        return userDto;
    }

    public async Task<List<UserDto>> GetUsersByIdsAsync(List<string> ids, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📋 批量获取用户 - Count: {Count}", ids.Count);

        if (ids == null || ids.Count == 0) return new List<UserDto>();

        var users = new List<UserDto>();

        // 批量获取用户
        foreach (var id in ids.Distinct())
        {
            var user = await _userRepository.GetByIdAsync(id, cancellationToken);
            if (user != null) users.Add(await MapToDtoAsync(user, cancellationToken));
        }

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
        string name,
        string email,
        string phone,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 更新用户: {UserId}", id);

        // 获取用户
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user == null) throw new KeyNotFoundException($"用户不存在: {id}");

        // 检查邮箱是否被其他用户使用
        if (user.Email != email)
        {
            var existingUser = await _userRepository.GetByEmailAsync(email, cancellationToken);
            if (existingUser != null && existingUser.Id != id)
                throw new InvalidOperationException($"邮箱 '{email}' 已被其他用户使用");
        }

        // 使用领域方法更新
        user.Update(name, email, phone);

        // 持久化
        var updatedUser = await _userRepository.UpdateAsync(user, cancellationToken);

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
        // 获取用户角色名称
        var role = await _roleRepository.GetByIdAsync(user.RoleId, cancellationToken);
        var roleName = role?.Name ?? "user"; // 默认为 user

        return new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            Role = roleName,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
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

    #endregion
}