using ConfigService.Application.DTOs;

namespace ConfigService.Application.Services;

public interface IConfigPublishService
{
    Task<ConfigSnapshotDto> PublishAsync(Guid userId);
    Task<IEnumerable<ConfigSnapshotDto>> GetSnapshotsAsync(int page, int pageSize);
    Task<int> GetSnapshotCountAsync();
    Task<ConfigSnapshotDetailDto?> GetSnapshotDetailAsync(Guid id);
    Task<ConfigSnapshotDto?> RollbackAsync(Guid snapshotId, Guid userId);
    Task<AppConfigDto?> GetPublishedConfigAsync(string? locale = null);
    Task<AppConfigVersionDto?> GetPublishedVersionAsync();
}
