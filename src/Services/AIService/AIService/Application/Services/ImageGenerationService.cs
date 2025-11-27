using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AIService.Application.DTOs;
using Client = Supabase.Client;

namespace AIService.Application.Services;

/// <summary>
///     图片生成服务实现 (通义万象 + Supabase Storage)
/// </summary>
public class ImageGenerationService : IImageGenerationService
{
    private const string WanxApiBaseUrl = "https://dashscope.aliyuncs.com/api/v1";
    private const string CreateTaskEndpoint = "/services/aigc/text2image/image-synthesis";
    private const string QueryTaskEndpoint = "/tasks";
    private const int MaxPollingAttempts = 60; // 最多轮询60次
    private const int PollingIntervalMs = 2000; // 每2秒轮询一次

    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ImageGenerationService> _logger;
    private readonly Client _supabaseClient;

    public ImageGenerationService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        Client supabaseClient,
        ILogger<ImageGenerationService> logger)
    {
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient("WanxClient");
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<GenerateImageResponse> GenerateImageAsync(GenerateImageRequest request, Guid userId)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = new GenerateImageResponse();

        try
        {
            _logger.LogInformation("开始生成图片，用户: {UserId}, 提示词: {Prompt}", userId, request.Prompt);

            // 1. 创建通义万象任务
            var taskId = await CreateWanxTaskAsync(request);
            response.TaskId = taskId;

            _logger.LogInformation("通义万象任务已创建，TaskId: {TaskId}", taskId);

            // 2. 轮询任务状态直到完成
            var taskResult = await PollTaskUntilCompleteAsync(taskId);

            if (taskResult.Status != "SUCCEEDED" || taskResult.ImageUrls.Count == 0)
            {
                response.Success = false;
                response.ErrorMessage = taskResult.ErrorMessage ?? "图片生成失败";
                _logger.LogWarning("图片生成失败，TaskId: {TaskId}, 状态: {Status}, 错误: {Error}",
                    taskId, taskResult.Status, taskResult.ErrorMessage);
                return response;
            }

            _logger.LogInformation("通义万象图片生成成功，共 {Count} 张", taskResult.ImageUrls.Count);

            // 3. 下载图片并上传到 Supabase Storage
            var uploadedImages = await DownloadAndUploadImagesAsync(
                taskResult.ImageUrls,
                request.Bucket,
                request.PathPrefix,
                userId);

            response.Images = uploadedImages;
            response.Success = true;
            stopwatch.Stop();
            response.GenerationTimeMs = (int)stopwatch.ElapsedMilliseconds;

            _logger.LogInformation("图片生成并上传完成，耗时: {Time}ms，共上传 {Count} 张",
                response.GenerationTimeMs, uploadedImages.Count);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            response.GenerationTimeMs = (int)stopwatch.ElapsedMilliseconds;
            response.Success = false;
            response.ErrorMessage = ex.Message;

            _logger.LogError(ex, "图片生成过程发生错误，用户: {UserId}", userId);
            return response;
        }
    }

    /// <inheritdoc />
    public async Task<GenerateCityImagesResponse> GenerateCityImagesAsync(GenerateCityImagesRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = new GenerateCityImagesResponse
        {
            CityId = request.CityId
        };

        try
        {
            _logger.LogInformation("开始批量生成城市图片，城市: {CityName} ({CityId})", request.CityName, request.CityId);

            // 生成默认提示词
            var cityDesc = string.IsNullOrEmpty(request.Country)
                ? request.CityName
                : $"{request.CityName}, {request.Country}";

            var portraitPrompt = request.PortraitPrompt
                ?? $"A stunning vertical cityscape of {cityDesc}, showing iconic landmarks and skyline, professional photography, high quality, vibrant colors, golden hour lighting";

            var landscapePrompt = request.LandscapePrompt
                ?? $"Beautiful panoramic view of {cityDesc}, featuring famous landmarks, streets, and local culture, professional travel photography, high resolution, vivid colors";

            var negativePrompt = request.NegativePrompt
                ?? "blurry, low quality, distorted, watermark, text, logo, ugly, deformed";

            // 1. 生成竖屏封面图 (720*1280)
            _logger.LogInformation("生成竖屏封面图...");
            var portraitRequest = new GenerateImageRequest
            {
                Prompt = portraitPrompt,
                NegativePrompt = negativePrompt,
                Style = request.Style,
                Size = "720*1280",
                Count = 1,
                Bucket = request.Bucket,
                PathPrefix = $"portrait/{request.CityId}"
            };

            var portraitResult = await GenerateImageAsync(portraitRequest, Guid.Empty);
            if (portraitResult.Success && portraitResult.Images.Count > 0)
            {
                response.PortraitImage = portraitResult.Images[0];
                _logger.LogInformation("竖屏封面图生成成功: {Url}", response.PortraitImage.Url);
            }
            else
            {
                _logger.LogWarning("竖屏封面图生成失败: {Error}", portraitResult.ErrorMessage);
            }

            // 2. 生成横屏图片 (1280*720) - 分两批，每批最多4张
            _logger.LogInformation("生成横屏图片...");
            var landscapeRequest = new GenerateImageRequest
            {
                Prompt = landscapePrompt,
                NegativePrompt = negativePrompt,
                Style = request.Style,
                Size = "1280*720",
                Count = 4,
                Bucket = request.Bucket,
                PathPrefix = $"landscape/{request.CityId}"
            };

            var landscapeResult = await GenerateImageAsync(landscapeRequest, Guid.Empty);
            if (landscapeResult.Success && landscapeResult.Images.Count > 0)
            {
                response.LandscapeImages = landscapeResult.Images;
                _logger.LogInformation("横屏图片生成成功，共 {Count} 张", response.LandscapeImages.Count);
            }
            else
            {
                _logger.LogWarning("横屏图片生成失败: {Error}", landscapeResult.ErrorMessage);
            }

            stopwatch.Stop();
            response.GenerationTimeMs = (int)stopwatch.ElapsedMilliseconds;
            response.Success = response.PortraitImage != null || response.LandscapeImages.Count > 0;

            if (!response.Success)
            {
                response.ErrorMessage = "所有图片生成均失败";
            }

            _logger.LogInformation("城市图片批量生成完成，耗时: {Time}ms，竖屏: {Portrait}张，横屏: {Landscape}张",
                response.GenerationTimeMs,
                response.PortraitImage != null ? 1 : 0,
                response.LandscapeImages.Count);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            response.GenerationTimeMs = (int)stopwatch.ElapsedMilliseconds;
            response.Success = false;
            response.ErrorMessage = ex.Message;

            _logger.LogError(ex, "城市图片批量生成发生错误，城市: {CityId}", request.CityId);
            return response;
        }
    }

    /// <inheritdoc />
    public async Task<ImageTaskStatusResponse> GetTaskStatusAsync(string taskId)
    {
        return await QueryWanxTaskStatusAsync(taskId);
    }

    /// <summary>
    ///     创建通义万象图片生成任务
    /// </summary>
    private async Task<string> CreateWanxTaskAsync(GenerateImageRequest request)
    {
        var apiKey = _configuration["Qwen:ApiKey"]
                     ?? throw new InvalidOperationException("未配置 Qwen:ApiKey");

        var requestBody = new WanxCreateTaskRequest
        {
            Model = "wanx-v1",
            Input = new WanxInput
            {
                Prompt = request.Prompt,
                NegativePrompt = request.NegativePrompt
            },
            Parameters = new WanxParameters
            {
                Style = request.Style,
                Size = request.Size,
                N = request.Count
            }
        };

        var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{WanxApiBaseUrl}{CreateTaskEndpoint}");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpRequest.Headers.Add("X-DashScope-Async", "enable");
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogDebug("发送通义万象创建任务请求: {Json}", json);

        var httpResponse = await _httpClient.SendAsync(httpRequest);
        var responseContent = await httpResponse.Content.ReadAsStringAsync();

        _logger.LogDebug("通义万象创建任务响应: {Response}", responseContent);

        if (!httpResponse.IsSuccessStatusCode)
            throw new HttpRequestException($"通义万象API调用失败: {httpResponse.StatusCode} - {responseContent}");

        var result = JsonSerializer.Deserialize<WanxCreateTaskResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        if (result?.Output?.TaskId == null)
            throw new InvalidOperationException($"通义万象返回无效响应: {responseContent}");

        return result.Output.TaskId;
    }

    /// <summary>
    ///     轮询任务状态直到完成
    /// </summary>
    private async Task<ImageTaskStatusResponse> PollTaskUntilCompleteAsync(string taskId)
    {
        for (var i = 0; i < MaxPollingAttempts; i++)
        {
            var status = await QueryWanxTaskStatusAsync(taskId);

            _logger.LogDebug("任务 {TaskId} 状态: {Status} (第 {Attempt} 次查询)",
                taskId, status.Status, i + 1);

            if (status.Status is "SUCCEEDED" or "FAILED" or "CANCELED")
                return status;

            await Task.Delay(PollingIntervalMs);
        }

        return new ImageTaskStatusResponse
        {
            TaskId = taskId,
            Status = "TIMEOUT",
            ErrorMessage = "任务轮询超时"
        };
    }

    /// <summary>
    ///     查询通义万象任务状态
    /// </summary>
    private async Task<ImageTaskStatusResponse> QueryWanxTaskStatusAsync(string taskId)
    {
        var apiKey = _configuration["Qwen:ApiKey"]
                     ?? throw new InvalidOperationException("未配置 Qwen:ApiKey");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"{WanxApiBaseUrl}{QueryTaskEndpoint}/{taskId}");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var httpResponse = await _httpClient.SendAsync(httpRequest);
        var responseContent = await httpResponse.Content.ReadAsStringAsync();

        _logger.LogDebug("通义万象任务状态响应: {Response}", responseContent);

        if (!httpResponse.IsSuccessStatusCode)
            return new ImageTaskStatusResponse
            {
                TaskId = taskId,
                Status = "ERROR",
                ErrorMessage = $"查询任务状态失败: {httpResponse.StatusCode}"
            };

        var result = JsonSerializer.Deserialize<WanxQueryTaskResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });

        var response = new ImageTaskStatusResponse
        {
            TaskId = taskId,
            Status = result?.Output?.TaskStatus ?? "UNKNOWN"
        };

        if (result?.Output?.TaskStatus == "SUCCEEDED" && result.Output.Results != null)
        {
            response.ImageUrls = result.Output.Results
                .Where(r => !string.IsNullOrEmpty(r.Url))
                .Select(r => r.Url!)
                .ToList();
            response.SucceededCount = result.Output.TaskMetrics?.Succeeded ?? response.ImageUrls.Count;
            response.FailedCount = result.Output.TaskMetrics?.Failed ?? 0;
        }

        if (result?.Output?.TaskStatus == "FAILED")
        {
            response.ErrorMessage = result.Output.Message ?? "任务执行失败";
        }

        return response;
    }

    /// <summary>
    ///     下载图片并上传到 Supabase Storage
    /// </summary>
    private async Task<List<GeneratedImageInfo>> DownloadAndUploadImagesAsync(
        List<string> imageUrls,
        string bucket,
        string? pathPrefix,
        Guid userId)
    {
        var uploadedImages = new List<GeneratedImageInfo>();

        foreach (var imageUrl in imageUrls)
        {
            try
            {
                // 下载图片
                var imageBytes = await _httpClient.GetByteArrayAsync(imageUrl);

                // 生成存储路径
                var fileName = $"{Guid.NewGuid():N}.png";
                var storagePath = string.IsNullOrEmpty(pathPrefix)
                    ? $"{userId}/{fileName}"
                    : $"{pathPrefix}/{userId}/{fileName}";

                _logger.LogDebug("上传图片到 Supabase Storage: {Bucket}/{Path}", bucket, storagePath);

                // 上传到 Supabase Storage
                await _supabaseClient.Storage
                    .From(bucket)
                    .Upload(imageBytes, storagePath, new Supabase.Storage.FileOptions
                    {
                        ContentType = "image/png",
                        Upsert = false
                    });

                // 获取公开 URL
                var publicUrl = _supabaseClient.Storage
                    .From(bucket)
                    .GetPublicUrl(storagePath);

                uploadedImages.Add(new GeneratedImageInfo
                {
                    Url = publicUrl,
                    StoragePath = storagePath,
                    OriginalUrl = imageUrl,
                    FileSize = imageBytes.Length
                });

                _logger.LogInformation("图片上传成功: {Url}", publicUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "下载或上传图片失败: {ImageUrl}", imageUrl);
                // 继续处理其他图片
            }
        }

        return uploadedImages;
    }

    #region 通义万象 API 数据模型

    private class WanxCreateTaskRequest
    {
        public string Model { get; set; } = string.Empty;
        public WanxInput Input { get; set; } = new();
        public WanxParameters Parameters { get; set; } = new();
    }

    private class WanxInput
    {
        public string Prompt { get; set; } = string.Empty;

        [JsonPropertyName("negative_prompt")]
        public string? NegativePrompt { get; set; }
    }

    private class WanxParameters
    {
        public string Style { get; set; } = "<auto>";
        public string Size { get; set; } = "1024*1024";
        public int N { get; set; } = 1;
    }

    private class WanxCreateTaskResponse
    {
        public WanxOutput? Output { get; set; }

        [JsonPropertyName("request_id")]
        public string? RequestId { get; set; }

        public string? Code { get; set; }
        public string? Message { get; set; }
    }

    private class WanxQueryTaskResponse
    {
        public WanxQueryOutput? Output { get; set; }

        [JsonPropertyName("request_id")]
        public string? RequestId { get; set; }

        public WanxUsage? Usage { get; set; }
    }

    private class WanxOutput
    {
        [JsonPropertyName("task_id")]
        public string? TaskId { get; set; }

        [JsonPropertyName("task_status")]
        public string? TaskStatus { get; set; }
    }

    private class WanxQueryOutput
    {
        [JsonPropertyName("task_id")]
        public string? TaskId { get; set; }

        [JsonPropertyName("task_status")]
        public string? TaskStatus { get; set; }

        public List<WanxResult>? Results { get; set; }
        public string? Message { get; set; }

        [JsonPropertyName("task_metrics")]
        public WanxTaskMetrics? TaskMetrics { get; set; }
    }

    private class WanxResult
    {
        public string? Url { get; set; }
    }

    private class WanxTaskMetrics
    {
        [JsonPropertyName("TOTAL")]
        public int Total { get; set; }

        [JsonPropertyName("SUCCEEDED")]
        public int Succeeded { get; set; }

        [JsonPropertyName("FAILED")]
        public int Failed { get; set; }
    }

    private class WanxUsage
    {
        [JsonPropertyName("image_count")]
        public int ImageCount { get; set; }
    }

    #endregion
}
