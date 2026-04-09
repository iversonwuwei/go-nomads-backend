using ConfigService.Domain.Entities;
using ConfigService.Domain.Repositories;
using Postgrest;
using Shared.Repositories;
using Client = Supabase.Client;

namespace ConfigService.Infrastructure.Repositories;

public class SupabaseOptionItemRepository : SupabaseRepositoryBase<OptionItem>, IOptionItemRepository
{
    public SupabaseOptionItemRepository(Client supabaseClient, ILogger<SupabaseOptionItemRepository> logger)
        : base(supabaseClient, logger)
    {
    }

    public async Task<IEnumerable<OptionItem>> GetByGroupIdAsync(Guid groupId)
    {
        var response = await SupabaseClient.From<OptionItem>()
            .Filter("group_id", Constants.Operator.Equals, groupId.ToString())
            .Filter("is_deleted", Constants.Operator.Equals, "false")
            .Order("sort_order", Constants.Ordering.Ascending)
            .Get();
        return response.Models;
    }

    public new async Task<OptionItem?> GetByIdAsync(Guid id)
    {
        var response = await SupabaseClient.From<OptionItem>()
            .Filter("id", Constants.Operator.Equals, id.ToString())
            .Filter("is_deleted", Constants.Operator.Equals, "false")
            .Single();
        return response;
    }

    public async Task<OptionItem> CreateAsync(OptionItem entity)
    {
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        var response = await SupabaseClient.From<OptionItem>().Insert(entity);
        return response.Models.First();
    }

    public async Task<OptionItem?> UpdateAsync(OptionItem entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        var response = await SupabaseClient.From<OptionItem>().Update(entity);
        return response.Models.FirstOrDefault();
    }

    public new async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null) return false;

        entity.MarkAsDeleted();
        await SupabaseClient.From<OptionItem>().Update(entity);
        return true;
    }

    public async Task<bool> ReorderAsync(Guid groupId, List<Guid> orderedIds)
    {
        for (var i = 0; i < orderedIds.Count; i++)
        {
            var item = await GetByIdAsync(orderedIds[i]);
            if (item == null || item.GroupId != groupId) continue;

            item.SortOrder = i;
            item.UpdatedAt = DateTime.UtcNow;
            await SupabaseClient.From<OptionItem>().Update(item);
        }
        return true;
    }

    public async Task<IEnumerable<OptionItem>> GetAllActiveByGroupIdAsync(Guid groupId)
    {
        var response = await SupabaseClient.From<OptionItem>()
            .Filter("group_id", Constants.Operator.Equals, groupId.ToString())
            .Filter("is_deleted", Constants.Operator.Equals, "false")
            .Filter("is_active", Constants.Operator.Equals, "true")
            .Order("sort_order", Constants.Ordering.Ascending)
            .Get();
        return response.Models;
    }
}
