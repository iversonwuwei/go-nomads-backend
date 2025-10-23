using UserService.Application.DTOs;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Application.Services;

/// <summary>
/// User 应用服务实现 - 协调领域对象和仓储
/// </summary>
public class UserApplicationService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ILogger<UserApplicationService> _logger;

    public UserApplicationService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        ILogger<UserApplicationService> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _logger = logger;
    }

    public async Task<(List<UserDto> Users, int Total)> GetUsersAsync(
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📋 获取用户列表 - Page: {Page}, PageSize: {PageSize}", page, pageSize);

        var (users, total) = await _userRepository.GetListAsync(page, pageSize, cancellationToken);

        var userDtos = users.Select(MapToDto).ToList();

        return (userDtos, total);
    }

    public async Task<UserDto?> GetUserByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        return user == null ? null : MapToDto(user);
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        return user == null ? null : MapToDto(user);
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
        if (existingUser != null)
        {
            throw new InvalidOperationException($"邮箱 '{email}' 已被注册");
        }

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
        return MapToDto(createdUser);
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
        if (existingUser != null)
        {
            throw new InvalidOperationException($"邮箱 '{email}' 已被注册");
        }

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
        return MapToDto(createdUser);
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
        if (user == null)
        {
            throw new KeyNotFoundException($"用户不存在: {id}");
        }

        // 检查邮箱是否被其他用户使用
        if (user.Email != email)
        {
            var existingUser = await _userRepository.GetByEmailAsync(email, cancellationToken);
            if (existingUser != null && existingUser.Id != id)
            {
                throw new InvalidOperationException($"邮箱 '{email}' 已被其他用户使用");
            }
        }

        // 使用领域方法更新
        user.Update(name, email, phone);

        // 持久化
        var updatedUser = await _userRepository.UpdateAsync(user, cancellationToken);

        _logger.LogInformation("✅ 成功更新用户: {UserId}", updatedUser.Id);
        return MapToDto(updatedUser);
    }

    public async Task<bool> DeleteUserAsync(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🗑️ 删除用户: {UserId}", id);

        var result = await _userRepository.DeleteAsync(id, cancellationToken);

        if (result)
        {
            _logger.LogInformation("✅ 成功删除用户: {UserId}", id);
        }

        return result;
    }

    public async Task<bool> UserExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _userRepository.ExistsAsync(id, cancellationToken);
    }

    #region 私有映射方法

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Phone = user.Phone,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    #endregion
}
