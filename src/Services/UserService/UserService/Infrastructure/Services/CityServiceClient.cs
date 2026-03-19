using System.Text.Json;
using GoNomads.Shared.Communication;

namespace UserService.Infrastructure.Services;

/// <summary>
///     城市服务客户端 - 用于调用 CityService API
/// </summary>
public interface ICityServiceClient
{
    /// <summary>
    ///     匹配城市 - 根据经纬度和城市名称匹配现有城市
    /// </summary>
    Task<CityMatchResult?> MatchCityAsync(
        CityMatchRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取城市详情（包含评分、花费等）
    /// </summary>
    Task<CityDetailDto?> GetCityDetailAsync(
        string cityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取城市当前天气
    /// </summary>
    Task<CityWeatherInfo?> GetCityWeatherAsync(
        string cityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取城市共享办公数量
    /// </summary>
    Task<int> GetCoworkingCountAsync(
        string cityId,
        CancellationToken cancellationToken = default);
}

/// <summary>
///     城市匹配请求
/// </summary>
public class CityMatchRequest
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? CityName { get; set; }
    public string? CityNameEn { get; set; }
    public string? CountryName { get; set; }
    public string? CountryCode { get; set; }
    public string? ProvinceName { get; set; }
}

/// <summary>
///     城市匹配结果
/// </summary>
public class CityMatchResult
{
    public bool IsMatched { get; set; }
    public string? CityId { get; set; }
    public string? CityName { get; set; }
    public string? CityNameEn { get; set; }
    public string? CountryName { get; set; }
    public string? MatchMethod { get; set; }
    public double? DistanceKm { get; set; }
    public double Confidence { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
///     城市详情 DTO（从 CityService 获取）
/// </summary>
public class CityDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string Country { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal? OverallScore { get; set; }
    public decimal? AverageCostOfLiving { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

/// <summary>
///     城市天气信息（匹配 CityService 返回的 WeatherDto）
/// </summary>
public class CityWeatherInfo
{
    public double Temperature { get; set; }
    public double? FeelsLike { get; set; }
    public double? TempMin { get; set; }
    public double? TempMax { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    
    /// <summary>
    /// 天气状况（如 "Clouds", "Clear" 等）
    /// </summary>
    public string Weather { get; set; } = string.Empty;
    public string? WeatherDescription { get; set; }
    public string? WeatherIcon { get; set; }
    
    public int? Humidity { get; set; }
    public double? WindSpeed { get; set; }
    public int? WindDirection { get; set; }
    public string? WindDirectionDescription { get; set; }
    public double? WindGust { get; set; }
    public int? Pressure { get; set; }
    public int? SeaLevelPressure { get; set; }
    public int? GroundLevelPressure { get; set; }
    public int? Visibility { get; set; }
    public int? Cloudiness { get; set; }
    public double? Rain1h { get; set; }
    public double? Rain3h { get; set; }
    public double? Snow1h { get; set; }
    public double? Snow3h { get; set; }
    public DateTime? Sunrise { get; set; }
    public DateTime? Sunset { get; set; }
    public int? TimezoneOffset { get; set; }
    public double? UvIndex { get; set; }
    public int? AirQualityIndex { get; set; }
    public string? DataSource { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? Timestamp { get; set; }
    public object? Forecast { get; set; }
    
    /// <summary>
    /// 获取天气状况描述（用于显示）
    /// </summary>
    public string Condition => Weather;
    
    /// <summary>
    /// 获取天气图标代码
    /// </summary>
    public string Icon => WeatherIcon ?? string.Empty;
}

/// <summary>
///     城市服务客户端实现
/// </summary>
public class CityServiceClient : ICityServiceClient
{
    private readonly ILogger<CityServiceClient> _logger;
    private readonly ServiceInvocationClient _serviceInvocationClient;
    private const string CityServiceAppId = "city-service";

    public CityServiceClient(ServiceInvocationClient serviceInvocationClient, ILogger<CityServiceClient> logger)
    {
        _serviceInvocationClient = serviceInvocationClient;
        _logger = logger;
    }

    public async Task<CityMatchResult?> MatchCityAsync(
        CityMatchRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "🔍 调用城市匹配 API: Lat={Lat}, Lng={Lng}, CityName={CityName}",
                request.Latitude, request.Longitude, request.CityName);

            var response = await _serviceInvocationClient.InvokeAsync<CityMatchRequest, ApiResponse<CityMatchResult>>(
                HttpMethod.Post,
                CityServiceAppId,
                "api/v1/cities/match",
                request,
                cancellationToken);

            if (response?.Success == true && response.Data != null)
            {
                _logger.LogInformation(
                    "✅ 城市匹配成功: CityId={CityId}, Method={Method}, Confidence={Confidence}",
                    response.Data.CityId, response.Data.MatchMethod, response.Data.Confidence);
                return response.Data;
            }

            _logger.LogWarning("⚠️ 城市匹配失败: {Message}", response?.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 调用城市匹配 API 异常");
            return null;
        }
    }

    public async Task<CityDetailDto?> GetCityDetailAsync(
        string cityId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("🏙️ 获取城市详情: CityId={CityId}", cityId);

            var response = await _serviceInvocationClient.InvokeAsync<ApiResponse<CityDetailDto>>(
                HttpMethod.Get,
                CityServiceAppId,
                $"api/v1/cities/{cityId}",
                cancellationToken);

            if (response?.Success == true && response.Data != null)
            {
                _logger.LogInformation("✅ 获取城市详情成功: {CityName}", response.Data.Name);
                return response.Data;
            }

            _logger.LogWarning("⚠️ 获取城市详情失败: {Message}", response?.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取城市详情异常: CityId={CityId}", cityId);
            return null;
        }
    }

    public async Task<CityWeatherInfo?> GetCityWeatherAsync(
        string cityId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("🌤️ 获取城市天气: CityId={CityId}", cityId);

            var response = await _serviceInvocationClient.InvokeAsync<ApiResponse<CityWeatherInfo>>(
                HttpMethod.Get,
                CityServiceAppId,
                $"api/v1/cities/{cityId}/weather",
                cancellationToken);

            if (response?.Success == true && response.Data != null)
            {
                _logger.LogInformation("✅ 获取天气成功: {Temp}°C, {Condition}",
                    response.Data.Temperature, response.Data.Condition);
                return response.Data;
            }

            _logger.LogWarning("⚠️ 获取城市天气失败: {Message}", response?.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取城市天气异常: CityId={CityId}", cityId);
            return null;
        }
    }

    public async Task<int> GetCoworkingCountAsync(
        string cityId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("🏢 获取共享办公数量: CityId={CityId}", cityId);

            var response = await _serviceInvocationClient.InvokeAsync<ApiResponse<CoworkingCountResponse>>(
                HttpMethod.Get,
                CityServiceAppId,
                $"api/v1/cities/{cityId}/coworking-count",
                cancellationToken);

            if (response?.Success == true && response.Data != null)
            {
                _logger.LogInformation("✅ 获取共享办公数量成功: {Count}", response.Data.Count);
                return response.Data.Count;
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取共享办公数量异常: CityId={CityId}", cityId);
            return 0;
        }
    }

    /// <summary>
    ///     API 响应包装类
    /// </summary>
    private class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
    }

    private class CoworkingCountResponse
    {
        public int Count { get; set; }
    }
}
