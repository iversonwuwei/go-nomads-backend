using ConfigService.Application.DTOs;
using ConfigService.Domain.Entities;
using ConfigService.Domain.Repositories;

namespace ConfigService.Application.Services;

public class StaticTextApplicationService : IStaticTextService
{
    private readonly IStaticTextRepository _repository;
    private readonly ILogger<StaticTextApplicationService> _logger;

    public StaticTextApplicationService(IStaticTextRepository repository, ILogger<StaticTextApplicationService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<StaticTextDto>> GetAllAsync(int page, int pageSize, string? category = null, string? key = null, string? locale = null)
    {
        var entities = await _repository.GetAllAsync(page, pageSize, category, key, locale);
        return entities.Select(MapToDto);
    }

    public async Task<int> GetTotalCountAsync(string? category = null, string? key = null, string? locale = null)
    {
        return await _repository.GetTotalCountAsync(category, key, locale);
    }

    public async Task<StaticTextDto?> GetByIdAsync(Guid id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity == null ? null : MapToDto(entity);
    }

    public async Task<StaticTextDto> CreateAsync(CreateStaticTextDto dto, Guid userId)
    {
        // 检查 key + locale 唯一性
        var existing = await _repository.GetByKeyAndLocaleAsync(dto.TextKey, dto.Locale);
        if (existing != null)
            throw new InvalidOperationException($"静态文本 key '{dto.TextKey}' 在 locale '{dto.Locale}' 下已存在");

        var entity = new StaticText
        {
            TextKey = dto.TextKey,
            Locale = dto.Locale,
            TextValue = dto.TextValue,
            Category = dto.Category,
            Description = dto.Description,
            Version = 1,
            CreatedBy = userId,
            UpdatedBy = userId
        };

        var created = await _repository.CreateAsync(entity);
        _logger.LogInformation("📝 静态文本已创建: {TextKey} [{Locale}]", dto.TextKey, dto.Locale);
        return MapToDto(created);
    }

    public async Task<StaticTextDto?> UpdateAsync(Guid id, UpdateStaticTextDto dto, Guid userId)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null) return null;

        if (dto.TextValue != null) entity.TextValue = dto.TextValue;
        if (dto.Category != null) entity.Category = dto.Category;
        if (dto.Description != null) entity.Description = dto.Description;
        if (dto.IsActive.HasValue) entity.IsActive = dto.IsActive.Value;

        entity.Version += 1;
        entity.UpdatedBy = userId;

        var updated = await _repository.UpdateAsync(entity);
        _logger.LogInformation("📝 静态文本已更新: {TextKey} v{Version}", entity.TextKey, entity.Version);
        return updated == null ? null : MapToDto(updated);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await _repository.DeleteAsync(id);
    }

    public async Task<IEnumerable<string>> GetCategoriesAsync()
    {
        return await _repository.GetCategoriesAsync();
    }

    private static StaticTextDto MapToDto(StaticText entity) => new()
    {
        Id = entity.Id,
        TextKey = entity.TextKey,
        Locale = entity.Locale,
        TextValue = entity.TextValue,
        Category = entity.Category,
        Description = entity.Description,
        IsActive = entity.IsActive,
        Version = entity.Version,
        UpdatedBy = entity.UpdatedBy,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt
    };
}
