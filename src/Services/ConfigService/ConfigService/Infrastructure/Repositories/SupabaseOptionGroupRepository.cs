using ConfigService.Domain.Entities;
using ConfigService.Domain.Repositories;
using Postgrest;
using Shared.Repositories;
using Client = Supabase.Client;

namespace ConfigService.Infrastructure.Repositories;

public class SupabaseOptionGroupRepository : SupabaseRepositoryBase<OptionGroup>, IOptionGroupRepository
{
    public SupabaseOptionGroupRepository(Client supabaseClient, ILogger<SupabaseOptionGroupRepository> logger)
        : base(supabaseClient, logger)
    {
    }

    public async Task<IEnumerable<OptionGroup>> GetAllAsync()
    {
        var response = await SupabaseClient.From<OptionGroup>()
            .Filter("is_deleted", Constants.Operator.Equals, "false")
            .Order("group_code", Constants.Ordering.Ascending)
            .Get();
        return response.Models;
    }

    public new async Task<OptionGroup?> GetByIdAsync(Guid id)
    {
        var response = await SupabaseClient.From<OptionGroup>()
            .Filter("id", Constants.Operator.Equals, id.ToString())
            .Filter("is_deleted", Constants.Operator.Equals, "false")
            .Single();
        return response;
    }

    public async Task<OptionGroup?> GetByCodeAsync(string groupCode)
    {
        var response = await SupabaseClient.From<OptionGroup>()
            .Filter("group_code", Constants.Operator.Equals, groupCode)
            .Filter("is_deleted", Constants.Operator.Equals, "false")
            .Single();
        return response;
    }

    public async Task<OptionGroup> CreateAsync(OptionGroup entity)
    {
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        var response = await SupabaseClient.From<OptionGroup>().Insert(entity);
        return response.Models.First();
    }

    public async Task<OptionGroup?> UpdateAsync(OptionGroup entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        var response = await SupabaseClient.From<OptionGroup>().Update(entity);
        return response.Models.FirstOrDefault();
    }

    public new async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null) return false;

        entity.MarkAsDeleted();
        await SupabaseClient.From<OptionGroup>().Update(entity);
        return true;
    }
}
