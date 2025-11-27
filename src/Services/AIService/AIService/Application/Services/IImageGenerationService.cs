using AIService.Application.DTOs;

namespace AIService.Application.Services;

/// <summary>
///     图片生成服务接口 (通义万象)
/// </summary>
public interface IImageGenerationService
{
    /// <summary>
    ///     生成图片并上传到 Supabase Storage
    /// </summary>
    /// <param name="request">生成图片请求</param>
    /// <param name="userId">用户ID</param>
    /// <returns>生成结果，包含 Supabase Storage 的公开 URL</returns>
    Task<GenerateImageResponse> GenerateImageAsync(GenerateImageRequest request, Guid userId);

    /// <summary>
    ///     批量生成城市图片（1张竖屏 + 4张横屏）
    /// </summary>
    /// <param name="request">城市图片生成请求</param>
    /// <returns>生成结果，包含竖屏和横屏图片</returns>
    Task<GenerateCityImagesResponse> GenerateCityImagesAsync(GenerateCityImagesRequest request);

    /// <summary>
    ///     检查图片生成任务状态
    /// </summary>
    /// <param name="taskId">任务ID</param>
    /// <returns>任务状态和结果</returns>
    Task<ImageTaskStatusResponse> GetTaskStatusAsync(string taskId);
}

/// <summary>
///     图片任务状态响应
/// </summary>
public class ImageTaskStatusResponse
{
    /// <summary>
    ///     任务ID
    /// </summary>
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
    ///     任务状态: PENDING, RUNNING, SUCCEEDED, FAILED, CANCELED, UNKNOWN
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    ///     生成的图片 URL 列表（任务成功时）
    /// </summary>
    public List<string> ImageUrls { get; set; } = new();

    /// <summary>
    ///     成功数量
    /// </summary>
    public int SucceededCount { get; set; }

    /// <summary>
    ///     失败数量
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    ///     错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }
}
