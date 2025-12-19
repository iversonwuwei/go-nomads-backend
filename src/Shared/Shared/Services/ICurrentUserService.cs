namespace GoNomads.Shared.Services;

/// <summary>
/// 当前用户服务接口
/// 用于统一处理用户身份获取和权限检查，替代各 Controller 中的私有方法
/// </summary>
public interface ICurrentUserService
{
    #region 用户身份获取

    /// <summary>
    /// 获取当前用户ID（必须已认证，否则抛出异常）
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">用户未认证时抛出</exception>
    Guid GetUserId();

    /// <summary>
    /// 尝试获取当前用户ID（未认证时返回 null）
    /// </summary>
    Guid? TryGetUserId();

    /// <summary>
    /// 获取当前用户ID字符串形式（未认证时返回 null）
    /// </summary>
    string? GetUserIdString();

    /// <summary>
    /// 获取当前用户邮箱
    /// </summary>
    string? GetUserEmail();

    /// <summary>
    /// 获取当前用户角色
    /// </summary>
    string? GetUserRole();

    /// <summary>
    /// 获取完整的用户上下文
    /// </summary>
    Models.UserContext? GetUserContext();

    #endregion

    #region 认证状态

    /// <summary>
    /// 当前请求是否已认证
    /// </summary>
    bool IsAuthenticated { get; }

    #endregion

    #region 权限检查

    /// <summary>
    /// 是否为管理员
    /// </summary>
    bool IsAdmin();

    /// <summary>
    /// 是否为版主（包含管理员）
    /// </summary>
    bool IsModerator();

    /// <summary>
    /// 是否拥有指定角色
    /// </summary>
    bool HasRole(string role);

    /// <summary>
    /// 是否拥有指定角色之一
    /// </summary>
    bool HasAnyRole(params string[] roles);

    /// <summary>
    /// 是否为管理员或版主
    /// </summary>
    bool HasAdminOrModeratorPrivileges();

    #endregion

    #region 资源所有权检查

    /// <summary>
    /// 检查当前用户是否为指定资源的所有者
    /// </summary>
    bool IsOwner(Guid resourceOwnerId);

    /// <summary>
    /// 检查当前用户是否有权限操作（所有者或管理员）
    /// </summary>
    bool CanAccess(Guid resourceOwnerId);

    /// <summary>
    /// 检查当前用户是否有权限操作（所有者、管理员或版主）
    /// </summary>
    bool CanAccessOrModerate(Guid resourceOwnerId);

    #endregion
}
