using Consul;
using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;
using YarpRouteConfig = Yarp.ReverseProxy.Configuration.RouteConfig;
using YarpClusterConfig = Yarp.ReverseProxy.Configuration.ClusterConfig;
using YarpDestinationConfig = Yarp.ReverseProxy.Configuration.DestinationConfig;
using YarpRouteMatch = Yarp.ReverseProxy.Configuration.RouteMatch;

namespace Gateway.Services;

public class ConsulProxyConfigProvider : IProxyConfigProvider, IDisposable
{
    private readonly IConsulClient _consulClient;
    private readonly ILogger<ConsulProxyConfigProvider> _logger;
    private readonly IHostApplicationLifetime _lifetime;
    private volatile InMemoryConfig _config;
    private CancellationTokenSource _changeToken;
    private CancellationTokenSource _watchCancellation;
    private int _retryCount = 0;
    private const int MaxRetryCount = 5;

    public ConsulProxyConfigProvider(
        IConsulClient consulClient,
        ILogger<ConsulProxyConfigProvider> logger,
        IHostApplicationLifetime lifetime)
    {
        _consulClient = consulClient;
        _logger = logger;
        _lifetime = lifetime;
        _changeToken = new CancellationTokenSource();
        _watchCancellation = new CancellationTokenSource();
        _config = new InMemoryConfig(
            new List<YarpRouteConfig>(), 
            new List<YarpClusterConfig>(),
            new CancellationChangeToken(_changeToken.Token));
        
        // Register graceful shutdown
        _lifetime.ApplicationStopping.Register(OnShutdown);
        
        // Start background task to watch for changes
        _ = WatchConsulServicesAsync(_watchCancellation.Token);
    }

    public IProxyConfig GetConfig() => _config;

    private async Task WatchConsulServicesAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await LoadConfigFromConsulAsync();
                _retryCount = 0; // Reset retry count on success
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Consul watch cancelled");
                break;
            }
            catch (Exception ex)
            {
                _retryCount++;
                var delay = CalculateRetryDelay(_retryCount);
                
                _logger.LogError(ex, 
                    "Error loading config from Consul (attempt {RetryCount}/{MaxRetry}). Retrying in {Delay}s", 
                    _retryCount, MaxRetryCount, delay.TotalSeconds);
                
                try
                {
                    await Task.Delay(delay, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        
        _logger.LogInformation("Consul watch loop exited");
    }
    
    private TimeSpan CalculateRetryDelay(int retryCount)
    {
        // Exponential backoff: 2^n seconds, max 60 seconds
        var delay = Math.Min(Math.Pow(2, retryCount), 60);
        return TimeSpan.FromSeconds(delay);
    }
    
    private void OnShutdown()
    {
        _logger.LogInformation("Application is shutting down, performing graceful cleanup...");
        
        try
        {
            // Cancel watch task
            _watchCancellation?.Cancel();
            
            // Deregister from Consul if registered
            // Note: In current setup, Gateway is registered manually via deployment script
            // Future: Add auto-registration on startup and deregistration here
            
            _logger.LogInformation("Graceful shutdown completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during graceful shutdown");
        }
    }
    
    public void Dispose()
    {
        _watchCancellation?.Cancel();
        _watchCancellation?.Dispose();
        _changeToken?.Dispose();
    }

    private async Task LoadConfigFromConsulAsync()
    {
        try
        {
            _logger.LogInformation("Loading service configuration from Consul...");
            
            // Get all services from Consul
            var services = await _consulClient.Catalog.Services();
            
            var routes = new List<YarpRouteConfig>();
            var clusters = new List<YarpClusterConfig>();

            // Filter services with 'dapr' tag (our backend services)
            foreach (var service in services.Response)
            {
                var serviceName = service.Key;
                
                // Skip consul itself
                if (serviceName == "consul")
                    continue;

                // Get service details with health check
                var healthServices = await _consulClient.Health.Service(serviceName, null, true); // true = passing only
                
                if (!healthServices.Response.Any())
                {
                    _logger.LogWarning("Service {ServiceName} has no healthy instances, skipping...", serviceName);
                    continue;
                }
                
                // Get all healthy instances
                var healthyInstances = healthServices.Response
                    .Where(s => s.Service.Tags?.Contains("dapr") == true)
                    .ToList();
                
                if (!healthyInstances.Any())
                {
                    _logger.LogWarning("Service {ServiceName} has no healthy instances with 'dapr' tag, skipping...", serviceName);
                    continue;
                }

                // Log all healthy instances
                _logger.LogInformation("Discovered {InstanceCount} healthy instance(s) for service: {ServiceName}", 
                    healthyInstances.Count, serviceName);

                var apiPath = GetApiPath(serviceName);
                
                // Create route config (match /api/products or /api/products/*)
                var route = new YarpRouteConfig
                {
                    RouteId = $"{serviceName}-route",
                    ClusterId = $"{serviceName}-cluster",
                    Match = new YarpRouteMatch
                    {
                        Path = $"/api/{apiPath}/{{**remainder}}"
                    }
                };
                routes.Add(route);
                
                // Create route for exact match without trailing path
                var exactRoute = new YarpRouteConfig
                {
                    RouteId = $"{serviceName}-exact-route",
                    ClusterId = $"{serviceName}-cluster",
                    Match = new YarpRouteMatch
                    {
                        Path = $"/api/{apiPath}"
                    }
                };
                routes.Add(exactRoute);

                // Create cluster config with all healthy instances
                var destinations = new Dictionary<string, YarpDestinationConfig>();
                
                for (int i = 0; i < healthyInstances.Count; i++)
                {
                    var instance = healthyInstances[i];
                    var destinationId = healthyInstances.Count > 1 
                        ? $"{serviceName}-{i}" 
                        : serviceName;
                    
                    destinations[destinationId] = new YarpDestinationConfig
                    {
                        Address = $"http://{instance.Service.Address}:{instance.Service.Port}",
                        Health = $"http://{instance.Service.Address}:{instance.Service.Port}/health",
                        Metadata = new Dictionary<string, string>
                        {
                            ["consul.service.id"] = instance.Service.ID,
                            ["consul.node"] = instance.Node.Name,
                            ["consul.version"] = instance.Service.Meta?.TryGetValue("version", out var version) == true ? version : "unknown",
                            ["consul.environment"] = instance.Service.Meta?.TryGetValue("environment", out var env) == true ? env : "unknown"
                        }
                    };
                    
                    _logger.LogDebug("  Instance {Index}: {Address}:{Port} (ID: {ServiceId}, Health: {Health})", 
                        i, instance.Service.Address, instance.Service.Port, instance.Service.ID,
                        string.Join(", ", instance.Checks.Select(c => $"{c.Name}={c.Status}")));
                }
                
                var cluster = new YarpClusterConfig
                {
                    ClusterId = $"{serviceName}-cluster",
                    Destinations = destinations,
                    LoadBalancingPolicy = "RoundRobin", // 轮询负载均衡
                    HealthCheck = new HealthCheckConfig
                    {
                        Active = new ActiveHealthCheckConfig
                        {
                            Enabled = true,
                            Interval = TimeSpan.FromSeconds(10),
                            Timeout = TimeSpan.FromSeconds(5),
                            Policy = "ConsecutiveFailures",
                            Path = "/health"
                        }
                    }
                };
                clusters.Add(cluster);
            }

            if (routes.Any())
            {
                _logger.LogInformation("Loaded {RouteCount} routes from Consul", routes.Count);
                
                // Log route details for debugging
                foreach (var route in routes)
                {
                    _logger.LogDebug("Route: {RouteId}, Path: {Path}, Cluster: {ClusterId}", 
                        route.RouteId, route.Match.Path, route.ClusterId);
                }
                
                foreach (var cluster in clusters)
                {
                    var dest = cluster.Destinations?.FirstOrDefault();
                    if (dest.HasValue)
                    {
                        _logger.LogDebug("Cluster: {ClusterId}, Destination: {Address}", 
                            cluster.ClusterId, dest.Value.Value?.Address);
                    }
                }
                
                // Update config
                var oldToken = _changeToken;
                _changeToken = new CancellationTokenSource();
                _config = new InMemoryConfig(routes, clusters, new CancellationChangeToken(_changeToken.Token));
                
                // Trigger change notification
                oldToken.Cancel();
                oldToken.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load configuration from Consul");
        }
    }

    private string GetApiPath(string serviceName)
    {
        // Map service names to API paths
        return serviceName switch
        {
            "product-service" => "products",
            "user-service" => "users",
            "gateway" => "gateway",
            _ => serviceName
        };
    }

    private class InMemoryConfig : IProxyConfig
    {
        public InMemoryConfig(
            IReadOnlyList<YarpRouteConfig> routes, 
            IReadOnlyList<YarpClusterConfig> clusters,
            IChangeToken changeToken)
        {
            Routes = routes;
            Clusters = clusters;
            ChangeToken = changeToken;
        }

        public IReadOnlyList<YarpRouteConfig> Routes { get; }
        public IReadOnlyList<YarpClusterConfig> Clusters { get; }
        public IChangeToken ChangeToken { get; }
    }
}
