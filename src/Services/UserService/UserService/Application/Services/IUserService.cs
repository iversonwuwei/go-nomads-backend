using UserService.Application.DTOs;

namespace UserService.Application.Services;

/// <summary>
/// User 应用服务接口
/// </summary>
public interface IUserService
{
    /// <summary>
    /// 获取用户列表（分页）
    /// </summary>
    Task<(List<UserDto> Users, int Total)> GetUsersAsync(
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据 ID 获取用户
    /// </summary>
    Task<UserDto?> GetUserByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量根据 ID 获取用户列表
    /// </summary>
    Task<List<UserDto>> GetUsersByIdsAsync(List<string> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据邮箱获取用户
    /// </summary>
    Task<UserDto?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建用户（不带密码）
    /// </summary>
    Task<UserDto> CreateUserAsync(string name, string email, string phone, CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建用户（带密码）
    /// </summary>
    Task<UserDto> CreateUserWithPasswordAsync(
        string name,
        string email,
        string password,
        string phone,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户信息
    /// </summary>
    Task<UserDto> UpdateUserAsync(
        string id,
        string name,
        string email,
        string phone,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除用户
    /// </summary>
    Task<bool> DeleteUserAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查用户是否存在
    /// </summary>
    Task<bool> UserExistsAsync(string id, CancellationToken cancellationToken = default);
}
