using CityService.Application.Services;
using MassTransit;
using Shared.Messages;

namespace CityService.Infrastructure.Consumers;

/// <summary>
///     城市图片生成完成消息消费者
///     接收 AIService 发送的图片生成完成消息，更新城市图片数据
/// </summary>
public class CityImageGeneratedMessageConsumer : IConsumer<CityImageGeneratedMessage>
{
    private readonly ICityService _cityService;
    private readonly ILogger<CityImageGeneratedMessageConsumer> _logger;

    public CityImageGeneratedMessageConsumer(
        ICityService cityService,
        ILogger<CityImageGeneratedMessageConsumer> logger)
    {
        _cityService = cityService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CityImageGeneratedMessage> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "🖼️ 收到城市图片生成完成消息: TaskId={TaskId}, CityId={CityId}, Success={Success}",
            message.TaskId, message.CityId, message.Success);

        if (!message.Success)
        {
            _logger.LogWarning(
                "⚠️ 城市图片生成失败: CityId={CityId}, Error={Error}",
                message.CityId, message.ErrorMessage);
            return;
        }

        try
        {
            // 解析城市 ID
            if (!Guid.TryParse(message.CityId, out var cityId))
            {
                _logger.LogError("❌ 无效的城市ID: {CityId}", message.CityId);
                return;
            }

            // 更新城市图片
            await _cityService.UpdateCityImagesAsync(
                cityId,
                message.PortraitImageUrl,
                message.LandscapeImageUrls);

            _logger.LogInformation(
                "✅ 城市图片已更新: CityId={CityId}, Portrait={HasPortrait}, LandscapeCount={LandscapeCount}",
                message.CityId,
                !string.IsNullOrEmpty(message.PortraitImageUrl),
                message.LandscapeImageUrls?.Count ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ 更新城市图片失败: CityId={CityId}",
                message.CityId);
            throw;
        }
    }
}
