using Supabase;
using UserService.DTOs;
using UserService.Repositories;

namespace UserService.Services;

/// <summary>
/// 认证服务实现
/// </summary>
public class AuthService : IAuthService
{
    private readonly Client _supabase;
    private readonly SupabaseUserRepository _userRepository;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        Client supabase,
        SupabaseUserRepository userRepository,
        ILogger<AuthService> logger)
    {
        _supabase = supabase;
        _userRepository = userRepository;
        _logger = logger;
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    public async Task<AuthResponseDto> LoginAsync(string email, string password)
    {
        try
        {
            _logger.LogInformation("尝试登录用户: {Email}", email);

            // 使用 Supabase Auth 进行登录
            var session = await _supabase.Auth.SignIn(email, password);

            if (session?.User == null)
            {
                throw new UnauthorizedAccessException("登录失败,请检查邮箱和密码");
            }

            _logger.LogInformation("用户 {Email} 登录成功", email);

            // 从数据库获取用户详细信息
            var user = await _userRepository.GetUserByEmailAsync(email);

            return new AuthResponseDto
            {
                AccessToken = session.AccessToken ?? string.Empty,
                RefreshToken = session.RefreshToken ?? string.Empty,
                TokenType = "Bearer",
                ExpiresIn = (int)session.ExpiresIn,
                User = user != null ? new UserDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Phone = user.Phone,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt
                } : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用户 {Email} 登录失败", email);
            throw;
        }
    }

    /// <summary>
    /// 刷新访问令牌
    /// </summary>
    public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            _logger.LogInformation("尝试刷新访问令牌");

            // 使用 Supabase Auth 刷新令牌
            var session = await _supabase.Auth.RefreshSession();

            if (session?.User == null)
            {
                throw new UnauthorizedAccessException("刷新令牌失败");
            }

            _logger.LogInformation("令牌刷新成功");

            // 从数据库获取用户详细信息
            var user = await _userRepository.GetUserByEmailAsync(session.User.Email ?? string.Empty);

            return new AuthResponseDto
            {
                AccessToken = session.AccessToken ?? string.Empty,
                RefreshToken = session.RefreshToken ?? string.Empty,
                TokenType = "Bearer",
                ExpiresIn = (int)session.ExpiresIn,
                User = user != null ? new UserDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Phone = user.Phone,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt
                } : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新令牌失败");
            throw;
        }
    }

    /// <summary>
    /// 用户登出
    /// </summary>
    public async Task SignOutAsync()
    {
        try
        {
            _logger.LogInformation("用户登出");
            await _supabase.Auth.SignOut();
            _logger.LogInformation("用户登出成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "用户登出失败");
            throw;
        }
    }
}
