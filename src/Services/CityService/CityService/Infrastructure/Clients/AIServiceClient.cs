using CityService.Application.DTOs;
using Dapr.Client;

namespace CityService.Infrastructure.Clients;

/// <summary>
///     AIService 客户端接口
/// </summary>
public interface IAIServiceClient
{
    /// <summary>
    ///     异步调用 AIService 生成城市图片（立即返回任务ID，不等待结果）
    /// </summary>
    /// <param name="cityId">城市ID</param>
    /// <param name="cityName">城市名称</param>
    /// <param name="country">国家</param>
    /// <param name="userId">用户ID（用于推送通知）</param>
    /// <param name="style">图片风格</param>
    /// <param name="bucket">存储桶名称</param>
    /// <returns>任务创建响应，包含任务ID</returns>
    Task<GenerateCityImagesTaskResponse?> GenerateCityImagesAsyncTask(
        string cityId,
        string cityName,
        string? country,
        string userId,
        string style = "<photography>",
        string bucket = "city-photos");

    /// <summary>
    ///     同步调用 AIService 生成城市图片（等待结果返回，可能超时）
    /// </summary>
    [Obsolete("请使用 GenerateCityImagesAsyncTask 方法，通过 SignalR 接收结果")]
    Task<GenerateCityImagesResponse?> GenerateCityImagesAsync(
        string cityId,
        string cityName,
        string? country,
        string style = "<photography>",
        string bucket = "city-photos");
}

/// <summary>
///     异步任务创建响应
/// </summary>
public class GenerateCityImagesTaskResponse
{
    public bool Success { get; set; }
    public string? TaskId { get; set; }
    public string? Status { get; set; }
    public int EstimatedTimeSeconds { get; set; }
    public string? Message { get; set; }
}

/// <summary>
///     AIService 客户端实现
///     使用 Dapr Service Invocation 调用 AIService
/// </summary>
public class AIServiceClient : IAIServiceClient
{
    private readonly DaprClient _daprClient;
    private readonly ILogger<AIServiceClient> _logger;
    private readonly string _aiServiceAppId;

    public AIServiceClient(
        DaprClient daprClient,
        ILogger<AIServiceClient> logger,
        IConfiguration configuration)
    {
        _daprClient = daprClient;
        _logger = logger;

        // 从配置读取 AIService app-id，默认为 "ai-service"
        _aiServiceAppId = configuration["AIService:AppId"] ?? "ai-service";

        _logger.LogInformation("AIServiceClient 初始化: AppId={AppId}", _aiServiceAppId);
    }

    /// <summary>
    ///     异步调用 AIService 生成城市图片（推荐使用）
    ///     立即返回任务ID，生成完成后通过 SignalR 通知
    /// </summary>
    public async Task<GenerateCityImagesTaskResponse?> GenerateCityImagesAsyncTask(
        string cityId,
        string cityName,
        string? country,
        string userId,
        string style = "<photography>",
        string bucket = "city-photos")
    {
        _logger.LogInformation(
            "🖼️ 通过 Dapr 调用 AIService 异步生成城市图片: CityId={CityId}, CityName={CityName}, Country={Country}, UserId={UserId}, AppId={AppId}",
            cityId, cityName, country, userId, _aiServiceAppId);

        var request = new
        {
            cityId,
            cityName,
            country,
            userId,  // 传递用户ID
            style,
            bucket,
            negativePrompt = "blurry, low quality, distorted, watermark, text, logo, ugly, deformed, cartoon, anime"
        };

        try
        {
            // 使用 Dapr Service Invocation 调用 AIService
            var response = await _daprClient.InvokeMethodAsync<object, ApiResponseWrapper<CreateTaskResponseData>>(
                HttpMethod.Post,
                _aiServiceAppId,
                "api/v1/ai/images/city/async",
                request);

            if (response?.Success == true && response.Data != null)
            {
                _logger.LogInformation(
                    "✅ AIService 图片生成任务已创建: CityId={CityId}, TaskId={TaskId}",
                    cityId, response.Data.TaskId);

                return new GenerateCityImagesTaskResponse
                {
                    Success = true,
                    TaskId = response.Data.TaskId,
                    Status = response.Data.Status,
                    EstimatedTimeSeconds = response.Data.EstimatedTimeSeconds,
                    Message = response.Data.Message
                };
            }

            _logger.LogWarning("⚠️ AIService 创建任务响应为空或失败: CityId={CityId}, Message={Message}",
                cityId, response?.Message);
            return new GenerateCityImagesTaskResponse
            {
                Success = false,
                Message = response?.Message ?? "创建任务失败"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Dapr 调用 AIService 创建图片生成任务失败: CityId={CityId}", cityId);
            return new GenerateCityImagesTaskResponse
            {
                Success = false,
                Message = $"创建任务失败: {ex.Message}"
            };
        }
    }

    [Obsolete("请使用 GenerateCityImagesAsyncTask 方法")]
    public async Task<GenerateCityImagesResponse?> GenerateCityImagesAsync(
        string cityId,
        string cityName,
        string? country,
        string style = "<photography>",
        string bucket = "city-photos")
    {
        _logger.LogInformation(
            "🖼️ 通过 Dapr 调用 AIService 生成城市图片: CityId={CityId}, CityName={CityName}, Country={Country}",
            cityId, cityName, country);

        var request = new
        {
            cityId,
            cityName,
            country,
            style,
            bucket,
            negativePrompt = "blurry, low quality, distorted, watermark, text, logo, ugly, deformed, cartoon, anime"
        };

        try
        {
            // 使用 Dapr Service Invocation 调用 AIService
            var response = await _daprClient.InvokeMethodAsync<object, ApiResponseWrapper<GenerateCityImagesResponse>>(
                HttpMethod.Post,
                _aiServiceAppId,
                "api/v1/ai/images/city",
                request);

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
            _logger.LogError(ex, "❌ Dapr 调用 AIService 生成城市图片失败: CityId={CityId}", cityId);
            throw;
        }
    }
}

/// <summary>
///     异步任务创建响应数据
/// </summary>
public class CreateTaskResponseData
{
    public string? TaskId { get; set; }
    public string? Status { get; set; }
    public int EstimatedTimeSeconds { get; set; }
    public string? Message { get; set; }
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
