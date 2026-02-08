// =============================================================================
// Go Nomads - Aspire ServiceDefaults
// 所有微服务共享的默认配置：OpenTelemetry、健康检查、服务发现、弹性策略
// =============================================================================
// 渐进式迁移:
// - 阶段1: 各服务可选引用此项目，与现有 Shared 项目的 Observability 并存
// - 阶段2: 逐步替换 Shared 中的手动 OpenTelemetry 配置
// - 阶段3+: 逐步替换 Consul 和 Dapr 相关功能
// =============================================================================

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Aspire ServiceDefaults 扩展方法。
/// 为所有 Go Nomads 微服务提供统一的:
/// - OpenTelemetry（追踪 + 指标）
/// - 健康检查
/// - 服务发现
/// - HTTP 弹性策略（重试、断路器）
/// </summary>
public static class GoNomadsServiceDefaultsExtensions
{
    /// <summary>
    /// 添加 Aspire 默认服务配置。
    /// 调用此方法替代手动配置 OpenTelemetry、HealthChecks 等。
    /// </summary>
    public static IHostApplicationBuilder AddServiceDefaults(this IHostApplicationBuilder builder)
    {
        // 添加服务发现（Aspire 自动配置，替代 Consul）
        builder.Services.AddServiceDiscovery();

        // 配置默认 HTTP 客户端：启用服务发现 + 弹性策略
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // 启用服务发现，让 HttpClient 可以通过服务名称访问
            // 例如: http://user-service/api/v1/users → 自动解析到实际地址
            http.AddServiceDiscovery();

            // 添加标准弹性策略（重试、断路器、超时）
            http.AddStandardResilienceHandler();
        });

        // 配置 OpenTelemetry
        builder.ConfigureOpenTelemetry();

        // 配置默认健康检查
        builder.AddDefaultHealthChecks();

        return builder;
    }

    /// <summary>
    /// 配置 OpenTelemetry：追踪、指标、日志。
    /// 替代 Shared 项目中的 AddGoNomadsObservability()。
    /// </summary>
    public static IHostApplicationBuilder ConfigureOpenTelemetry(this IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(builder.Environment.ApplicationName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        // Aspire Dashboard 自动配置 OTLP 导出
        // 当通过 AppHost 启动时，OTEL_EXPORTER_OTLP_ENDPOINT 环境变量会自动注入
        builder.AddOpenTelemetryExporters();

        return builder;
    }

    /// <summary>
    /// 添加 OTLP 导出器。Aspire AppHost 会自动设置导出端点。
    /// </summary>
    private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(
            builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]);

        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }

    /// <summary>
    /// 添加默认健康检查端点。
    /// 替代各服务中手动配置的 /health 端点。
    /// </summary>
    public static IHostApplicationBuilder AddDefaultHealthChecks(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks()
            // 添加默认的存活检查
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        return builder;
    }

    /// <summary>
    /// 映射默认健康检查端点。
    /// 在 app.MapControllers() 之后调用。
    /// </summary>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // 非开发环境下也启用健康检查（用于 K8s/容器健康探针）
        // /health - 综合健康检查
        app.MapHealthChecks("/health");

        // /alive - 存活检查（仅检查应用进程是否运行）
        app.MapHealthChecks("/alive", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });

        return app;
    }
}
