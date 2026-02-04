using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;

namespace GoNomads.Shared.Observability;

/// <summary>
/// 分布式追踪中间件
/// 增强请求追踪，添加自定义标签和事件
/// </summary>
public class TracingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TracingMiddleware> _logger;
    private readonly ActivitySource _activitySource;
    private readonly GoNomadsMetrics _metrics;

    public TracingMiddleware(
        RequestDelegate next,
        ILogger<TracingMiddleware> logger,
        ActivitySource activitySource,
        GoNomadsMetrics metrics)
    {
        _next = next;
        _logger = logger;
        _activitySource = activitySource;
        _metrics = metrics;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        _metrics.IncrementActiveRequests();

        // 获取或创建 trace 上下文
        var activity = Activity.Current;
        
        // 添加请求开始事件
        activity?.AddEvent(new ActivityEvent("request.started", tags: new ActivityTagsCollection
        {
            { "http.method", context.Request.Method },
            { "http.path", context.Request.Path.Value }
        }));

        // 从请求头获取追踪信息
        var traceId = context.Request.Headers["X-Trace-Id"].FirstOrDefault() 
                      ?? activity?.TraceId.ToString() 
                      ?? Guid.NewGuid().ToString("N");
        
        var spanId = activity?.SpanId.ToString() ?? Guid.NewGuid().ToString("N")[..16];
        var parentSpanId = context.Request.Headers["X-Parent-Span-Id"].FirstOrDefault();

        // 设置响应头，便于客户端追踪
        context.Response.Headers["X-Trace-Id"] = traceId;
        context.Response.Headers["X-Span-Id"] = spanId;
        context.Response.Headers["X-Request-Id"] = context.TraceIdentifier;

        // 添加自定义标签
        activity?.SetTag("request.id", context.TraceIdentifier);
        activity?.SetTag("trace.id", traceId);
        
        if (!string.IsNullOrEmpty(parentSpanId))
        {
            activity?.SetTag("parent.span.id", parentSpanId);
        }

        // 添加用户信息标签
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst("sub")?.Value;
            var userEmail = context.User.FindFirst("email")?.Value;
            var userRole = context.User.FindFirst("role")?.Value;

            activity?.SetTag("enduser.id", userId);
            activity?.SetTag("enduser.email", userEmail);
            activity?.SetTag("enduser.role", userRole);
        }

        // 添加请求元数据
        activity?.SetTag("http.client_ip", GetClientIpAddress(context));
        activity?.SetTag("http.request_content_type", context.Request.ContentType);
        activity?.SetTag("http.request_content_length", context.Request.ContentLength);

        // 使用日志作用域关联 trace 信息
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["TraceId"] = traceId,
            ["SpanId"] = spanId,
            ["RequestId"] = context.TraceIdentifier,
            ["RequestPath"] = context.Request.Path.Value ?? "",
            ["RequestMethod"] = context.Request.Method
        }))
        {
            try
            {
                await _next(context);

                // 记录成功响应
                activity?.SetTag("http.status_code", context.Response.StatusCode);
                activity?.SetStatus(context.Response.StatusCode < 400 
                    ? ActivityStatusCode.Ok 
                    : ActivityStatusCode.Error);

                activity?.AddEvent(new ActivityEvent("request.completed", tags: new ActivityTagsCollection
                {
                    { "http.status_code", context.Response.StatusCode },
                    { "http.response_content_length", context.Response.ContentLength }
                }));
            }
            catch (Exception ex)
            {
                // 记录异常信息
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity?.AddException(ex);
                
                activity?.AddEvent(new ActivityEvent("request.failed", tags: new ActivityTagsCollection
                {
                    { "exception.type", ex.GetType().FullName },
                    { "exception.message", ex.Message }
                }));

                _logger.LogError(ex, 
                    "Request failed: {Method} {Path} - TraceId: {TraceId}", 
                    context.Request.Method, 
                    context.Request.Path, 
                    traceId);

                throw;
            }
            finally
            {
                stopwatch.Stop();
                _metrics.DecrementActiveRequests();

                // 记录请求指标
                _metrics.RecordHttpRequest(
                    context.Request.Method,
                    context.Request.Path.Value ?? "/",
                    context.Response.StatusCode,
                    stopwatch.Elapsed.TotalSeconds);

                // 添加持续时间标签
                activity?.SetTag("http.duration_ms", stopwatch.ElapsedMilliseconds);

                _logger.LogInformation(
                    "Request completed: {Method} {Path} - Status: {StatusCode} - Duration: {Duration}ms - TraceId: {TraceId}",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds,
                    traceId);
            }
        }
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        // 尝试从 X-Forwarded-For 头获取（通过代理）
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        // 尝试从 X-Real-IP 头获取
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        // 回退到远程 IP 地址
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

/// <summary>
/// TracingMiddleware 扩展方法
/// </summary>
public static class TracingMiddlewareExtensions
{
    public static IApplicationBuilder UseGoNomadsTracing(this IApplicationBuilder app)
    {
        return app.UseMiddleware<TracingMiddleware>();
    }
}
