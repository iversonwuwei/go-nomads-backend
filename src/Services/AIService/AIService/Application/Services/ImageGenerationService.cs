using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AIService.Application.DTOs;
using SkiaSharp;
using Client = Supabase.Client;

namespace AIService.Application.Services;

/// <summary>
///     图片生成服务实现 (通义万象 + Supabase Storage)
///     支持并发控制，防止 API 限流
/// </summary>
public class ImageGenerationService : IImageGenerationService
{
    private const string WanxApiBaseUrl = "https://dashscope.aliyuncs.com/api/v1";
    private const string CreateTaskEndpoint = "/services/aigc/text2image/image-synthesis";
    private const string QueryTaskEndpoint = "/tasks";
    
    // 并发控制：最多同时处理 3 个城市图片生成请求
    private static readonly SemaphoreSlim _cityImageSemaphore = new(3, 3);
    // 跟踪正在处理的城市，避免重复请求
    private static readonly ConcurrentDictionary<string, Task<GenerateCityImagesResponse>> _pendingCityRequests = new();

    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ImageGenerationService> _logger;
    private readonly Client _supabaseClient;
    
    // 从配置读取的轮询参数
    private readonly int _maxPollingAttempts;
    private readonly int _pollingIntervalMs;
    
    // 移动端图片优化参数
    private readonly int _coverImageWidth;      // 封面图宽度（横屏）
    private readonly int _coverImageHeight;     // 封面图高度（横屏）
    private readonly int _portraitImageWidth;   // 竖屏封面图宽度
    private readonly int _portraitImageHeight;  // 竖屏封面图高度
    private readonly int _landscapeImageWidth;  // 横屏跑马灯图宽度
    private readonly int _landscapeImageHeight; // 横屏跑马灯图高度
    private readonly int _webpQuality;          // WebP 压缩质量 (0-100)

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
        
        // 从配置读取轮询参数，使用更宽裕的默认值（90次 × 2秒 = 3分钟）
        _maxPollingAttempts = configuration.GetValue("ImageGeneration:MaxPollingAttempts", 90);
        _pollingIntervalMs = configuration.GetValue("ImageGeneration:PollingIntervalMs", 2000);
        
        // 移动端图片优化参数（适配手机屏幕，减少传输体积）
        // 封面图：用于旅行计划卡片，手机屏宽一般 360-428dp，2x/3x 屏 → 750*422 足够
        _coverImageWidth = configuration.GetValue("ImageGeneration:Mobile:CoverImageWidth", 750);
        _coverImageHeight = configuration.GetValue("ImageGeneration:Mobile:CoverImageHeight", 422);
        // 竖屏封面图：用于城市详情页头图，手机屏宽 → 750*1334 (iPhone 6/7/8 比例)
        _portraitImageWidth = configuration.GetValue("ImageGeneration:Mobile:PortraitImageWidth", 750);
        _portraitImageHeight = configuration.GetValue("ImageGeneration:Mobile:PortraitImageHeight", 1334);
        // 横屏跑马灯图：用于详情页轮播，750*422 (16:9 比例)
        _landscapeImageWidth = configuration.GetValue("ImageGeneration:Mobile:LandscapeImageWidth", 750);
        _landscapeImageHeight = configuration.GetValue("ImageGeneration:Mobile:LandscapeImageHeight", 422);
        // WebP 压缩质量：75 在视觉质量和文件大小之间取得良好平衡
        _webpQuality = configuration.GetValue("ImageGeneration:Mobile:WebpQuality", 75);
        
        _logger.LogInformation(
            "图片生成服务初始化，轮询配置: MaxAttempts={MaxAttempts}, IntervalMs={IntervalMs}, 总超时={TotalTimeout}秒, " +
            "移动端优化: Cover={CoverW}x{CoverH}, Portrait={PortraitW}x{PortraitH}, Landscape={LandscapeW}x{LandscapeH}, WebpQuality={Quality}",
            _maxPollingAttempts, _pollingIntervalMs, _maxPollingAttempts * _pollingIntervalMs / 1000,
            _coverImageWidth, _coverImageHeight, _portraitImageWidth, _portraitImageHeight,
            _landscapeImageWidth, _landscapeImageHeight, _webpQuality);
    }

    /// <inheritdoc />
    public async Task<GenerateImageResponse> GenerateImageAsync(
        GenerateImageRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = new GenerateImageResponse();

        try
        {
            _logger.LogInformation("开始生成图片，用户: {UserId}, 提示词: {Prompt}", userId, request.Prompt);

            // 1. 创建通义万象任务
            var taskId = await CreateWanxTaskAsync(request, cancellationToken);
            response.TaskId = taskId;

            _logger.LogInformation("通义万象任务已创建，TaskId: {TaskId}", taskId);

            // 2. 轮询任务状态直到完成
            var taskResult = await PollTaskUntilCompleteAsync(taskId, cancellationToken);

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
    public async Task<GenerateCityImagesResponse> GenerateCityImagesAsync(
        GenerateCityImagesRequest request, CancellationToken cancellationToken = default)
    {
        // 检查是否有相同城市的请求正在处理中
        if (_pendingCityRequests.TryGetValue(request.CityId, out var existingTask))
        {
            _logger.LogInformation("⏳ 城市 {CityId} 的图片生成请求正在进行中，等待现有任务完成...", request.CityId);
            try
            {
                return await existingTask;
            }
            catch
            {
                // 如果现有任务失败，移除并继续新请求
                _pendingCityRequests.TryRemove(request.CityId, out _);
            }
        }

        // 创建新任务并注册
        var taskCompletionSource = new TaskCompletionSource<GenerateCityImagesResponse>();
        var newTask = taskCompletionSource.Task;
        
        if (!_pendingCityRequests.TryAdd(request.CityId, newTask))
        {
            // 如果添加失败，说明有其他线程刚刚添加了任务，等待它
            if (_pendingCityRequests.TryGetValue(request.CityId, out var concurrentTask))
            {
                return await concurrentTask;
            }
        }

        try
        {
            var result = await GenerateCityImagesInternalAsync(request, cancellationToken);
            taskCompletionSource.SetResult(result);
            return result;
        }
        catch (Exception ex)
        {
            taskCompletionSource.SetException(ex);
            throw;
        }
        finally
        {
            // 任务完成后移除
            _pendingCityRequests.TryRemove(request.CityId, out _);
        }
    }

    /// <summary>
    /// 内部方法：实际执行城市图片生成（带并发控制）
    /// </summary>
    private async Task<GenerateCityImagesResponse> GenerateCityImagesInternalAsync(
        GenerateCityImagesRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = new GenerateCityImagesResponse
        {
            CityId = request.CityId
        };

        // 获取信号量，控制并发数
        _logger.LogInformation("🔄 城市 {CityId} 等待获取并发槽位... (当前可用: {Available})", 
            request.CityId, _cityImageSemaphore.CurrentCount);
        
        await _cityImageSemaphore.WaitAsync();
        
        try
        {
            _logger.LogInformation("✅ 城市 {CityId} 获取到并发槽位，开始生成图片", request.CityId);
            _logger.LogInformation("开始批量生成城市图片，城市: {CityName} ({CityId})", request.CityName, request.CityId);

            // 提取英文城市名（如果有中英文格式如"北戴河/Beidaihe"，只取英文部分）
            var cityName = request.CityName;
            if (cityName.Contains('/'))
            {
                cityName = cityName.Split('/').Last().Trim();
            }

            // 生成默认提示词 - 融合美食、旅游景点和现代化城市元素的创意构图（纯英文避免敏感词检测）
            var cityDesc = string.IsNullOrEmpty(request.Country)
                ? cityName
                : $"{cityName}, {request.Country}";

            var portraitPrompt = request.PortraitPrompt
                ?? $"Beautiful vertical travel photograph of {cityDesc}, artistic composition with local cuisine in foreground, historic landmark in middle, modern skyline in background, vibrant colors, warm atmosphere, professional photography, high quality, cinematic lighting";

            var landscapePrompt = request.LandscapePrompt
                ?? $"Panoramic travel photograph of {cityDesc}, featuring local food culture, famous scenic spots, modern architecture, vibrant street life, colorful composition, professional photography, high resolution, vivid colors";

            var negativePrompt = request.NegativePrompt
                ?? "blurry, low quality, distorted, watermark, text, logo, ugly, deformed, dull colors";

            // 并行生成竖屏和横屏图片
            var portraitTask = GeneratePortraitImageAsync(request, portraitPrompt, negativePrompt);
            var landscapeTask = GenerateLandscapeImagesAsync(request, landscapePrompt, negativePrompt);

            // 等待两个任务都完成
            await Task.WhenAll(portraitTask, landscapeTask);

            // 获取结果
            var portraitResult = await portraitTask;
            var landscapeResult = await landscapeTask;

            if (portraitResult != null)
            {
                response.PortraitImage = portraitResult;
            }

            if (landscapeResult != null && landscapeResult.Count > 0)
            {
                response.LandscapeImages = landscapeResult;
            }

            stopwatch.Stop();
            response.GenerationTimeMs = (int)stopwatch.ElapsedMilliseconds;
            response.Success = response.PortraitImage != null || response.LandscapeImages.Count > 0;

            if (!response.Success)
            {
                response.ErrorMessage = "所有图片生成均失败";
            }

            _logger.LogInformation("🎉 城市图片批量生成完成，耗时: {Time}ms，竖屏: {Portrait}张，横屏: {Landscape}张",
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
        finally
        {
            // 释放信号量
            _cityImageSemaphore.Release();
            _logger.LogInformation("🔓 城市 {CityId} 释放并发槽位 (当前可用: {Available})", 
                request.CityId, _cityImageSemaphore.CurrentCount);
        }
    }

    /// <summary>
    /// 生成竖屏封面图
    /// </summary>
    private async Task<GeneratedImageInfo?> GeneratePortraitImageAsync(
        GenerateCityImagesRequest request, string prompt, string negativePrompt)
    {
        try
        {
            _logger.LogInformation("生成竖屏封面图...");
            var portraitRequest = new GenerateImageRequest
            {
                Prompt = prompt,
                NegativePrompt = negativePrompt,
                Style = request.Style,
                Size = "720*1280",
                Count = 1,
                Bucket = request.Bucket,
                PathPrefix = $"portrait/{request.CityId}"
            };

            var result = await GenerateImageAsync(portraitRequest, Guid.Empty);
            if (result.Success && result.Images.Count > 0)
            {
                _logger.LogInformation("竖屏封面图生成成功: {Url}", result.Images[0].Url);
                return result.Images[0];
            }
            
            _logger.LogWarning("竖屏封面图生成失败: {Error}", result.ErrorMessage);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成竖屏封面图异常");
            return null;
        }
    }

    /// <summary>
    /// 生成横屏图片列表
    /// </summary>
    private async Task<List<GeneratedImageInfo>> GenerateLandscapeImagesAsync(
        GenerateCityImagesRequest request, string prompt, string negativePrompt)
    {
        try
        {
            _logger.LogInformation("生成横屏图片...");
            var landscapeRequest = new GenerateImageRequest
            {
                Prompt = prompt,
                NegativePrompt = negativePrompt,
                Style = request.Style,
                Size = "1280*720",
                Count = 4,
                Bucket = request.Bucket,
                PathPrefix = $"landscape/{request.CityId}"
            };

            var result = await GenerateImageAsync(landscapeRequest, Guid.Empty);
            if (result.Success && result.Images.Count > 0)
            {
                _logger.LogInformation("横屏图片生成成功，共 {Count} 张", result.Images.Count);
                return result.Images;
            }
            
            _logger.LogWarning("横屏图片生成失败: {Error}", result.ErrorMessage);
            return new List<GeneratedImageInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成横屏图片异常");
            return new List<GeneratedImageInfo>();
        }
    }

    /// <inheritdoc />
    public async Task<ImageTaskStatusResponse> GetTaskStatusAsync(
        string taskId, CancellationToken cancellationToken = default)
    {
        return await QueryWanxTaskStatusAsync(taskId, cancellationToken);
    }

    /// <summary>
    ///     创建通义万象图片生成任务
    /// </summary>
    private async Task<string> CreateWanxTaskAsync(
        GenerateImageRequest request, CancellationToken cancellationToken = default)
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

        var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var responseContent = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

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
    ///     轮询任务状态直到完成（支持取消）
    /// </summary>
    private async Task<ImageTaskStatusResponse> PollTaskUntilCompleteAsync(
        string taskId, CancellationToken cancellationToken = default)
    {
        var totalTimeoutMs = _maxPollingAttempts * _pollingIntervalMs;
        _logger.LogInformation("开始轮询任务 {TaskId}，最大尝试次数: {MaxAttempts}，间隔: {IntervalMs}ms，总超时: {TotalTimeout}秒",
            taskId, _maxPollingAttempts, _pollingIntervalMs, totalTimeoutMs / 1000);

        for (var i = 0; i < _maxPollingAttempts; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var status = await QueryWanxTaskStatusAsync(taskId, cancellationToken);

            _logger.LogDebug("任务 {TaskId} 状态: {Status} (第 {Attempt}/{MaxAttempts} 次查询)",
                taskId, status.Status, i + 1, _maxPollingAttempts);

            if (status.Status is "SUCCEEDED" or "FAILED" or "CANCELED")
                return status;

            await Task.Delay(_pollingIntervalMs, cancellationToken);
        }

        _logger.LogWarning("任务 {TaskId} 轮询超时，已尝试 {MaxAttempts} 次（共 {TotalTimeout}秒）",
            taskId, _maxPollingAttempts, totalTimeoutMs / 1000);

        return new ImageTaskStatusResponse
        {
            TaskId = taskId,
            Status = "TIMEOUT",
            ErrorMessage = $"任务轮询超时（已等待 {totalTimeoutMs / 1000} 秒）"
        };
    }

    /// <summary>
    ///     查询通义万象任务状态
    /// </summary>
    private async Task<ImageTaskStatusResponse> QueryWanxTaskStatusAsync(
        string taskId, CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["Qwen:ApiKey"]
                     ?? throw new InvalidOperationException("未配置 Qwen:ApiKey");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"{WanxApiBaseUrl}{QueryTaskEndpoint}/{taskId}");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var responseContent = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

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
    ///     下载图片并上传到 Supabase Storage（带重试机制 + 移动端优化：缩放 + WebP 压缩）
    /// </summary>
    private async Task<List<GeneratedImageInfo>> DownloadAndUploadImagesAsync(
        List<string> imageUrls,
        string bucket,
        string? pathPrefix,
        Guid userId)
    {
        var uploadedImages = new List<GeneratedImageInfo>();
        const int maxRetries = 3;

        // 根据 pathPrefix 推断目标尺寸
        var (targetWidth, targetHeight) = InferTargetSize(pathPrefix);

        foreach (var imageUrl in imageUrls)
        {
            try
            {
                // 下载图片（带重试机制）
                byte[]? imageBytes = null;

                for (int retry = 0; retry < maxRetries; retry++)
                {
                    try
                    {
                        _logger.LogInformation("📥 下载图片 (尝试 {Retry}/{Max}): {Url}", retry + 1, maxRetries, imageUrl);
                        imageBytes = await _httpClient.GetByteArrayAsync(imageUrl);
                        _logger.LogInformation("✅ 图片下载成功，原始大小: {Size} bytes ({SizeKB} KB)", 
                            imageBytes.Length, imageBytes.Length / 1024);
                        break;
                    }
                    catch (HttpRequestException ex) when (retry < maxRetries - 1)
                    {
                        _logger.LogWarning("⚠️ 下载图片失败 (尝试 {Retry}/{Max}): {Error}，将在 2 秒后重试",
                            retry + 1, maxRetries, ex.Message);
                        await Task.Delay(2000);
                    }
                }

                if (imageBytes == null || imageBytes.Length == 0)
                {
                    _logger.LogError("❌ 图片下载失败（已重试 {Max} 次）: {Url}", maxRetries, imageUrl);
                    continue;
                }

                // 🔧 移动端优化：缩放 + WebP 压缩
                var originalSize = imageBytes.Length;
                var optimizedBytes = OptimizeImageForMobile(imageBytes, targetWidth, targetHeight);
                
                _logger.LogInformation(
                    "📱 图片移动端优化完成: {OriginalKB} KB → {OptimizedKB} KB (节省 {SavedPercent}%), 目标尺寸: {W}x{H}, WebP 质量: {Quality}",
                    originalSize / 1024, optimizedBytes.Length / 1024,
                    (int)((1 - (double)optimizedBytes.Length / originalSize) * 100),
                    targetWidth, targetHeight, _webpQuality);

                // 生成存储路径（使用 .webp 扩展名）
                var fileName = $"{Guid.NewGuid():N}.webp";
                var storagePath = string.IsNullOrEmpty(pathPrefix)
                    ? $"{userId}/{fileName}"
                    : $"{pathPrefix}/{userId}/{fileName}";

                // 上传到 Supabase Storage（带重试机制）
                string? publicUrl = null;

                for (int retry = 0; retry < maxRetries; retry++)
                {
                    try
                    {
                        _logger.LogInformation("📤 上传 WebP 图片到 Supabase (尝试 {Retry}/{Max}): {Bucket}/{Path}, 大小: {Size} KB",
                            retry + 1, maxRetries, bucket, storagePath, optimizedBytes.Length / 1024);

                        await _supabaseClient.Storage
                            .From(bucket)
                            .Upload(optimizedBytes, storagePath, new Supabase.Storage.FileOptions
                            {
                                ContentType = "image/webp",
                                Upsert = true
                            });

                        publicUrl = _supabaseClient.Storage
                            .From(bucket)
                            .GetPublicUrl(storagePath);

                        _logger.LogInformation("✅ 图片上传成功: {Url}", publicUrl);
                        break;
                    }
                    catch (Exception ex) when (retry < maxRetries - 1)
                    {
                        _logger.LogWarning("⚠️ 上传图片失败 (尝试 {Retry}/{Max}): {Error}，将在 2 秒后重试",
                            retry + 1, maxRetries, ex.Message);
                        await Task.Delay(2000);
                    }
                }

                if (string.IsNullOrEmpty(publicUrl))
                {
                    _logger.LogError("❌ 图片上传失败（已重试 {Max} 次）: {Path}", maxRetries, storagePath);
                    continue;
                }

                uploadedImages.Add(new GeneratedImageInfo
                {
                    Url = publicUrl,
                    StoragePath = storagePath,
                    OriginalUrl = imageUrl,
                    FileSize = optimizedBytes.Length
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 处理图片失败: {ImageUrl}", imageUrl);
            }
        }

        return uploadedImages;
    }

    /// <summary>
    ///     根据存储路径前缀推断目标图片尺寸
    /// </summary>
    private (int width, int height) InferTargetSize(string? pathPrefix)
    {
        if (string.IsNullOrEmpty(pathPrefix))
            return (_coverImageWidth, _coverImageHeight);

        // 竖屏封面图
        if (pathPrefix.Contains("portrait", StringComparison.OrdinalIgnoreCase))
            return (_portraitImageWidth, _portraitImageHeight);

        // 横屏跑马灯图 / 附近城市 / 旅行计划封面
        if (pathPrefix.Contains("landscape", StringComparison.OrdinalIgnoreCase) ||
            pathPrefix.Contains("nearby", StringComparison.OrdinalIgnoreCase) ||
            pathPrefix.Contains("travel-plans", StringComparison.OrdinalIgnoreCase))
            return (_landscapeImageWidth, _landscapeImageHeight);

        return (_coverImageWidth, _coverImageHeight);
    }

    /// <summary>
    ///     移动端图片优化：缩放到目标尺寸 + WebP 压缩
    ///     适合手机屏幕显示，大幅减少传输体积
    /// </summary>
    private byte[] OptimizeImageForMobile(byte[] originalBytes, int targetWidth, int targetHeight)
    {
        try
        {
            using var originalBitmap = SKBitmap.Decode(originalBytes);
            if (originalBitmap == null)
            {
                _logger.LogWarning("⚠️ 无法解码图片，将使用原始数据上传");
                return originalBytes;
            }

            // 如果原图已经小于等于目标尺寸，只做 WebP 压缩不缩放
            SKBitmap targetBitmap;
            if (originalBitmap.Width <= targetWidth && originalBitmap.Height <= targetHeight)
            {
                targetBitmap = originalBitmap;
            }
            else
            {
                // 等比缩放到目标尺寸（保持宽高比，取较小缩放比）
                var scaleX = (float)targetWidth / originalBitmap.Width;
                var scaleY = (float)targetHeight / originalBitmap.Height;
                var scale = Math.Min(scaleX, scaleY);

                var newWidth = (int)(originalBitmap.Width * scale);
                var newHeight = (int)(originalBitmap.Height * scale);

                targetBitmap = originalBitmap.Resize(new SKImageInfo(newWidth, newHeight), SKSamplingOptions.Default);
                if (targetBitmap == null)
                {
                    _logger.LogWarning("⚠️ 图片缩放失败，将使用原始尺寸进行 WebP 压缩");
                    targetBitmap = originalBitmap;
                }

                _logger.LogDebug("图片缩放: {OrigW}x{OrigH} → {NewW}x{NewH}", 
                    originalBitmap.Width, originalBitmap.Height, newWidth, newHeight);
            }

            // 编码为 WebP
            using var image = SKImage.FromBitmap(targetBitmap);
            using var data = image.Encode(SKEncodedImageFormat.Webp, _webpQuality);
            
            if (targetBitmap != originalBitmap)
                targetBitmap.Dispose();

            return data.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 图片优化失败，将使用原始数据上传");
            return originalBytes;
        }
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
