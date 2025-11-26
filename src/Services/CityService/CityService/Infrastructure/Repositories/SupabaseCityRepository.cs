using CityService.Domain.Entities;
using CityService.Domain.Repositories;
using CityService.Domain.ValueObjects;
using Postgrest;
using Shared.Repositories;
using Client = Supabase.Client;

namespace CityService.Infrastructure.Repositories;

/// <summary>
///     基于 Supabase 的城市仓储实现
/// </summary>
public class SupabaseCityRepository : SupabaseRepositoryBase<City>, ICityRepository
{
    private readonly IConfiguration _configuration;

    public SupabaseCityRepository(Client supabaseClient, ILogger<SupabaseCityRepository> logger, IConfiguration configuration)
        : base(supabaseClient, logger)
    {
        _configuration = configuration;
    }

    public async Task<IEnumerable<City>> GetAllAsync(int pageNumber, int pageSize)
    {
        var offset = (pageNumber - 1) * pageSize;

        var response = await SupabaseClient
            .From<City>()
            .Filter("is_active", Constants.Operator.Equals, "true")
            .Order(x => x.OverallScore!, Constants.Ordering.Descending)
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

    public async Task<IEnumerable<City>> SearchAsync(CitySearchCriteria criteria)
    {
        var response = await SupabaseClient
            .From<City>()
            .Filter("is_active", Constants.Operator.Equals, "true")
            .Order(x => x.OverallScore!, Constants.Ordering.Descending)
            .Get();

        var cities = response.Models.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(criteria.Name))
            // 支持中英文搜索: 在 name 或 name_en 字段中搜索
            cities = cities.Where(c =>
                c.Name.Contains(criteria.Name, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(c.NameEn) &&
                 c.NameEn.Contains(criteria.Name, StringComparison.OrdinalIgnoreCase))
            );

        if (!string.IsNullOrWhiteSpace(criteria.Country))
            cities = cities.Where(c => c.Country.Contains(criteria.Country, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(criteria.Region))
            cities = cities.Where(c =>
                c.Region != null && c.Region.Contains(criteria.Region, StringComparison.OrdinalIgnoreCase));

        if (criteria.MinCostOfLiving.HasValue)
            cities = cities.Where(c => c.AverageCostOfLiving >= criteria.MinCostOfLiving.Value);

        if (criteria.MaxCostOfLiving.HasValue)
            cities = cities.Where(c => c.AverageCostOfLiving <= criteria.MaxCostOfLiving.Value);

        if (criteria.MinScore.HasValue) cities = cities.Where(c => c.OverallScore >= criteria.MinScore.Value);

        if (criteria.Tags is { Count: > 0 })
            cities = cities.Where(c => c.Tags != null && criteria.Tags.All(tag => c.Tags.Contains(tag)));

        return cities
            .Skip((criteria.PageNumber - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToList();
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
            Logger.LogInformation("🔄 [SupabaseCityRepository] 开始更新城市: Id={Id}", id);
            
            // 注意：moderator_id 字段已不在 cities 表中，改用 city_moderators 表
            // 这里只更新城市的基本信息字段
            var response = await SupabaseClient
                .From<City>()
                .Where(x => x.Id == id)
                .Update(city);

            var updatedCity = response.Models.FirstOrDefault();
            
            if (updatedCity != null)
            {
                Logger.LogInformation("✅ [SupabaseCityRepository] 城市更新成功: Id={Id}", updatedCity.Id);
            }
            else
            {
                Logger.LogWarning("⚠️ [SupabaseCityRepository] 更新返回空结果: Id={Id}", id);
            }
            
            return updatedCity;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ [SupabaseCityRepository] 更新城市失败: Id={Id}, Error={Error}", id, ex.Message);
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
            .Filter("is_active", Constants.Operator.Equals, "true")
            .Get();

        return response.Models.Count;
    }

    public async Task<IEnumerable<City>> GetRecommendedAsync(int count)
    {
        var response = await SupabaseClient
            .From<City>()
            .Filter("is_active", Constants.Operator.Equals, "true")
            .Order(x => x.OverallScore!, Constants.Ordering.Descending)
            .Order(x => x.CommunityScore!, Constants.Ordering.Descending)
            .Limit(count)
            .Get();

        return response.Models;
    }

    public async Task<IEnumerable<City>> GetByCountryAsync(string countryName)
    {
        var response = await SupabaseClient
            .From<City>()
            .Filter("is_active", Constants.Operator.Equals, "true")
            .Filter("country", Constants.Operator.ILike, $"%{countryName}%")
            .Order(x => x.Name, Constants.Ordering.Ascending)
            .Get();

        return response.Models;
    }

    public async Task<IEnumerable<City>> GetByIdsAsync(IEnumerable<Guid> cityIds)
    {
        var idList = cityIds?.Where(id => id != Guid.Empty).Distinct().ToList();
        if (idList == null || idList.Count == 0) return Enumerable.Empty<City>();

        // Postgrest In operator 需要传递 List，不是字符串
        var response = await SupabaseClient
            .From<City>()
            .Filter("id", Constants.Operator.In, idList)
            .Get();

        return response.Models;
    }
}