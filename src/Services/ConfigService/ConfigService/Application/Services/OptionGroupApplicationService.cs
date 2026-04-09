using ConfigService.Application.DTOs;
using ConfigService.Domain.Entities;
using ConfigService.Domain.Repositories;

namespace ConfigService.Application.Services;

public class OptionGroupApplicationService : IOptionGroupService
{
    private readonly IOptionGroupRepository _groupRepository;
    private readonly IOptionItemRepository _itemRepository;
    private readonly ILogger<OptionGroupApplicationService> _logger;

    public OptionGroupApplicationService(
        IOptionGroupRepository groupRepository,
        IOptionItemRepository itemRepository,
        ILogger<OptionGroupApplicationService> logger)
    {
        _groupRepository = groupRepository;
        _itemRepository = itemRepository;
        _logger = logger;
    }

    public async Task<(IEnumerable<OptionGroupDto> Items, int TotalCount)> GetAllAsync(int page = 1, int pageSize = 50)
    {
        var groups = await _groupRepository.GetAllAsync();
        var allGroups = groups.ToList();
        var totalCount = allGroups.Count;

        var pagedGroups = allGroups
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var dtos = new List<OptionGroupDto>();
        foreach (var group in pagedGroups)
        {
            var items = await _itemRepository.GetByGroupIdAsync(group.Id);
            dtos.Add(MapToDto(group, items.Count()));
        }

        return (dtos, totalCount);
    }

    public async Task<OptionGroupDto?> GetByIdAsync(Guid id)
    {
        var group = await _groupRepository.GetByIdAsync(id);
        if (group == null) return null;

        var items = await _itemRepository.GetByGroupIdAsync(id);
        return MapToDto(group, items.Count());
    }

    public async Task<OptionGroupDto> CreateAsync(CreateOptionGroupDto dto, Guid userId)
    {
        var existing = await _groupRepository.GetByCodeAsync(dto.GroupCode);
        if (existing != null)
            throw new InvalidOperationException($"分组编码 '{dto.GroupCode}' 已存在");

        var entity = new OptionGroup
        {
            GroupCode = dto.GroupCode,
            GroupName = dto.GroupName,
            GroupNameEn = dto.GroupNameEn,
            Description = dto.Description,
            IsSystem = dto.IsSystem,
            CreatedBy = userId,
            UpdatedBy = userId
        };

        var created = await _groupRepository.CreateAsync(entity);
        _logger.LogInformation("📋 选项分组已创建: {GroupCode}", dto.GroupCode);
        return MapToDto(created, 0);
    }

    public async Task<OptionGroupDto?> UpdateAsync(Guid id, UpdateOptionGroupDto dto, Guid userId)
    {
        var entity = await _groupRepository.GetByIdAsync(id);
        if (entity == null) return null;

        if (dto.GroupName != null) entity.GroupName = dto.GroupName;
        if (dto.GroupNameEn != null) entity.GroupNameEn = dto.GroupNameEn;
        if (dto.Description != null) entity.Description = dto.Description;
        if (dto.IsActive.HasValue) entity.IsActive = dto.IsActive.Value;
        entity.UpdatedBy = userId;

        var updated = await _groupRepository.UpdateAsync(entity);
        if (updated == null) return null;

        var items = await _itemRepository.GetByGroupIdAsync(id);
        _logger.LogInformation("📋 选项分组已更新: {GroupCode}", entity.GroupCode);
        return MapToDto(updated, items.Count());
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var group = await _groupRepository.GetByIdAsync(id);
        if (group == null) return false;
        if (group.IsSystem)
            throw new InvalidOperationException("系统内置分组不可删除");

        return await _groupRepository.DeleteAsync(id);
    }

    public async Task<IEnumerable<OptionItemDto>> GetItemsAsync(Guid groupId)
    {
        var items = await _itemRepository.GetByGroupIdAsync(groupId);
        return items.Select(MapItemToDto);
    }

    public async Task<OptionItemDto> CreateItemAsync(Guid groupId, CreateOptionItemDto dto, Guid userId)
    {
        var group = await _groupRepository.GetByIdAsync(groupId);
        if (group == null)
            throw new InvalidOperationException($"分组 '{groupId}' 不存在");

        var entity = new OptionItem
        {
            GroupId = groupId,
            OptionCode = dto.OptionCode,
            OptionValue = dto.OptionValue,
            OptionValueEn = dto.OptionValueEn,
            Icon = dto.Icon,
            Color = dto.Color,
            SortOrder = dto.SortOrder,
            Metadata = dto.Metadata,
            CreatedBy = userId,
            UpdatedBy = userId
        };

        var created = await _itemRepository.CreateAsync(entity);
        _logger.LogInformation("📋 选项已创建: {GroupCode}/{OptionCode}", group.GroupCode, dto.OptionCode);
        return MapItemToDto(created);
    }

    public async Task<OptionItemDto?> UpdateItemAsync(Guid groupId, Guid itemId, UpdateOptionItemDto dto, Guid userId)
    {
        var item = await _itemRepository.GetByIdAsync(itemId);
        if (item == null || item.GroupId != groupId) return null;

        if (dto.OptionValue != null) item.OptionValue = dto.OptionValue;
        if (dto.OptionValueEn != null) item.OptionValueEn = dto.OptionValueEn;
        if (dto.Icon != null) item.Icon = dto.Icon;
        if (dto.Color != null) item.Color = dto.Color;
        if (dto.SortOrder.HasValue) item.SortOrder = dto.SortOrder.Value;
        if (dto.IsActive.HasValue) item.IsActive = dto.IsActive.Value;
        if (dto.Metadata != null) item.Metadata = dto.Metadata;
        item.UpdatedBy = userId;

        var updated = await _itemRepository.UpdateAsync(item);
        return updated == null ? null : MapItemToDto(updated);
    }

    public async Task<bool> DeleteItemAsync(Guid groupId, Guid itemId)
    {
        var item = await _itemRepository.GetByIdAsync(itemId);
        if (item == null || item.GroupId != groupId) return false;
        return await _itemRepository.DeleteAsync(itemId);
    }

    public async Task<bool> ReorderItemsAsync(Guid groupId, ReorderItemsDto dto)
    {
        return await _itemRepository.ReorderAsync(groupId, dto.OrderedIds);
    }

    public async Task<bool> ToggleItemAsync(Guid groupId, Guid itemId)
    {
        var item = await _itemRepository.GetByIdAsync(itemId);
        if (item == null || item.GroupId != groupId) return false;

        item.IsActive = !item.IsActive;
        item.UpdatedAt = DateTime.UtcNow;
        await _itemRepository.UpdateAsync(item);
        return true;
    }

    public async Task<OptionGroupDto?> ToggleGroupAsync(Guid id)
    {
        var group = await _groupRepository.GetByIdAsync(id);
        if (group == null) return null;

        group.IsActive = !group.IsActive;
        group.UpdatedAt = DateTime.UtcNow;
        var updated = await _groupRepository.UpdateAsync(group);
        if (updated == null) return null;

        var items = await _itemRepository.GetByGroupIdAsync(id);
        _logger.LogInformation("📋 选项分组已切换状态: {GroupCode} -> {IsActive}", updated.GroupCode, updated.IsActive);
        return MapToDto(updated, items.Count());
    }

    private static OptionGroupDto MapToDto(OptionGroup entity, int itemCount) => new()
    {
        Id = entity.Id,
        GroupCode = entity.GroupCode,
        GroupName = entity.GroupName,
        GroupNameEn = entity.GroupNameEn,
        Description = entity.Description,
        IsSystem = entity.IsSystem,
        IsActive = entity.IsActive,
        ItemCount = itemCount,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };

    private static OptionItemDto MapItemToDto(OptionItem entity) => new()
    {
        Id = entity.Id,
        GroupId = entity.GroupId,
        OptionCode = entity.OptionCode,
        OptionValue = entity.OptionValue,
        OptionValueEn = entity.OptionValueEn,
        Icon = entity.Icon,
        Color = entity.Color,
        SortOrder = entity.SortOrder,
        IsActive = entity.IsActive,
        Metadata = entity.Metadata,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
