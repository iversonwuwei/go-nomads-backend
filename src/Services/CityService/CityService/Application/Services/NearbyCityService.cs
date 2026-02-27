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

    public async Task<List<NearbyCity>> GetByUserAndSourceCityIdAsync(string userId, string sourceCityId)
    {
        _logger.LogInformation("📖 获取附近城市: userId={UserId}, sourceCityId={SourceCityId}", userId, sourceCityId);

        var nearbyCities = await _repository.GetByUserAndSourceCityIdAsync(userId, sourceCityId);

        _logger.LogInformation("✅ 找到 {Count} 个附近城市: userId={UserId}, sourceCityId={SourceCityId}",
            nearbyCities.Count, userId, sourceCityId);

        return nearbyCities;
    }

    public async Task<List<NearbyCity>> SaveBatchAsync(string userId, string sourceCityId, List<NearbyCity> nearbyCities)
    {
        _logger.LogInformation("💾 批量保存附近城市: userId={UserId}, sourceCityId={SourceCityId}, count={Count}",
            userId, sourceCityId, nearbyCities.Count);

        var savedCities = await _repository.SaveBatchAsync(userId, sourceCityId, nearbyCities);

        _logger.LogInformation("✅ 附近城市保存成功: userId={UserId}, sourceCityId={SourceCityId}, savedCount={Count}",
            userId, sourceCityId, savedCities.Count);

        return savedCities;
    }

    public async Task<bool> DeleteByUserAndSourceCityIdAsync(string userId, string sourceCityId)
    {
        _logger.LogInformation("🗑️ 删除附近城市: userId={UserId}, sourceCityId={SourceCityId}", userId, sourceCityId);

        var result = await _repository.DeleteByUserAndSourceCityIdAsync(userId, sourceCityId);

        if (result)
            _logger.LogInformation("✅ 附近城市删除成功: userId={UserId}, sourceCityId={SourceCityId}", userId, sourceCityId);
        else
            _logger.LogWarning("⚠️ 附近城市删除失败: userId={UserId}, sourceCityId={SourceCityId}", userId, sourceCityId);

        return result;
    }

    public async Task<bool> ExistsByUserAndSourceCityIdAsync(string userId, string sourceCityId)
    {
        return await _repository.ExistsByUserAndSourceCityIdAsync(userId, sourceCityId);
    }
}
