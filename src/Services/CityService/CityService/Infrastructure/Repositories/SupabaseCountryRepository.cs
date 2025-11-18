using CityService.Domain.Entities;
using CityService.Domain.Repositories;
using Postgrest;
using Client = Supabase.Client;

namespace CityService.Infrastructure.Repositories;

/// <summary>
///     基于 Supabase 的国家仓储实现
/// </summary>
public class SupabaseCountryRepository : ICountryRepository
{
    private readonly ILogger<SupabaseCountryRepository> _logger;
    private readonly Client _supabaseClient;

    public SupabaseCountryRepository(Client supabaseClient, ILogger<SupabaseCountryRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<IEnumerable<Country>> GetAllCountriesAsync()
    {
        try
        {
            var response = await _supabaseClient
                .From<Country>()
                .Filter("is_active", Constants.Operator.Equals, "true")
                .Order("name", Constants.Ordering.Ascending)
                .Get();

            return response.Models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all countries");
            throw;
        }
    }

    public async Task<Country?> GetCountryByIdAsync(Guid id)
    {
        try
        {
            var response = await _supabaseClient
                .From<Country>()
                .Where(x => x.Id == id)
                .Single();

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting country by id: {Id}", id);
            return null;
        }
    }

    public async Task<Country?> GetCountryByCodeAsync(string code)
    {
        try
        {
            var response = await _supabaseClient
                .From<Country>()
                .Filter("code", Constants.Operator.Equals, code.ToUpperInvariant())
                .Single();

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting country by code: {Code}", code);
            return null;
        }
    }

    public async Task<Country> CreateCountryAsync(Country country)
    {
        try
        {
            country.Id = Guid.NewGuid();
            country.CreatedAt = DateTime.UtcNow;

            var response = await _supabaseClient
                .From<Country>()
                .Insert(country);

            return response.Models.First();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating country: {Name}", country.Name);
            throw;
        }
    }

    public async Task<Country> UpdateCountryAsync(Country country)
    {
        try
        {
            country.UpdatedAt = DateTime.UtcNow;

            var response = await _supabaseClient
                .From<Country>()
                .Update(country);

            return response.Models.First();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating country: {Id}", country.Id);
            throw;
        }
    }

    public async Task<bool> DeleteCountryAsync(Guid id)
    {
        try
        {
            await _supabaseClient
                .From<Country>()
                .Where(x => x.Id == id)
                .Delete();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting country: {Id}", id);
            return false;
        }
    }
}