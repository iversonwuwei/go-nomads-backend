using System.Text.Json;
using Dapr.Client;

namespace CoworkingService.Services;

/// <summary>
///     城市信息响应 DTO (简化版，只包含需要的字段)
/// </summary>
public class CityInfoDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
}

/// <summary>
///     CityService 客户端接口 - 通过 Dapr Service Invocation 调用
/// </summary>
public interface ICityServiceClient
{
    /// <summary>
    ///     获取城市信息
    /// </summary>
    Task<CityInfoDto?> GetCityInfoAsync(string cityId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     批量获取城市信息
    /// </summary>
    Task<Dictionary<string, CityInfoDto>> GetCitiesInfoAsync(IEnumerable<string> cityIds,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     CityService 客户端实现 - 通过 Dapr Service Invocation 调用
/// </summary>
public class CityServiceClient : ICityServiceClient
{
    private readonly DaprClient _daprClient;
    private readonly ILogger<CityServiceClient> _logger;
    private readonly string _cityServiceAppId;

    public CityServiceClient(
        DaprClient daprClient,
        IConfiguration configuration,
        ILogger<CityServiceClient> logger)
    {
        _daprClient = daprClient;
        _logger = logger;
        // Dapr app-id 从配置读取,默认为 "city-service"
        _cityServiceAppId = configuration["Dapr:CityServiceAppId"] ?? "city-service";
    }

    /// <summary>
    ///     获取单个城市信息
    /// </summary>
    public async Task<CityInfoDto?> GetCityInfoAsync(string cityId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cityId))
        {
            return null;
        }

        try
        {
            _logger.LogDebug("📞 通过 Dapr 调用 CityService - GET /api/v1/cities/{CityId}", cityId);

            // 使用 Dapr Service Invocation 调用 CityService
            var response = await _daprClient.InvokeMethodAsync<JsonElement>(
                HttpMethod.Get,
                _cityServiceAppId,
                $"api/v1/cities/{cityId}",
                cancellationToken);

            // 手动解析 JSON 响应
            if (response.ValueKind == JsonValueKind.Object)
            {
                var success = response.TryGetProperty("success", out var successProp) && successProp.GetBoolean();

                if (success && response.TryGetProperty("data", out var dataElement))
                {
                    // 从 data 中提取城市信息
                    var cityInfo = new CityInfoDto
                    {
                        Id = GetStringProperty(dataElement, "id") ?? cityId,
                        Name = GetStringProperty(dataElement, "name") ?? string.Empty,
                        NameEn = GetStringProperty(dataElement, "nameEn") ?? string.Empty,
                        Country = GetStringProperty(dataElement, "country") ?? string.Empty,
                        CountryCode = GetStringProperty(dataElement, "countryCode") ?? string.Empty
                    };

                    _logger.LogDebug("✅ 获取城市信息成功: {CityId} -> {CityName}, {Country}",
                        cityId, cityInfo.Name, cityInfo.Country);

                    return cityInfo;
                }
            }

            _logger.LogWarning("⚠️ 无法解析城市信息响应: {CityId}", cityId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "❌ 获取城市信息失败: {CityId}", cityId);
            return null;
        }
    }

    /// <summary>
    ///     批量获取城市信息 (使用批量 API)
    /// </summary>
    public async Task<Dictionary<string, CityInfoDto>> GetCitiesInfoAsync(
        IEnumerable<string> cityIds,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, CityInfoDto>();
        var uniqueCityIds = cityIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();

        if (!uniqueCityIds.Any())
        {
            return result;
        }

        try
        {
            _logger.LogDebug("📞 批量获取城市信息 - POST /api/v1/cities/lookup: {Count} 个城市", uniqueCityIds.Count);

            // 转换为 Guid 列表
            var cityGuids = uniqueCityIds
                .Where(id => Guid.TryParse(id, out _))
                .Select(id => Guid.Parse(id))
                .ToList();

            if (!cityGuids.Any())
            {
                return result;
            }

            // 使用批量 API 一次获取所有城市信息
            var requestBody = new { CityIds = cityGuids };
            var response = await _daprClient.InvokeMethodAsync<object, JsonElement>(
                HttpMethod.Post,
                _cityServiceAppId,
                "api/v1/cities/lookup",
                requestBody,
                cancellationToken);

            // 解析响应
            if (response.ValueKind == JsonValueKind.Object)
            {
                var success = response.TryGetProperty("success", out var successProp) && successProp.GetBoolean();

                if (success && response.TryGetProperty("data", out var dataElement) &&
                    dataElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var cityElement in dataElement.EnumerateArray())
                    {
                        var cityInfo = new CityInfoDto
                        {
                            Id = GetStringProperty(cityElement, "id") ?? string.Empty,
                            Name = GetStringProperty(cityElement, "name") ?? string.Empty,
                            NameEn = GetStringProperty(cityElement, "nameEn") ?? string.Empty,
                            Country = GetStringProperty(cityElement, "country") ?? string.Empty,
                            CountryCode = GetStringProperty(cityElement, "countryCode") ?? string.Empty
                        };

                        if (!string.IsNullOrEmpty(cityInfo.Id))
                        {
                            result[cityInfo.Id] = cityInfo;
                        }
                    }
                }
            }

            _logger.LogDebug("✅ 批量 API 获取城市信息完成: {Success}/{Total}",
                result.Count, uniqueCityIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 批量 API 失败，降级为并发单独调用");

            // 降级：并发获取城市信息
            var tasks = uniqueCityIds.Select(async cityId =>
            {
                var cityInfo = await GetCityInfoAsync(cityId, cancellationToken);
                return (cityId, cityInfo);
            });

            var results = await Task.WhenAll(tasks);

            foreach (var (cityId, cityInfo) in results)
            {
                if (cityInfo != null)
                {
                    result[cityId] = cityInfo;
                }
            }
        }

        return result;
    }

    /// <summary>
    ///     安全获取 JSON 属性字符串值
    /// </summary>
    private static string? GetStringProperty(JsonElement element, string propertyName)
    {
        // 尝试 camelCase
        if (element.TryGetProperty(propertyName, out var prop))
        {
            return prop.ValueKind == JsonValueKind.String ? prop.GetString() : prop.ToString();
        }

        // 尝试 PascalCase
        var pascalName = char.ToUpperInvariant(propertyName[0]) + propertyName[1..];
        if (element.TryGetProperty(pascalName, out var pascalProp))
        {
            return pascalProp.ValueKind == JsonValueKind.String ? pascalProp.GetString() : pascalProp.ToString();
        }

        return null;
    }
}
