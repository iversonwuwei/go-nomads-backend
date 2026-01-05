namespace Gateway.Middleware;

/// <summary>
/// HTTP 方法重写中间件
/// 用于解决某些网络环境（如部分 ISP、IDC 防火墙）不支持 PUT/DELETE 方法的问题
/// 
/// 工作原理：
/// 1. 客户端发送 POST 请求，并在请求头中添加 X-HTTP-Method-Override: PUT 或 DELETE
/// 2. 此中间件读取该请求头，将请求方法重写为指定的方法
/// 3. 后续中间件和控制器将看到重写后的方法
/// 
/// 使用示例（客户端）：
/// POST /api/v1/users/me HTTP/1.1
/// X-HTTP-Method-Override: PUT
/// Content-Type: application/json
/// 
/// {"name": "New Name"}
/// </summary>
public class HttpMethodOverrideMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HttpMethodOverrideMiddleware> _logger;
    
    /// <summary>
    /// HTTP 方法重写请求头名称
    /// </summary>
    public const string HttpMethodOverrideHeader = "X-HTTP-Method-Override";
    
    /// <summary>
    /// 允许重写的目标方法
    /// </summary>
    private static readonly HashSet<string> AllowedOverrideMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "PUT",
        "DELETE",
        "PATCH"
    };

    public HttpMethodOverrideMiddleware(RequestDelegate next, ILogger<HttpMethodOverrideMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 仅当请求方法为 POST 时才检查重写
        if (HttpMethods.IsPost(context.Request.Method))
        {
            // 检查是否存在方法重写头
            if (context.Request.Headers.TryGetValue(HttpMethodOverrideHeader, out var methodOverride))
            {
                var targetMethod = methodOverride.ToString().ToUpperInvariant();
                
                if (AllowedOverrideMethods.Contains(targetMethod))
                {
                    _logger.LogDebug(
                        "🔄 HTTP Method Override: POST -> {TargetMethod} for {Path}",
                        targetMethod,
                        context.Request.Path);
                    
                    // 重写请求方法
                    context.Request.Method = targetMethod;
                }
                else
                {
                    _logger.LogWarning(
                        "⚠️ Invalid HTTP Method Override attempted: {AttemptedMethod} for {Path}",
                        targetMethod,
                        context.Request.Path);
                }
            }
        }

        await _next(context);
    }
}

/// <summary>
/// HttpMethodOverrideMiddleware 扩展方法
/// </summary>
public static class HttpMethodOverrideMiddlewareExtensions
{
    /// <summary>
    /// 启用 HTTP 方法重写中间件
    /// 
    /// 此中间件应该放在路由中间件之前，以确保路由能够正确匹配重写后的方法
    /// </summary>
    public static IApplicationBuilder UseCustomHttpMethodOverride(this IApplicationBuilder app)
    {
        return app.UseMiddleware<HttpMethodOverrideMiddleware>();
    }
}
