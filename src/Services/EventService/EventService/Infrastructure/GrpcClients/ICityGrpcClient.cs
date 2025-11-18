using EventService.Application.DTOs;

namespace EventService.Infrastructure.GrpcClients;

/// <summary>
///     City Service gRPC 客户端接口
/// </summary>
public interface ICityGrpcClient
{
    /// <summary>
    ///     根据城市 ID 获取城市信息
    /// </summary>
    Task<CityInfo?> GetCityByIdAsync(Guid cityId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     批量获取城市信息
    /// </summary>
    Task<Dictionary<Guid, CityInfo>> GetCitiesByIdsAsync(IEnumerable<Guid> cityIds,
        CancellationToken cancellationToken = default);
}