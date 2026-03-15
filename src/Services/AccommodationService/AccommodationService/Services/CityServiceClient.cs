using System.Net.Http.Json;
using System.Text.Json;

namespace AccommodationService.Services;

public class CityInfoDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string Country { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public interface ICityServiceClient
{
    Task<CityInfoDto?> GetCityInfoAsync(string cityId, CancellationToken cancellationToken = default);
}

public class CityServiceClient : ICityServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CityServiceClient> _logger;

    public CityServiceClient(HttpClient httpClient, ILogger<CityServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CityInfoDto?> GetCityInfoAsync(string cityId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cityId))
        {
            return null;
        }

        try
        {
            var response = await _httpClient.GetAsync($"api/v1/cities/{cityId}", cancellationToken);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
            if (payload.ValueKind != JsonValueKind.Object ||
                !payload.TryGetProperty("success", out var successNode) ||
                !successNode.GetBoolean() ||
                !payload.TryGetProperty("data", out var dataNode))
            {
                return null;
            }

            return new CityInfoDto
            {
                Id = ReadString(dataNode, "id") ?? cityId,
                Name = ReadString(dataNode, "name") ?? string.Empty,
                NameEn = ReadString(dataNode, "nameEn"),
                Country = ReadString(dataNode, "country") ?? string.Empty,
                Latitude = ReadDouble(dataNode, "latitude"),
                Longitude = ReadDouble(dataNode, "longitude")
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取城市信息失败: {CityId}", cityId);
            return null;
        }
    }

    private static string? ReadString(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var node))
        {
            return node.ValueKind == JsonValueKind.String ? node.GetString() : node.ToString();
        }

        var pascalName = char.ToUpperInvariant(propertyName[0]) + propertyName[1..];
        if (element.TryGetProperty(pascalName, out var pascalNode))
        {
            return pascalNode.ValueKind == JsonValueKind.String ? pascalNode.GetString() : pascalNode.ToString();
        }

        return null;
    }

    private static double? ReadDouble(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var node) ||
            element.TryGetProperty(char.ToUpperInvariant(propertyName[0]) + propertyName[1..], out node))
        {
            if (node.ValueKind == JsonValueKind.Number && node.TryGetDouble(out var value))
            {
                return value;
            }

            if (node.ValueKind == JsonValueKind.String && double.TryParse(node.GetString(), out value))
            {
                return value;
            }
        }

        return null;
    }
}