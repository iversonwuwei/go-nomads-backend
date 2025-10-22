using GoNomads.Shared.Models;

namespace UserService.Repositories;

public interface IRoleRepository
{
    /// <summary>
    /// 获取所有角色
    /// </summary>
    Task<List<Role>> GetAllRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据ID获取角色
    /// </summary>
    Task<Role?> GetRoleByIdAsync(string roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据名称获取角色
    /// </summary>
    Task<Role?> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建新角色
    /// </summary>
    Task<Role> CreateRoleAsync(Role role, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新角色
    /// </summary>
    Task<Role> UpdateRoleAsync(Role role, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除角色
    /// </summary>
    Task<bool> DeleteRoleAsync(string roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查角色是否存在
    /// </summary>
    Task<bool> RoleExistsAsync(string roleId, CancellationToken cancellationToken = default);
}
