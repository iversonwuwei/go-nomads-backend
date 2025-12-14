using System.Collections.Concurrent;
using System.Security.Claims;
using GoNomads.Shared.Security;
using Microsoft.Extensions.Options;
using UserService.Application.DTOs;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;
using UserService.Infrastructure.Configuration;

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
    private readonly IAliyunSmsService _smsService;
    private readonly AliyunSmsSettings _smsSettings;

    /// <summary>
    ///     验证码缓存 (手机号 -> (验证码, 过期时间))
    ///     生产环境建议使用 Redis
    /// </summary>
    private static readonly ConcurrentDictionary<string, (string Code, DateTime ExpiresAt)> _verificationCodes = new();

    public AuthApplicationService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        JwtTokenService jwtTokenService,
        IAliyunSmsService smsService,
        IOptions<AliyunSmsSettings> smsSettings,
        ILogger<AuthApplicationService> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _jwtTokenService = jwtTokenService;
        _smsService = smsService;
        _smsSettings = smsSettings.Value;
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
        catch (ArgumentException ex)
        {
            _logger.LogWarning("⚠️ 注册参数错误: {Message}", ex.Message);
            throw new InvalidOperationException(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 用户注册失败: {Email}, 错误: {Error}", request.Email, ex.Message);
            throw new Exception($"注册失败: {ex.Message}");
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
                // 邮箱登录时明确提示用户不存在，引导用户去注册
                throw new KeyNotFoundException("该邮箱尚未注册，请先注册账号");
            }

            // 验证密码
            if (!user.ValidatePassword(request.Password))
            {
                _logger.LogWarning("⚠️ 用户 {Email} 密码错误", request.Email);
                throw new UnauthorizedAccessException("密码错误");
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
        catch (KeyNotFoundException)
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

    /// <summary>
    ///     发送短信验证码
    /// </summary>
    public async Task<SendSmsCodeResponse> SendSmsCodeAsync(
        SendSmsCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📱 发送验证码请求: {Phone}, 用途: {Purpose}",
            MaskPhoneNumber(request.PhoneNumber), request.Purpose);

        try
        {
            // 生成验证码
            var code = _smsService.GenerateVerificationCode(_smsSettings.CodeLength);

            // 发送短信
            var result = await _smsService.SendVerificationCodeAsync(
                request.PhoneNumber, code, cancellationToken);

            if (!result.Success)
            {
                _logger.LogWarning("⚠️ 验证码发送失败: {Phone}, {Message}",
                    MaskPhoneNumber(request.PhoneNumber), result.Message);

                return new SendSmsCodeResponse
                {
                    Success = false,
                    Message = result.Message,
                    RequestId = result.RequestId
                };
            }

            // 存储验证码（用于后续验证）
            var expiresAt = DateTime.UtcNow.AddMinutes(_smsSettings.CodeExpirationMinutes);
            var normalizedPhone = NormalizePhoneNumber(request.PhoneNumber);
            _verificationCodes[normalizedPhone] = (code, expiresAt);

            // 清理过期的验证码
            CleanupExpiredCodes();

            _logger.LogInformation("✅ 验证码发送成功: {Phone}", MaskPhoneNumber(request.PhoneNumber));

            return new SendSmsCodeResponse
            {
                Success = true,
                Message = "验证码已发送",
                ExpiresInSeconds = _smsSettings.CodeExpirationMinutes * 60,
                RequestId = result.RequestId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 发送验证码异常: {Phone}", MaskPhoneNumber(request.PhoneNumber));
            return new SendSmsCodeResponse
            {
                Success = false,
                Message = "发送验证码失败,请稍后重试"
            };
        }
    }

    /// <summary>
    ///     手机号验证码登录
    /// </summary>
    public async Task<AuthResponseDto> LoginWithPhoneAsync(
        PhoneLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📱 手机号登录: {Phone}", MaskPhoneNumber(request.PhoneNumber));

        try
        {
            // 验证验证码
            var normalizedPhone = NormalizePhoneNumber(request.PhoneNumber);
            if (!ValidateCode(normalizedPhone, request.Code))
            {
                throw new InvalidOperationException("验证码错误或已过期");
            }

            // 移除已使用的验证码
            _verificationCodes.TryRemove(normalizedPhone, out _);

            // 查找用户（通过手机号）
            var user = await _userRepository.GetByPhoneAsync(normalizedPhone, cancellationToken);

            if (user == null)
            {
                // 自动注册新用户
                _logger.LogInformation("📝 手机号首次登录,自动注册: {Phone}", MaskPhoneNumber(request.PhoneNumber));

                var defaultRole = await _roleRepository.GetByNameAsync(Role.RoleNames.User, cancellationToken);
                if (defaultRole == null)
                {
                    throw new InvalidOperationException("系统配置错误: 默认用户角色不存在");
                }

                user = User.CreateWithPhone(
                    $"用户{normalizedPhone[^4..]}",
                    normalizedPhone,
                    defaultRole.Id);

                user = await _userRepository.CreateAsync(user, cancellationToken);

                _logger.LogInformation("✅ 新用户注册成功: {UserId}", user.Id);

                return BuildAuthResponse(user, defaultRole.Name);
            }

            // 获取用户角色
            var role = await _roleRepository.GetByIdAsync(user.RoleId, cancellationToken);
            var roleName = role?.Name ?? Role.RoleNames.User;

            _logger.LogInformation("✅ 手机号登录成功: {UserId}", user.Id);

            return BuildAuthResponse(user, roleName);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 手机号登录失败: {Phone}", MaskPhoneNumber(request.PhoneNumber));
            throw new Exception("登录失败,请稍后重试");
        }
    }

    /// <summary>
    ///     验证验证码
    /// </summary>
    private bool ValidateCode(string phoneNumber, string code)
    {
        // 测试验证码：123456 始终有效（用于开发测试）
        if (code == "123456")
        {
            _logger.LogWarning("⚠️ 使用测试验证码登录: {Phone}", MaskPhoneNumber(phoneNumber));
            return true;
        }

        if (!_verificationCodes.TryGetValue(phoneNumber, out var stored))
        {
            return false;
        }

        if (DateTime.UtcNow > stored.ExpiresAt)
        {
            _verificationCodes.TryRemove(phoneNumber, out _);
            return false;
        }

        return stored.Code == code;
    }

    /// <summary>
    ///     清理过期的验证码
    /// </summary>
    private static void CleanupExpiredCodes()
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _verificationCodes
            .Where(kv => kv.Value.ExpiresAt < now)
            .Select(kv => kv.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _verificationCodes.TryRemove(key, out _);
        }
    }

    /// <summary>
    ///     规范化手机号
    /// </summary>
    private static string NormalizePhoneNumber(string phoneNumber)
    {
        return new string(phoneNumber.Where(char.IsDigit).ToArray());
    }

    /// <summary>
    ///     脱敏手机号
    /// </summary>
    private static string MaskPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 7)
            return "***";
        return phoneNumber[..3] + "****" + phoneNumber[^4..];
    }

    /// <summary>
    ///     社交登录（用户不存在时自动创建）
    /// </summary>
    public async Task<AuthResponseDto> SocialLoginAsync(
        SocialLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔐 社交登录: Provider={Provider}", request.Provider);

        try
        {
            var provider = request.Provider.ToLower();

            // 必须提供 OpenId 或可以从 code/accessToken 获取
            var openId = request.OpenId;
            if (string.IsNullOrEmpty(openId))
            {
                // TODO: 如果没有 OpenId，需要通过 code 或 accessToken 调用社交平台 API 获取
                // 这里暂时要求客户端直接提供 OpenId
                throw new InvalidOperationException("社交登录需要提供 OpenId");
            }

            // 查找已存在的用户
            var user = await _userRepository.GetBySocialLoginAsync(provider, openId, cancellationToken);

            if (user == null)
            {
                // 自动注册新用户
                _logger.LogInformation("📝 社交登录首次使用,自动注册: Provider={Provider}", provider);

                var defaultRole = await _roleRepository.GetByNameAsync(Role.RoleNames.User, cancellationToken);
                if (defaultRole == null)
                {
                    throw new InvalidOperationException("系统配置错误: 默认用户角色不存在");
                }

                // 生成默认用户名
                var defaultName = $"{provider}用户{openId[^4..]}";

                user = User.CreateWithSocialLogin(
                    defaultName,
                    provider,
                    openId,
                    defaultRole.Id);

                user = await _userRepository.CreateAsync(user, cancellationToken);

                _logger.LogInformation("✅ 社交登录新用户注册成功: {UserId}", user.Id);

                return BuildAuthResponse(user, defaultRole.Name);
            }

            // 获取用户角色
            var role = await _roleRepository.GetByIdAsync(user.RoleId, cancellationToken);
            var roleName = role?.Name ?? Role.RoleNames.User;

            _logger.LogInformation("✅ 社交登录成功: {UserId}", user.Id);

            return BuildAuthResponse(user, roleName);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 社交登录失败: Provider={Provider}", request.Provider);
            throw new Exception("社交登录失败,请稍后重试");
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
