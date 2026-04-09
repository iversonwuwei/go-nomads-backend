using ConfigService.Domain.Entities;

namespace ConfigService.Domain.Repositories;

public interface IConfigSnapshotRepository
{
    Task<IEnumerable<ConfigSnapshot>> GetAllAsync(int page, int pageSize);
    Task<int> GetTotalCountAsync();
    Task<ConfigSnapshot?> GetByIdAsync(Guid id);
    Task<ConfigSnapshot?> GetPublishedAsync();
    Task<int> GetNextVersionAsync();
    Task<ConfigSnapshot> CreateAsync(ConfigSnapshot entity);
    Task<bool> UnpublishAllAsync();
    Task<bool> PublishAsync(Guid id, Guid publishedBy);
}
