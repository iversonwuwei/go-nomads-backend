using System.ComponentModel.DataAnnotations;
using GoNomads.Shared.Middleware;
using GoNomads.Shared.Models;
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
    ///     社交登录（微信/QQ/支付宝等）
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