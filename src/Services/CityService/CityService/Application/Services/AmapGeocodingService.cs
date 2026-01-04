using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CityService.Application.Services;

/// <summary>
///     高德地图地理编码服务
///     用于中国境内的地理编码
/// </summary>
public class AmapGeocodingService : IGeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AmapGeocodingService> _logger;
    private readonly string _apiKey;

    private const string BaseUrl = "https://restapi.amap.com/v3";

    public AmapGeocodingService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AmapGeocodingService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["Amap:ApiKey"] ?? throw new InvalidOperationException("Amap:ApiKey 配置缺失");
    }

    public async Task<GeocodingResult?> ReverseGeocodeAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 高德坐标格式: longitude,latitude
            var location = $"{longitude:F6},{latitude:F6}";
            var url = $"{BaseUrl}/geocode/regeo?key={_apiKey}&location={location}&extensions=base&output=json";

            _logger.LogDebug("🌍 调用高德反向地理编码: {Location}", location);

            var response = await _httpClient.GetFromJsonAsync<AmapRegeoResponse>(url, cancellationToken);

            if (response?.Status == "1" && response.Regeocode != null)
            {
                var addressComponent = response.Regeocode.AddressComponent;
                
                return new GeocodingResult
                {
                    Latitude = latitude,
                    Longitude = longitude,
                    FormattedAddress = response.Regeocode.FormattedAddress,
                    CountryName = addressComponent?.Country,
                    CountryCode = "CN", // 高德仅支持中国
                    ProvinceName = addressComponent?.Province,
                    CityName = ParseCityName(addressComponent),
                    DistrictName = addressComponent?.District,
                    StreetAddress = addressComponent?.StreetNumber?.Street
                };
            }

            _logger.LogWarning("⚠️ 高德反向地理编码失败: {Info}", response?.Info);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 高德反向地理编码异常");
            return null;
        }
    }

    public async Task<GeocodingResult?> GeocodeAsync(
        string address,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var encodedAddress = Uri.EscapeDataString(address);
            var url = $"{BaseUrl}/geocode/geo?key={_apiKey}&address={encodedAddress}&output=json";

            _logger.LogDebug("🌍 调用高德正向地理编码: {Address}", address);

            var response = await _httpClient.GetFromJsonAsync<AmapGeoResponse>(url, cancellationToken);

            if (response?.Status == "1" && response.Geocodes?.Count > 0)
            {
                var geocode = response.Geocodes[0];
                var location = geocode.Location?.Split(',');
                
                if (location?.Length == 2 &&
                    double.TryParse(location[0], out var lng) &&
                    double.TryParse(location[1], out var lat))
                {
                    return new GeocodingResult
                    {
                        Latitude = lat,
                        Longitude = lng,
                        FormattedAddress = geocode.FormattedAddress,
                        CountryName = "中国",
                        CountryCode = "CN",
                        ProvinceName = geocode.Province,
                        CityName = geocode.City,
                        DistrictName = geocode.District
                    };
                }
            }

            _logger.LogWarning("⚠️ 高德正向地理编码失败: {Info}", response?.Info);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 高德正向地理编码异常");
            return null;
        }
    }

    /// <summary>
    ///     解析城市名称（处理直辖市情况）
    /// </summary>
    private static string? ParseCityName(AmapAddressComponent? component)
    {
        if (component == null) return null;

        // 直辖市情况：city 可能为空数组 []，使用 province
        if (string.IsNullOrEmpty(component.City) || component.City == "[]")
        {
            return component.Province;
        }

        return component.City;
    }

    #region 高德 API 响应模型

    private class AmapRegeoResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("info")]
        public string? Info { get; set; }

        [JsonPropertyName("regeocode")]
        public AmapRegeocode? Regeocode { get; set; }
    }

    private class AmapRegeocode
    {
        [JsonPropertyName("formatted_address")]
        public string? FormattedAddress { get; set; }

        [JsonPropertyName("addressComponent")]
        public AmapAddressComponent? AddressComponent { get; set; }
    }

    private class AmapAddressComponent
    {
        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("province")]
        public string? Province { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("district")]
        public string? District { get; set; }

        [JsonPropertyName("streetNumber")]
        public AmapStreetNumber? StreetNumber { get; set; }
    }

    private class AmapStreetNumber
    {
        [JsonPropertyName("street")]
        public string? Street { get; set; }

        [JsonPropertyName("number")]
        public string? Number { get; set; }
    }

    private class AmapGeoResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("info")]
        public string? Info { get; set; }

        [JsonPropertyName("geocodes")]
        public List<AmapGeocode>? Geocodes { get; set; }
    }

    private class AmapGeocode
    {
        [JsonPropertyName("formatted_address")]
        public string? FormattedAddress { get; set; }

        [JsonPropertyName("location")]
        public string? Location { get; set; }

        [JsonPropertyName("province")]
        public string? Province { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("district")]
        public string? District { get; set; }
    }

    #endregion
}
