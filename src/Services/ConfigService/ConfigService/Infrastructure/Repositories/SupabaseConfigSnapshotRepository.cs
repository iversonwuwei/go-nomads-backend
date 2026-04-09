using ConfigService.Domain.Entities;
using ConfigService.Domain.Repositories;
using Postgrest;
using Shared.Repositories;
using Client = Supabase.Client;

namespace ConfigService.Infrastructure.Repositories;

public class SupabaseConfigSnapshotRepository : SupabaseRepositoryBase<ConfigSnapshot>, IConfigSnapshotRepository
{
    public SupabaseConfigSnapshotRepository(Client supabaseClient, ILogger<SupabaseConfigSnapshotRepository> logger)
        : base(supabaseClient, logger)
    {
    }

    public async Task<IEnumerable<ConfigSnapshot>> GetAllAsync(int page, int pageSize)
    {
        var offset = (page - 1) * pageSize;
        var response = await SupabaseClient.From<ConfigSnapshot>()
            .Filter("is_deleted", Constants.Operator.Equals, "false")
            .Order("version", Constants.Ordering.Descending)
            .Range(offset, offset + pageSize - 1)
            .Get();
        return response.Models;
    }

    public async Task<int> GetTotalCountAsync()
    {
        var count = await SupabaseClient.From<ConfigSnapshot>()
            .Filter("is_deleted", Constants.Operator.Equals, "false")
            .Count(Constants.CountType.Exact);
        return count;
    }

    public new async Task<ConfigSnapshot?> GetByIdAsync(Guid id)
    {
        var response = await SupabaseClient.From<ConfigSnapshot>()
            .Filter("id", Constants.Operator.Equals, id.ToString())
            .Filter("is_deleted", Constants.Operator.Equals, "false")
            .Single();
        return response;
    }

    public async Task<ConfigSnapshot?> GetPublishedAsync()
    {
        var response = await SupabaseClient.From<ConfigSnapshot>()
            .Filter("is_published", Constants.Operator.Equals, "true")
            .Filter("is_deleted", Constants.Operator.Equals, "false")
            .Single();
        return response;
    }

    public async Task<int> GetNextVersionAsync()
    {
        var response = await SupabaseClient.From<ConfigSnapshot>()
            .Filter("is_deleted", Constants.Operator.Equals, "false")
            .Order("version", Constants.Ordering.Descending)
            .Limit(1)
            .Get();

        var latest = response.Models.FirstOrDefault();
        return (latest?.Version ?? 0) + 1;
    }

    public async Task<ConfigSnapshot> CreateAsync(ConfigSnapshot entity)
    {
        entity.CreatedAt = DateTime.UtcNow;
        var response = await SupabaseClient.From<ConfigSnapshot>().Insert(entity);
        return response.Models.First();
    }

    public async Task<bool> UnpublishAllAsync()
    {
        var published = await SupabaseClient.From<ConfigSnapshot>()
            .Filter("is_published", Constants.Operator.Equals, "true")
            .Filter("is_deleted", Constants.Operator.Equals, "false")
            .Get();

        foreach (var snapshot in published.Models)
        {
            snapshot.IsPublished = false;
            snapshot.UpdatedAt = DateTime.UtcNow;
            await SupabaseClient.From<ConfigSnapshot>().Update(snapshot);
        }
        return true;
    }

    public async Task<bool> PublishAsync(Guid id, Guid publishedBy)
    {
        var snapshot = await GetByIdAsync(id);
        if (snapshot == null) return false;

        snapshot.IsPublished = true;
        snapshot.PublishedBy = publishedBy;
        snapshot.PublishedAt = DateTime.UtcNow;
        snapshot.UpdatedAt = DateTime.UtcNow;
        await SupabaseClient.From<ConfigSnapshot>().Update(snapshot);
        return true;
    }
}
