using ConfigService.Domain.Entities;

namespace ConfigService.Domain.Repositories;

public interface IStaticTextRepository
{
    Task<IEnumerable<StaticText>> GetAllAsync(int page, int pageSize, string? category = null, string? key = null, string? locale = null);
    Task<int> GetTotalCountAsync(string? category = null, string? key = null, string? locale = null);
    Task<StaticText?> GetByIdAsync(Guid id);
    Task<StaticText?> GetByKeyAndLocaleAsync(string textKey, string locale);
    Task<StaticText> CreateAsync(StaticText entity);
    Task<StaticText?> UpdateAsync(StaticText entity);
    Task<bool> DeleteAsync(Guid id);
    Task<IEnumerable<string>> GetCategoriesAsync();
    Task<IEnumerable<StaticText>> GetAllActiveAsync(string? locale = null);
}
