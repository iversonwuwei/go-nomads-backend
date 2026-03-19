using GoNomads.Shared.Communication;

namespace CityService.Services;

public interface ICacheServiceClient
{
    Task UpdateCityScoreCacheAsync(string cityId, decimal overallScore, string? statistics = null);

    Task UpdateCityCostCacheAsync(string cityId, decimal averageCost, string? statistics = null);
}

public class CacheServiceClient : ICacheServiceClient
{
    private readonly ILogger<CacheServiceClient> _logger;
    private readonly string _cacheServiceName;
    private readonly ServiceInvocationClient _serviceClient;

    public CacheServiceClient(
        ServiceInvocationClient serviceClient,
        IConfiguration configuration,
        ILogger<CacheServiceClient> logger)
    {
        _serviceClient = serviceClient;
        _logger = logger;
        _cacheServiceName = configuration["ServiceNames:CacheService"] ?? "cache-service";
    }

    public async Task UpdateCityScoreCacheAsync(string cityId, decimal overallScore, string? statistics = null)
    {
        try
        {
            _logger.LogInformation("Updating score cache for city {CityId}", cityId);

            var request = new
            {
                OverallScore = overallScore,
                Statistics = statistics
            };

            await _serviceClient.InvokeAsync(
                HttpMethod.Put,
                _cacheServiceName,
                $"api/v1/cache/scores/city/{cityId}",
                request);

            _logger.LogInformation("Successfully updated score cache for city {CityId}", cityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update score cache for city {CityId}", cityId);
        }
    }

    public async Task UpdateCityCostCacheAsync(string cityId, decimal averageCost, string? statistics = null)
    {
        try
        {
            _logger.LogInformation("Updating cost cache for city {CityId}", cityId);

            var request = new
            {
                AverageCost = averageCost,
                Statistics = statistics
            };

            await _serviceClient.InvokeAsync(
                HttpMethod.Put,
                _cacheServiceName,
                $"api/v1/cache/costs/city/{cityId}",
                request);

            _logger.LogInformation("Successfully updated cost cache for city {CityId}", cityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update cost cache for city {CityId}", cityId);
        }
    }
}
