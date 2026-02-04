using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace GoNomads.Shared.Observability;

/// <summary>
/// OpenTelemetry 可观测性配置扩展
/// 企业级解决方案：OpenTelemetry + Jaeger + Prometheus
/// </summary>
public static class OpenTelemetryExtensions
{
    /// <summary>
    /// 添加完整的可观测性支持（Traces + Metrics + Logs）
    /// </summary>
    public static IServiceCollection AddGoNomadsObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        string? serviceVersion = null)
    {
        var version = serviceVersion ?? Assembly.GetCallingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "1.0.0";

        var observabilityConfig = configuration.GetSection("Observability").Get<ObservabilityConfig>()
                                  ?? new ObservabilityConfig();

        // 构建资源信息
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(
                serviceName: serviceName,
                serviceVersion: version,
                serviceInstanceId: Environment.MachineName)
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment"] = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development",
                ["service.namespace"] = "go-nomads",
                ["host.name"] = Environment.MachineName
            });

        // 添加自定义 Metrics 服务
        services.AddSingleton(new GoNomadsMetrics(serviceName));
        
        // 添加 Activity Source 用于自定义 tracing
        services.AddSingleton(new ActivitySource(serviceName, version));

        // 配置 OpenTelemetry
        services.AddOpenTelemetry()
            .WithTracing(tracerBuilder => ConfigureTracing(tracerBuilder, resourceBuilder, observabilityConfig, serviceName))
            .WithMetrics(metricsBuilder => ConfigureMetrics(metricsBuilder, resourceBuilder, observabilityConfig, serviceName));

        return services;
    }

    /// <summary>
    /// 添加 OpenTelemetry 日志支持
    /// </summary>
    public static ILoggingBuilder AddGoNomadsLogging(
        this ILoggingBuilder logging,
        IConfiguration configuration,
        string serviceName)
    {
        var observabilityConfig = configuration.GetSection("Observability").Get<ObservabilityConfig>()
                                  ?? new ObservabilityConfig();

        logging.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
            options.ParseStateValues = true;

            var resourceBuilder = ResourceBuilder.CreateDefault()
                .AddService(serviceName);

            options.SetResourceBuilder(resourceBuilder);

            // 导出到 OTLP (可以发送到 Jaeger/Grafana Tempo)
            if (observabilityConfig.OtlpEnabled && !string.IsNullOrEmpty(observabilityConfig.OtlpEndpoint))
            {
                options.AddOtlpExporter(otlpOptions =>
                {
                    otlpOptions.Endpoint = new Uri(observabilityConfig.OtlpEndpoint);
                    otlpOptions.Protocol = OtlpExportProtocol.Grpc;
                });
            }

            // 控制台输出（开发环境）
            if (observabilityConfig.ConsoleExporterEnabled)
            {
                options.AddConsoleExporter();
            }
        });

        return logging;
    }

    private static TracerProviderBuilder ConfigureTracing(
        TracerProviderBuilder builder,
        ResourceBuilder resourceBuilder,
        ObservabilityConfig config,
        string serviceName)
    {
        builder
            .SetResourceBuilder(resourceBuilder)
            // 添加 ASP.NET Core 自动 instrumentation
            .AddAspNetCoreInstrumentation(options =>
            {
                options.RecordException = true;
                options.EnrichWithHttpRequest = (activity, request) =>
                {
                    activity.SetTag("http.request.id", request.HttpContext.TraceIdentifier);
                    activity.SetTag("http.user_agent", request.Headers.UserAgent.ToString());
                    
                    // 添加用户信息（如果存在）
                    if (request.HttpContext.User.Identity?.IsAuthenticated == true)
                    {
                        var userId = request.HttpContext.User.FindFirst("sub")?.Value;
                        if (!string.IsNullOrEmpty(userId))
                        {
                            activity.SetTag("enduser.id", userId);
                        }
                    }
                };
                options.EnrichWithHttpResponse = (activity, response) =>
                {
                    activity.SetTag("http.response.content_length", response.ContentLength);
                };
                options.EnrichWithException = (activity, exception) =>
                {
                    activity.SetTag("exception.type", exception.GetType().FullName);
                    activity.SetTag("exception.message", exception.Message);
                };
                // 过滤健康检查和 metrics 端点
                options.Filter = context =>
                {
                    var path = context.Request.Path.Value?.ToLower() ?? "";
                    return !path.Contains("/health") && 
                           !path.Contains("/metrics") &&
                           !path.Contains("/ready") &&
                           !path.Contains("/live");
                };
            })
            // 添加 HTTP Client 自动 instrumentation
            .AddHttpClientInstrumentation(options =>
            {
                options.RecordException = true;
                options.EnrichWithHttpRequestMessage = (activity, request) =>
                {
                    activity.SetTag("http.request.method", request.Method.Method);
                };
                options.EnrichWithHttpResponseMessage = (activity, response) =>
                {
                    activity.SetTag("http.response.status_code", (int)response.StatusCode);
                };
                // 过滤内部健康检查请求
                options.FilterHttpRequestMessage = request =>
                {
                    var uri = request.RequestUri?.ToString() ?? "";
                    return !uri.Contains("/health") && !uri.Contains("/metrics");
                };
            })
            // 添加自定义 Activity Source
            .AddSource(serviceName)
            .AddSource("GoNomads.*");

        // Jaeger 导出器
        if (config.JaegerEnabled && !string.IsNullOrEmpty(config.JaegerEndpoint))
        {
            builder.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(config.JaegerEndpoint);
                options.Protocol = OtlpExportProtocol.Grpc;
            });
        }

        // OTLP 导出器（可用于 Grafana Tempo 等）
        if (config.OtlpEnabled && !string.IsNullOrEmpty(config.OtlpEndpoint))
        {
            builder.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(config.OtlpEndpoint);
                options.Protocol = OtlpExportProtocol.Grpc;
            });
        }

        // 控制台导出器（开发环境调试用）
        if (config.ConsoleExporterEnabled)
        {
            builder.AddConsoleExporter();
        }

        return builder;
    }

    private static MeterProviderBuilder ConfigureMetrics(
        MeterProviderBuilder builder,
        ResourceBuilder resourceBuilder,
        ObservabilityConfig config,
        string serviceName)
    {
        builder
            .SetResourceBuilder(resourceBuilder)
            // ASP.NET Core metrics
            .AddAspNetCoreInstrumentation()
            // HTTP Client metrics
            .AddHttpClientInstrumentation()
            // Runtime metrics (.NET 运行时指标)
            .AddRuntimeInstrumentation()
            // Process metrics (CPU, 内存等)
            .AddProcessInstrumentation()
            // 自定义业务指标
            .AddMeter(serviceName)
            .AddMeter("GoNomads.Shared");

        // Prometheus 导出器
        if (config.PrometheusEnabled)
        {
            builder.AddPrometheusExporter();
        }

        // OTLP 导出器
        if (config.OtlpEnabled && !string.IsNullOrEmpty(config.OtlpEndpoint))
        {
            builder.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(config.OtlpEndpoint);
                options.Protocol = OtlpExportProtocol.Grpc;
            });
        }

        // 控制台导出器（调试用）
        if (config.ConsoleExporterEnabled)
        {
            builder.AddConsoleExporter();
        }

        return builder;
    }

    /// <summary>
    /// 使用 OpenTelemetry Prometheus 端点
    /// </summary>
    public static IApplicationBuilder UseGoNomadsObservability(this IApplicationBuilder app)
    {
        // Prometheus metrics endpoint
        app.UseOpenTelemetryPrometheusScrapingEndpoint();
        
        return app;
    }
}

/// <summary>
/// 可观测性配置
/// </summary>
public class ObservabilityConfig
{
    /// <summary>
    /// 是否启用 Jaeger
    /// </summary>
    public bool JaegerEnabled { get; set; } = true;

    /// <summary>
    /// Jaeger OTLP 端点 (gRPC)
    /// </summary>
    public string JaegerEndpoint { get; set; } = "http://jaeger:4317";

    /// <summary>
    /// 是否启用 Prometheus
    /// </summary>
    public bool PrometheusEnabled { get; set; } = true;

    /// <summary>
    /// 是否启用 OTLP 导出
    /// </summary>
    public bool OtlpEnabled { get; set; } = false;

    /// <summary>
    /// OTLP 端点
    /// </summary>
    public string? OtlpEndpoint { get; set; }

    /// <summary>
    /// 是否启用控制台导出（调试用）
    /// </summary>
    public bool ConsoleExporterEnabled { get; set; } = false;

    /// <summary>
    /// 采样率 (0.0 - 1.0)
    /// </summary>
    public double SamplingRate { get; set; } = 1.0;
}
