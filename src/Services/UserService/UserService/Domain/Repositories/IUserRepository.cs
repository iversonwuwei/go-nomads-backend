using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

/// <summary>
///     User 仓储接口 - 定义在领域层
/// </summary>
public interface IUserRepository
{
    /// <summary>
    ///     创建用户
    /// </summary>
    Task<User> CreateAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    ///     根据 ID 获取用户
    /// </summary>
    Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     根据邮箱获取用户
    /// </summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    ///     根据手机号获取用户
    /// </summary>
    Task<User?> GetByPhoneAsync(string phone, CancellationToken cancellationToken = default);

    /// <summary>
    ///     根据邮箱获取用户（带角色信息，单次 JOIN 查询）
    /// </summary>
    Task<UserWithRole?> GetByEmailWithRoleAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    ///     根据 ID 获取用户（带角色信息，单次 JOIN 查询）
    /// </summary>
    Task<UserWithRole?> GetByIdWithRoleAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     更新用户
    /// </summary>
    Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    ///     删除用户
    /// </summary>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     检查用户是否存在
    /// </summary>
    Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取用户列表（分页）
    /// </summary>
    Task<(List<User> Users, int Total)> GetListAsync(
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     搜索用户（按名称或邮箱，可筛选角色）
    /// </summary>
    Task<(List<User> Users, int Total)> SearchAsync(
        string? searchTerm = null,
        string? role = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     根据角色ID获取所有用户
    /// </summary>
    Task<List<User>> GetUsersByRoleIdAsync(string roleId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取版主候选人列表（Pro及以上会员或Admin用户）
    ///     使用 Supabase JOIN 一次性查询用户、角色和会员信息
    /// </summary>
    Task<(List<UserWithMembership> Users, int Total)> GetModeratorCandidatesAsync(
        string? searchTerm = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     根据社交登录信息获取用户
    /// </summary>
    /// <param name="provider">社交平台（wechat, douyin 等）</param>
    /// <param name="openId">社交平台用户唯一标识</param>
    Task<User?> GetBySocialLoginAsync(string provider, string openId, CancellationToken cancellationToken = default);
}