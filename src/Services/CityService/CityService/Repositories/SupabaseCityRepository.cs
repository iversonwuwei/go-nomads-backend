using CityService.Models;
using CityService.DTOs;
using Microsoft.Extensions.Logging;
using Shared.Repositories;
using Supabase;

namespace CityService.Repositories;

/// <summary>
/// City 仓储 - 使用 Supabase
/// </summary>
public class SupabaseCityRepository : SupabaseRepositoryBase<City>, ICityRepository
{
    public SupabaseCityRepository(Client supabaseClient, ILogger<SupabaseCityRepository> logger) 
        : base(supabaseClient, logger)
    {
    }

    public async Task<IEnumerable<City>> GetAllAsync(int pageNumber, int pageSize)
    {
        var offset = (pageNumber - 1) * pageSize;
        
        var response = await SupabaseClient
            .From<City>()
            .Filter("is_active", Postgrest.Constants.Operator.Equals, "true")
            .Order(x => x.OverallScore, Postgrest.Constants.Ordering.Descending)
            .Range(offset, offset + pageSize - 1)
            .Get();

        return response.Models;
    }

    public async Task<City?> GetByIdAsync(Guid id)
    {
        try
        {
            var response = await SupabaseClient
                .From<City>()
                .Where(x => x.Id == id)
                .Single();

            return response;
        }
        catch
        {
            return null;
        }
    }

    public async Task<IEnumerable<City>> SearchAsync(CitySearchDto searchDto)
    {
        // 获取所有活跃城市
        var response = await SupabaseClient
            .From<City>()
            .Filter("is_active", Postgrest.Constants.Operator.Equals, "true")
            .Order(x => x.OverallScore, Postgrest.Constants.Ordering.Descending)
            .Get();

        var cities = response.Models.AsEnumerable();

        // 客户端过滤
        if (!string.IsNullOrWhiteSpace(searchDto.Name))
        {
            cities = cities.Where(c => c.Name.Contains(searchDto.Name, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(searchDto.Country))
        {
            cities = cities.Where(c => c.Country.Contains(searchDto.Country, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(searchDto.Region))
        {
            cities = cities.Where(c => c.Region != null && c.Region.Contains(searchDto.Region, StringComparison.OrdinalIgnoreCase));
        }

        if (searchDto.MinCostOfLiving.HasValue)
        {
            cities = cities.Where(c => c.AverageCostOfLiving >= searchDto.MinCostOfLiving.Value);
        }

        if (searchDto.MaxCostOfLiving.HasValue)
        {
            cities = cities.Where(c => c.AverageCostOfLiving <= searchDto.MaxCostOfLiving.Value);
        }

        if (searchDto.MinScore.HasValue)
        {
            cities = cities.Where(c => c.OverallScore >= searchDto.MinScore.Value);
        }

        if (searchDto.Tags != null && searchDto.Tags.Any())
        {
            cities = cities.Where(c => c.Tags != null && searchDto.Tags.All(tag => c.Tags.Contains(tag)));
        }

        // 应用分页
        cities = cities
            .Skip((searchDto.PageNumber - 1) * searchDto.PageSize)
            .Take(searchDto.PageSize);

        return cities.ToList();
    }

    public async Task<City> CreateAsync(City city)
    {
        city.Id = Guid.NewGuid();
        city.CreatedAt = DateTime.UtcNow;
        city.UpdatedAt = DateTime.UtcNow;

        var response = await SupabaseClient
            .From<City>()
            .Insert(city);

        return response.Models.First();
    }

    public async Task<City?> UpdateAsync(Guid id, City city)
    {
        city.UpdatedAt = DateTime.UtcNow;

        try
        {
            var response = await SupabaseClient
                .From<City>()
                .Where(x => x.Id == id)
                .Update(city);

            return response.Models.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            await SupabaseClient
                .From<City>()
                .Where(x => x.Id == id)
                .Delete();

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<int> GetTotalCountAsync()
    {
        var response = await SupabaseClient
            .From<City>()
            .Filter("is_active", Postgrest.Constants.Operator.Equals, "true")
            .Get();

        return response.Models.Count;
    }

    public async Task<IEnumerable<City>> GetRecommendedAsync(int count)
    {
        var response = await SupabaseClient
            .From<City>()
            .Filter("is_active", Postgrest.Constants.Operator.Equals, "true")
            .Order(x => x.OverallScore, Postgrest.Constants.Ordering.Descending)
            .Order(x => x.CommunityScore, Postgrest.Constants.Ordering.Descending)
            .Limit(count)
            .Get();

        return response.Models;
    }
}
