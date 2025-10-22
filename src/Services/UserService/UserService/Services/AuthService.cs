using UserService.DTOs;
using UserService.Repositories;
using GoNomads.Shared.Security;

namespace UserService.Services;

public class AuthService : IAuthService
{
    private readonly SupabaseUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly JwtTokenService _jwtTokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        SupabaseUserRepository userRepository,
        IRoleRepository roleRepository,
        JwtTokenService jwtTokenService,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    public async Task<AuthResponseDto> LoginAsync(string email, string password)
    {
        try
        {
            _logger.LogInformation("尝试登录用户: {Email}", email);
            var user = await _userRepository.GetUserByEmailAsync(email);
            
            if (user == null)
            {
                _logger.LogWarning("用户 {Email} 不存在", email);
                throw new UnauthorizedAccessException("用户名或密码错误");
            }

            if (!PasswordHasher.VerifyPassword(password, user.PasswordHash))
            {
                _logger.LogWarning("用户 {Email} 密码错误", email);
                throw new UnauthorizedAccessException("用户名或密码错误");
            }

            // 通过 RoleId 获取角色名称
            var role = await _roleRepository.GetRoleByIdAsync(user.RoleId);
            var roleName = role?.Name ?? "user"; // 如果角色不存在,默认使用 "user"

            _logger.LogInformation("用户 {Email} 登录成功,角色: {Role}", email, roleName);

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
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt
                }
            };
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用户 {Email} 登录时发生错误", email);
            throw new Exception("登录失败,请稍后重试");
        }
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            _logger.LogInformation("尝试刷新访问令牌");

            // 验证 refresh token 的有效性
            var principal = _jwtTokenService.ValidateToken(refreshToken);
            if (principal == null)
            {
                _logger.LogWarning("刷新令牌无效或已过期");
                throw new UnauthorizedAccessException("刷新令牌无效或已过期,请重新登录");
            }

            // 调试: 打印所有 claims
            _logger.LogDebug("刷新令牌包含的 Claims:");
            foreach (var claim in principal.Claims)
            {
                _logger.LogDebug("  {Type}: {Value}", claim.Type, claim.Value);
            }

            // 直接从已验证的 principal 中提取 userId,避免重复验证
            // 尝试多种可能的 claim 类型
            var userId = principal.FindFirst("sub")?.Value
                ?? principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? principal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("刷新令牌中未找到用户ID");
                throw new UnauthorizedAccessException("无效的刷新令牌");
            }

            _logger.LogDebug("从刷新令牌中提取用户ID: {UserId}", userId);

            var user = await _userRepository.GetUserByIdAsync(userId);
            
            if (user == null)
            {
                throw new UnauthorizedAccessException("用户不存在");
            }

            // 通过 RoleId 获取角色名称
            var role = await _roleRepository.GetRoleByIdAsync(user.RoleId);
            var roleName = role?.Name ?? "user";

            _logger.LogInformation("令牌刷新成功,用户: {UserId}, 角色: {Role}", userId, roleName);

            // 生成新的 access token 和 refresh token (token rotation 最佳实践)
            var newAccessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email, roleName);
            var newRefreshToken = _jwtTokenService.GenerateRefreshToken(user.Id);

            return new AuthResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                TokenType = "Bearer",
                ExpiresIn = 3600,
                User = new UserDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Phone = user.Phone,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt
                }
            };
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新令牌失败");
            throw new Exception("刷新令牌失败,请重新登录");
        }
    }

    /// <summary>
    /// 用户登出
    /// 注意: 由于使用无状态 JWT,令牌在过期前无法真正撤销
    /// 客户端应该:
    /// 1. 删除本地存储的 access token 和 refresh token
    /// 2. 清除所有用户相关的本地状态
    /// 未来改进: 可考虑实现 token 黑名单机制 (需要 Redis 等缓存支持)
    /// </summary>
    public async Task SignOutAsync()
    {
        _logger.LogInformation("用户登出 - 客户端应删除本地 token");
        await Task.CompletedTask;
    }
}
