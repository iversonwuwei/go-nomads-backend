using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using EventService.Application.DTOs;
using GoNomads.Shared.Communication;
using GoNomads.Shared.Models;

namespace EventService.Infrastructure.GrpcClients;

/// <summary>
///     City Service gRPC 客户端实现
/// </summary>
public class CityGrpcClient : ICityGrpcClient
{
    private const string CityServiceAppId = "city-service";
    private const string CityLookupEndpoint = "api/v1/cities/lookup";
    private const int LookupBatchSize = 50;
    private readonly ILogger<CityGrpcClient> _logger;
    private readonly ServiceInvocationClient _serviceInvocationClient;

    public CityGrpcClient(ServiceInvocationClient serviceInvocationClient, ILogger<CityGrpcClient> logger)
    {
        _serviceInvocationClient = serviceInvocationClient;
        _logger = logger;
    }

    public async Task<CityInfo?> GetCityByIdAsync(Guid cityId, CancellationToken cancellationToken = default)
    {
        if (cityId == Guid.Empty)
        {
            _logger.LogWarning("⚠️ 无效的城市 ID");
            return null;
        }

        var cities = await GetCitiesByIdsAsync(new[] { cityId }, cancellationToken);
        if (cities.TryGetValue(cityId, out var cityInfo))
            return cityInfo;

        _logger.LogWarning("⚠️ CityService 返回空数据或失败: CityId={CityId}", cityId);
        return null;
    }

    public async Task<Dictionary<Guid, CityInfo>> GetCitiesByIdsAsync(
        IEnumerable<Guid> cityIds,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<Guid, CityInfo>();

        if (cityIds == null)
            return result;

        var uniqueCityIds = cityIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (!uniqueCityIds.Any())
            return result;

        _logger.LogInformation("🌍 批量获取城市信息: Count={Count}", uniqueCityIds.Count);

        foreach (var batch in uniqueCityIds.Chunk(LookupBatchSize))
        {
            var payload = new CityLookupRequest(batch.ToList());
            try
            {
                var response = await _serviceInvocationClient.InvokeAsync<CityLookupRequest, ApiResponse<List<CityDto>>>(
                    HttpMethod.Post,
                    CityServiceAppId,
                    CityLookupEndpoint,
                    payload,
                    cancellationToken);

                if (response?.Success == true && response.Data != null)
                {
                    foreach (var cityDto in response.Data)
                        result[cityDto.Id] = MapToCityInfo(cityDto);
                }
                else
                {
                    _logger.LogWarning("⚠️ City lookup failed for batch size {BatchSize}", batch.Length);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ City lookup request failed for batch size {BatchSize}", batch.Length);
            }
        }

        _logger.LogInformation("✅ 批量获取城市信息完成: 请求={Requested}, 成功={Success}",
            uniqueCityIds.Count,
            result.Count);

        return result;
    }

    private static CityInfo MapToCityInfo(CityDto cityDto)
    {
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

    private sealed record CityLookupRequest(List<Guid> CityIds);
}

/// <summary>
///     CityService 返回的 DTO（映射）
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