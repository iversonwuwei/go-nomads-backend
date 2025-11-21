using CacheService.Application.DTOs;

namespace CacheService.Application.Abstractions.Services;

/// <summary>
/// 费用缓存服务接口
/// </summary>
public interface ICostCacheService
{
    /// <summary>
    /// 获取城市平均费用 (带缓存)
    /// </summary>
    Task<CostResponseDto> GetCityCostAsync(string cityId);

    /// <summary>
    /// 批量获取城市平均费用 (带缓存)
    /// </summary>
    Task<BatchCostResponseDto> GetCityCostsBatchAsync(IEnumerable<string> cityIds);

    /// <summary>
    /// 保存/更新城市平均费用到缓存
    /// </summary>
    Task SaveCityCostAsync(string cityId, decimal averageCost, string? statistics = null);

    /// <summary>
    /// 使城市费用缓存失效
    /// </summary>
    Task InvalidateCityCostAsync(string cityId);
}
