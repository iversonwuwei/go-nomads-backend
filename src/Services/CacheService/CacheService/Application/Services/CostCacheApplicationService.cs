using System.Text.Json;
using CacheService.Application.Abstractions.Services;
using CacheService.Application.DTOs;
using CacheService.Domain.Entities;
using CacheService.Domain.Repositories;
using CacheService.Infrastructure.Integrations;

namespace CacheService.Application.Services;

/// <summary>
/// 费用缓存应用服务
/// </summary>
public class CostCacheApplicationService : ICostCacheService
{
    private readonly ICostCacheRepository _cacheRepository;
    private readonly ICityServiceClient _cityServiceClient;
    private readonly ILogger<CostCacheApplicationService> _logger;
    private readonly TimeSpan _cacheTtl;

    public CostCacheApplicationService(
        ICostCacheRepository cacheRepository,
        ICityServiceClient cityServiceClient,
        ILogger<CostCacheApplicationService> logger,
        IConfiguration configuration)
    {
        _cacheRepository = cacheRepository;
        _cityServiceClient = cityServiceClient;
        _logger = logger;
        
        // 从配置读取TTL,默认24小时
        var ttlHours = configuration.GetValue<int?>("Cache:CostTtlHours") ?? 24;
        _cacheTtl = TimeSpan.FromHours(ttlHours);
    }

    public async Task<CostResponseDto> GetCityCostAsync(string cityId)
    {
        _logger.LogInformation("Getting city cost for cityId: {CityId}", cityId);

        // 尝试从缓存获取
        var cachedCost = await _cacheRepository.GetAsync(CostEntityType.City, cityId);
        if (cachedCost != null && !cachedCost.IsExpired())
        {
            _logger.LogInformation("Cache hit for city cost {CityId}", cityId);
            return new CostResponseDto
            {
                EntityId = cityId,
                AverageCost = cachedCost.AverageCost,
                FromCache = true,
                Statistics = cachedCost.Statistics
            };
        }

        _logger.LogInformation("Cache miss for city cost {CityId}, fetching from CityService", cityId);

        // 从 CityService 计算
        var cost = await _cityServiceClient.CalculateCityCostAsync(cityId);
        
        // 存入缓存
        var costCache = new CostCache(
            CostEntityType.City, 
            cityId, 
            cost.AverageCost, 
            _cacheTtl,
            cost.Statistics != null ? JsonSerializer.Serialize(cost.Statistics) : null
        );
        await _cacheRepository.SetAsync(costCache);

        return new CostResponseDto
        {
            EntityId = cityId,
            AverageCost = cost.AverageCost,
            FromCache = false,
            Statistics = costCache.Statistics
        };
    }

    public async Task<BatchCostResponseDto> GetCityCostsBatchAsync(IEnumerable<string> cityIds)
    {
        var cityIdList = cityIds.ToList();
        _logger.LogInformation("Getting batch city costs for {Count} cities", cityIdList.Count);

        var result = new BatchCostResponseDto
        {
            TotalCount = cityIdList.Count
        };

        // 批量获取缓存
        var cachedCosts = await _cacheRepository.GetBatchAsync(CostEntityType.City, cityIdList);
        var missingIds = new List<string>();

        // 处理缓存结果
        foreach (var cityId in cityIdList)
        {
            if (cachedCosts.TryGetValue(cityId, out var cachedCost) && !cachedCost.IsExpired())
            {
                result.Costs.Add(new CostResponseDto
                {
                    EntityId = cityId,
                    AverageCost = cachedCost.AverageCost,
                    FromCache = true,
                    Statistics = cachedCost.Statistics
                });
                result.CachedCount++;
            }
            else
            {
                missingIds.Add(cityId);
            }
        }

        // 批量计算缺失的费用
        if (missingIds.Any())
        {
            _logger.LogInformation("Calculating {Count} missing city costs", missingIds.Count);
            var calculatedCosts = await _cityServiceClient.CalculateCityCostsBatchAsync(missingIds);
            
            var newCaches = new List<CostCache>();
            foreach (var cost in calculatedCosts)
            {
                result.Costs.Add(new CostResponseDto
                {
                    EntityId = cost.CityId,
                    AverageCost = cost.AverageCost,
                    FromCache = false,
                    Statistics = cost.Statistics != null ? JsonSerializer.Serialize(cost.Statistics) : null
                });

                newCaches.Add(new CostCache(
                    CostEntityType.City,
                    cost.CityId,
                    cost.AverageCost,
                    _cacheTtl,
                    cost.Statistics != null ? JsonSerializer.Serialize(cost.Statistics) : null
                ));
                result.CalculatedCount++;
            }

            // 批量存入缓存
            if (newCaches.Any())
            {
                await _cacheRepository.SetBatchAsync(newCaches);
                _logger.LogInformation("Set {Count} cost caches in batch", newCaches.Count);
            }
        }

        return result;
    }

    public async Task SaveCityCostAsync(string cityId, decimal averageCost, string? statistics = null)
    {
        _logger.LogInformation("Saving city cost for cityId: {CityId}, Cost: {Cost}", cityId, averageCost);

        var costCache = new CostCache(
            CostEntityType.City,
            cityId,
            averageCost,
            _cacheTtl,
            statistics
        );

        await _cacheRepository.SetAsync(costCache);
    }

    public async Task InvalidateCityCostAsync(string cityId)
    {
        _logger.LogInformation("Invalidating city cost cache for cityId: {CityId}", cityId);
        await _cacheRepository.InvalidateAsync(CostEntityType.City, cityId);
    }
}
