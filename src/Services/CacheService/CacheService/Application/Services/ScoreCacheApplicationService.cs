using System.Text.Json;
using CacheService.Application.Abstractions.Services;
using CacheService.Application.DTOs;
using CacheService.Domain.Entities;
using CacheService.Domain.Repositories;
using CacheService.Infrastructure.Integrations;

namespace CacheService.Application.Services;

/// <summary>
/// 分数缓存应用服务
/// </summary>
public class ScoreCacheApplicationService : IScoreCacheService
{
    private readonly IScoreCacheRepository _cacheRepository;
    private readonly ICityServiceClient _cityServiceClient;
    private readonly ICoworkingServiceClient _coworkingServiceClient;
    private readonly ILogger<ScoreCacheApplicationService> _logger;
    private readonly TimeSpan _cacheTtl;

    public ScoreCacheApplicationService(
        IScoreCacheRepository cacheRepository,
        ICityServiceClient cityServiceClient,
        ICoworkingServiceClient coworkingServiceClient,
        ILogger<ScoreCacheApplicationService> logger,
        IConfiguration configuration)
    {
        _cacheRepository = cacheRepository;
        _cityServiceClient = cityServiceClient;
        _coworkingServiceClient = coworkingServiceClient;
        _logger = logger;
        
        // 从配置读取TTL,默认24小时
        var ttlHours = configuration.GetValue<int?>("Cache:ScoreTtlHours") ?? 24;
        _cacheTtl = TimeSpan.FromHours(ttlHours);
    }

    public async Task<ScoreResponseDto> GetCityScoreAsync(string cityId)
    {
        _logger.LogInformation("Getting city score for cityId: {CityId}", cityId);

        // 尝试从缓存获取
        var cachedScore = await _cacheRepository.GetAsync(ScoreEntityType.City, cityId);
        if (cachedScore != null && !cachedScore.IsExpired())
        {
            _logger.LogInformation("Cache hit for city {CityId}", cityId);
            return new ScoreResponseDto
            {
                EntityId = cityId,
                OverallScore = cachedScore.OverallScore,
                FromCache = true,
                Statistics = cachedScore.Statistics
            };
        }

        _logger.LogInformation("Cache miss for city {CityId}, fetching from CityService", cityId);

        // 从 CityService 计算
        var score = await _cityServiceClient.CalculateCityScoreAsync(cityId);
        
        // 存入缓存
        var scoreCache = new ScoreCache(
            ScoreEntityType.City, 
            cityId, 
            score.OverallScore, 
            _cacheTtl,
            score.Statistics != null ? JsonSerializer.Serialize(score.Statistics) : null
        );
        await _cacheRepository.SetAsync(scoreCache);

        return new ScoreResponseDto
        {
            EntityId = cityId,
            OverallScore = score.OverallScore,
            FromCache = false,
            Statistics = scoreCache.Statistics
        };
    }

    public async Task<BatchScoreResponseDto> GetCityScoresBatchAsync(IEnumerable<string> cityIds)
    {
        var cityIdList = cityIds.ToList();
        _logger.LogInformation("Getting batch city scores for {Count} cities", cityIdList.Count);

        var result = new BatchScoreResponseDto
        {
            TotalCount = cityIdList.Count
        };

        // 批量获取缓存
        var cachedScores = await _cacheRepository.GetBatchAsync(ScoreEntityType.City, cityIdList);
        var missingIds = new List<string>();

        // 处理缓存结果
        foreach (var cityId in cityIdList)
        {
            if (cachedScores.TryGetValue(cityId, out var cachedScore) && !cachedScore.IsExpired())
            {
                result.Scores.Add(new ScoreResponseDto
                {
                    EntityId = cityId,
                    OverallScore = cachedScore.OverallScore,
                    FromCache = true,
                    Statistics = cachedScore.Statistics
                });
                result.CachedCount++;
            }
            else
            {
                missingIds.Add(cityId);
            }
        }

        // 批量计算缺失的分数
        if (missingIds.Any())
        {
            _logger.LogInformation("Calculating {Count} missing city scores", missingIds.Count);
            var calculatedScores = await _cityServiceClient.CalculateCityScoresBatchAsync(missingIds);
            
            var newCaches = new List<ScoreCache>();
            foreach (var score in calculatedScores)
            {
                result.Scores.Add(new ScoreResponseDto
                {
                    EntityId = score.CityId,
                    OverallScore = score.OverallScore,
                    FromCache = false,
                    Statistics = score.Statistics != null ? JsonSerializer.Serialize(score.Statistics) : null
                });

                newCaches.Add(new ScoreCache(
                    ScoreEntityType.City,
                    score.CityId,
                    score.OverallScore,
                    _cacheTtl,
                    score.Statistics != null ? JsonSerializer.Serialize(score.Statistics) : null
                ));
                result.CalculatedCount++;
            }

            // 批量存入缓存
            if (newCaches.Any())
            {
                await _cacheRepository.SetBatchAsync(newCaches);
            }
        }

        return result;
    }

    public async Task SaveCityScoreAsync(string cityId, double overallScore, string? statistics = null)
    {
        _logger.LogInformation("Saving city score: CityId={CityId}, OverallScore={OverallScore}", cityId, overallScore);

        var scoreCache = new ScoreCache(
            ScoreEntityType.City,
            cityId,
            (decimal)overallScore,  // 转换为 decimal
            _cacheTtl,
            statistics
        );

        await _cacheRepository.SetAsync(scoreCache);
        _logger.LogInformation("✅ City score saved to cache: CityId={CityId}, OverallScore={OverallScore}", cityId, overallScore);
    }

    public async Task<ScoreResponseDto> GetCoworkingScoreAsync(string coworkingId)
    {
        _logger.LogInformation("Getting coworking score for coworkingId: {CoworkingId}", coworkingId);

        // 尝试从缓存获取
        var cachedScore = await _cacheRepository.GetAsync(ScoreEntityType.Coworking, coworkingId);
        if (cachedScore != null && !cachedScore.IsExpired())
        {
            _logger.LogInformation("Cache hit for coworking {CoworkingId}", coworkingId);
            return new ScoreResponseDto
            {
                EntityId = coworkingId,
                OverallScore = cachedScore.OverallScore,
                FromCache = true,
                Statistics = cachedScore.Statistics
            };
        }

        _logger.LogInformation("Cache miss for coworking {CoworkingId}, fetching from CoworkingService", coworkingId);

        // 从 CoworkingService 获取
        var score = await _coworkingServiceClient.GetCoworkingScoreAsync(coworkingId);
        
        // 存入缓存
        var scoreCache = new ScoreCache(
            ScoreEntityType.Coworking,
            coworkingId,
            score.Rating,
            _cacheTtl,
            JsonSerializer.Serialize(new { score.ReviewCount })
        );
        await _cacheRepository.SetAsync(scoreCache);

        return new ScoreResponseDto
        {
            EntityId = coworkingId,
            OverallScore = score.Rating,
            FromCache = false,
            Statistics = scoreCache.Statistics
        };
    }

    public async Task<BatchScoreResponseDto> GetCoworkingScoresBatchAsync(IEnumerable<string> coworkingIds)
    {
        var coworkingIdList = coworkingIds.ToList();
        _logger.LogInformation("Getting batch coworking scores for {Count} coworkings", coworkingIdList.Count);

        var result = new BatchScoreResponseDto
        {
            TotalCount = coworkingIdList.Count
        };

        // 批量获取缓存
        var cachedScores = await _cacheRepository.GetBatchAsync(ScoreEntityType.Coworking, coworkingIdList);
        var missingIds = new List<string>();

        // 处理缓存结果
        foreach (var coworkingId in coworkingIdList)
        {
            if (cachedScores.TryGetValue(coworkingId, out var cachedScore) && !cachedScore.IsExpired())
            {
                result.Scores.Add(new ScoreResponseDto
                {
                    EntityId = coworkingId,
                    OverallScore = cachedScore.OverallScore,
                    FromCache = true,
                    Statistics = cachedScore.Statistics
                });
                result.CachedCount++;
            }
            else
            {
                missingIds.Add(coworkingId);
            }
        }

        // 批量获取缺失的分数
        if (missingIds.Any())
        {
            _logger.LogInformation("Fetching {Count} missing coworking scores", missingIds.Count);
            var scores = await _coworkingServiceClient.GetCoworkingScoresBatchAsync(missingIds);
            
            var newCaches = new List<ScoreCache>();
            foreach (var score in scores)
            {
                result.Scores.Add(new ScoreResponseDto
                {
                    EntityId = score.Id,
                    OverallScore = score.Rating,
                    FromCache = false,
                    Statistics = JsonSerializer.Serialize(new { score.ReviewCount })
                });

                newCaches.Add(new ScoreCache(
                    ScoreEntityType.Coworking,
                    score.Id,
                    score.Rating,
                    _cacheTtl,
                    JsonSerializer.Serialize(new { score.ReviewCount })
                ));
                result.CalculatedCount++;
            }

            // 批量存入缓存
            if (newCaches.Any())
            {
                await _cacheRepository.SetBatchAsync(newCaches);
            }
        }

        return result;
    }

    public async Task InvalidateCityScoreAsync(string cityId)
    {
        _logger.LogInformation("Invalidating city score cache for cityId: {CityId}", cityId);
        await _cacheRepository.InvalidateAsync(ScoreEntityType.City, cityId);
    }

    public async Task InvalidateCityScoresBatchAsync(IEnumerable<string> cityIds)
    {
        var cityIdList = cityIds.ToList();
        _logger.LogInformation("Invalidating city score cache for {Count} cities", cityIdList.Count);
        await _cacheRepository.InvalidateBatchAsync(ScoreEntityType.City, cityIdList);
    }

    public async Task InvalidateCoworkingScoreAsync(string coworkingId)
    {
        _logger.LogInformation("Invalidating coworking score cache for coworkingId: {CoworkingId}", coworkingId);
        await _cacheRepository.InvalidateAsync(ScoreEntityType.Coworking, coworkingId);
    }

    public async Task InvalidateCoworkingScoresBatchAsync(IEnumerable<string> coworkingIds)
    {
        var coworkingIdList = coworkingIds.ToList();
        _logger.LogInformation("Invalidating coworking score cache for {Count} coworkings", coworkingIdList.Count);
        await _cacheRepository.InvalidateBatchAsync(ScoreEntityType.Coworking, coworkingIdList);
    }
}
