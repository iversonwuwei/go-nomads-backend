using Dapr.Client;

namespace AIService.Infrastructure.GrpcClients;

/// <summary>
///     城市服务 gRPC 客户端实现 (通过 Dapr Service Invocation)
/// </summary>
public class CityGrpcClient : ICityGrpcClient
{
    private const string CityServiceName = "city-service";
    private readonly DaprClient _daprClient;
    private readonly ILogger<CityGrpcClient> _logger;

    public CityGrpcClient(DaprClient daprClient, ILogger<CityGrpcClient> logger)
    {
        _daprClient = daprClient;
        _logger = logger;
    }

    public async Task<string?> GetCityImageAsync(Guid cityId)
    {
        try
        {
            _logger.LogInformation("获取城市图片，城市ID: {CityId}", cityId);

            var cityInfo = await GetCityInfoAsync(cityId);
            if (cityInfo == null)
            {
                _logger.LogWarning("⚠️ 城市不存在，无法获取图片，城市ID: {CityId}", cityId);
                return null;
            }

            var imageUrl = cityInfo.GetBestImageUrl();
            _logger.LogInformation("✅ 成功获取城市图片，城市ID: {CityId}, ImageUrl: {ImageUrl}", cityId, imageUrl);
            return imageUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取城市图片失败，城市ID: {CityId}", cityId);
            return null;
        }
    }

    public async Task<CityInfo?> GetCityInfoAsync(Guid cityId)
    {
        try
        {
            _logger.LogInformation("获取城市信息，城市ID: {CityId}", cityId);

            var response = await _daprClient.InvokeMethodAsync<ApiResponse<CityDto>>(
                HttpMethod.Get,
                CityServiceName,
                $"api/v1/cities/{cityId}"
            );

            if (response?.Success == true && response.Data != null)
            {
                var cityInfo = new CityInfo
                {
                    Id = response.Data.Id,
                    Name = response.Data.Name,
                    NameEn = response.Data.NameEn,
                    Country = response.Data.Country,
                    ImageUrl = response.Data.ImageUrl,
                    PortraitImageUrl = response.Data.PortraitImageUrl,
                    LandscapeImageUrls = response.Data.LandscapeImageUrls
                };

                _logger.LogInformation("✅ 成功获取城市信息，城市ID: {CityId}, Name: {Name}", cityId, cityInfo.Name);
                return cityInfo;
            }

            _logger.LogWarning("⚠️ 城市不存在或获取失败，城市ID: {CityId}", cityId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取城市信息失败，城市ID: {CityId}", cityId);
            return null;
        }
    }

    // 内部 DTO 类
    private class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    private class CityDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? NameEn { get; set; }
        public string Country { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? PortraitImageUrl { get; set; }
        public List<string>? LandscapeImageUrls { get; set; }
    }
}
