using ConfigService.Application.DTOs;

namespace ConfigService.Application.Services;

public interface IOptionGroupService
{
    Task<(IEnumerable<OptionGroupDto> Items, int TotalCount)> GetAllAsync(int page = 1, int pageSize = 50);
    Task<OptionGroupDto?> GetByIdAsync(Guid id);
    Task<OptionGroupDto> CreateAsync(CreateOptionGroupDto dto, Guid userId);
    Task<OptionGroupDto?> UpdateAsync(Guid id, UpdateOptionGroupDto dto, Guid userId);
    Task<bool> DeleteAsync(Guid id);
    Task<IEnumerable<OptionItemDto>> GetItemsAsync(Guid groupId);
    Task<OptionItemDto> CreateItemAsync(Guid groupId, CreateOptionItemDto dto, Guid userId);
    Task<OptionItemDto?> UpdateItemAsync(Guid groupId, Guid itemId, UpdateOptionItemDto dto, Guid userId);
    Task<bool> DeleteItemAsync(Guid groupId, Guid itemId);
    Task<bool> ReorderItemsAsync(Guid groupId, ReorderItemsDto dto);
    Task<bool> ToggleItemAsync(Guid groupId, Guid itemId);
    Task<OptionGroupDto?> ToggleGroupAsync(Guid id);
}
