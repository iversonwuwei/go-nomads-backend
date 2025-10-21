using UserService.DTOs;
using UserService.Repositories;
using GoNomads.Shared.Security;

namespace UserService.Services;

public class AuthService : IAuthService
{
    private readonly SupabaseUserRepository _userRepository;
    private readonly JwtTokenService _jwtTokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        SupabaseUserRepository userRepository,
        JwtTokenService jwtTokenService,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
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

            _logger.LogInformation("用户 {Email} 登录成功", email);

            var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email, user.Role);
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
            var userId = _jwtTokenService.GetUserIdFromToken(refreshToken);
            
            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("无效的刷新令牌");
            }

            var user = await _userRepository.GetUserByIdAsync(userId);
            
            if (user == null)
            {
                throw new UnauthorizedAccessException("用户不存在");
            }

            _logger.LogInformation("令牌刷新成功,用户: {UserId}", userId);

            var newAccessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email, user.Role);
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

    public async Task SignOutAsync()
    {
        _logger.LogInformation("用户登出");
        await Task.CompletedTask;
    }
}
