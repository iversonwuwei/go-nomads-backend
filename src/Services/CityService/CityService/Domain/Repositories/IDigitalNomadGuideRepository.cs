using CityService.Domain.Entities;

namespace CityService.Domain.Repositories;

/// <summary>
///     数字游民指南Repository接口
/// </summary>
public interface IDigitalNomadGuideRepository
{
    /// <summary>
    ///     根据城市ID获取指南
    /// </summary>
    Task<DigitalNomadGuide?> GetByCityIdAsync(string cityId);

    /// <summary>
    ///     保存指南 (新增或更新)
    /// </summary>
    Task<DigitalNomadGuide> SaveAsync(DigitalNomadGuide guide);

    /// <summary>
    ///     删除指南
    /// </summary>
    Task<bool> DeleteAsync(string id);

    /// <summary>
    ///     检查城市是否有指南
    /// </summary>
    Task<bool> ExistsByCityIdAsync(string cityId);
}