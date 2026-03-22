using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;
using YarpClusterConfig = Yarp.ReverseProxy.Configuration.ClusterConfig;
using YarpDestinationConfig = Yarp.ReverseProxy.Configuration.DestinationConfig;
using YarpRouteConfig = Yarp.ReverseProxy.Configuration.RouteConfig;
using YarpRouteMatch = Yarp.ReverseProxy.Configuration.RouteMatch;

namespace Gateway.Services;

public class ServiceUrlProxyConfigProvider : IProxyConfigProvider
{
    private readonly IProxyConfig _config;
    private readonly ILogger<ServiceUrlProxyConfigProvider> _logger;

    private static readonly Dictionary<string, string> DefaultServiceUrls = new()
    {
        ["user-service"] = "http://user-service:8080",
        ["city-service"] = "http://city-service:8080",
        ["coworking-service"] = "http://coworking-service:8080",
        ["event-service"] = "http://event-service:8080",
        ["ai-service"] = "http://ai-service:8080",
        ["cache-service"] = "http://cache-service:8080",
        ["message-service"] = "http://message-service:8080",
        ["innovation-service"] = "http://innovation-service:8080",
        ["search-service"] = "http://search-service:8080",
        ["accommodation-service"] = "http://accommodation-service:8080",
        ["product-service"] = "http://product-service:8080"
    };

    public ServiceUrlProxyConfigProvider(
        IConfiguration configuration,
        ILogger<ServiceUrlProxyConfigProvider> logger)
    {
        _logger = logger;
        _config = BuildConfig(configuration);
    }

    public IProxyConfig GetConfig()
    {
        return _config;
    }

    private IProxyConfig BuildConfig(IConfiguration configuration)
    {
        var serviceUrls = LoadServiceUrls(configuration);
        var routes = new List<YarpRouteConfig>();
        var clusters = new List<YarpClusterConfig>();

        foreach (var (serviceName, serviceUrl) in serviceUrls)
        {
            var servicePathMappings = GetServicePathMappings(serviceName);
            if (!servicePathMappings.Any())
            {
                continue;
            }

            foreach (var (pathPattern, order) in servicePathMappings)
            {
                routes.Add(new YarpRouteConfig
                {
                    RouteId = $"{serviceName}-{pathPattern.Replace("/", "-").Replace("{", "").Replace("}", "").Replace("*", "")}-route",
                    ClusterId = $"{serviceName}-cluster",
                    Match = new YarpRouteMatch
                    {
                        Path = pathPattern
                    },
                    Order = order
                });
            }

            clusters.Add(new YarpClusterConfig
            {
                ClusterId = $"{serviceName}-cluster",
                Destinations = new Dictionary<string, YarpDestinationConfig>
                {
                    [serviceName] = new()
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
                    ["source"] = "service-urls"
                }
            });

            _logger.LogInformation("Configured gateway route for {ServiceName} -> {ServiceUrl}", serviceName, serviceUrl);
        }

        return new InMemoryConfig(routes, clusters, new CancellationChangeToken(new CancellationToken(false)));
    }

    private Dictionary<string, string> LoadServiceUrls(IConfiguration configuration)
    {
        var serviceUrls = new Dictionary<string, string>(DefaultServiceUrls);
        var configSection = configuration.GetSection("ServiceUrls");

        if (!configSection.Exists())
        {
            return serviceUrls;
        }

        foreach (var child in configSection.GetChildren())
        {
            var serviceName = child.Key switch
            {
                "UserService" => "user-service",
                "CityService" => "city-service",
                "CoworkingService" => "coworking-service",
                "EventService" => "event-service",
                "AIService" => "ai-service",
                "CacheService" => "cache-service",
                "MessageService" => "message-service",
                "InnovationService" => "innovation-service",
                "SearchService" => "search-service",
                "AccommodationService" => "accommodation-service",
                "ProductService" => "product-service",
                _ => child.Key.ToLowerInvariant().Replace("service", "-service")
            };

            if (!string.IsNullOrWhiteSpace(child.Value))
            {
                serviceUrls[serviceName] = child.Value;
            }
        }

        return serviceUrls;
    }

    private List<(string PathPattern, int Order)> GetServicePathMappings(string serviceName)
    {
        return serviceName switch
        {
            "city-service" => new List<(string, int)>
            {
                ("/api/v1/user-favorite-cities", 1),
                ("/api/v1/user-favorite-cities/{**catch-all}", 2),
                ("/api/v1/user-content/pros-cons", 3),
                ("/api/v1/user-content/pros-cons/{**catch-all}", 4),
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
                ("/api/v1/event-types", 1),
                ("/api/v1/event-types/{**catch-all}", 2),
                ("/api/v1/events", 3),
                ("/api/v1/events/{**catch-all}", 4),
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
                ("/api/v1/im", 1),
                ("/api/v1/im/{**catch-all}", 2),
                ("/api/v1/notifications", 3),
                ("/api/v1/notifications/{**catch-all}", 4),
                ("/api/v1/chats", 5),
                ("/api/v1/chats/{**catch-all}", 6),
                ("/hubs/chat", 7),
                ("/hubs/chat/{**catch-all}", 8),
                ("/hubs/notifications", 9),
                ("/hubs/notifications/{**catch-all}", 10),
                ("/hubs/ai-progress", 11),
                ("/hubs/ai-progress/{**catch-all}", 12)
            },
            "innovation-service" => new List<(string, int)>
            {
                ("/api/innovations", 1),
                ("/api/innovations/{**catch-all}", 2),
                ("/api/v1/innovations", 3),
                ("/api/v1/innovations/{**catch-all}", 4)
            },
            _ => new List<(string, int)>()
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