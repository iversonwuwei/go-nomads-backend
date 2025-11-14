using CityService.Domain.Entities;
using CityService.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Shared.Repositories;
using Supabase;

namespace CityService.Infrastructure.Repositories;

/// <summary>
/// 基于 Supabase 的城市 Pros & Cons 仓储实现
/// </summary>
public class SupabaseUserCityProsConsRepository : SupabaseRepositoryBase<CityProsCons>, IUserCityProsConsRepository
{
    public SupabaseUserCityProsConsRepository(Client supabaseClient, ILogger<SupabaseUserCityProsConsRepository> logger)
        : base(supabaseClient, logger)
    {
    }

    public async Task<CityProsCons> AddAsync(CityProsCons prosCons)
    {
        prosCons.CreatedAt = DateTime.UtcNow;
        prosCons.UpdatedAt = DateTime.UtcNow;
        
        var response = await SupabaseClient
            .From<CityProsCons>()
            .Insert(prosCons);

        return response.Models.First();
    }

    public async Task<List<CityProsCons>> GetByCityIdAsync(string cityId, bool? isPro = null)
    {
        var query = SupabaseClient
            .From<CityProsCons>()
            .Where(x => x.CityId == cityId)
            .Where(x => x.IsDeleted == false);

        if (isPro.HasValue)
        {
            query = query.Where(x => x.IsPro == isPro.Value);
        }

        var response = await query
            .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
            .Get();

        return response.Models;
    }

    public async Task<CityProsCons?> GetByIdAsync(Guid id)
    {
        try
        {
            var response = await SupabaseClient
                .From<CityProsCons>()
                .Where(x => x.Id == id)
                .Single();

            return response;
        }
        catch
        {
            return null;
        }
    }

    public async Task<CityProsCons> UpdateAsync(CityProsCons prosCons)
    {
        prosCons.UpdatedAt = DateTime.UtcNow;
        
        var response = await SupabaseClient
            .From<CityProsCons>()
            .Where(x => x.Id == prosCons.Id)
            .Update(prosCons);

        return response.Models.First();
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            // 逻辑删除：设置 IsDeleted = true
            var prosCons = await GetByIdAsync(id);
            if (prosCons == null || prosCons.UserId != userId)
            {
                return false;
            }

            prosCons.IsDeleted = true;
            prosCons.UpdatedAt = DateTime.UtcNow;

            await SupabaseClient
                .From<CityProsCons>()
                .Where(x => x.Id == id)
                .Update(prosCons);

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "删除 Pros & Cons 失败: {Id}, {UserId}", id, userId);
            return false;
        }
    }
}
