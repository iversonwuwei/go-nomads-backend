namespace CityService.Application.Services;

/// <summary>
///     地理编码服务接口 - 支持反向地理编码（坐标转地址）
/// </summary>
public interface IGeocodingService
{
    /// <summary>
    ///     反向地理编码 - 根据经纬度获取地址信息
    /// </summary>
    /// <param name="latitude">纬度</param>
    /// <param name="longitude">经度</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>地理编码结果</returns>
    Task<GeocodingResult?> ReverseGeocodeAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     正向地理编码 - 根据地址获取经纬度
    /// </summary>
    /// <param name="address">地址</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>地理编码结果</returns>
    Task<GeocodingResult?> GeocodeAsync(
        string address,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     地理编码结果
/// </summary>
public class GeocodingResult
{
    /// <summary>
    ///     纬度
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    ///     经度
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    ///     格式化地址
    /// </summary>
    public string? FormattedAddress { get; set; }

    /// <summary>
    ///     国家名称
    /// </summary>
    public string? CountryName { get; set; }

    /// <summary>
    ///     国家代码 (ISO 3166-1 alpha-2)
    /// </summary>
    public string? CountryCode { get; set; }

    /// <summary>
    ///     省/州名称
    /// </summary>
    public string? ProvinceName { get; set; }

    /// <summary>
    ///     城市名称
    /// </summary>
    public string? CityName { get; set; }

    /// <summary>
    ///     区/县名称
    /// </summary>
    public string? DistrictName { get; set; }

    /// <summary>
    ///     街道地址
    /// </summary>
    public string? StreetAddress { get; set; }
}
