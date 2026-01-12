using Consul;
using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;
using YarpRouteConfig = Yarp.ReverseProxy.Configuration.RouteConfig;
using YarpClusterConfig = Yarp.ReverseProxy.Configuration.ClusterConfig;
using YarpDestinationConfig = Yarp.ReverseProxy.Configuration.DestinationConfig;
using YarpRouteMatch = Yarp.ReverseProxy.Configuration.RouteMatch;

namespace Gateway.Services;

public class ConsulProxyConfigProvider : IProxyConfigProvider, IDisposable
{
    private const int MaxRetryCount = 5;
    private readonly IConsulClient _consulClient;
    private readonly IConfiguration _configuration;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<ConsulProxyConfigProvider> _logger;
    private readonly CancellationTokenSource _watchCancellation;
    private CancellationTokenSource _changeToken;
    private volatile InMemoryConfig _config;
    private int _retryCount;
    
    // K8s 静态服务配置 (当 Consul 中无服务时作为后备)
    private static readonly Dictionary<string, string> K8sServiceUrls = new()
    {
        ["user-service"] = "http://user-service:80",
        ["city-service"] = "http://city-service:8002",
        ["coworking-service"] = "http://coworking-service:8003",
        ["event-service"] = "http://event-service:8005",
        ["ai-service"] = "http://ai-service:8080",
        ["cache-service"] = "http://cache-service:8011",
        ["message-service"] = "http://message-service:8010",
        ["innovation-service"] = "http://innovation-service:8011",
        ["search-service"] = "http://search-service:8080"
    };

    public ConsulProxyConfigProvider(
        IConsulClient consulClient,
        IConfiguration configuration,
        ILogger<ConsulProxyConfigProvider> logger,
        IHostApplicationLifetime lifetime)
    {
        _consulClient = consulClient;
        _configuration = configuration;
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

    public void Dispose()
    {
        _watchCancellation?.Cancel();
        _watchCancellation?.Dispose();
        _changeToken?.Dispose();
    }

    public IProxyConfig GetConfig()
    {
        return _config;
    }

    private async Task WatchConsulServicesAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
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

                // Skip consul and gateway itself (avoid self-routing loops)
                if (serviceName == "consul" ||
                    serviceName.Equals("gateway", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Get service details with health check
                var healthServices = await _consulClient.Health.Service(serviceName, null, true); // true = passing only

                if (!healthServices.Response.Any())
                {
                    _logger.LogWarning("Service {ServiceName} has no healthy instances, skipping...", serviceName);
                    continue;
                }

                // Get all healthy instances
                // 不再要求 'dapr' 标签，所有健康的服务实例都可以路由
                var healthyInstances = healthServices.Response.ToList();

                if (!healthyInstances.Any())
                {
                    _logger.LogWarning("Service {ServiceName} has no healthy instances, skipping...", serviceName);
                    continue;
                }

                // Log all healthy instances
                _logger.LogInformation("Discovered {InstanceCount} healthy instance(s) for service: {ServiceName}",
                    healthyInstances.Count, serviceName);

                // Get the service-specific API path mapping
                var servicePathMappings = GetServicePathMappings(serviceName);

                // Create routes for each path mapping
                foreach (var (pathPattern, order) in servicePathMappings)
                {
                    var route = new YarpRouteConfig
                    {
                        RouteId =
                            $"{serviceName}-{pathPattern.Replace("/", "-").Replace("{", "").Replace("}", "").Replace("*", "")}-route",
                        ClusterId = $"{serviceName}-cluster",
                        Match = new YarpRouteMatch
                        {
                            Path = pathPattern
                        },
                        Order = order // Higher priority routes should have lower order numbers
                    };
                    routes.Add(route);
                    _logger.LogInformation("Added route {RouteId}: {Path} (order: {Order})", route.RouteId, pathPattern,
                        order);
                }

                // Create cluster config with all healthy instances
                var destinations = new Dictionary<string, YarpDestinationConfig>();

                for (var i = 0; i < healthyInstances.Count; i++)
                {
                    var instance = healthyInstances[i];
                    var destinationId = healthyInstances.Count > 1
                        ? $"{serviceName}-{i}"
                        : serviceName;

                    destinations[destinationId] = new YarpDestinationConfig
                    {
                        Address = $"http://{instance.Service.Address}:{instance.Service.Port}",
                        // Don't set Health URL here - YARP will use Address + HealthCheck.Path
                        Metadata = new Dictionary<string, string>
                        {
                            ["consul.service.id"] = instance.Service.ID,
                            ["consul.node"] = instance.Node.Name,
                            ["consul.version"] = instance.Service.Meta?.TryGetValue("version", out var version) == true
                                ? version
                                : "unknown",
                            ["consul.environment"] =
                                instance.Service.Meta?.TryGetValue("environment", out var env) == true ? env : "unknown"
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
                    // 设置 HttpClient 配置
                    HttpClient = new HttpClientConfig
                    {
                        RequestHeaderEncoding = "utf-8"
                    },
                    // 设置请求超时（支持长时间运行的请求如 AI 图片生成）
                    HttpRequest = new ForwarderRequestConfig
                    {
                        ActivityTimeout = TimeSpan.FromMinutes(10) // 10分钟超时
                    },
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

            // 如果 Consul 中没有服务，使用 K8s 静态配置作为后备
            if (!routes.Any())
            {
                _logger.LogWarning("No services found in Consul, falling back to K8s static service configuration...");
                LoadK8sStaticConfig(routes, clusters);
            }

            if (routes.Any())
            {
                _logger.LogInformation("Loaded {RouteCount} routes (source: {Source})", 
                    routes.Count, 
                    clusters.Any(c => c.Metadata?.ContainsKey("source") == true && c.Metadata["source"] == "k8s") 
                        ? "K8s Static" 
                        : "Consul");

                // Log route details for debugging
                foreach (var route in routes)
                    _logger.LogDebug("Route: {RouteId}, Path: {Path}, Cluster: {ClusterId}",
                        route.RouteId, route.Match.Path, route.ClusterId);

                foreach (var cluster in clusters)
                {
                    var dest = cluster.Destinations?.FirstOrDefault();
                    if (dest.HasValue)
                        _logger.LogDebug("Cluster: {ClusterId}, Destination: {Address}",
                            cluster.ClusterId, dest.Value.Value?.Address);
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
            _logger.LogError(ex, "Failed to load configuration from Consul, attempting K8s static fallback...");
            
            // 即使 Consul 出错，也尝试加载 K8s 静态配置
            try
            {
                var routes = new List<YarpRouteConfig>();
                var clusters = new List<YarpClusterConfig>();
                LoadK8sStaticConfig(routes, clusters);
                
                if (routes.Any())
                {
                    var oldToken = _changeToken;
                    _changeToken = new CancellationTokenSource();
                    _config = new InMemoryConfig(routes, clusters, new CancellationChangeToken(_changeToken.Token));
                    oldToken.Cancel();
                    oldToken.Dispose();
                    _logger.LogInformation("Loaded {RouteCount} routes from K8s static configuration", routes.Count);
                }
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "Failed to load K8s static fallback configuration");
            }
        }
    }
    
    /// <summary>
    /// 加载 K8s 静态服务配置作为 Consul 的后备
    /// </summary>
    private void LoadK8sStaticConfig(List<YarpRouteConfig> routes, List<YarpClusterConfig> clusters)
    {
        // 首先尝试从配置文件读取 ServiceUrls
        var serviceUrls = new Dictionary<string, string>(K8sServiceUrls);
        
        var configSection = _configuration.GetSection("ServiceUrls");
        if (configSection.Exists())
        {
            foreach (var child in configSection.GetChildren())
            {
                var serviceName = child.Key.ToLower().Replace("service", "-service");
                if (!serviceName.EndsWith("-service"))
                    serviceName = child.Key.ToLower() + "-service";
                
                // 转换为标准格式: UserService -> user-service
                serviceName = child.Key switch
                {
                    "UserService" => "user-service",
                    "CityService" => "city-service",
                    "CoworkingService" => "coworking-service",
                    "EventService" => "event-service",
                    "AIService" => "ai-service",
                    "CacheService" => "cache-service",
                    _ => serviceName
                };
                
                if (!string.IsNullOrEmpty(child.Value))
                {
                    serviceUrls[serviceName] = child.Value;
                    _logger.LogDebug("Loaded ServiceUrl from config: {ServiceName} -> {Url}", serviceName, child.Value);
                }
            }
        }
        
        foreach (var (serviceName, serviceUrl) in serviceUrls)
        {
            var servicePathMappings = GetServicePathMappings(serviceName);
            
            foreach (var (pathPattern, order) in servicePathMappings)
            {
                var route = new YarpRouteConfig
                {
                    RouteId = $"{serviceName}-{pathPattern.Replace("/", "-").Replace("{", "").Replace("}", "").Replace("*", "")}-k8s-route",
                    ClusterId = $"{serviceName}-cluster",
                    Match = new YarpRouteMatch
                    {
                        Path = pathPattern
                    },
                    Order = order
                };
                routes.Add(route);
            }
            
            var cluster = new YarpClusterConfig
            {
                ClusterId = $"{serviceName}-cluster",
                Destinations = new Dictionary<string, YarpDestinationConfig>
                {
                    [serviceName] = new YarpDestinationConfig
                    {
                        Address = serviceUrl
                    }
                },
                LoadBalancingPolicy = "RoundRobin",
                HttpClient = new HttpClientConfig
                {
                    RequestHeaderEncoding = "utf-8"
                },
                HttpRequest = new ForwarderRequestConfig
                {
                    ActivityTimeout = TimeSpan.FromMinutes(10)
                },
                HealthCheck = new HealthCheckConfig
                {
                    Active = new ActiveHealthCheckConfig
                    {
                        Enabled = true,
                        Interval = TimeSpan.FromSeconds(30),
                        Timeout = TimeSpan.FromSeconds(10),
                        Policy = "ConsecutiveFailures",
                        Path = "/health"
                    }
                },
                Metadata = new Dictionary<string, string>
                {
                    ["source"] = "k8s"
                }
            };
            clusters.Add(cluster);
            
            _logger.LogInformation("Added K8s static route for {ServiceName} -> {ServiceUrl}", serviceName, serviceUrl);
        }
    }

    private string GetApiPath(string serviceName)
    {
        // Map service names to API paths
        return serviceName switch
        {
            "product-service" => "v1/products",
            "user-service" => "v1/users",
            "city-service" => "v1/cities",
            "event-service" => "v1/events",
            "coworking-service" => "v1/coworking",
            "ai-service" => "v1/ai",
            "search-service" => "v1/search",
            "gateway" => "gateway",
            _ => serviceName
        };
    }

    /// <summary>
    ///     Get route path mappings for each service
    ///     Returns list of (pathPattern, order) tuples
    ///     Lower order = higher priority
    /// </summary>
    private List<(string PathPattern, int Order)> GetServicePathMappings(string serviceName)
    {
        return serviceName switch
        {
            "city-service" => new List<(string, int)>
            {
                // Specific paths first (higher priority, lower order number)
                ("/api/v1/user-favorite-cities", 1),
                ("/api/v1/user-favorite-cities/{**catch-all}", 2),
                ("/api/v1/user-content/pros-cons", 3),
                ("/api/v1/user-content/pros-cons/{**catch-all}", 4), // Pros & Cons voting endpoint
                ("/api/v1/cities/{cityId}/user-content/{**catch-all}", 5),
                ("/api/v1/cities", 6),
                ("/api/v1/cities/{**catch-all}", 7),
                ("/api/v1/countries", 8),
                ("/api/v1/countries/{**catch-all}", 9),
                ("/api/v1/provinces", 10),
                ("/api/v1/provinces/{**catch-all}", 11)
            },
            "cache-service" => new List<(string, int)>
            {
                // Cache Service endpoints for score caching
                ("/api/v1/cache", 1),
                ("/api/v1/cache/{**catch-all}", 2)
            },
            "user-service" => new List<(string, int)>
            {
                ("/api/v1/auth", 1),
                ("/api/v1/auth/{**catch-all}", 2),
                ("/api/v1/users", 3),
                ("/api/v1/users/{**catch-all}", 4),
                ("/api/v1/travel-history", 5),
                ("/api/v1/travel-history/{**catch-all}", 6),
                ("/api/v1/visited-places", 7),
                ("/api/v1/visited-places/{**catch-all}", 8),
                ("/api/v1/membership", 9),
                ("/api/v1/membership/{**catch-all}", 10),
                ("/api/v1/payments", 11),
                ("/api/v1/payments/{**catch-all}", 12),
                ("/api/v1/roles", 13),
                ("/api/v1/roles/{**catch-all}", 14),
                ("/api/v1/skills", 15),
                ("/api/v1/skills/{**catch-all}", 16),
                ("/api/v1/interests", 17),
                ("/api/v1/interests/{**catch-all}", 18)
            },
            "event-service" => new List<(string, int)>
            {
                ("/api/v1/event-types", 1),  // Event types endpoint
                ("/api/v1/event-types/{**catch-all}", 2),
                ("/api/v1/events", 3),
                ("/api/v1/events/{**catch-all}", 4),
                // SignalR Hub endpoints for Meetup real-time updates
                ("/hubs/meetup", 5),
                ("/hubs/meetup/{**catch-all}", 6)
            },
            "ai-service" => new List<(string, int)>
            {
                ("/api/v1/ai", 1),
                ("/api/v1/ai/{**catch-all}", 2)
            },
            "coworking-service" => new List<(string, int)>
            {
                ("/api/v1/coworking", 1),
                ("/api/v1/coworking/{**catch-all}", 2)
            },
            "search-service" => new List<(string, int)>
            {
                ("/api/v1/search", 1),
                ("/api/v1/search/{**catch-all}", 2),
                ("/api/v1/index", 3),
                ("/api/v1/index/{**catch-all}", 4)
            },
            "accommodation-service" => new List<(string, int)>
            {
                ("/api/v1/hotels", 1),
                ("/api/v1/hotels/{**catch-all}", 2)
            },
            "product-service" => new List<(string, int)>
            {
                ("/api/v1/products", 1),
                ("/api/v1/products/{**catch-all}", 2)
            },
            "message-service" => new List<(string, int)>
            {
                ("/api/v1/notifications", 1),
                ("/api/v1/notifications/{**catch-all}", 2),
                ("/api/v1/chats", 3),
                ("/api/v1/chats/{**catch-all}", 4),
                // SignalR Hub endpoints
                ("/hubs/chat", 5),
                ("/hubs/chat/{**catch-all}", 6),
                ("/hubs/notifications", 7),
                ("/hubs/notifications/{**catch-all}", 8),
                ("/hubs/ai-progress", 9),
                ("/hubs/ai-progress/{**catch-all}", 10)
            },
            "innovation-service" => new List<(string, int)>
            {
                ("/api/innovations", 1),
                ("/api/innovations/{**catch-all}", 2),
                ("/api/v1/innovations", 3),
                ("/api/v1/innovations/{**catch-all}", 4)
            },
            _ => new List<(string, int)>
            {
                // Default catch-all for unknown services
                ($"/api/v1/{serviceName}/{{**catch-all}}", 100)
            }
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