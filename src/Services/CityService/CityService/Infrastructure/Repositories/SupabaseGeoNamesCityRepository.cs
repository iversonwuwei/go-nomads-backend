using CityService.Domain.Entities;
using CityService.Domain.Repositories;
using Postgrest;
using Postgrest.Attributes;
using Shared.Repositories;
using Client = Supabase.Client;

namespace CityService.Infrastructure.Repositories;

/// <summary>
///     基于 Supabase 的 GeoNames 城市仓储实现
/// </summary>
public class SupabaseGeoNamesCityRepository : SupabaseRepositoryBase<GeoNamesCity>, IGeoNamesCityRepository
{
    public SupabaseGeoNamesCityRepository(Client supabaseClient, ILogger<SupabaseGeoNamesCityRepository> logger)
        : base(supabaseClient, logger)
    {
    }

    public async Task<GeoNamesCity> UpsertAsync(GeoNamesCity city)
    {
        try
        {
            // 添加调试日志
            var tableAttr = typeof(GeoNamesCity).GetCustomAttributes(typeof(TableAttribute), true)
                .FirstOrDefault() as TableAttribute;
            Logger.LogInformation("Upserting to table: {TableName} (from GeoNamesCity type)",
                tableAttr?.Name ?? "UNKNOWN");
            Logger.LogInformation("City to upsert: GeonameId={GeonameId}, Name={Name}", city.GeonameId, city.Name);

            // 检查是否已存在
            var existing = await GetByGeonameIdAsync(city.GeonameId);

            if (existing != null)
            {
                // 更新
                Logger.LogInformation("Updating existing city: Id={Id}", existing.Id);
                city.Id = existing.Id;
                city.UpdatedAt = DateTime.UtcNow;

                var updateResponse = await SupabaseClient
                    .From<GeoNamesCity>()
                    .Where(x => x.Id == existing.Id)
                    .Update(city);

                return updateResponse.Models.First();
            }

            // 插入
            Logger.LogInformation("Inserting new city: Name={Name}", city.Name);
            city.Id = Guid.NewGuid();
            city.ImportedAt = DateTime.UtcNow;
            city.UpdatedAt = DateTime.UtcNow;

            var insertResponse = await SupabaseClient
                .From<GeoNamesCity>()
                .Insert(city);

            return insertResponse.Models.First();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error upserting city: {Name}, GeonameId={GeonameId}", city.Name, city.GeonameId);
            throw;
        }
    }

    public async Task<IEnumerable<GeoNamesCity>> UpsertBatchAsync(IEnumerable<GeoNamesCity> cities)
    {
        var result = new List<GeoNamesCity>();

        foreach (var city in cities)
            try
            {
                var upserted = await UpsertAsync(city);
                result.Add(upserted);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to upsert GeoNames city {Name}", city.Name);
            }

        return result;
    }

    public async Task<GeoNamesCity?> GetByGeonameIdAsync(long geonameId)
    {
        try
        {
            var response = await SupabaseClient
                .From<GeoNamesCity>()
                .Where(x => x.GeonameId == geonameId)
                .Single();

            return response;
        }
        catch
        {
            return null;
        }
    }

    public async Task<GeoNamesCity?> GetByNameAndCountryAsync(string name, string countryCode)
    {
        try
        {
            var response = await SupabaseClient
                .From<GeoNamesCity>()
                .Filter("name", Constants.Operator.Equals, name)
                .Filter("country_code", Constants.Operator.Equals, countryCode)
                .Single();

            return response;
        }
        catch
        {
            return null;
        }
    }

    public async Task<IEnumerable<GeoNamesCity>> GetByCountryCodeAsync(string countryCode)
    {
        var response = await SupabaseClient
            .From<GeoNamesCity>()
            .Filter("country_code", Constants.Operator.Equals, countryCode)
            .Order(x => x.Population!, Constants.Ordering.Descending)
            .Get();

        return response.Models;
    }

    public async Task<IEnumerable<GeoNamesCity>> GetUnsyncedAsync(int limit = 100)
    {
        var response = await SupabaseClient
            .From<GeoNamesCity>()
            .Filter("synced_to_cities", Constants.Operator.Equals, "false")
            .Order(x => x.Population!, Constants.Ordering.Descending)
            .Limit(limit)
            .Get();

        return response.Models;
    }

    public async Task MarkAsSyncedAsync(Guid id, Guid cityId)
    {
        var city = new GeoNamesCity
        {
            Id = id,
            SyncedToCities = true,
            CityId = cityId,
            UpdatedAt = DateTime.UtcNow
        };

        await SupabaseClient
            .From<GeoNamesCity>()
            .Where(x => x.Id == id)
            .Update(city);
    }

    public async Task<IEnumerable<GeoNamesCity>> SearchAsync(string? namePattern = null, string? countryCode = null,
        long? minPopulation = null)
    {
        var response = await SupabaseClient
            .From<GeoNamesCity>()
            .Get();

        var cities = response.Models.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(namePattern))
            cities = cities.Where(c => c.Name.Contains(namePattern, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(countryCode)) cities = cities.Where(c => c.CountryCode == countryCode);

        if (minPopulation.HasValue) cities = cities.Where(c => c.Population >= minPopulation.Value);

        return cities
            .OrderByDescending(c => c.Population)
            .Take(100)
            .ToList();
    }

    public async Task<int> GetCountAsync(string? countryCode = null)
    {
        if (!string.IsNullOrWhiteSpace(countryCode))
        {
            var response = await SupabaseClient
                .From<GeoNamesCity>()
                .Filter("country_code", Constants.Operator.Equals, countryCode)
                .Get();
            return response.Models.Count;
        }
        else
        {
            var response = await SupabaseClient
                .From<GeoNamesCity>()
                .Get();
            return response.Models.Count;
        }
    }

    public async Task<bool> DeleteByCountryCodeAsync(string countryCode)
    {
        try
        {
            await SupabaseClient
                .From<GeoNamesCity>()
                .Filter("country_code", Constants.Operator.Equals, countryCode)
                .Delete();

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to delete GeoNames cities for country {CountryCode}", countryCode);
            return false;
        }
    }
}