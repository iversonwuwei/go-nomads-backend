using UserService.Application.DTOs;

namespace UserService.Application.Services;

/// <summary>
///     User 应用服务接口
/// </summary>
public interface IUserService
{
    /// <summary>
    ///     获取用户列表（分页）
    /// </summary>
    Task<(List<UserDto> Users, int Total)> GetUsersAsync(
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     搜索用户（按名称或邮箱，分页）
    /// </summary>
    Task<(List<UserDto> Users, int Total)> SearchUsersAsync(
        string? searchTerm = null,
        string? role = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     根据 ID 获取用户
    /// </summary>
    Task<UserDto?> GetUserByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     批量根据 ID 获取用户列表
    /// </summary>
    Task<List<UserDto>> GetUsersByIdsAsync(List<string> ids, CancellationToken cancellationToken = default);

    /// <summary>
    ///     根据邮箱获取用户
    /// </summary>
    Task<UserDto?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    ///     创建用户（不带密码）
    /// </summary>
    Task<UserDto> CreateUserAsync(string name, string email, string phone,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     创建用户（带密码）
    /// </summary>
    Task<UserDto> CreateUserWithPasswordAsync(
        string name,
        string email,
        string password,
        string phone,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     更新用户信息（支持部分更新，只更新非null字段）
    /// </summary>
    Task<UserDto> UpdateUserAsync(
        string id,
        string? name = null,
        string? email = null,
        string? phone = null,
        string? avatarUrl = null,
        string? bio = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     删除用户
    /// </summary>
    Task<bool> DeleteUserAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     检查用户是否存在
    /// </summary>
    Task<bool> UserExistsAsync(string id, CancellationToken cancellationToken = default);

    // ============================================================================
    // 角色管理相关方法
    // ============================================================================

    /// <summary>
    ///     获取所有角色
    /// </summary>
    Task<List<RoleDto>> GetAllRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     根据 ID 获取角色
    /// </summary>
    Task<RoleDto?> GetRoleByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     根据名称获取角色
    /// </summary>
    Task<RoleDto?> GetRoleByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    ///     创建角色
    /// </summary>
    Task<RoleDto> CreateRoleAsync(string name, string? description = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     更新角色
    /// </summary>
    Task<RoleDto> UpdateRoleAsync(string id, string name, string? description = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     删除角色
    /// </summary>
    Task<bool> DeleteRoleAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     更改用户角色
    /// </summary>
    Task<UserDto> ChangeUserRoleAsync(string userId, string roleId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取指定角色的所有用户
    /// </summary>
    Task<List<UserDto>> GetUsersByRoleAsync(string roleId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取所有管理员用户ID列表
    /// </summary>
    Task<List<Guid>> GetAdminUserIdsAsync(CancellationToken cancellationToken = default);
}