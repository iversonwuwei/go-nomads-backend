using Dapr.Client;
using EventService.Application.DTOs;
using GoNomads.Shared.Models;
using System.Text.Json;

namespace EventService.Infrastructure.GrpcClients;

/// <summary>
/// City Service gRPC 客户端实现（通过 Dapr）
/// </summary>
public class CityGrpcClient : ICityGrpcClient
{
    private readonly DaprClient _daprClient;
    private readonly ILogger<CityGrpcClient> _logger;
    private const string CityServiceAppId = "city-service";

    public CityGrpcClient(DaprClient daprClient, ILogger<CityGrpcClient> logger)
    {
        _daprClient = daprClient;
        _logger = logger;
    }

    public async Task<CityInfo?> GetCityByIdAsync(Guid cityId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("🌍 通过 Dapr 调用 CityService 获取城市信息: CityId={CityId}", cityId);

            // 使用 Dapr Service Invocation 调用 CityService
            var response = await _daprClient.InvokeMethodAsync<ApiResponse<CityDto>>(
                HttpMethod.Get,
                CityServiceAppId,
                $"api/v1/cities/{cityId}",
                cancellationToken);

            if (response?.Success == true && response.Data != null)
            {
                var cityDto = response.Data;
                return new CityInfo
                {
                    Id = cityDto.Id,
                    Name = cityDto.Name,
                    Country = cityDto.Country,
                    Region = cityDto.Region,
                    ImageUrl = cityDto.ImageUrl,
                    TimeZone = cityDto.TimeZone
                };
            }

            _logger.LogWarning("⚠️ CityService 返回空数据或失败: CityId={CityId}", cityId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 调用 CityService 失败: CityId={CityId}", cityId);
            return null;
        }
    }

    public async Task<Dictionary<Guid, CityInfo>> GetCitiesByIdsAsync(
        IEnumerable<Guid> cityIds,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<Guid, CityInfo>();
        var uniqueCityIds = cityIds.Distinct().Where(id => id != Guid.Empty).ToList();

        if (!uniqueCityIds.Any())
        {
            return result;
        }

        _logger.LogInformation("🌍 批量获取城市信息: Count={Count}", uniqueCityIds.Count);

        // 并行获取城市信息
        var tasks = uniqueCityIds.Select(async cityId =>
        {
            var cityInfo = await GetCityByIdAsync(cityId, cancellationToken);
            return (cityId, cityInfo);
        });

        var cities = await Task.WhenAll(tasks);

        foreach (var (cityId, cityInfo) in cities)
        {
            if (cityInfo != null)
            {
                result[cityId] = cityInfo;
            }
        }

        _logger.LogInformation("✅ 批量获取城市信息完成: 请求={Requested}, 成功={Success}",
            uniqueCityIds.Count, result.Count);

        return result;
    }
}

/// <summary>
/// CityService 返回的 DTO（映射）
/// </summary>
internal class CityDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? Region { get; set; }
    public string? ImageUrl { get; set; }
    public string? TimeZone { get; set; }
}
