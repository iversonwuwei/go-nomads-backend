using System;
using System.Collections.Generic;
using System.Linq;
using CityService.Domain.Entities;
using CityService.Domain.Repositories;
using CityService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Shared.Repositories;
using Supabase;

namespace CityService.Infrastructure.Repositories;

/// <summary>
/// 基于 Supabase 的城市仓储实现
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
            .Order(x => x.OverallScore!, Postgrest.Constants.Ordering.Descending)
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
            .Filter("is_active", Postgrest.Constants.Operator.Equals, "true")
            .Order(x => x.OverallScore!, Postgrest.Constants.Ordering.Descending)
            .Get();

        var cities = response.Models.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(criteria.Name))
        {
            cities = cities.Where(c => c.Name.Contains(criteria.Name, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Country))
        {
            cities = cities.Where(c => c.Country.Contains(criteria.Country, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Region))
        {
            cities = cities.Where(c => c.Region != null && c.Region.Contains(criteria.Region, StringComparison.OrdinalIgnoreCase));
        }

        if (criteria.MinCostOfLiving.HasValue)
        {
            cities = cities.Where(c => c.AverageCostOfLiving >= criteria.MinCostOfLiving.Value);
        }

        if (criteria.MaxCostOfLiving.HasValue)
        {
            cities = cities.Where(c => c.AverageCostOfLiving <= criteria.MaxCostOfLiving.Value);
        }

        if (criteria.MinScore.HasValue)
        {
            cities = cities.Where(c => c.OverallScore >= criteria.MinScore.Value);
        }

        if (criteria.Tags is { Count: > 0 })
        {
            cities = cities.Where(c => c.Tags != null && criteria.Tags.All(tag => c.Tags.Contains(tag)));
        }

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
            .Order(x => x.OverallScore!, Postgrest.Constants.Ordering.Descending)
            .Order(x => x.CommunityScore!, Postgrest.Constants.Ordering.Descending)
            .Limit(count)
            .Get();

        return response.Models;
    }

    public async Task<IEnumerable<City>> GetByCountryAsync(string countryName)
    {
        var response = await SupabaseClient
            .From<City>()
            .Filter("is_active", Postgrest.Constants.Operator.Equals, "true")
            .Filter("country", Postgrest.Constants.Operator.ILike, $"%{countryName}%")
            .Order(x => x.Name, Postgrest.Constants.Ordering.Ascending)
            .Get();

        return response.Models;
    }
}
