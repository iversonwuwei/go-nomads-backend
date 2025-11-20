using CacheService.Application.DTOs;
using CacheService.Domain.Entities;

namespace CacheService.Application.Abstractions.Services;

/// <summary>
/// 分数缓存服务接口
/// </summary>
public interface IScoreCacheService
{
    /// <summary>
    /// 获取城市评分 (带缓存)
    /// </summary>
    Task<ScoreResponseDto> GetCityScoreAsync(string cityId);

    /// <summary>
    /// 批量获取城市评分 (带缓存)
    /// </summary>
    Task<BatchScoreResponseDto> GetCityScoresBatchAsync(IEnumerable<string> cityIds);

    /// <summary>
    /// 保存/更新城市评分到缓存
    /// </summary>
    Task SaveCityScoreAsync(string cityId, double overallScore, string? statistics = null);

    /// <summary>
    /// 获取共享办公空间评分 (带缓存)
    /// </summary>
    Task<ScoreResponseDto> GetCoworkingScoreAsync(string coworkingId);

    /// <summary>
    /// 批量获取共享办公空间评分 (带缓存)
    /// </summary>
    Task<BatchScoreResponseDto> GetCoworkingScoresBatchAsync(IEnumerable<string> coworkingIds);

    /// <summary>
    /// 使城市评分缓存失效
    /// </summary>
    Task InvalidateCityScoreAsync(string cityId);

    /// <summary>
    /// 批量使城市评分缓存失效
    /// </summary>
    Task InvalidateCityScoresBatchAsync(IEnumerable<string> cityIds);

    /// <summary>
    /// 使共享办公空间评分缓存失效
    /// </summary>
    Task InvalidateCoworkingScoreAsync(string coworkingId);

    /// <summary>
    /// 批量使共享办公空间评分缓存失效
    /// </summary>
    Task InvalidateCoworkingScoresBatchAsync(IEnumerable<string> coworkingIds);
}
