using ConfigService.Domain.Entities;

namespace ConfigService.Domain.Repositories;

public interface ISystemSettingRepository
{
    Task<List<SystemSetting>> GetAllAsync(string? section = null, CancellationToken cancellationToken = default);
    Task<List<SystemSetting>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<SystemSetting?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SystemSetting?> GetByKeyAsync(string section, string settingKey, CancellationToken cancellationToken = default);
    Task<SystemSetting> CreateAsync(SystemSetting entity, CancellationToken cancellationToken = default);
    Task<SystemSetting?> UpdateAsync(SystemSetting entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, Guid? deletedBy = null, CancellationToken cancellationToken = default);
}