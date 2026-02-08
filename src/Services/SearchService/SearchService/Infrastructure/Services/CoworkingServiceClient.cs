using SearchService.Domain.Models;

namespace SearchService.Infrastructure.Services;

/// <summary>
/// 共享办公空间服务客户端接口
/// </summary>
public interface ICoworkingServiceClient
{
    /// <summary>
    /// 获取所有共享办公空间
    /// </summary>
    Task<List<CoworkingSearchDocument>> GetAllCoworkingsAsync();

    /// <summary>
    /// 获取单个共享办公空间
    /// </summary>
    Task<CoworkingSearchDocument?> GetCoworkingByIdAsync(Guid id);
}

/// <summary>
/// 共享办公空间服务客户端实现 (通过 HttpClient 调用)
/// </summary>
public class CoworkingServiceClient : ICoworkingServiceClient
{
    private readonly ILogger<CoworkingServiceClient> _logger;
    private readonly HttpClient _httpClient;

    public CoworkingServiceClient(
        ILogger<CoworkingServiceClient> logger,
        HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<List<CoworkingSearchDocument>> GetAllCoworkingsAsync()
    {
        var result = new List<CoworkingSearchDocument>();

        try
        {
            var page = 1;
            const int pageSize = 100;
            bool hasMore = true;

            _logger.LogInformation("开始获取共享办公空间数据...");

            while (hasMore)
            {
                var response = await _httpClient.GetAsync(
                    $"coworkings?page={page}&pageSize={pageSize}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("获取共享办公空间列表失败: {StatusCode}, 响应: {Response}", 
                        response.StatusCode, errorContent);
                    break;
                }

                var content = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("收到共享办公空间响应: {Content}", content.Length > 500 ? content[..500] + "..." : content);

                var data = System.Text.Json.JsonSerializer.Deserialize<CoworkingListResponse>(content,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (data?.Items == null || !data.Items.Any())
                {
                    _logger.LogInformation("页码 {Page} 无数据返回", page);
                    hasMore = false;
                    continue;
                }

                foreach (var coworking in data.Items)
                {
                    result.Add(MapToSearchDocument(coworking));
                }

                hasMore = data.Items.Count >= pageSize;
                page++;

                _logger.LogDebug("已获取 {Count} 个共享办公空间, 页码: {Page}", result.Count, page - 1);
            }

            _logger.LogInformation("共获取 {Count} 个共享办公空间", result.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取共享办公空间列表时发生异常");
        }

        return result;
    }

    public async Task<CoworkingSearchDocument?> GetCoworkingByIdAsync(Guid id)
    {
        try
        {
            _logger.LogInformation("获取共享办公空间 {CoworkingId}...", id);

            var response = await _httpClient.GetAsync($"coworkings/{id}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("获取共享办公空间 {Id} 失败: {StatusCode}", id, response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = System.Text.Json.JsonSerializer.Deserialize<ApiResponse<CoworkingDto>>(content,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (apiResponse?.Data == null)
            {
                return null;
            }

            return MapToSearchDocument(apiResponse.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取共享办公空间 {Id} 时发生异常", id);
            return null;
        }
    }

    private static CoworkingSearchDocument MapToSearchDocument(CoworkingDto coworking)
    {
        var doc = new CoworkingSearchDocument
        {
            Id = coworking.Id,
            Name = coworking.Name,
            CityId = coworking.CityId,
            CityName = coworking.CityName,
            CountryName = coworking.CountryName,
            Address = coworking.Address,
            Description = coworking.Description,
            ImageUrl = coworking.ImageUrl,
            PricePerDay = coworking.PricePerDay,
            PricePerMonth = coworking.PricePerMonth,
            PricePerHour = coworking.PricePerHour,
            Currency = coworking.Currency,
            Rating = coworking.Rating,
            ReviewCount = coworking.ReviewCount,
            WifiSpeed = coworking.WifiSpeed,
            Desks = coworking.Desks,
            MeetingRooms = coworking.MeetingRooms,
            HasMeetingRoom = coworking.HasMeetingRoom,
            HasCoffee = coworking.HasCoffee,
            HasParking = coworking.HasParking,
            Has247Access = coworking.Has247Access,
            Amenities = coworking.Amenities,
            Capacity = coworking.Capacity,
            Latitude = coworking.Latitude,
            Longitude = coworking.Longitude,
            Phone = coworking.Phone,
            Email = coworking.Email,
            Website = coworking.Website,
            OpeningHours = coworking.OpeningHours,
            IsActive = coworking.IsActive,
            VerificationStatus = coworking.VerificationStatus,
            CreatedAt = coworking.CreatedAt,
            UpdatedAt = coworking.UpdatedAt
        };

        // 设置地理位置
        if (coworking.Latitude.HasValue && coworking.Longitude.HasValue)
        {
            doc.Location = new GeoLocation
            {
                Lat = (double)coworking.Latitude.Value,
                Lon = (double)coworking.Longitude.Value
            };
        }

        // 设置搜索建议文本
        var suggestParts = new List<string> { coworking.Name };
        if (!string.IsNullOrEmpty(coworking.CityName)) suggestParts.Add(coworking.CityName);
        if (!string.IsNullOrEmpty(coworking.Address)) suggestParts.Add(coworking.Address);
        doc.Suggest = string.Join(" ", suggestParts);

        return doc;
    }

    // DTO类
    private class CoworkingListResponse
    {
        public List<CoworkingDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
    }

    private class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
    }

    private class CoworkingDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid? CityId { get; set; }
        public string? CityName { get; set; }
        public string? CountryName { get; set; }
        public string Address { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public decimal? PricePerDay { get; set; }
        public decimal? PricePerMonth { get; set; }
        public decimal? PricePerHour { get; set; }
        public string Currency { get; set; } = "USD";
        public decimal Rating { get; set; }
        public int ReviewCount { get; set; }
        public decimal? WifiSpeed { get; set; }
        public int? Desks { get; set; }
        public int? MeetingRooms { get; set; }
        public bool HasMeetingRoom { get; set; }
        public bool HasCoffee { get; set; }
        public bool HasParking { get; set; }
        public bool Has247Access { get; set; }
        public string[]? Amenities { get; set; }
        public int? Capacity { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? OpeningHours { get; set; }
        public bool IsActive { get; set; }
        public string VerificationStatus { get; set; } = "unverified";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
