using System.Security.Claims;
using GoNomads.Shared.Security;
using UserService.Application.DTOs;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Application.Services;

/// <summary>
///     认证应用服务实现 - 协调用户认证相关领域逻辑
///     优化：使用 Supabase JOIN 查询，减少数据库往返次数
/// </summary>
public class AuthApplicationService : IAuthService
{
    private readonly JwtTokenService _jwtTokenService;
    private readonly ILogger<AuthApplicationService> _logger;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRepository _userRepository;

    public AuthApplicationService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        JwtTokenService jwtTokenService,
        ILogger<AuthApplicationService> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    /// <summary>
    ///     用户注册
    ///     DB 查询：3 次（检查邮箱 + 获取默认角色 + 创建用户）
    /// </summary>
    public async Task<AuthResponseDto> RegisterAsync(RegisterDto request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 用户注册: {Email}", request.Email);

        try
        {
            // 检查邮箱是否已存在
            var existingUser = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
            if (existingUser != null)
            {
                _logger.LogWarning("⚠️ 邮箱已被注册: {Email}", request.Email);
                throw new InvalidOperationException($"邮箱 '{request.Email}' 已被注册");
            }

            // 获取默认角色
            var defaultRole = await _roleRepository.GetByNameAsync(Role.RoleNames.User, cancellationToken);
            if (defaultRole == null)
            {
                _logger.LogError("❌ 默认角色 'user' 不存在");
                throw new InvalidOperationException("系统配置错误: 默认用户角色不存在");
            }

            // 使用领域工厂方法创建用户（带密码）
            var user = User.CreateWithPassword(
                request.Name,
                request.Email,
                request.Password,
                request.Phone ?? string.Empty,
                defaultRole.Id);

            // 持久化
            var createdUser = await _userRepository.CreateAsync(user, cancellationToken);

            _logger.LogInformation("✅ 用户注册成功: {UserId}, Email: {Email}", createdUser.Id, createdUser.Email);

            return BuildAuthResponse(createdUser, defaultRole.Name);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 用户注册失败: {Email}", request.Email);
            throw new Exception("注册失败,请稍后重试");
        }
    }

    /// <summary>
    ///     用户登录
    /// </summary>
    public async Task<AuthResponseDto> LoginAsync(LoginDto request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔐 尝试登录用户: {Email}", request.Email);

        try
        {
            // 查询用户
            var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("⚠️ 用户不存在: {Email}", request.Email);
                throw new UnauthorizedAccessException("用户名或密码错误");
            }

            // 验证密码
            if (!user.ValidatePassword(request.Password))
            {
                _logger.LogWarning("⚠️ 用户 {Email} 密码错误", request.Email);
                throw new UnauthorizedAccessException("用户名或密码错误");
            }

            // 获取角色名称
            var role = await _roleRepository.GetByIdAsync(user.RoleId, cancellationToken);
            var roleName = role?.Name ?? "user";

            _logger.LogInformation("✅ 用户 {Email} 登录成功, 角色: {Role}", request.Email, roleName);

            return BuildAuthResponse(user, roleName);
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 用户 {Email} 登录时发生错误", request.Email);
            throw new Exception("登录失败,请稍后重试");
        }
    }

    /// <summary>
    ///     刷新令牌
    /// </summary>
    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔄 尝试刷新访问令牌");

        try
        {
            // 验证 refresh token 的有效性
            var principal = _jwtTokenService.ValidateToken(request.RefreshToken);
            if (principal == null)
            {
                _logger.LogWarning("⚠️ 刷新令牌无效或已过期");
                throw new UnauthorizedAccessException("刷新令牌无效或已过期,请重新登录");
            }

            // 提取用户 ID
            var userId = principal.FindFirst("sub")?.Value
                         ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? principal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
                             ?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("⚠️ 刷新令牌中未找到用户ID");
                throw new UnauthorizedAccessException("无效的刷新令牌");
            }

            // 查询用户
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("⚠️ 用户不存在: {UserId}", userId);
                throw new UnauthorizedAccessException("用户不存在");
            }

            // 获取角色名称
            var role = await _roleRepository.GetByIdAsync(user.RoleId, cancellationToken);
            var roleName = role?.Name ?? "user";

            _logger.LogInformation("✅ 令牌刷新成功, 用户: {UserId}, 角色: {Role}", userId, roleName);

            return BuildAuthResponse(user, roleName);
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 刷新令牌失败");
            throw new Exception("刷新令牌失败,请重新登录");
        }
    }

    /// <summary>
    ///     用户登出
    /// </summary>
    public Task SignOutAsync(string userId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("👋 用户登出: {UserId}", userId);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     修改密码
    /// </summary>
    public async Task ChangePasswordAsync(
        string userId,
        string oldPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔐 用户修改密码: {UserId}", userId);

        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("⚠️ 用户不存在: {UserId}", userId);
                throw new KeyNotFoundException($"用户不存在: {userId}");
            }

            user.ChangePassword(oldPassword, newPassword);
            await _userRepository.UpdateAsync(user, cancellationToken);

            _logger.LogInformation("✅ 用户 {UserId} 密码修改成功", userId);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (KeyNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 用户 {UserId} 修改密码失败", userId);
            throw new Exception("修改密码失败,请稍后重试");
        }
    }

    #region 私有辅助方法

    /// <summary>
    ///     构建认证响应（从 User + 已知角色名）
    /// </summary>
    private AuthResponseDto BuildAuthResponse(User user, string roleName)
    {
        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email, roleName);
        var refreshToken = _jwtTokenService.GenerateRefreshToken(user.Id);

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            TokenType = "Bearer",
            ExpiresIn = 3600,
            User = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone,
                Role = roleName,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            }
        };
    }

    #endregion
}
