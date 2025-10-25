using System;
using System.Collections.Generic;
using System.Linq;
using CityService.Domain.Entities;
using CityService.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Supabase;

namespace CityService.Infrastructure.Repositories;

/// <summary>
/// 基于 Supabase 的省份仓储实现
/// </summary>
public class SupabaseProvinceRepository : IProvinceRepository
{
    private readonly Client _supabaseClient;
    private readonly ILogger<SupabaseProvinceRepository> _logger;

    public SupabaseProvinceRepository(Client supabaseClient, ILogger<SupabaseProvinceRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<IEnumerable<Province>> GetAllProvincesAsync()
    {
        try
        {
            var response = await _supabaseClient
                .From<Province>()
                .Filter("is_active", Postgrest.Constants.Operator.Equals, "true")
                .Order("name", Postgrest.Constants.Ordering.Ascending)
                .Get();

            return response.Models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all provinces");
            throw;
        }
    }

    public async Task<IEnumerable<Province>> GetProvincesByCountryIdAsync(Guid countryId)
    {
        try
        {
            var response = await _supabaseClient
                .From<Province>()
                .Filter("country_id", Postgrest.Constants.Operator.Equals, countryId.ToString())
                .Filter("is_active", Postgrest.Constants.Operator.Equals, "true")
                .Order("name", Postgrest.Constants.Ordering.Ascending)
                .Get();

            return response.Models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting provinces by country id: {CountryId}", countryId);
            throw;
        }
    }

    public async Task<Province?> GetProvinceByIdAsync(Guid id)
    {
        try
        {
            var response = await _supabaseClient
                .From<Province>()
                .Where(x => x.Id == id)
                .Single();

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting province by id: {Id}", id);
            return null;
        }
    }

    public async Task<Province> CreateProvinceAsync(Province province)
    {
        try
        {
            province.Id = Guid.NewGuid();
            province.CreatedAt = DateTime.UtcNow;

            var response = await _supabaseClient
                .From<Province>()
                .Insert(province);

            return response.Models.First();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating province: {Name}", province.Name);
            throw;
        }
    }

    public async Task<Province> UpdateProvinceAsync(Province province)
    {
        try
        {
            province.UpdatedAt = DateTime.UtcNow;

            var response = await _supabaseClient
                .From<Province>()
                .Update(province);

            return response.Models.First();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating province: {Id}", province.Id);
            throw;
        }
    }

    public async Task<bool> DeleteProvinceAsync(Guid id)
    {
        try
        {
            await _supabaseClient
                .From<Province>()
                .Where(x => x.Id == id)
                .Delete();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting province: {Id}", id);
            return false;
        }
    }

    public async Task<int> BulkCreateProvincesAsync(IEnumerable<Province> provinces)
    {
        try
        {
            var provinceList = provinces.ToList();
            foreach (var province in provinceList)
            {
                province.Id = Guid.NewGuid();
                province.CreatedAt = DateTime.UtcNow;
            }

            var response = await _supabaseClient
                .From<Province>()
                .Insert(provinceList);

            return response.Models.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk creating provinces");
            throw;
        }
    }
}
