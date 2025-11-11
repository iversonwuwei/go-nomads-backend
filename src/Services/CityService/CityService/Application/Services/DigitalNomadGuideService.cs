using CityService.Domain.Entities;
using CityService.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace CityService.Application.Services;

/// <summary>
/// 数字游民指南服务实现
/// </summary>
public class DigitalNomadGuideService : IDigitalNomadGuideService
{
    private readonly IDigitalNomadGuideRepository _repository;
    private readonly ILogger<DigitalNomadGuideService> _logger;

    public DigitalNomadGuideService(
        IDigitalNomadGuideRepository repository,
        ILogger<DigitalNomadGuideService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<DigitalNomadGuide?> GetByCityIdAsync(string cityId)
    {
        _logger.LogInformation("📖 获取城市指南: cityId={CityId}", cityId);
        
        var guide = await _repository.GetByCityIdAsync(cityId);
        
        if (guide != null)
        {
            _logger.LogInformation("✅ 找到指南: guideId={GuideId}, cityName={CityName}", guide.Id, guide.CityName);
        }
        else
        {
            _logger.LogInformation("📭 未找到指南: cityId={CityId}", cityId);
        }
        
        return guide;
    }

    public async Task<DigitalNomadGuide> SaveAsync(DigitalNomadGuide guide)
    {
        _logger.LogInformation("💾 保存指南: cityId={CityId}, cityName={CityName}", guide.CityId, guide.CityName);
        
        var savedGuide = await _repository.SaveAsync(guide);
        
        _logger.LogInformation("✅ 指南保存成功: guideId={GuideId}", savedGuide.Id);
        
        return savedGuide;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        _logger.LogInformation("🗑️ 删除指南: guideId={GuideId}", id);
        
        var result = await _repository.DeleteAsync(id);
        
        if (result)
        {
            _logger.LogInformation("✅ 指南删除成功: guideId={GuideId}", id);
        }
        else
        {
            _logger.LogWarning("⚠️ 指南删除失败: guideId={GuideId}", id);
        }
        
        return result;
    }
}
