namespace CityService.Application.Services;

/// <summary>
///     城市匹配服务接口 - 根据经纬度 + 城市名称匹配现有城市
/// </summary>
public interface ICityMatchingService
{
    /// <summary>
    ///     根据经纬度和城市名称匹配城市
    /// </summary>
    Task<CityMatchResult> MatchCityAsync(
        CityMatchRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     城市匹配请求
/// </summary>
public class CityMatchRequest
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
    ///     城市名称（本地语言，如中文）
    /// </summary>
    public string? CityName { get; set; }

    /// <summary>
    ///     城市英文名称
    /// </summary>
    public string? CityNameEn { get; set; }

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
}

/// <summary>
///     城市匹配结果
/// </summary>
public class CityMatchResult
{
    /// <summary>
    ///     是否匹配成功
    /// </summary>
    public bool IsMatched { get; set; }

    /// <summary>
    ///     匹配到的城市 ID
    /// </summary>
    public string? CityId { get; set; }

    /// <summary>
    ///     匹配到的城市名称
    /// </summary>
    public string? CityName { get; set; }

    /// <summary>
    ///     匹配到的城市英文名称
    /// </summary>
    public string? CityNameEn { get; set; }

    /// <summary>
    ///     匹配到的国家名称
    /// </summary>
    public string? CountryName { get; set; }

    /// <summary>
    ///     匹配方式
    /// </summary>
    public CityMatchMethod MatchMethod { get; set; }

    /// <summary>
    ///     匹配距离（公里）
    /// </summary>
    public double? DistanceKm { get; set; }

    /// <summary>
    ///     匹配置信度 (0-1)
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    ///     错误信息（如果匹配失败）
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
///     城市匹配方式
/// </summary>
public enum CityMatchMethod
{
    /// <summary>
    ///     未匹配
    /// </summary>
    None = 0,

    /// <summary>
    ///     名称精确匹配
    /// </summary>
    ExactName = 1,

    /// <summary>
    ///     名称 + 坐标组合匹配
    /// </summary>
    NameAndCoordinate = 2,

    /// <summary>
    ///     基于坐标距离匹配
    /// </summary>
    CoordinateOnly = 3,

    /// <summary>
    ///     基于反向地理编码匹配
    /// </summary>
    ReverseGeocode = 4
}
