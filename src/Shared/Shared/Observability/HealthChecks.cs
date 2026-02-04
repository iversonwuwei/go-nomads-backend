using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace GoNomads.Shared.Observability;

/// <summary>
/// 可观测性基础设施健康检查
/// </summary>
public class ObservabilityHealthCheck : IHealthCheck
{
    private readonly ILogger<ObservabilityHealthCheck> _logger;
    private readonly HttpClient _httpClient;

    public ObservabilityHealthCheck(ILogger<ObservabilityHealthCheck> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("HealthCheck");
        _httpClient.Timeout = TimeSpan.FromSeconds(5);
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();
        var unhealthyComponents = new List<string>();

        // 检查 Jaeger
        try
        {
            var jaegerEndpoint = Environment.GetEnvironmentVariable("JAEGER_ENDPOINT") ?? "http://jaeger:16686";
            var response = await _httpClient.GetAsync($"{jaegerEndpoint}/", cancellationToken);
            data["jaeger"] = response.IsSuccessStatusCode ? "healthy" : "unhealthy";
            if (!response.IsSuccessStatusCode)
                unhealthyComponents.Add("Jaeger");
        }
        catch (Exception ex)
        {
            data["jaeger"] = $"error: {ex.Message}";
            unhealthyComponents.Add("Jaeger");
            _logger.LogWarning(ex, "Jaeger health check failed");
        }

        // 检查 Prometheus (可选)
        try
        {
            var prometheusEndpoint = Environment.GetEnvironmentVariable("PROMETHEUS_ENDPOINT") ?? "http://prometheus:9090";
            var response = await _httpClient.GetAsync($"{prometheusEndpoint}/-/healthy", cancellationToken);
            data["prometheus"] = response.IsSuccessStatusCode ? "healthy" : "unhealthy";
            if (!response.IsSuccessStatusCode)
                unhealthyComponents.Add("Prometheus");
        }
        catch (Exception ex)
        {
            data["prometheus"] = $"error: {ex.Message}";
            // Prometheus 是可选的，不加入 unhealthyComponents
            _logger.LogWarning(ex, "Prometheus health check failed (optional)");
        }

        if (unhealthyComponents.Count > 0)
        {
            return HealthCheckResult.Degraded(
                $"Some observability components are unhealthy: {string.Join(", ", unhealthyComponents)}",
                data: data);
        }

        return HealthCheckResult.Healthy("All observability components are healthy", data);
    }
}

/// <summary>
/// Jaeger 连接健康检查
/// </summary>
public class JaegerHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;
    private readonly string _jaegerEndpoint;

    public JaegerHealthCheck(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("HealthCheck");
        _httpClient.Timeout = TimeSpan.FromSeconds(3);
        _jaegerEndpoint = Environment.GetEnvironmentVariable("JAEGER_UI_ENDPOINT") ?? "http://jaeger:16686";
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_jaegerEndpoint}/", cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("Jaeger is accessible");
            }

            return HealthCheckResult.Degraded($"Jaeger returned status code: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Unable to connect to Jaeger: {ex.Message}", ex);
        }
    }
}
