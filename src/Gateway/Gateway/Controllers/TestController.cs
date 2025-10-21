using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Gateway.Services;

namespace Gateway.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]  // 允许匿名访问测试端点
public class TestController : ControllerBase
{
    private readonly ILogger<TestController> _logger;

    public TestController(ILogger<TestController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 测试登录限流 (5次/分钟)
    /// </summary>
    [HttpPost("login")]
    [EnableRateLimiting(RateLimitConfig.LoginPolicy)]
    public IActionResult TestLogin([FromBody] TestRequest request)
    {
        _logger.LogInformation("Test login attempt for email: {Email}", request.Email);
        
        return Ok(new
        {
            success = false,
            message = "测试登录失败（这是预期的）",
            email = request.Email,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 测试注册限流 (3次/小时)
    /// </summary>
    [HttpPost("register")]
    [EnableRateLimiting(RateLimitConfig.RegisterPolicy)]
    public IActionResult TestRegister([FromBody] TestRequest request)
    {
        _logger.LogInformation("Test register attempt for email: {Email}", request.Email);
        
        return Ok(new
        {
            success = false,
            message = "测试注册失败（这是预期的）",
            email = request.Email,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 测试 API 限流 (100次/分钟)
    /// </summary>
    [HttpGet]
    [EnableRateLimiting(RateLimitConfig.ApiPolicy)]
    public IActionResult TestApi()
    {
        return Ok(new
        {
            success = true,
            message = "API 测试成功",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 测试严格限流（令牌桶）
    /// </summary>
    [HttpPost("admin")]
    [EnableRateLimiting(RateLimitConfig.StrictPolicy)]
    public IActionResult TestAdmin()
    {
        return Ok(new
        {
            success = true,
            message = "管理员操作测试",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// 无限流测试端点
    /// </summary>
    [HttpGet("unlimited")]
    [DisableRateLimiting]
    public IActionResult TestUnlimited()
    {
        return Ok(new
        {
            success = true,
            message = "无限流端点",
            timestamp = DateTime.UtcNow
        });
    }
}

public record TestRequest(string Email, string? Password);
