using ConfigService.Domain.Entities;

namespace ConfigService.Domain.Repositories;

public interface IOptionGroupRepository
{
    Task<IEnumerable<OptionGroup>> GetAllAsync();
    Task<OptionGroup?> GetByIdAsync(Guid id);
    Task<OptionGroup?> GetByCodeAsync(string groupCode);
    Task<OptionGroup> CreateAsync(OptionGroup entity);
    Task<OptionGroup?> UpdateAsync(OptionGroup entity);
    Task<bool> DeleteAsync(Guid id);
}
