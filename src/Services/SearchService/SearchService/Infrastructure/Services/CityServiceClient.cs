using SearchService.Domain.Models;

namespace SearchService.Infrastructure.Services;

/// <summary>
/// 城市服务客户端接口
/// </summary>
public interface ICityServiceClient
{
    /// <summary>
    /// 获取所有城市
    /// </summary>
    Task<List<CitySearchDocument>> GetAllCitiesAsync();

    /// <summary>
    /// 获取单个城市
    /// </summary>
    Task<CitySearchDocument?> GetCityByIdAsync(Guid id);
}

/// <summary>
/// 城市服务客户端实现 (通过 HttpClient 调用)
/// </summary>
public class CityServiceClient : ICityServiceClient
{
    private readonly ILogger<CityServiceClient> _logger;
    private readonly HttpClient _httpClient;

    public CityServiceClient(
        ILogger<CityServiceClient> logger,
        HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<List<CitySearchDocument>> GetAllCitiesAsync()
    {
        var result = new List<CitySearchDocument>();

        try
        {
            var page = 1;
            const int pageSize = 100;
            bool hasMore = true;

            _logger.LogInformation("开始获取城市数据...");

            while (hasMore)
            {
                var response = await _httpClient.GetAsync(
                    $"api/v1/cities?pageNumber={page}&pageSize={pageSize}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("获取城市列表失败: {StatusCode}, 响应: {Response}", 
                        response.StatusCode, errorContent);
                    break;
                }

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("收到城市响应: {Content}", content.Length > 500 ? content[..500] + "..." : content);

                var apiResponse = System.Text.Json.JsonSerializer.Deserialize<ApiResponse<CityListResponse>>(content,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var data = apiResponse?.Data;
                if (data?.Items == null || !data.Items.Any())
                {
                    _logger.LogInformation("页码 {Page} 无数据返回", page);
                    hasMore = false;
                    continue;
                }

                foreach (var city in data.Items)
                {
                    result.Add(MapToSearchDocument(city));
                }

                hasMore = data.Items.Count >= pageSize;
                page++;

                _logger.LogDebug("已获取 {Count} 个城市, 页码: {Page}", result.Count, page - 1);
            }

            _logger.LogInformation("共获取 {Count} 个城市", result.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取城市列表时发生异常");
        }

        return result;
    }

    public async Task<CitySearchDocument?> GetCityByIdAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("获取城市 {CityId}...", id);

            var response = await _httpClient.GetAsync($"api/v1/cities/{id}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("获取城市 {Id} 失败: {StatusCode}", id, response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = System.Text.Json.JsonSerializer.Deserialize<ApiResponse<CityDto>>(content,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (apiResponse?.Data == null)
            {
                return null;
            }

            return MapToSearchDocument(apiResponse.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取城市 {Id} 时发生异常", id);
            return null;
        }
    }

    private static CitySearchDocument MapToSearchDocument(CityDto city)
    {
        var doc = new CitySearchDocument
        {
            Id = city.Id,
            Name = city.Name,
            NameEn = city.NameEn,
            Country = city.Country,
            CountryId = city.CountryId,
            ProvinceId = city.ProvinceId,
            Region = city.Region,
            Description = city.Description,
            Latitude = city.Latitude,
            Longitude = city.Longitude,
            Population = city.Population,
            Climate = city.Climate,
            TimeZone = city.TimeZone,
            Currency = city.Currency,
            ImageUrl = city.ImageUrl,
            PortraitImageUrl = city.PortraitImageUrl,
            OverallScore = city.OverallScore,
            InternetQualityScore = city.InternetQualityScore,
            SafetyScore = city.SafetyScore,
            CostScore = city.CostScore,
            CommunityScore = city.CommunityScore,
            WeatherScore = city.WeatherScore,
            Tags = city.Tags ?? new List<string>(),
            IsActive = city.IsActive,
            CreatedAt = city.CreatedAt,
            UpdatedAt = city.UpdatedAt,
            // 扩展字段
            AverageCost = city.AverageCost,
            UserCount = 0, // 暂不实现，需要从 travel_history 统计
            ModeratorId = city.ModeratorId,
            ModeratorName = city.Moderator?.Name,
            ModeratorCount = city.ModeratorId.HasValue ? 1 : 0,
            CoworkingCount = city.CoworkingCount,
            MeetupCount = city.MeetupCount,
            ReviewCount = city.ReviewCount
        };

        // 设置地理位置
        if (city.Latitude.HasValue && city.Longitude.HasValue)
        {
            doc.Location = new GeoLocation
            {
                Lat = city.Latitude.Value,
                Lon = city.Longitude.Value
            };
        }

        // 设置搜索建议文本
        var suggestParts = new List<string> { city.Name };
        if (!string.IsNullOrEmpty(city.NameEn)) suggestParts.Add(city.NameEn);
        if (!string.IsNullOrEmpty(city.Country)) suggestParts.Add(city.Country);
        doc.Suggest = string.Join(" ", suggestParts);

        return doc;
    }

    // DTO类
    private class CityListResponse
    {
        public List<CityDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
    }

    private class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
    }

    private class CityDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? NameEn { get; set; }
        public string Country { get; set; } = string.Empty;
        public Guid? CountryId { get; set; }
        public Guid? ProvinceId { get; set; }
        public string? Region { get; set; }
        public string? Description { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public int? Population { get; set; }
        public string? Climate { get; set; }
        public string? TimeZone { get; set; }
        public string? Currency { get; set; }
        public string? ImageUrl { get; set; }
        public string? PortraitImageUrl { get; set; }
        public decimal? OverallScore { get; set; }
        public decimal? InternetQualityScore { get; set; }
        public decimal? SafetyScore { get; set; }
        public decimal? CostScore { get; set; }
        public decimal? CommunityScore { get; set; }
        public decimal? WeatherScore { get; set; }
        public List<string>? Tags { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // 扩展字段 - 与 CityService.CityDto 对应
        public decimal? AverageCost { get; set; }
        public Guid? ModeratorId { get; set; }
        public ModeratorDto? Moderator { get; set; }
        public int MeetupCount { get; set; }
        public int CoworkingCount { get; set; }
        public int ReviewCount { get; set; }
    }

    private class ModeratorDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Avatar { get; set; }
    }
}
