using ConfigService.Domain.Entities;
using ConfigService.Domain.Repositories;
using Postgrest;
using Shared.Repositories;
using Client = Supabase.Client;

namespace ConfigService.Infrastructure.Repositories;

public class SupabaseStaticTextRepository : SupabaseRepositoryBase<StaticText>, IStaticTextRepository
{
    public SupabaseStaticTextRepository(Client supabaseClient, ILogger<SupabaseStaticTextRepository> logger)
        : base(supabaseClient, logger)
    {
    }

    public async Task<IEnumerable<StaticText>> GetAllAsync(int page, int pageSize, string? category = null, string? key = null, string? locale = null)
    {
        var query = SupabaseClient.From<StaticText>()
            .Filter("is_deleted", Constants.Operator.Equals, "false");

        if (!string.IsNullOrEmpty(category))
            query = query.Filter("category", Constants.Operator.Equals, category);
        if (!string.IsNullOrEmpty(key))
            query = query.Filter("text_key", Constants.Operator.ILike, $"%{key}%");
        if (!string.IsNullOrEmpty(locale))
            query = query.Filter("locale", Constants.Operator.Equals, locale);

        var offset = (page - 1) * pageSize;
        var response = await query
            .Order("category", Constants.Ordering.Ascending)
            .Order("text_key", Constants.Ordering.Ascending)
            .Range(offset, offset + pageSize - 1)
            .Get();

        return response.Models;
    }

    public async Task<int> GetTotalCountAsync(string? category = null, string? key = null, string? locale = null)
    {
        var query = SupabaseClient.From<StaticText>()
            .Filter("is_deleted", Constants.Operator.Equals, "false");

        if (!string.IsNullOrEmpty(category))
            query = query.Filter("category", Constants.Operator.Equals, category);
        if (!string.IsNullOrEmpty(key))
            query = query.Filter("text_key", Constants.Operator.ILike, $"%{key}%");
        if (!string.IsNullOrEmpty(locale))
            query = query.Filter("locale", Constants.Operator.Equals, locale);

        var count = await query.Count(Constants.CountType.Exact);
        return count;
    }

    public new async Task<StaticText?> GetByIdAsync(Guid id)
    {
        var response = await SupabaseClient.From<StaticText>()
            .Filter("id", Constants.Operator.Equals, id.ToString())
            .Filter("is_deleted", Constants.Operator.Equals, "false")
            .Single();
        return response;
    }

    public async Task<StaticText?> GetByKeyAndLocaleAsync(string textKey, string locale)
    {
        var response = await SupabaseClient.From<StaticText>()
            .Filter("text_key", Constants.Operator.Equals, textKey)
            .Filter("locale", Constants.Operator.Equals, locale)
            .Filter("is_deleted", Constants.Operator.Equals, "false")
            .Single();
        return response;
    }

    public async Task<StaticText> CreateAsync(StaticText entity)
    {
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        var response = await SupabaseClient.From<StaticText>().Insert(entity);
        return response.Models.First();
    }

    public async Task<StaticText?> UpdateAsync(StaticText entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        var response = await SupabaseClient.From<StaticText>().Update(entity);
        return response.Models.FirstOrDefault();
    }

    public new async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null) return false;

        entity.MarkAsDeleted();
        await SupabaseClient.From<StaticText>().Update(entity);
        return true;
    }

    public async Task<IEnumerable<string>> GetCategoriesAsync()
    {
        var response = await SupabaseClient.From<StaticText>()
            .Filter("is_deleted", Constants.Operator.Equals, "false")
            .Select("category")
            .Get();

        return response.Models
            .Select(x => x.Category)
            .Distinct()
            .OrderBy(x => x);
    }

    public async Task<IEnumerable<StaticText>> GetAllActiveAsync(string? locale = null)
    {
        var query = SupabaseClient.From<StaticText>()
            .Filter("is_deleted", Constants.Operator.Equals, "false")
            .Filter("is_active", Constants.Operator.Equals, "true");

        if (!string.IsNullOrEmpty(locale))
            query = query.Filter("locale", Constants.Operator.Equals, locale);

        var response = await query.Get();
        return response.Models;
    }
}
