using System.Security.Claims;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace Gateway.Middleware;

/// <summary>
///     YARP 转换器 - 将 JWT 认证信息添加到转发的请求头中
/// </summary>
public class JwtAuthenticationTransform : ITransformProvider
{
    private readonly ILogger<JwtAuthenticationTransform> _logger;

    public JwtAuthenticationTransform(ILogger<JwtAuthenticationTransform> logger)
    {
        _logger = logger;
    }

    public void ValidateRoute(TransformRouteValidationContext context)
    {
        // 验证路由配置 - 这里我们不需要做什么
    }

    public void ValidateCluster(TransformClusterValidationContext context)
    {
        // 验证集群配置 - 这里我们不需要做什么
    }

    public void Apply(TransformBuilderContext context)
    {
        // 为所有路由添加请求转换
        context.AddRequestTransform(async transformContext =>
        {
            var httpContext = transformContext.HttpContext;

            _logger.LogInformation("🔍 JwtAuthenticationTransform - 请求路径: {Path}", httpContext.Request.Path);
            _logger.LogInformation("   User.Identity?.IsAuthenticated: {IsAuth}",
                httpContext.User.Identity?.IsAuthenticated);

            // 检查用户是否已认证
            if (httpContext.User.Identity?.IsAuthenticated == true)
            {
                // 提取用户信息 (优先使用 Supabase 的标准 Claim 名称)
                var userId = httpContext.User.FindFirst("sub")?.Value
                             ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var email = httpContext.User.FindFirst("email")?.Value
                            ?? httpContext.User.FindFirst(ClaimTypes.Email)?.Value;
                var role = httpContext.User.FindFirst("role")?.Value
                           ?? httpContext.User.FindFirst(ClaimTypes.Role)?.Value;

                _logger.LogInformation("   提取到的用户信息: UserId={UserId}, Email={Email}, Role={Role}", userId, email, role);

                // 添加自定义请求头，传递给下游服务
                // 先移除可能存在的旧头，避免重复
                if (!string.IsNullOrEmpty(userId))
                {
                    transformContext.ProxyRequest.Headers.Remove("X-User-Id");
                    transformContext.ProxyRequest.Headers.Add("X-User-Id", userId);
                    _logger.LogInformation("   ✅ 添加 X-User-Id: {UserId}", userId);
                }
                else
                {
                    _logger.LogWarning("   ⚠️ UserId 为空，未添加 X-User-Id header");
                }

                if (!string.IsNullOrEmpty(email))
                {
                    transformContext.ProxyRequest.Headers.Remove("X-User-Email");
                    transformContext.ProxyRequest.Headers.Add("X-User-Email", email);
                    _logger.LogInformation("   ✅ 添加 X-User-Email: {Email}", email);
                }

                if (!string.IsNullOrEmpty(role))
                {
                    transformContext.ProxyRequest.Headers.Remove("X-User-Role");
                    transformContext.ProxyRequest.Headers.Add("X-User-Role", role);
                    _logger.LogInformation("   ✅ 添加 X-User-Role: {Role}", role);
                }

                // 传递原始的 Authorization 头
                if (httpContext.Request.Headers.TryGetValue("Authorization", out var authHeader))
                    transformContext.ProxyRequest.Headers.TryAddWithoutValidation("Authorization",
                        authHeader.ToString());

                _logger.LogDebug(
                    "JWT Authentication - User authenticated: UserId={UserId}, Email={Email}, Role={Role}",
                    userId, email, role);
            }
            else
            {
                _logger.LogWarning("⚠️ JWT Authentication - Request is not authenticated. Path: {Path}",
                    httpContext.Request.Path);
            }

            await Task.CompletedTask;
        });
    }
}