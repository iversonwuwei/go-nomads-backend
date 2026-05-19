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
        ["user-service"] = "http://user-service:5001",
        ["city-service"] = "http://city-service:5202",
        ["coworking-service"] = "http://coworking-service:5203",
        ["event-service"] = "http://event-service:5205",
        ["ai-service"] = "http://ai-service:5209",
        ["cache-service"] = "http://cache-service:5210",
        ["message-service"] = "http://message-service:5005",
        ["innovation-service"] = "http://innovation-service:5206",
        ["search-service"] = "http://search-service:5215",
        ["accommodation-service"] = "http://accommodation-service:5204",
        ["product-service"] = "http://product-service:5002",
        ["config-service"] = "http://config-service:5213"
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
        var routeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (serviceName, serviceUrl) in serviceUrls)
        {
            var servicePathMappings = GetServicePathMappings(serviceName);
            if (!servicePathMappings.Any())
            {
                continue;
            }

            foreach (var (pathPattern, order) in servicePathMappings)
            {
                var routeId =
                    $"{serviceName}-{pathPattern.Replace("/", "-").Replace("{", "").Replace("}", "").Replace("*", "")}-route";

                if (!routeIds.Add(routeId))
                {
                    _logger.LogWarning(
                        "Skipping duplicate gateway route {RouteId} for service {ServiceName} and path {PathPattern}",
                        routeId,
                        serviceName,
                        pathPattern);
                    continue;
                }

                routes.Add(new YarpRouteConfig
                {
                    RouteId = routeId,
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
                "ConfigService" => "config-service",
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
                ("/api/v1/admin/city-reviews", 1),
                ("/api/v1/admin/city-reviews/{**catch-all}", 2),
                ("/api/v1/admin/pros-cons", 3),
                ("/api/v1/admin/pros-cons/{**catch-all}", 4),
                ("/api/v1/admin/moderators", 5),
                ("/api/v1/admin/moderators/{**catch-all}", 6),
                ("/api/v1/admin/moderator-applications", 7),
                ("/api/v1/admin/moderator-applications/{**catch-all}", 8),
                ("/api/v1/user-favorite-cities", 9),
                ("/api/v1/user-favorite-cities/{**catch-all}", 10),
                ("/api/v1/user-content/pros-cons", 11),
                ("/api/v1/user-content/pros-cons/{**catch-all}", 12),
                ("/api/v1/cities/{cityId}/user-content/{**catch-all}", 13),
                ("/api/v1/cities", 14),
                ("/api/v1/cities/{**catch-all}", 15),
                ("/api/v1/countries", 16),
                ("/api/v1/countries/{**catch-all}", 17),
                ("/api/v1/provinces", 18),
                ("/api/v1/provinces/{**catch-all}", 19)
            },
            "cache-service" => new List<(string, int)>
            {
                ("/api/v1/cache", 1),
                ("/api/v1/cache/{**catch-all}", 2)
            },
            "user-service" => new List<(string, int)>
            {
                ("/api/v1/admin/membership", 1),
                ("/api/v1/admin/membership/{**catch-all}", 2),
                ("/api/v1/admin/legal", 3),
                ("/api/v1/admin/legal/{**catch-all}", 4),
                ("/api/v1/admin/audit/events", 5),
                ("/api/v1/admin/audit/events/{**catch-all}", 6),
                ("/api/v1/auth", 7),
                ("/api/v1/auth/{**catch-all}", 8),
                ("/api/v1/reports", 9),
                ("/api/v1/reports/{**catch-all}", 10),
                ("/api/v1/users", 11),
                ("/api/v1/users/{**catch-all}", 12),
                ("/api/v1/travel-history", 13),
                ("/api/v1/travel-history/{**catch-all}", 14),
                ("/api/v1/visited-places", 15),
                ("/api/v1/visited-places/{**catch-all}", 16),
                ("/api/v1/membership", 17),
                ("/api/v1/membership/{**catch-all}", 18),
                ("/api/v1/payments", 19),
                ("/api/v1/payments/{**catch-all}", 20),
                ("/api/v1/roles", 21),
                ("/api/v1/roles/{**catch-all}", 22),
                ("/api/v1/skills", 23),
                ("/api/v1/skills/{**catch-all}", 24),
                ("/api/v1/interests", 25),
                ("/api/v1/interests/{**catch-all}", 26),
                ("/api/v1/profile-snapshot", 27),
                ("/api/v1/profile-snapshot/{**catch-all}", 28)
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
                ("/api/v1/admin/travel-plans", 1),
                ("/api/v1/admin/travel-plans/{**catch-all}", 2),
                ("/api/v1/admin/community", 3),
                ("/api/v1/admin/community/{**catch-all}", 4),
                ("/api/v1/admin/ai", 5),
                ("/api/v1/admin/ai/{**catch-all}", 6),
                ("/api/v1/ai", 7),
                ("/api/v1/ai/{**catch-all}", 8),
                ("/api/v1/migration-workspace", 9),
                ("/api/v1/migration-workspace/{**catch-all}", 10),
                ("/api/v1/explore-dashboard", 11),
                ("/api/v1/explore-dashboard/{**catch-all}", 12),
                ("/api/v1/land-hub", 13),
                ("/api/v1/land-hub/{**catch-all}", 14),
                ("/api/v1/community-snapshot", 15),
                ("/api/v1/community-snapshot/{**catch-all}", 16),
                ("/api/v1/community", 17),
                ("/api/v1/community/{**catch-all}", 18),
                ("/api/v1/budgets", 19),
                ("/api/v1/budgets/{**catch-all}", 20),
                ("/api/v1/visa", 21),
                ("/api/v1/visa/{**catch-all}", 22)
            },
            "coworking-service" => new List<(string, int)>
            {
                ("/api/v1/coworking", 1),
                ("/api/v1/coworking/{**catch-all}", 2),
                ("/api/v1/coworking-spaces", 3),
                ("/api/v1/coworking-spaces/{**catch-all}", 4)
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
                ("/api/v1/admin/hotel-reviews", 1),
                ("/api/v1/admin/hotel-reviews/{**catch-all}", 2),
                ("/api/v1/hotels", 3),
                ("/api/v1/hotels/{**catch-all}", 4)
            },
            "product-service" => new List<(string, int)>
            {
                ("/api/v1/products", 1),
                ("/api/v1/products/{**catch-all}", 2)
            },
            "message-service" => new List<(string, int)>
            {
                ("/api/v1/admin/notifications", 1),
                ("/api/v1/admin/notifications/{**catch-all}", 2),
                ("/api/v1/admin/chats", 3),
                ("/api/v1/admin/chats/{**catch-all}", 4),
                ("/api/v1/im", 5),
                ("/api/v1/im/{**catch-all}", 6),
                ("/api/v1/notifications", 7),
                ("/api/v1/notifications/{**catch-all}", 8),
                ("/api/v1/chats", 9),
                ("/api/v1/chats/{**catch-all}", 10),
                ("/hubs/chat", 11),
                ("/hubs/chat/{**catch-all}", 12),
                ("/hubs/notifications", 13),
                ("/hubs/notifications/{**catch-all}", 14),
                ("/hubs/ai-progress", 15),
                ("/hubs/ai-progress/{**catch-all}", 16),
                ("/api/v1/inbox", 17),
                ("/api/v1/inbox/{**catch-all}", 18)
            },
            "innovation-service" => new List<(string, int)>
            {
                ("/api/innovations", 1),
                ("/api/innovations/{**catch-all}", 2),
                ("/api/v1/innovations", 3),
                ("/api/v1/innovations/{**catch-all}", 4),
                ("/api/v1/innovation-projects", 5),
                ("/api/v1/innovation-projects/{**catch-all}", 6)
            },
            "config-service" => new List<(string, int)>
            {
                ("/api/v1/admin/static-texts", 1),
                ("/api/v1/admin/static-texts/{**catch-all}", 2),
                ("/api/v1/admin/option-groups", 3),
                ("/api/v1/admin/option-groups/{**catch-all}", 4),
                ("/api/v1/admin/config", 5),
                ("/api/v1/admin/config/{**catch-all}", 6),
                ("/api/v1/app/config", 7),
                ("/api/v1/app/config/{**catch-all}", 8)
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