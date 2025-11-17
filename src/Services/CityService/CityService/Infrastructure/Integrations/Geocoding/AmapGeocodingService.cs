using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using CityService.Application.Abstractions.Services;
using CityService.Application.DTOs;

namespace CityService.Infrastructure.Integrations.Geocoding;

/// <summary>
/// AMap REST geocoding implementation.
/// </summary>
public class AmapGeocodingService : IAmapGeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AmapGeocodingService> _logger;
    private readonly string _apiKey;
    private readonly string _geocodeEndpoint;

    public AmapGeocodingService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AmapGeocodingService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["Amap:ApiKey"] ?? throw new InvalidOperationException("Amap:ApiKey is not configured");
        _geocodeEndpoint = configuration["Amap:GeocodeEndpoint"] ?? "https://restapi.amap.com/v3/geocode/geo";
    }

    public async Task<AmapGeocodeResult?> GeocodeAsync(string query, string? cityFilter = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        var requestUri = BuildRequestUri(query, cityFilter);
        try
        {
            _logger.LogInformation("Calling AMap geocode for query {Query} (city filter: {CityFilter})", query, string.IsNullOrWhiteSpace(cityFilter) ? "(none)" : cityFilter);
            var response = await _httpClient.GetAsync(requestUri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("AMap geocoding request failed with status {StatusCode} for query {Query}", response.StatusCode, query);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var payload = JsonSerializer.Deserialize<AmapGeocodeResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (payload?.Status != "1" || payload.Geocodes == null || payload.Geocodes.Count == 0)
            {
                _logger.LogInformation("AMap returned no geocodes for query {Query}", query);
                return null;
            }

            var first = payload.Geocodes.First();
            var (longitude, latitude) = ParseLocation(first.Location);
            var placeName = first.Building?.Name?.Trim().Length > 0
                ? first.Building.Name
                : first.POIName ?? first.Neighborhood?.Name ?? first.Township ?? first.City ?? first.Province ?? query;
            _logger.LogDebug("AMap geocode success for query {Query}: {{lat={Latitude}, lng={Longitude}, place={Place}}}", query, latitude, longitude, first.FormattedAddress ?? placeName);

            return new AmapGeocodeResult
            {
                Latitude = latitude,
                Longitude = longitude,
                FormattedAddress = first.FormattedAddress,
                PlaceName = placeName
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Unexpected error when calling AMap geocoding for query {Query}", query);
            return null;
        }
    }

    private string BuildRequestUri(string query, string? cityFilter)
    {
        var builder = new StringBuilder(_geocodeEndpoint);
        builder.Append("?key=").Append(Uri.EscapeDataString(_apiKey));
        builder.Append("&address=").Append(Uri.EscapeDataString(query));
        if (!string.IsNullOrWhiteSpace(cityFilter))
        {
            builder.Append("&city=").Append(Uri.EscapeDataString(cityFilter));
        }
        builder.Append("&output=JSON");
        return builder.ToString();
    }

    private static (double? Longitude, double? Latitude) ParseLocation(string? location)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return (null, null);
        }

        var parts = location.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            return (null, null);
        }

        if (double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude) &&
            double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude))
        {
            return (longitude, latitude);
        }

        return (null, null);
    }

    private sealed class AmapGeocodeResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("geocodes")]
        public List<AmapGeocodeItem> Geocodes { get; set; } = new();
    }

    private sealed class AmapGeocodeItem
    {
        [JsonPropertyName("formatted_address")]
        public string? FormattedAddress { get; set; }

        [JsonPropertyName("location")]
        public string? Location { get; set; }

        [JsonPropertyName("building")]
        public AmapNameValue? Building { get; set; }

        [JsonPropertyName("neighborhood")]
        public AmapNameValue? Neighborhood { get; set; }

        [JsonPropertyName("city")]
        public JsonElement CityElement { get; set; }

        [JsonIgnore]
        public string? City => ExtractFlexibleString(CityElement);

        [JsonPropertyName("province")]
        public JsonElement ProvinceElement { get; set; }

        [JsonIgnore]
        public string? Province => ExtractFlexibleString(ProvinceElement);

        [JsonPropertyName("township")]
        public JsonElement TownshipElement { get; set; }

        [JsonIgnore]
        public string? Township => ExtractFlexibleString(TownshipElement);

        [JsonPropertyName("pois")]
        public List<AmapPoi>? Pois { get; set; }

        [JsonPropertyName("name")]
        public string? POIName => Pois?.FirstOrDefault()?.Name;
    }

    private sealed class AmapNameValue
    {
        [JsonPropertyName("name")]
        public JsonElement NameElement { get; set; }

        [JsonIgnore]
        public string? Name => ExtractFlexibleString(NameElement);
    }

    private sealed class AmapPoi
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    private static string? ExtractFlexibleString(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => Normalize(element.GetString()),
            JsonValueKind.Number => element.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Array => element
                .EnumerateArray()
                .Select(ExtractFlexibleString)
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)),
            JsonValueKind.Object when element.TryGetProperty("name", out var nameElement) => ExtractFlexibleString(nameElement),
            _ => null
        };

        static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
