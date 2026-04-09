using ConfigService.Application.DTOs;

namespace ConfigService.Application.Services;

public interface ISystemSettingService
{
    Task<(List<SystemSettingDto> Items, int TotalCount)> GetAllAsync(
        int page = 1,
        int pageSize = 20,
        string? section = null,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<SystemSettingDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SystemSettingDto> CreateAsync(
        CreateSystemSettingDto dto,
        Guid operatorUserId,
        CancellationToken cancellationToken = default);

    Task<SystemSettingDto?> UpdateAsync(
        Guid id,
        UpdateSystemSettingDto dto,
        Guid operatorUserId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid id,
        Guid operatorUserId,
        CancellationToken cancellationToken = default);
}