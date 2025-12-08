using CityService.Domain.Entities;
using CityService.Domain.Repositories;

namespace CityService.Application.Services;

/// <summary>
///     附近城市服务实现
/// </summary>
public class NearbyCityService : INearbyCityService
{
    private readonly ILogger<NearbyCityService> _logger;
    private readonly INearbyCityRepository _repository;

    public NearbyCityService(
        INearbyCityRepository repository,
        ILogger<NearbyCityService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<NearbyCity>> GetBySourceCityIdAsync(string sourceCityId)
    {
        _logger.LogInformation("📖 获取附近城市: sourceCityId={SourceCityId}", sourceCityId);

        var nearbyCities = await _repository.GetBySourceCityIdAsync(sourceCityId);

        _logger.LogInformation("✅ 找到 {Count} 个附近城市: sourceCityId={SourceCityId}",
            nearbyCities.Count, sourceCityId);

        return nearbyCities;
    }

    public async Task<List<NearbyCity>> SaveBatchAsync(string sourceCityId, List<NearbyCity> nearbyCities)
    {
        _logger.LogInformation("💾 批量保存附近城市: sourceCityId={SourceCityId}, count={Count}",
            sourceCityId, nearbyCities.Count);

        var savedCities = await _repository.SaveBatchAsync(sourceCityId, nearbyCities);

        _logger.LogInformation("✅ 附近城市保存成功: sourceCityId={SourceCityId}, savedCount={Count}",
            sourceCityId, savedCities.Count);

        return savedCities;
    }

    public async Task<bool> DeleteBySourceCityIdAsync(string sourceCityId)
    {
        _logger.LogInformation("🗑️ 删除附近城市: sourceCityId={SourceCityId}", sourceCityId);

        var result = await _repository.DeleteBySourceCityIdAsync(sourceCityId);

        if (result)
            _logger.LogInformation("✅ 附近城市删除成功: sourceCityId={SourceCityId}", sourceCityId);
        else
            _logger.LogWarning("⚠️ 附近城市删除失败: sourceCityId={SourceCityId}", sourceCityId);

        return result;
    }

    public async Task<bool> ExistsBySourceCityIdAsync(string sourceCityId)
    {
        return await _repository.ExistsBySourceCityIdAsync(sourceCityId);
    }
}
