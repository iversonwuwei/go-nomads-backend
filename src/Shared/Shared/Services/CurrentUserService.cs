using GoNomads.Shared.Middleware;
using GoNomads.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GoNomads.Shared.Services;

/// <summary>
/// 当前用户服务实现
/// 从 HttpContext 中获取用户信息，提供统一的用户身份和权限检查
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CurrentUserService> _logger;

    // 角色常量 - 统一大小写
    private static class Roles
    {
        public const string Admin = "admin";
        public const string Moderator = "moderator";
        public const string User = "user";
        public const string Guest = "guest";
    }

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        ILogger<CurrentUserService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    #region 私有辅助方法

    private HttpContext? HttpContext => _httpContextAccessor.HttpContext;

    private UserContext? CachedUserContext
    {
        get
        {
            if (HttpContext == null) return null;
            return UserContextMiddleware.GetUserContext(HttpContext);
        }
    }

    /// <summary>
    /// 规范化角色名称（统一转小写）
    /// </summary>
    private static string? NormalizeRole(string? role)
    {
        return role?.ToLowerInvariant();
    }

    #endregion

    #region 用户身份获取

    /// <inheritdoc />
    public Guid GetUserId()
    {
        var userId = TryGetUserId();
        if (userId == null)
        {
            _logger.LogWarning("尝试获取用户ID但用户未认证");
            throw new UnauthorizedAccessException("用户未认证");
        }
        return userId.Value;
    }

    /// <inheritdoc />
    public Guid? TryGetUserId()
    {
        var userIdString = GetUserIdString();
        if (string.IsNullOrEmpty(userIdString))
        {
            return null;
        }

        if (Guid.TryParse(userIdString, out var userId))
        {
            return userId;
        }

        _logger.LogWarning("无法解析用户ID: {UserId}", userIdString);
        return null;
    }

    /// <inheritdoc />
    public string? GetUserIdString()
    {
        return CachedUserContext?.UserId;
    }

    /// <inheritdoc />
    public string? GetUserEmail()
    {
        return CachedUserContext?.Email;
    }

    /// <inheritdoc />
    public string? GetUserRole()
    {
        return CachedUserContext?.Role;
    }

    /// <inheritdoc />
    public UserContext? GetUserContext()
    {
        return CachedUserContext;
    }

    #endregion

    #region 认证状态

    /// <inheritdoc />
    public bool IsAuthenticated => CachedUserContext?.IsAuthenticated ?? false;

    #endregion

    #region 权限检查

    /// <inheritdoc />
    public bool IsAdmin()
    {
        var role = NormalizeRole(GetUserRole());
        return role == Roles.Admin;
    }

    /// <inheritdoc />
    public bool IsModerator()
    {
        var role = NormalizeRole(GetUserRole());
        return role == Roles.Moderator || role == Roles.Admin;
    }

    /// <inheritdoc />
    public bool HasRole(string role)
    {
        var currentRole = NormalizeRole(GetUserRole());
        var targetRole = NormalizeRole(role);
        return currentRole == targetRole;
    }

    /// <inheritdoc />
    public bool HasAnyRole(params string[] roles)
    {
        var currentRole = NormalizeRole(GetUserRole());
        if (string.IsNullOrEmpty(currentRole)) return false;

        return roles.Any(r => NormalizeRole(r) == currentRole);
    }

    /// <inheritdoc />
    public bool HasAdminOrModeratorPrivileges()
    {
        return IsAdmin() || IsModerator();
    }

    #endregion

    #region 资源所有权检查

    /// <inheritdoc />
    public bool IsOwner(Guid resourceOwnerId)
    {
        var userId = TryGetUserId();
        return userId.HasValue && userId.Value == resourceOwnerId;
    }

    /// <inheritdoc />
    public bool CanAccess(Guid resourceOwnerId)
    {
        return IsAdmin() || IsOwner(resourceOwnerId);
    }

    /// <inheritdoc />
    public bool CanAccessOrModerate(Guid resourceOwnerId)
    {
        return IsAdmin() || IsModerator() || IsOwner(resourceOwnerId);
    }

    #endregion
}
