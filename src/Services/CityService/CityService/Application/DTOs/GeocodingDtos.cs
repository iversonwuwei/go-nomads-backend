namespace CityService.Application.DTOs;

/// <summary>
/// Represents a subset of the AMap geocoding payload used across the application.
/// </summary>
public class AmapGeocodeResult
{
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string? FormattedAddress { get; init; }
    public string? PlaceName { get; init; }
}
