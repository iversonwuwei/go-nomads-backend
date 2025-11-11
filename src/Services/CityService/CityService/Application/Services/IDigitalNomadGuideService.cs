using CityService.Domain.Entities;

namespace CityService.Application.Services;

/// <summary>
/// 数字游民指南服务接口
/// </summary>
public interface IDigitalNomadGuideService
{
    /// <summary>
    /// 根据城市ID获取指南
    /// </summary>
    Task<DigitalNomadGuide?> GetByCityIdAsync(string cityId);

    /// <summary>
    /// 保存指南
    /// </summary>
    Task<DigitalNomadGuide> SaveAsync(DigitalNomadGuide guide);

    /// <summary>
    /// 删除指南
    /// </summary>
    Task<bool> DeleteAsync(string id);
}
