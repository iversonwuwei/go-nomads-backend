using System.Net.Http.Json;
using System.Text.Json;
using CityService.Application.DTOs;
using Dapr.Client;

namespace CityService.Infrastructure.Clients;

/// <summary>
///     AIService 客户端接口
/// </summary>
public interface IAIServiceClient
{
    /// <summary>
    ///     调用 AIService 生成城市图片
    /// </summary>
    /// <param name="cityId">城市ID</param>
    /// <param name="cityName">城市名称</param>
    /// <param name="country">国家</param>
    /// <param name="style">图片风格</param>
    /// <param name="bucket">存储桶名称</param>
    /// <returns>生成的图片响应</returns>
    Task<GenerateCityImagesResponse?> GenerateCityImagesAsync(
        string cityId,
        string cityName,
        string? country,
        string style = "<photography>",
        string bucket = "city-photos");
}

/// <summary>
///     AIService 客户端实现 (支持直接 HTTP 调用和 Dapr Service Invocation)
/// </summary>
public class AIServiceClient : IAIServiceClient
{
    private readonly DaprClient _daprClient;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AIServiceClient> _logger;
    private readonly IConfiguration _configuration;
    private readonly bool _useDapr;
    private readonly string _aiServiceAppId;

    public AIServiceClient(
        DaprClient daprClient, 
        HttpClient httpClient,
        ILogger<AIServiceClient> logger,
        IConfiguration configuration)
    {
        _daprClient = daprClient;
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
        
        // 从配置读取 AIService app-id，默认为 "ai-service"
        _aiServiceAppId = configuration["AIService:AppId"] ?? "ai-service";
        
        // 检查是否使用 Dapr（通过环境变量或配置）
        _useDapr = Environment.GetEnvironmentVariable("USE_DAPR")?.ToLower() == "true" 
                   || configuration.GetValue<bool>("Dapr:Enabled", false);
        
        // 设置 HttpClient 超时时间为 10 分钟（AI 图片生成需要较长时间）
        _httpClient.Timeout = TimeSpan.FromMinutes(10);
        
        _logger.LogInformation("AIServiceClient 初始化: UseDapr={UseDapr}, AppId={AppId}", _useDapr, _aiServiceAppId);
    }

    public async Task<GenerateCityImagesResponse?> GenerateCityImagesAsync(
        string cityId,
        string cityName,
        string? country,
        string style = "<photography>",
        string bucket = "city-photos")
    {
        try
        {
            _logger.LogInformation(
                "🖼️ 开始调用 AIService 生成城市图片: CityId={CityId}, CityName={CityName}, Country={Country}, UseDapr={UseDapr}",
                cityId, cityName, country, _useDapr);

            var request = new
            {
                cityId,
                cityName,
                country,
                style,
                bucket,
                negativePrompt = "blurry, low quality, distorted, watermark, text, logo, ugly, deformed, cartoon, anime"
            };

            ApiResponseWrapper<GenerateCityImagesResponse>? response;

            // 设置 10 分钟超时（AI 图片生成需要较长时间）
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));

            if (_useDapr)
            {
                // 通过 Dapr Service Invocation 调用 AIService
                response = await _daprClient.InvokeMethodAsync<object, ApiResponseWrapper<GenerateCityImagesResponse>>(
                    HttpMethod.Post,
                    _aiServiceAppId,
                    "api/v1/ai/images/city",
                    request,
                    cts.Token);
            }
            else
            {
                // 直接通过 HTTP 调用 AIService（本地开发模式）
                var aiServiceBaseUrl = _configuration.GetValue<string>("AIService:BaseUrl") ?? "http://localhost:8009";
                var httpResponse = await _httpClient.PostAsJsonAsync(
                    $"{aiServiceBaseUrl}/api/v1/ai/images/city",
                    request,
                    cts.Token);

                httpResponse.EnsureSuccessStatusCode();
                response = await httpResponse.Content.ReadFromJsonAsync<ApiResponseWrapper<GenerateCityImagesResponse>>(
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }

            if (response?.Success == true && response.Data != null)
            {
                _logger.LogInformation(
                    "✅ AIService 图片生成成功: CityId={CityId}, 竖屏={HasPortrait}, 横屏数量={LandscapeCount}",
                    cityId,
                    response.Data.PortraitImage != null,
                    response.Data.LandscapeImages?.Count ?? 0);

                return response.Data;
            }

            _logger.LogWarning("⚠️ AIService 图片生成响应为空或失败: CityId={CityId}, Message={Message}", 
                cityId, response?.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 调用 AIService 生成城市图片失败: CityId={CityId}", cityId);
            throw;
        }
    }
}

/// <summary>
///     API 响应包装器（匹配 AIService 的响应格式）
/// </summary>
public class ApiResponseWrapper<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
}
