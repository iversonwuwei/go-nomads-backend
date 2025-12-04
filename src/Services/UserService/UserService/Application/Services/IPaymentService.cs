using UserService.Application.DTOs;

namespace UserService.Application.Services;

/// <summary>
///     支付服务接口
/// </summary>
public interface IPaymentService
{
    /// <summary>
    ///     创建订单
    /// </summary>
    Task<OrderDto> CreateOrderAsync(string userId, CreateOrderRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    ///     确认支付
    /// </summary>
    Task<PaymentResultDto> CapturePaymentAsync(string userId, CapturePaymentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取订单详情
    /// </summary>
    Task<OrderDto?> GetOrderAsync(string userId, string orderId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取用户订单列表
    /// </summary>
    Task<List<OrderDto>> GetUserOrdersAsync(string userId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);

    /// <summary>
    ///     取消订单
    /// </summary>
    Task<bool> CancelOrderAsync(string userId, string orderId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     处理 PayPal Webhook
    /// </summary>
    Task HandleWebhookAsync(string eventType, string resourceId, string rawBody, CancellationToken cancellationToken = default);
}
