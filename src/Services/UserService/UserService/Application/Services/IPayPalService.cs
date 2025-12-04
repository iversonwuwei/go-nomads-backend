namespace UserService.Application.Services;

/// <summary>
///     PayPal 订单响应
/// </summary>
public class PayPalOrderResponse
{
    public string OrderId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ApprovalUrl { get; set; }
}

/// <summary>
///     PayPal 支付确认响应
/// </summary>
public class PayPalCaptureResponse
{
    public bool Success { get; set; }
    public string? CaptureId { get; set; }
    public string? TransactionId { get; set; }
    public string? PayerId { get; set; }
    public string? PayerEmail { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? RawResponse { get; set; }
}

/// <summary>
///     PayPal 服务接口
/// </summary>
public interface IPayPalService
{
    /// <summary>
    ///     创建 PayPal 订单
    /// </summary>
    Task<PayPalOrderResponse> CreateOrderAsync(
        decimal amount,
        string currency,
        string description,
        string referenceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     确认支付 (Capture)
    /// </summary>
    Task<PayPalCaptureResponse> CapturePaymentAsync(
        string paypalOrderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     获取订单详情
    /// </summary>
    Task<PayPalOrderResponse?> GetOrderDetailsAsync(
        string paypalOrderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     验证 Webhook 签名
    /// </summary>
    Task<bool> VerifyWebhookSignatureAsync(
        string webhookId,
        string transmissionId,
        string transmissionTime,
        string certUrl,
        string authAlgo,
        string transmissionSig,
        string webhookEventBody,
        CancellationToken cancellationToken = default);
}
