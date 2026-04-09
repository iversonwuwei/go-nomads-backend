using ConfigService.Domain.Entities;

namespace ConfigService.Domain.Repositories;

public interface IOptionItemRepository
{
    Task<IEnumerable<OptionItem>> GetByGroupIdAsync(Guid groupId);
    Task<OptionItem?> GetByIdAsync(Guid id);
    Task<OptionItem> CreateAsync(OptionItem entity);
    Task<OptionItem?> UpdateAsync(OptionItem entity);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ReorderAsync(Guid groupId, List<Guid> orderedIds);
    Task<IEnumerable<OptionItem>> GetAllActiveByGroupIdAsync(Guid groupId);
}
