using CityService.Application.DTOs;

namespace CityService.Application.Abstractions.Services;

/// <summary>
///     Provides access to AMap (Gaode) geocoding capabilities.
/// </summary>
public interface IAmapGeocodingService
{
    /// <summary>
    ///     Attempts to geocode the specified query text and returns coordinate/address metadata when available.
    /// </summary>
    /// <param name="query">The free-form query text (e.g. landmark name or address).</param>
    /// <param name="cityFilter">
    ///     Optional city identifier/keyword that will be forwarded to AMap to improve accuracy. Can be an adcode or city name.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<AmapGeocodeResult?> GeocodeAsync(string query, string? cityFilter = null,
        CancellationToken cancellationToken = default);
}