using ConfigService.Application.DTOs;

namespace ConfigService.Application.Services;

public interface IStaticTextService
{
    Task<IEnumerable<StaticTextDto>> GetAllAsync(int page, int pageSize, string? category = null, string? key = null, string? locale = null);
    Task<int> GetTotalCountAsync(string? category = null, string? key = null, string? locale = null);
    Task<StaticTextDto?> GetByIdAsync(Guid id);
    Task<StaticTextDto> CreateAsync(CreateStaticTextDto dto, Guid userId);
    Task<StaticTextDto?> UpdateAsync(Guid id, UpdateStaticTextDto dto, Guid userId);
    Task<bool> DeleteAsync(Guid id);
    Task<IEnumerable<string>> GetCategoriesAsync();
}
