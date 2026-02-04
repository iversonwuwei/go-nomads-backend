using System.Diagnostics;
using OpenTelemetry.Trace;

namespace GoNomads.Shared.Observability;

/// <summary>
/// 分布式追踪辅助类
/// 提供便捷的 Span 创建和管理方法
/// </summary>
public static class TracingHelper
{
    /// <summary>
    /// 创建一个新的业务操作 Span
    /// </summary>
    public static Activity? StartActivity(
        ActivitySource source,
        string operationName,
        ActivityKind kind = ActivityKind.Internal,
        IDictionary<string, object?>? tags = null)
    {
        var activity = source.StartActivity(operationName, kind);
        
        if (activity != null && tags != null)
        {
            foreach (var tag in tags)
            {
                activity.SetTag(tag.Key, tag.Value);
            }
        }

        return activity;
    }

    /// <summary>
    /// 创建数据库操作 Span
    /// </summary>
    public static Activity? StartDatabaseActivity(
        ActivitySource source,
        string operation,
        string table,
        string? statement = null)
    {
        var activity = source.StartActivity($"db.{operation}", ActivityKind.Client);
        
        activity?.SetTag("db.system", "postgresql");
        activity?.SetTag("db.operation", operation);
        activity?.SetTag("db.sql.table", table);
        
        if (!string.IsNullOrEmpty(statement))
        {
            activity?.SetTag("db.statement", statement);
        }

        return activity;
    }

    /// <summary>
    /// 创建外部 HTTP 调用 Span
    /// </summary>
    public static Activity? StartHttpClientActivity(
        ActivitySource source,
        string method,
        string url,
        string targetService)
    {
        var activity = source.StartActivity($"http.{method.ToLower()}", ActivityKind.Client);
        
        activity?.SetTag("http.method", method);
        activity?.SetTag("http.url", url);
        activity?.SetTag("peer.service", targetService);

        return activity;
    }

    /// <summary>
    /// 创建消息队列发布 Span
    /// </summary>
    public static Activity? StartMessagePublishActivity(
        ActivitySource source,
        string messageType,
        string exchange)
    {
        var activity = source.StartActivity($"publish.{messageType}", ActivityKind.Producer);
        
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination", exchange);
        activity?.SetTag("messaging.destination_kind", "exchange");
        activity?.SetTag("messaging.message_type", messageType);

        return activity;
    }

    /// <summary>
    /// 创建消息队列消费 Span
    /// </summary>
    public static Activity? StartMessageConsumeActivity(
        ActivitySource source,
        string messageType,
        string queue)
    {
        var activity = source.StartActivity($"consume.{messageType}", ActivityKind.Consumer);
        
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.source", queue);
        activity?.SetTag("messaging.source_kind", "queue");
        activity?.SetTag("messaging.message_type", messageType);

        return activity;
    }

    /// <summary>
    /// 创建缓存操作 Span
    /// </summary>
    public static Activity? StartCacheActivity(
        ActivitySource source,
        string operation,
        string key)
    {
        var activity = source.StartActivity($"cache.{operation}", ActivityKind.Client);
        
        activity?.SetTag("db.system", "redis");
        activity?.SetTag("db.operation", operation);
        activity?.SetTag("cache.key", key);

        return activity;
    }

    /// <summary>
    /// 记录异常到当前 Activity
    /// </summary>
    public static void RecordException(Exception exception, IDictionary<string, object?>? additionalTags = null)
    {
        var activity = Activity.Current;
        if (activity == null) return;

        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity.AddException(exception);
        
        activity.SetTag("error", true);
        activity.SetTag("exception.type", exception.GetType().FullName);
        activity.SetTag("exception.message", exception.Message);

        if (additionalTags != null)
        {
            foreach (var tag in additionalTags)
            {
                activity.SetTag(tag.Key, tag.Value);
            }
        }
    }

    /// <summary>
    /// 添加事件到当前 Activity
    /// </summary>
    public static void AddEvent(string name, IDictionary<string, object?>? attributes = null)
    {
        var activity = Activity.Current;
        if (activity == null) return;

        if (attributes != null)
        {
            var tagsCollection = new ActivityTagsCollection();
            foreach (var attr in attributes)
            {
                tagsCollection.Add(attr.Key, attr.Value);
            }
            activity.AddEvent(new ActivityEvent(name, tags: tagsCollection));
        }
        else
        {
            activity.AddEvent(new ActivityEvent(name));
        }
    }

    /// <summary>
    /// 设置当前 Activity 的状态为成功
    /// </summary>
    public static void SetSuccess(string? description = null)
    {
        Activity.Current?.SetStatus(ActivityStatusCode.Ok, description);
    }

    /// <summary>
    /// 设置当前 Activity 的状态为错误
    /// </summary>
    public static void SetError(string description)
    {
        var activity = Activity.Current;
        if (activity == null) return;

        activity.SetStatus(ActivityStatusCode.Error, description);
        activity.SetTag("error", true);
    }

    /// <summary>
    /// 获取当前 Trace ID
    /// </summary>
    public static string? GetCurrentTraceId()
    {
        return Activity.Current?.TraceId.ToString();
    }

    /// <summary>
    /// 获取当前 Span ID
    /// </summary>
    public static string? GetCurrentSpanId()
    {
        return Activity.Current?.SpanId.ToString();
    }

    /// <summary>
    /// 添加 Baggage（跨服务传递的上下文数据）
    /// </summary>
    public static void AddBaggage(string key, string value)
    {
        Activity.Current?.AddBaggage(key, value);
    }

    /// <summary>
    /// 获取 Baggage 值
    /// </summary>
    public static string? GetBaggage(string key)
    {
        return Activity.Current?.GetBaggageItem(key);
    }
}

/// <summary>
/// Activity 扩展方法
/// </summary>
public static class ActivityExtensions
{
    /// <summary>
    /// 设置用户信息标签
    /// </summary>
    public static Activity SetUserInfo(this Activity activity, string? userId, string? email = null, string? role = null)
    {
        if (!string.IsNullOrEmpty(userId))
            activity.SetTag("enduser.id", userId);
        
        if (!string.IsNullOrEmpty(email))
            activity.SetTag("enduser.email", email);
        
        if (!string.IsNullOrEmpty(role))
            activity.SetTag("enduser.role", role);

        return activity;
    }

    /// <summary>
    /// 设置请求信息标签
    /// </summary>
    public static Activity SetRequestInfo(this Activity activity, string method, string path, string? clientIp = null)
    {
        activity.SetTag("http.method", method);
        activity.SetTag("http.route", path);
        
        if (!string.IsNullOrEmpty(clientIp))
            activity.SetTag("http.client_ip", clientIp);

        return activity;
    }

    /// <summary>
    /// 设置响应信息标签
    /// </summary>
    public static Activity SetResponseInfo(this Activity activity, int statusCode, long? contentLength = null)
    {
        activity.SetTag("http.status_code", statusCode);
        
        if (contentLength.HasValue)
            activity.SetTag("http.response_content_length", contentLength.Value);

        activity.SetStatus(statusCode < 400 ? ActivityStatusCode.Ok : ActivityStatusCode.Error);

        return activity;
    }

    /// <summary>
    /// 设置数据库信息标签
    /// </summary>
    public static Activity SetDatabaseInfo(this Activity activity, string operation, string table, int? rowsAffected = null)
    {
        activity.SetTag("db.operation", operation);
        activity.SetTag("db.sql.table", table);
        
        if (rowsAffected.HasValue)
            activity.SetTag("db.rows_affected", rowsAffected.Value);

        return activity;
    }

    /// <summary>
    /// 记录耗时
    /// </summary>
    public static Activity SetDuration(this Activity activity, TimeSpan duration)
    {
        activity.SetTag("duration_ms", duration.TotalMilliseconds);
        return activity;
    }
}
