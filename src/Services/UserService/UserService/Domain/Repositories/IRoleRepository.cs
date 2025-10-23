using UserService.Domain.Entities;

namespace UserService.Domain.Repositories;

/// <summary>
/// Role 仓储接口 - 定义在领域层
/// </summary>
public interface IRoleRepository
{
    /// <summary>
    /// 创建角色
    /// </summary>
    Task<Role> CreateAsync(Role role, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据 ID 获取角色
    /// </summary>
    Task<Role?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据名称获取角色
    /// </summary>
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有角色
    /// </summary>
    Task<List<Role>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新角色
    /// </summary>
    Task<Role> UpdateAsync(Role role, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除角色
    /// </summary>
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查角色是否存在
    /// </summary>
    Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default);
}
