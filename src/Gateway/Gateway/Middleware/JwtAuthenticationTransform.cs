using System.Security.Claims;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace Gateway.Middleware;

/// <summary>
/// YARP 转换器 - 将 JWT 认证信息添加到转发的请求头中
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

                // 添加自定义请求头，传递给下游服务
                if (!string.IsNullOrEmpty(userId))
                {
                    transformContext.ProxyRequest.Headers.Add("X-User-Id", userId);
                }
                
                if (!string.IsNullOrEmpty(email))
                {
                    transformContext.ProxyRequest.Headers.Add("X-User-Email", email);
                }
                
                if (!string.IsNullOrEmpty(role))
                {
                    transformContext.ProxyRequest.Headers.Add("X-User-Role", role);
                }

                // 传递原始的 Authorization 头
                if (httpContext.Request.Headers.TryGetValue("Authorization", out var authHeader))
                {
                    transformContext.ProxyRequest.Headers.TryAddWithoutValidation("Authorization", authHeader.ToString());
                }

                _logger.LogDebug(
                    "JWT Authentication - User authenticated: UserId={UserId}, Email={Email}, Role={Role}", 
                    userId, email, role);
            }
            else
            {
                _logger.LogDebug("JWT Authentication - Request is not authenticated");
            }

            await Task.CompletedTask;
        });
    }
}
