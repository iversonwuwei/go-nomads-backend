using ConfigService.Domain.Entities;
using ConfigService.Domain.Repositories;
using Postgrest;
using Shared.Repositories;
using Client = Supabase.Client;

namespace ConfigService.Infrastructure.Repositories;

public class SupabaseSystemSettingRepository : SupabaseRepositoryBase<SystemSetting>, ISystemSettingRepository
{
    public SupabaseSystemSettingRepository(Client supabaseClient, ILogger<SupabaseSystemSettingRepository> logger)
        : base(supabaseClient, logger)
    {
    }

    public async Task<List<SystemSetting>> GetAllAsync(string? section = null, CancellationToken cancellationToken = default)
    {
        var query = SupabaseClient.From<SystemSetting>()
            .Filter("is_deleted", Constants.Operator.Equals, "false");

        if (!string.IsNullOrWhiteSpace(section))
            query = query.Filter("section", Constants.Operator.Equals, section);

        var response = await query
            .Order("section", Constants.Ordering.Ascending)
            .Order("sort_order", Constants.Ordering.Ascending)
            .Get(cancellationToken);

        return response.Models;
    }

    public async Task<List<SystemSetting>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        var response = await SupabaseClient.From<SystemSetting>()
            .Filter("is_deleted", Constants.Operator.Equals, "false")
            .Filter("is_active", Constants.Operator.Equals, "true")
            .Order("section", Constants.Ordering.Ascending)
            .Order("sort_order", Constants.Ordering.Ascending)
            .Get(cancellationToken);

        return response.Models;
    }

    public new async Task<SystemSetting?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await SupabaseClient.From<SystemSetting>()
            .Filter("id", Constants.Operator.Equals, id.ToString())
            .Filter("is_deleted", Constants.Operator.Equals, "false")
            .Single(cancellationToken);
    }

    public async Task<SystemSetting?> GetByKeyAsync(
        string section,
        string settingKey,
        CancellationToken cancellationToken = default)
    {
        return await SupabaseClient.From<SystemSetting>()
            .Filter("section", Constants.Operator.Equals, section)
            .Filter("setting_key", Constants.Operator.Equals, settingKey)
            .Filter("is_deleted", Constants.Operator.Equals, "false")
            .Single(cancellationToken);
    }

    public async Task<SystemSetting> CreateAsync(SystemSetting entity, CancellationToken cancellationToken = default)
    {
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        var response = await SupabaseClient.From<SystemSetting>().Insert(entity, cancellationToken: cancellationToken);
        return response.Models.First();
    }

    public async Task<SystemSetting?> UpdateAsync(SystemSetting entity, CancellationToken cancellationToken = default)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        var response = await SupabaseClient.From<SystemSetting>()
            .Where(setting => setting.Id == entity.Id)
            .Update(entity, cancellationToken: cancellationToken);

        return response.Models.FirstOrDefault();
    }

    public async Task<bool> DeleteAsync(Guid id, Guid? deletedBy = null, CancellationToken cancellationToken = default)
    {
        var existing = await GetByIdAsync(id, cancellationToken);
        if (existing == null)
            return false;

        existing.MarkAsDeleted(deletedBy);
        await SupabaseClient.From<SystemSetting>()
            .Where(setting => setting.Id == id)
            .Update(existing, cancellationToken: cancellationToken);

        return true;
    }
}