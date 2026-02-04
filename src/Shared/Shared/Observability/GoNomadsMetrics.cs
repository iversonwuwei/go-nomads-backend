using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace GoNomads.Shared.Observability;

/// <summary>
/// Go Nomads 业务指标收集器
/// 用于收集自定义业务指标，与 Prometheus 集成
/// </summary>
public class GoNomadsMetrics
{
    private readonly Meter _meter;
    private readonly string _serviceName;

    // HTTP 请求相关指标
    private readonly Counter<long> _httpRequestsTotal;
    private readonly Histogram<double> _httpRequestDuration;
    private readonly Counter<long> _httpRequestErrors;

    // 业务操作指标
    private readonly Counter<long> _businessOperationsTotal;
    private readonly Histogram<double> _businessOperationDuration;
    private readonly Counter<long> _businessOperationErrors;

    // 外部服务调用指标
    private readonly Counter<long> _externalServiceCalls;
    private readonly Histogram<double> _externalServiceDuration;
    private readonly Counter<long> _externalServiceErrors;

    // 数据库操作指标
    private readonly Counter<long> _databaseOperations;
    private readonly Histogram<double> _databaseOperationDuration;
    private readonly Counter<long> _databaseOperationErrors;

    // 缓存操作指标
    private readonly Counter<long> _cacheHits;
    private readonly Counter<long> _cacheMisses;

    // 认证相关指标
    private readonly Counter<long> _authenticationAttempts;
    private readonly Counter<long> _authenticationFailures;
    private readonly Counter<long> _authorizationFailures;

    // 消息队列指标
    private readonly Counter<long> _messagesPublished;
    private readonly Counter<long> _messagesConsumed;
    private readonly Counter<long> _messageProcessingErrors;

    // 当前活动连接数（Gauge 通过 ObservableGauge 实现）
    private int _activeConnections;
    private int _activeRequests;

    public GoNomadsMetrics(string serviceName)
    {
        _serviceName = serviceName;
        _meter = new Meter(serviceName, "1.0.0");

        // HTTP 请求指标
        _httpRequestsTotal = _meter.CreateCounter<long>(
            "gonomads_http_requests_total",
            "requests",
            "Total number of HTTP requests");

        _httpRequestDuration = _meter.CreateHistogram<double>(
            "gonomads_http_request_duration_seconds",
            "seconds",
            "HTTP request duration in seconds");

        _httpRequestErrors = _meter.CreateCounter<long>(
            "gonomads_http_request_errors_total",
            "errors",
            "Total number of HTTP request errors");

        // 业务操作指标
        _businessOperationsTotal = _meter.CreateCounter<long>(
            "gonomads_business_operations_total",
            "operations",
            "Total number of business operations");

        _businessOperationDuration = _meter.CreateHistogram<double>(
            "gonomads_business_operation_duration_seconds",
            "seconds",
            "Business operation duration in seconds");

        _businessOperationErrors = _meter.CreateCounter<long>(
            "gonomads_business_operation_errors_total",
            "errors",
            "Total number of business operation errors");

        // 外部服务调用指标
        _externalServiceCalls = _meter.CreateCounter<long>(
            "gonomads_external_service_calls_total",
            "calls",
            "Total number of external service calls");

        _externalServiceDuration = _meter.CreateHistogram<double>(
            "gonomads_external_service_duration_seconds",
            "seconds",
            "External service call duration in seconds");

        _externalServiceErrors = _meter.CreateCounter<long>(
            "gonomads_external_service_errors_total",
            "errors",
            "Total number of external service call errors");

        // 数据库操作指标
        _databaseOperations = _meter.CreateCounter<long>(
            "gonomads_database_operations_total",
            "operations",
            "Total number of database operations");

        _databaseOperationDuration = _meter.CreateHistogram<double>(
            "gonomads_database_operation_duration_seconds",
            "seconds",
            "Database operation duration in seconds");

        _databaseOperationErrors = _meter.CreateCounter<long>(
            "gonomads_database_operation_errors_total",
            "errors",
            "Total number of database operation errors");

        // 缓存指标
        _cacheHits = _meter.CreateCounter<long>(
            "gonomads_cache_hits_total",
            "hits",
            "Total number of cache hits");

        _cacheMisses = _meter.CreateCounter<long>(
            "gonomads_cache_misses_total",
            "misses",
            "Total number of cache misses");

        // 认证指标
        _authenticationAttempts = _meter.CreateCounter<long>(
            "gonomads_authentication_attempts_total",
            "attempts",
            "Total number of authentication attempts");

        _authenticationFailures = _meter.CreateCounter<long>(
            "gonomads_authentication_failures_total",
            "failures",
            "Total number of authentication failures");

        _authorizationFailures = _meter.CreateCounter<long>(
            "gonomads_authorization_failures_total",
            "failures",
            "Total number of authorization failures");

        // 消息队列指标
        _messagesPublished = _meter.CreateCounter<long>(
            "gonomads_messages_published_total",
            "messages",
            "Total number of messages published");

        _messagesConsumed = _meter.CreateCounter<long>(
            "gonomads_messages_consumed_total",
            "messages",
            "Total number of messages consumed");

        _messageProcessingErrors = _meter.CreateCounter<long>(
            "gonomads_message_processing_errors_total",
            "errors",
            "Total number of message processing errors");

        // Gauge 指标 (Observable)
        _meter.CreateObservableGauge(
            "gonomads_active_connections",
            () => _activeConnections,
            "connections",
            "Number of active connections");

        _meter.CreateObservableGauge(
            "gonomads_active_requests",
            () => _activeRequests,
            "requests",
            "Number of active requests being processed");
    }

    #region HTTP 请求指标

    public void RecordHttpRequest(string method, string endpoint, int statusCode, double durationSeconds)
    {
        var tags = new TagList
        {
            { "service", _serviceName },
            { "method", method },
            { "endpoint", NormalizeEndpoint(endpoint) },
            { "status_code", statusCode.ToString() },
            { "status_class", GetStatusClass(statusCode) }
        };

        _httpRequestsTotal.Add(1, tags);
        _httpRequestDuration.Record(durationSeconds, tags);

        if (statusCode >= 400)
        {
            _httpRequestErrors.Add(1, tags);
        }
    }

    #endregion

    #region 业务操作指标

    public void RecordBusinessOperation(string operation, string result, double durationSeconds)
    {
        var tags = new TagList
        {
            { "service", _serviceName },
            { "operation", operation },
            { "result", result }
        };

        _businessOperationsTotal.Add(1, tags);
        _businessOperationDuration.Record(durationSeconds, tags);
    }

    public void RecordBusinessOperationError(string operation, string errorType)
    {
        var tags = new TagList
        {
            { "service", _serviceName },
            { "operation", operation },
            { "error_type", errorType }
        };

        _businessOperationErrors.Add(1, tags);
    }

    /// <summary>
    /// 创建业务操作计时器
    /// </summary>
    public BusinessOperationTimer StartBusinessOperation(string operation)
    {
        return new BusinessOperationTimer(this, operation);
    }

    #endregion

    #region 外部服务调用指标

    public void RecordExternalServiceCall(string serviceName, string operation, bool success, double durationSeconds)
    {
        var tags = new TagList
        {
            { "service", _serviceName },
            { "target_service", serviceName },
            { "operation", operation },
            { "success", success.ToString().ToLower() }
        };

        _externalServiceCalls.Add(1, tags);
        _externalServiceDuration.Record(durationSeconds, tags);

        if (!success)
        {
            _externalServiceErrors.Add(1, tags);
        }
    }

    /// <summary>
    /// 创建外部服务调用计时器
    /// </summary>
    public ExternalServiceTimer StartExternalServiceCall(string serviceName, string operation)
    {
        return new ExternalServiceTimer(this, serviceName, operation);
    }

    #endregion

    #region 数据库操作指标

    public void RecordDatabaseOperation(string operation, string table, bool success, double durationSeconds)
    {
        var tags = new TagList
        {
            { "service", _serviceName },
            { "operation", operation },
            { "table", table },
            { "success", success.ToString().ToLower() }
        };

        _databaseOperations.Add(1, tags);
        _databaseOperationDuration.Record(durationSeconds, tags);

        if (!success)
        {
            _databaseOperationErrors.Add(1, tags);
        }
    }

    /// <summary>
    /// 创建数据库操作计时器
    /// </summary>
    public DatabaseOperationTimer StartDatabaseOperation(string operation, string table)
    {
        return new DatabaseOperationTimer(this, operation, table);
    }

    #endregion

    #region 缓存指标

    public void RecordCacheHit(string cacheType, string key)
    {
        var tags = new TagList
        {
            { "service", _serviceName },
            { "cache_type", cacheType },
            { "key_prefix", GetKeyPrefix(key) }
        };

        _cacheHits.Add(1, tags);
    }

    public void RecordCacheMiss(string cacheType, string key)
    {
        var tags = new TagList
        {
            { "service", _serviceName },
            { "cache_type", cacheType },
            { "key_prefix", GetKeyPrefix(key) }
        };

        _cacheMisses.Add(1, tags);
    }

    #endregion

    #region 认证指标

    public void RecordAuthenticationAttempt(bool success, string method = "jwt")
    {
        var tags = new TagList
        {
            { "service", _serviceName },
            { "method", method },
            { "success", success.ToString().ToLower() }
        };

        _authenticationAttempts.Add(1, tags);

        if (!success)
        {
            _authenticationFailures.Add(1, tags);
        }
    }

    public void RecordAuthorizationFailure(string resource, string action)
    {
        var tags = new TagList
        {
            { "service", _serviceName },
            { "resource", resource },
            { "action", action }
        };

        _authorizationFailures.Add(1, tags);
    }

    #endregion

    #region 消息队列指标

    public void RecordMessagePublished(string messageType, string exchange)
    {
        var tags = new TagList
        {
            { "service", _serviceName },
            { "message_type", messageType },
            { "exchange", exchange }
        };

        _messagesPublished.Add(1, tags);
    }

    public void RecordMessageConsumed(string messageType, string queue, bool success)
    {
        var tags = new TagList
        {
            { "service", _serviceName },
            { "message_type", messageType },
            { "queue", queue },
            { "success", success.ToString().ToLower() }
        };

        _messagesConsumed.Add(1, tags);

        if (!success)
        {
            _messageProcessingErrors.Add(1, tags);
        }
    }

    #endregion

    #region 连接数指标

    public void IncrementActiveConnections() => Interlocked.Increment(ref _activeConnections);
    public void DecrementActiveConnections() => Interlocked.Decrement(ref _activeConnections);
    public void IncrementActiveRequests() => Interlocked.Increment(ref _activeRequests);
    public void DecrementActiveRequests() => Interlocked.Decrement(ref _activeRequests);

    #endregion

    #region 辅助方法

    private static string GetStatusClass(int statusCode) => statusCode switch
    {
        >= 200 and < 300 => "2xx",
        >= 300 and < 400 => "3xx",
        >= 400 and < 500 => "4xx",
        >= 500 => "5xx",
        _ => "unknown"
    };

    private static string NormalizeEndpoint(string endpoint)
    {
        // 移除 ID 参数，保持端点一致性
        // 例如: /api/users/123 -> /api/users/{id}
        var parts = endpoint.Split('/');
        for (var i = 0; i < parts.Length; i++)
        {
            if (Guid.TryParse(parts[i], out _) || long.TryParse(parts[i], out _))
            {
                parts[i] = "{id}";
            }
        }
        return string.Join("/", parts);
    }

    private static string GetKeyPrefix(string key)
    {
        // 获取缓存 key 的前缀，避免高基数
        var colonIndex = key.IndexOf(':');
        return colonIndex > 0 ? key[..colonIndex] : key;
    }

    #endregion
}

#region 计时器类

/// <summary>
/// 业务操作计时器
/// </summary>
public class BusinessOperationTimer : IDisposable
{
    private readonly GoNomadsMetrics _metrics;
    private readonly string _operation;
    private readonly DateTime _startTime;
    private string _result = "success";
    private bool _disposed;

    internal BusinessOperationTimer(GoNomadsMetrics metrics, string operation)
    {
        _metrics = metrics;
        _operation = operation;
        _startTime = DateTime.UtcNow;
    }

    public void SetResult(string result) => _result = result;
    public void SetError(string errorType)
    {
        _result = "error";
        _metrics.RecordBusinessOperationError(_operation, errorType);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        var duration = (DateTime.UtcNow - _startTime).TotalSeconds;
        _metrics.RecordBusinessOperation(_operation, _result, duration);
    }
}

/// <summary>
/// 外部服务调用计时器
/// </summary>
public class ExternalServiceTimer : IDisposable
{
    private readonly GoNomadsMetrics _metrics;
    private readonly string _serviceName;
    private readonly string _operation;
    private readonly DateTime _startTime;
    private bool _success = true;
    private bool _disposed;

    internal ExternalServiceTimer(GoNomadsMetrics metrics, string serviceName, string operation)
    {
        _metrics = metrics;
        _serviceName = serviceName;
        _operation = operation;
        _startTime = DateTime.UtcNow;
    }

    public void SetError() => _success = false;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        var duration = (DateTime.UtcNow - _startTime).TotalSeconds;
        _metrics.RecordExternalServiceCall(_serviceName, _operation, _success, duration);
    }
}

/// <summary>
/// 数据库操作计时器
/// </summary>
public class DatabaseOperationTimer : IDisposable
{
    private readonly GoNomadsMetrics _metrics;
    private readonly string _operation;
    private readonly string _table;
    private readonly DateTime _startTime;
    private bool _success = true;
    private bool _disposed;

    internal DatabaseOperationTimer(GoNomadsMetrics metrics, string operation, string table)
    {
        _metrics = metrics;
        _operation = operation;
        _table = table;
        _startTime = DateTime.UtcNow;
    }

    public void SetError() => _success = false;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        var duration = (DateTime.UtcNow - _startTime).TotalSeconds;
        _metrics.RecordDatabaseOperation(_operation, _table, _success, duration);
    }
}

#endregion
