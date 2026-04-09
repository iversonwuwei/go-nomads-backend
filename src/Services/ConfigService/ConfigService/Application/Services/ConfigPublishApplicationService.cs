using System.Text.Json;
using ConfigService.Application.DTOs;
using ConfigService.Domain.Entities;
using ConfigService.Domain.Repositories;

namespace ConfigService.Application.Services;

public class ConfigPublishApplicationService : IConfigPublishService
{
    private readonly IConfigSnapshotRepository _snapshotRepository;
    private readonly IStaticTextRepository _staticTextRepository;
    private readonly IOptionGroupRepository _optionGroupRepository;
    private readonly IOptionItemRepository _optionItemRepository;
    private readonly ISystemSettingRepository _systemSettingRepository;
    private readonly ILogger<ConfigPublishApplicationService> _logger;

    public ConfigPublishApplicationService(
        IConfigSnapshotRepository snapshotRepository,
        IStaticTextRepository staticTextRepository,
        IOptionGroupRepository optionGroupRepository,
        IOptionItemRepository optionItemRepository,
        ISystemSettingRepository systemSettingRepository,
        ILogger<ConfigPublishApplicationService> logger)
    {
        _snapshotRepository = snapshotRepository;
        _staticTextRepository = staticTextRepository;
        _optionGroupRepository = optionGroupRepository;
        _optionItemRepository = optionItemRepository;
        _systemSettingRepository = systemSettingRepository;
        _logger = logger;
    }

    public async Task<ConfigSnapshotDto> PublishAsync(Guid userId)
    {
        // 1. 收集当前全量生效数据
        var staticTexts = await _staticTextRepository.GetAllActiveAsync();
        var optionGroups = await _optionGroupRepository.GetAllAsync();
        var systemSettings = await _systemSettingRepository.GetAllActiveAsync();

        // 构建静态文本快照
        var textsSnapshot = staticTexts
            .GroupBy(t => t.Locale)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(t => t.TextKey, t => t.TextValue));

        // 构建选项分组快照
        var groupsSnapshot = new Dictionary<string, object>();
        foreach (var group in optionGroups.Where(g => g.IsActive))
        {
            var items = await _optionItemRepository.GetAllActiveByGroupIdAsync(group.Id);
            groupsSnapshot[group.GroupCode] = items.Select(i => new
            {
                code = i.OptionCode,
                label = i.OptionValue,
                labelEn = i.OptionValueEn,
                icon = i.Icon,
                color = i.Color
            });
        }

        var settingsSnapshot = systemSettings
            .GroupBy(setting => setting.Section)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(setting => setting.SortOrder)
                    .ToDictionary(
                        setting => setting.SettingKey,
                        setting => new AppSystemSettingDto
                        {
                            Label = setting.Label,
                            ValueType = setting.ValueType,
                            Value = setting.Value,
                            DefaultValue = setting.DefaultValue,
                            Description = setting.Description
                        }));

        // 2. 生成新版本
        var nextVersion = await _snapshotRepository.GetNextVersionAsync();

        var snapshot = new ConfigSnapshot
        {
            Version = nextVersion,
            StaticTexts = JsonSerializer.Serialize(textsSnapshot),
            OptionGroups = JsonSerializer.Serialize(groupsSnapshot),
            SystemSettings = JsonSerializer.Serialize(settingsSnapshot),
            IsPublished = true,
            PublishedBy = userId,
            PublishedAt = DateTime.UtcNow,
            CreatedBy = userId
        };

        // 3. 取消旧的发布版本
        await _snapshotRepository.UnpublishAllAsync();

        // 4. 创建并发布新快照
        var created = await _snapshotRepository.CreateAsync(snapshot);
        _logger.LogInformation("🚀 配置已发布: v{Version} by {UserId}", nextVersion, userId);

        return MapToDto(created);
    }

    public async Task<IEnumerable<ConfigSnapshotDto>> GetSnapshotsAsync(int page, int pageSize)
    {
        var snapshots = await _snapshotRepository.GetAllAsync(page, pageSize);
        return snapshots.Select(MapToDto);
    }

    public async Task<int> GetSnapshotCountAsync()
    {
        return await _snapshotRepository.GetTotalCountAsync();
    }

    public async Task<ConfigSnapshotDetailDto?> GetSnapshotDetailAsync(Guid id)
    {
        var snapshot = await _snapshotRepository.GetByIdAsync(id);
        if (snapshot == null) return null;

        return new ConfigSnapshotDetailDto
        {
            Id = snapshot.Id,
            Version = snapshot.Version,
            IsPublished = snapshot.IsPublished,
            PublishedBy = snapshot.PublishedBy,
            PublishedAt = snapshot.PublishedAt,
            CreatedAt = snapshot.CreatedAt,
            StaticTexts = JsonSerializer.Deserialize<object>(snapshot.StaticTexts),
            OptionGroups = JsonSerializer.Deserialize<object>(snapshot.OptionGroups),
            SystemSettings = JsonSerializer.Deserialize<object>(snapshot.SystemSettings)
        };
    }

    public async Task<ConfigSnapshotDto?> RollbackAsync(Guid snapshotId, Guid userId)
    {
        var snapshot = await _snapshotRepository.GetByIdAsync(snapshotId);
        if (snapshot == null) return null;

        await _snapshotRepository.UnpublishAllAsync();
        await _snapshotRepository.PublishAsync(snapshotId, userId);

        _logger.LogInformation("⏪ 配置已回滚到: v{Version} by {UserId}", snapshot.Version, userId);

        // 重新读取已发布的快照返回
        var published = await _snapshotRepository.GetByIdAsync(snapshotId);
        return published == null ? null : MapToDto(published);
    }

    public async Task<AppConfigDto?> GetPublishedConfigAsync(string? locale = null)
    {
        var snapshot = await _snapshotRepository.GetPublishedAsync();
        if (snapshot == null) return null;

        var textsDict = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(snapshot.StaticTexts)
            ?? new Dictionary<string, Dictionary<string, string>>();

        var groupsDict = JsonSerializer.Deserialize<Dictionary<string, List<AppOptionItemDto>>>(snapshot.OptionGroups)
            ?? new Dictionary<string, List<AppOptionItemDto>>();
        var settingsDict = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, AppSystemSettingDto>>>(snapshot.SystemSettings)
            ?? new Dictionary<string, Dictionary<string, AppSystemSettingDto>>();

        // 按 locale 筛选静态文本，回退到 zh-CN
        var effectiveLocale = locale ?? "zh-CN";
        var effectiveTexts = textsDict.ContainsKey(effectiveLocale)
            ? textsDict[effectiveLocale]
            : textsDict.ContainsKey("zh-CN")
                ? textsDict["zh-CN"]
                : new Dictionary<string, string>();

        return new AppConfigDto
        {
            Version = snapshot.Version,
            PublishedAt = snapshot.PublishedAt,
            StaticTexts = effectiveTexts,
            OptionGroups = groupsDict,
            SystemSettings = settingsDict
        };
    }

    public async Task<AppConfigVersionDto?> GetPublishedVersionAsync()
    {
        var snapshot = await _snapshotRepository.GetPublishedAsync();
        if (snapshot == null) return null;

        return new AppConfigVersionDto
        {
            Version = snapshot.Version,
            PublishedAt = snapshot.PublishedAt
        };
    }

    private static ConfigSnapshotDto MapToDto(ConfigSnapshot entity) => new()
    {
        Id = entity.Id,
        Version = entity.Version,
        IsPublished = entity.IsPublished,
        PublishedBy = entity.PublishedBy,
        PublishedAt = entity.PublishedAt,
        CreatedAt = entity.CreatedAt
    };
}
