using System.ComponentModel.DataAnnotations;
using GoNomads.Shared.Middleware;
using GoNomads.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs;
using UserService.Application.Services;

namespace UserService.API.Controllers;

/// <summary>
///     认证相关 API - 薄层控制器
/// </summary>
/// <summary>
///     Authentication API - RESTful endpoints for authentication
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    ///     发送注册邮箱验证码
    /// </summary>
    [AllowAnonymous]
    [HttpPost("register/send-code")]
    public async Task<ActionResult<ApiResponse<SendRegistrationCodeResponse>>> RegisterSendCode(
        [FromBody] SendRegistrationCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📧 注册-发送邮箱验证码");

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new ApiResponse<SendRegistrationCodeResponse>
            {
                Success = false,
                Message = "请输入邮箱地址"
            });
        }

        try
        {
            var result = await _authService.SendRegistrationCodeAsync(request, cancellationToken);

            if (!result.Success)
            {
                return BadRequest(new ApiResponse<SendRegistrationCodeResponse>
                {
                    Success = false,
                    Message = result.Message,
                    Data = result
                });
            }

            return Ok(new ApiResponse<SendRegistrationCodeResponse>
            {
                Success = true,
                Message = result.Message,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 注册发送验证码异常");
            return StatusCode(500, new ApiResponse<SendRegistrationCodeResponse>
            {
                Success = false,
                Message = "发送验证码失败，请稍后重试"
            });
        }
    }

    /// <summary>
    ///     用户注册
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Register(
        [FromBody] RegisterDto dto,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 用户尝试注册: {Email}", dto.Email);

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(new ApiResponse<AuthResponseDto>
            {
                Success = false,
                Message = "验证失败",
                Errors = errors
            });
        }

        try
        {
            var authResponse = await _authService.RegisterAsync(dto, cancellationToken);

            return Ok(new ApiResponse<AuthResponseDto>
            {
                Success = true,
                Message = "注册成功",
                Data = authResponse
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "⚠️ 用户 {Email} 注册失败: {Message}", dto.Email, ex.Message);
            return BadRequest(new ApiResponse<AuthResponseDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 用户 {Email} 注册时发生错误", dto.Email);
            return StatusCode(500, new ApiResponse<AuthResponseDto>
            {
                Success = false,
                Message = "注册失败,请稍后重试"
            });
        }
    }

    /// <summary>
    ///     用户登录
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login(
        [FromBody] LoginDto dto,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔐 用户尝试登录: {Email}", dto.Email);

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(new ApiResponse<AuthResponseDto>
            {
                Success = false,
                Message = "验证失败",
                Errors = errors
            });
        }

        try
        {
            var authResponse = await _authService.LoginAsync(dto, cancellationToken);

            return Ok(new ApiResponse<AuthResponseDto>
            {
                Success = true,
                Message = "登录成功",
                Data = authResponse
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "⚠️ 用户 {Email} 登录失败: 未授权", dto.Email);
            return Unauthorized(new ApiResponse<AuthResponseDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (KeyNotFoundException ex)
        {
            // 用户不存在 - 返回 404 并提示注册
            _logger.LogWarning(ex, "⚠️ 用户 {Email} 不存在", dto.Email);
            return NotFound(new ApiResponse<AuthResponseDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 用户 {Email} 登录时发生错误", dto.Email);
            return StatusCode(500, new ApiResponse<AuthResponseDto>
            {
                Success = false,
                Message = "登录失败,请稍后重试"
            });
        }
    }

    /// <summary>
    ///     刷新访问令牌
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> RefreshToken(
        [FromBody] RefreshTokenDto dto,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔄 尝试刷新访问令牌");

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(new ApiResponse<AuthResponseDto>
            {
                Success = false,
                Message = "验证失败",
                Errors = errors
            });
        }

        try
        {
            var authResponse = await _authService.RefreshTokenAsync(dto, cancellationToken);

            return Ok(new ApiResponse<AuthResponseDto>
            {
                Success = true,
                Message = "令牌刷新成功",
                Data = authResponse
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "⚠️ 刷新令牌失败: 未授权");
            return Unauthorized(new ApiResponse<AuthResponseDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 刷新令牌时发生错误");
            return StatusCode(500, new ApiResponse<AuthResponseDto>
            {
                Success = false,
                Message = "刷新令牌失败,请稍后重试"
            });
        }
    }

    /// <summary>
    ///     用户登出
    ///     注意: JWT 是无状态的,客户端需要删除本地存储的 token
    /// </summary>
    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<object>>> Logout(CancellationToken cancellationToken = default)
    {
        // 从 UserContext 获取当前用户 ID
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "未认证用户"
            });

        _logger.LogInformation("👋 用户登出: {UserId}", userContext.UserId);

        try
        {
            await _authService.SignOutAsync(userContext.UserId!, cancellationToken);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "登出成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 登出时发生错误");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "登出失败,请稍后重试"
            });
        }
    }

    /// <summary>
    ///     修改密码
    /// </summary>
    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        // 从 UserContext 获取当前用户 ID
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "未认证用户"
            });

        _logger.LogInformation("🔐 用户修改密码: {UserId}", userContext.UserId);

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "验证失败",
                Errors = errors
            });
        }

        try
        {
            await _authService.ChangePasswordAsync(
                userContext.UserId!,
                request.OldPassword,
                request.NewPassword,
                cancellationToken);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "密码修改成功"
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "⚠️ 用户 {UserId} 修改密码失败: {Message}", userContext.UserId, ex.Message);
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "⚠️ 用户不存在: {UserId}", userContext.UserId);
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 用户 {UserId} 修改密码时发生错误", userContext.UserId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "修改密码失败,请稍后重试"
            });
        }
    }

    /// <summary>
    ///     设置密码（用于未设置密码的用户，如手机号/社交登录用户）
    /// </summary>
    [HttpPost("set-password")]
    public async Task<ActionResult<ApiResponse<object>>> SetPassword(
        [FromBody] SetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
            return Unauthorized(new ApiResponse<object>
            {
                Success = false,
                Message = "未认证用户"
            });

        _logger.LogInformation("🔐 用户设置密码: {UserId}", userContext.UserId);

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "验证失败",
                Errors = errors
            });
        }

        try
        {
            await _authService.SetPasswordAsync(
                userContext.UserId!,
                request.NewPassword,
                cancellationToken);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "密码设置成功"
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 用户 {UserId} 设置密码时发生错误", userContext.UserId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "设置密码失败,请稍后重试"
            });
        }
    }

    /// <summary>
    ///     检查当前用户是否已设置密码
    /// </summary>
    [HttpGet("has-password")]
    public async Task<ActionResult<ApiResponse<HasPasswordResponse>>> HasPassword(
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
            return Unauthorized(new ApiResponse<HasPasswordResponse>
            {
                Success = false,
                Message = "未认证用户"
            });

        try
        {
            var hasPassword = await _authService.HasPasswordAsync(userContext.UserId!, cancellationToken);

            return Ok(new ApiResponse<HasPasswordResponse>
            {
                Success = true,
                Message = "查询成功",
                Data = new HasPasswordResponse { HasPassword = hasPassword }
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResponse<HasPasswordResponse>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 查询用户密码状态失败: {UserId}", userContext.UserId);
            return StatusCode(500, new ApiResponse<HasPasswordResponse>
            {
                Success = false,
                Message = "查询失败,请稍后重试"
            });
        }
    }

    /// <summary>
    ///     检查邮箱是否可用
    /// </summary>
    [HttpGet("check-email")]
    public async Task<ActionResult<ApiResponse<CheckEmailResponse>>> CheckEmail(
        [FromQuery] string email,
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true)
            return Unauthorized(new ApiResponse<CheckEmailResponse>
            {
                Success = false,
                Message = "未认证用户"
            });

        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new ApiResponse<CheckEmailResponse>
            {
                Success = false,
                Message = "邮箱不能为空"
            });

        try
        {
            var available = await _authService.CheckEmailAvailabilityAsync(
                email, userContext.UserId!, cancellationToken);

            return Ok(new ApiResponse<CheckEmailResponse>
            {
                Success = true,
                Message = available ? "邮箱可用" : "邮箱已被占用",
                Data = new CheckEmailResponse { Available = available }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 检查邮箱可用性失败: {Email}", email);
            return StatusCode(500, new ApiResponse<CheckEmailResponse>
            {
                Success = false,
                Message = "检查失败,请稍后重试"
            });
        }
    }

    /// <summary>
    ///     发送短信验证码
    /// </summary>
    [HttpPost("sms/send-code")]
    public async Task<ActionResult<ApiResponse<SendSmsCodeResponse>>> SendSmsCode(
        [FromBody] SendSmsCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📱 发送验证码请求: {Phone}", request.PhoneNumber);

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(new ApiResponse<SendSmsCodeResponse>
            {
                Success = false,
                Message = "验证失败",
                Errors = errors
            });
        }

        try
        {
            var result = await _authService.SendSmsCodeAsync(request, cancellationToken);

            if (!result.Success)
            {
                return BadRequest(new ApiResponse<SendSmsCodeResponse>
                {
                    Success = false,
                    Message = result.Message,
                    Data = result
                });
            }

            return Ok(new ApiResponse<SendSmsCodeResponse>
            {
                Success = true,
                Message = "验证码已发送",
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 发送验证码失败: {Phone}", request.PhoneNumber);
            return StatusCode(500, new ApiResponse<SendSmsCodeResponse>
            {
                Success = false,
                Message = "发送验证码失败,请稍后重试"
            });
        }
    }

    /// <summary>
    ///     手机号验证码登录
    /// </summary>
    [HttpPost("login/phone")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> LoginWithPhone(
        [FromBody] PhoneLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📱 手机号登录: {Phone}", request.PhoneNumber);

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(new ApiResponse<AuthResponseDto>
            {
                Success = false,
                Message = "验证失败",
                Errors = errors
            });
        }

        try
        {
            var authResponse = await _authService.LoginWithPhoneAsync(request, cancellationToken);

            return Ok(new ApiResponse<AuthResponseDto>
            {
                Success = true,
                Message = "登录成功",
                Data = authResponse
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "⚠️ 手机号登录失败: {Phone}, {Message}", request.PhoneNumber, ex.Message);
            return BadRequest(new ApiResponse<AuthResponseDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 手机号登录异常: {Phone}", request.PhoneNumber);
            return StatusCode(500, new ApiResponse<AuthResponseDto>
            {
                Success = false,
                Message = "登录失败,请稍后重试"
            });
        }
    }

    /// <summary>
    ///     社交登录（微信/抖音等）
    ///     用户不存在时自动创建
    /// </summary>
    [HttpPost("social-login")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> SocialLogin(
        [FromBody] SocialLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔐 社交登录: Provider={Provider}", request.Provider);

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(new ApiResponse<AuthResponseDto>
            {
                Success = false,
                Message = "验证失败",
                Errors = errors
            });
        }

        try
        {
            var authResponse = await _authService.SocialLoginAsync(request, cancellationToken);

            return Ok(new ApiResponse<AuthResponseDto>
            {
                Success = true,
                Message = "登录成功",
                Data = authResponse
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "⚠️ 社交登录失败: {Provider}, {Message}", request.Provider, ex.Message);
            return BadRequest(new ApiResponse<AuthResponseDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 社交登录异常: {Provider}", request.Provider);
            return StatusCode(500, new ApiResponse<AuthResponseDto>
            {
                Success = false,
                Message = "登录失败,请稍后重试"
            });
        }
    }

    /// <summary>
    ///     发送找回密码验证码（支持邮箱和手机号，无需登录）
    /// </summary>
    [AllowAnonymous]
    [HttpPost("forgot-password/send-code")]
    public async Task<ActionResult<ApiResponse<SendResetCodeResponse>>> ForgotPasswordSendCode(
        [FromBody] SendResetCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔑 找回密码-发送验证码");

        if (string.IsNullOrWhiteSpace(request.EmailOrPhone))
        {
            return BadRequest(new ApiResponse<SendResetCodeResponse>
            {
                Success = false,
                Message = "请输入邮箱或手机号"
            });
        }

        try
        {
            var result = await _authService.SendResetPasswordCodeAsync(request, cancellationToken);

            if (!result.Success)
            {
                return BadRequest(new ApiResponse<SendResetCodeResponse>
                {
                    Success = false,
                    Message = result.Message,
                    Data = result
                });
            }

            return Ok(new ApiResponse<SendResetCodeResponse>
            {
                Success = true,
                Message = result.Message,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 找回密码发送验证码异常");
            return StatusCode(500, new ApiResponse<SendResetCodeResponse>
            {
                Success = false,
                Message = "发送验证码失败，请稍后重试"
            });
        }
    }

    /// <summary>
    ///     验证验证码并重置密码（无需登录）
    /// </summary>
    [AllowAnonymous]
    [HttpPost("forgot-password/reset")]
    public async Task<ActionResult<ApiResponse<object>>> ForgotPasswordReset(
        [FromBody] ForgotPasswordResetRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔐 找回密码-重置密码");

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = "验证失败",
                Errors = errors
            });
        }

        try
        {
            await _authService.ResetPasswordWithCodeAsync(new ResetPasswordRequest
            {
                EmailOrPhone = request.EmailOrPhone,
                Code = request.Code,
                NewPassword = request.NewPassword
            }, cancellationToken);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "密码重置成功，请使用新密码登录"
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ApiResponse<object>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 重置密码异常");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "重置密码失败，请稍后重试"
            });
        }
    }
}

/// <summary>
///     修改密码请求 DTO
/// </summary>
public class ChangePasswordRequest
{
    [Required(ErrorMessage = "旧密码不能为空")] public string OldPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "新密码不能为空")]
    [MinLength(6, ErrorMessage = "新密码至少需要6个字符")]
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
///     设置密码请求 DTO（用于未设置密码的用户）
/// </summary>
public class SetPasswordRequest
{
    [Required(ErrorMessage = "新密码不能为空")]
    [MinLength(6, ErrorMessage = "密码至少需要6个字符")]
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
///     是否已设置密码响应 DTO
/// </summary>
public class HasPasswordResponse
{
    public bool HasPassword { get; set; }
}

/// <summary>
///     检查邮箱可用性响应 DTO
/// </summary>
public class CheckEmailResponse
{
    public bool Available { get; set; }
}

/// <summary>
///     找回密码-重置密码请求 DTO
/// </summary>
public class ForgotPasswordResetRequest
{
    [Required(ErrorMessage = "请输入邮箱或手机号")]
    public string EmailOrPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "请输入验证码")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "请输入新密码")]
    [MinLength(6, ErrorMessage = "密码至少需要6个字符")]
    public string NewPassword { get; set; } = string.Empty;
}