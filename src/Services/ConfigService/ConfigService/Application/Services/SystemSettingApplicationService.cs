using ConfigService.Application.DTOs;
using ConfigService.Domain.Entities;
using ConfigService.Domain.Repositories;

namespace ConfigService.Application.Services;

public class SystemSettingApplicationService : ISystemSettingService
{
    private static readonly HashSet<string> AllowedValueTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "string",
        "number",
        "boolean",
        "json"
    };

    private readonly ILogger<SystemSettingApplicationService> _logger;
    private readonly ISystemSettingRepository _systemSettingRepository;

    public SystemSettingApplicationService(
        ISystemSettingRepository systemSettingRepository,
        ILogger<SystemSettingApplicationService> logger)
    {
        _systemSettingRepository = systemSettingRepository;
        _logger = logger;
    }

    public async Task<(List<SystemSettingDto> Items, int TotalCount)> GetAllAsync(
        int page = 1,
        int pageSize = 20,
        string? section = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var settings = await _systemSettingRepository.GetAllAsync(section, cancellationToken);
        var query = settings.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToLowerInvariant();
            query = query.Where(setting =>
                setting.SettingKey.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                setting.Label.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                (setting.Description?.Contains(normalized, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        var filtered = query
            .OrderBy(setting => setting.Section)
            .ThenBy(setting => setting.SortOrder)
            .ThenBy(setting => setting.Label)
            .ToList();

        var totalCount = filtered.Count;
        var items = filtered
            .Skip((Math.Max(page, 1) - 1) * Math.Max(pageSize, 1))
            .Take(Math.Max(pageSize, 1))
            .Select(MapToDto)
            .ToList();

        return (items, totalCount);
    }

    public async Task<SystemSettingDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var setting = await _systemSettingRepository.GetByIdAsync(id, cancellationToken);
        return setting == null ? null : MapToDto(setting);
    }

    public async Task<SystemSettingDto> CreateAsync(
        CreateSystemSettingDto dto,
        Guid operatorUserId,
        CancellationToken cancellationToken = default)
    {
        Validate(dto.Section, dto.SettingKey, dto.Label, dto.ValueType);

        var existing = await _systemSettingRepository.GetByKeyAsync(dto.Section.Trim(), dto.SettingKey.Trim(), cancellationToken);
        if (existing != null)
            throw new InvalidOperationException("同 section 下已存在相同 settingKey");

        var entity = new SystemSetting
        {
            Section = dto.Section.Trim(),
            SettingKey = dto.SettingKey.Trim(),
            Label = dto.Label.Trim(),
            Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            ValueType = dto.ValueType.Trim().ToLowerInvariant(),
            Value = dto.Value,
            DefaultValue = dto.DefaultValue,
            IsActive = dto.IsActive,
            IsSecret = dto.IsSecret,
            SortOrder = dto.SortOrder,
            CreatedBy = operatorUserId,
            UpdatedBy = operatorUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _systemSettingRepository.CreateAsync(entity, cancellationToken);
        _logger.LogInformation("⚙️ 创建系统配置成功: {Section}.{SettingKey}", created.Section, created.SettingKey);
        return MapToDto(created);
    }

    public async Task<SystemSettingDto?> UpdateAsync(
        Guid id,
        UpdateSystemSettingDto dto,
        Guid operatorUserId,
        CancellationToken cancellationToken = default)
    {
        Validate(dto.Section, dto.SettingKey, dto.Label, dto.ValueType);

        var existing = await _systemSettingRepository.GetByIdAsync(id, cancellationToken);
        if (existing == null)
            return null;

        var duplicated = await _systemSettingRepository.GetByKeyAsync(dto.Section.Trim(), dto.SettingKey.Trim(), cancellationToken);
        if (duplicated != null && duplicated.Id != id)
            throw new InvalidOperationException("同 section 下已存在相同 settingKey");

        existing.Section = dto.Section.Trim();
        existing.SettingKey = dto.SettingKey.Trim();
        existing.Label = dto.Label.Trim();
        existing.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        existing.ValueType = dto.ValueType.Trim().ToLowerInvariant();
        existing.Value = dto.Value;
        existing.DefaultValue = dto.DefaultValue;
        existing.IsActive = dto.IsActive;
        existing.IsSecret = dto.IsSecret;
        existing.SortOrder = dto.SortOrder;
        existing.UpdatedBy = operatorUserId;
        existing.UpdatedAt = DateTime.UtcNow;

        var updated = await _systemSettingRepository.UpdateAsync(existing, cancellationToken);
        return updated == null ? null : MapToDto(updated);
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        Guid operatorUserId,
        CancellationToken cancellationToken = default)
    {
        return await _systemSettingRepository.DeleteAsync(id, operatorUserId, cancellationToken);
    }

    private static void Validate(string section, string settingKey, string label, string valueType)
    {
        if (string.IsNullOrWhiteSpace(section))
            throw new InvalidOperationException("section 不能为空");

        if (string.IsNullOrWhiteSpace(settingKey))
            throw new InvalidOperationException("settingKey 不能为空");

        if (string.IsNullOrWhiteSpace(label))
            throw new InvalidOperationException("label 不能为空");

        if (!AllowedValueTypes.Contains(valueType.Trim()))
            throw new InvalidOperationException("valueType 仅支持 string / number / boolean / json");
    }

    private static SystemSettingDto MapToDto(SystemSetting entity)
    {
        return new SystemSettingDto
        {
            Id = entity.Id,
            Section = entity.Section,
            SettingKey = entity.SettingKey,
            Label = entity.Label,
            Description = entity.Description,
            ValueType = entity.ValueType,
            Value = entity.Value,
            DefaultValue = entity.DefaultValue,
            IsActive = entity.IsActive,
            IsSecret = entity.IsSecret,
            SortOrder = entity.SortOrder,
            UpdatedBy = entity.UpdatedBy,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }
}