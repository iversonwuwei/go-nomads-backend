using UserService.Application.DTOs;

namespace UserService.Application.Services;

/// <summary>
///     微信支付 APP 下单响应
/// </summary>
public class WeChatPayAppOrderResponse
{
    /// <summary>
    ///     预支付交易会话标识
    /// </summary>
    public string PrepayId { get; set; } = string.Empty;
}

/// <summary>
///     微信支付 APP SDK 调起参数
/// </summary>
public class WeChatPayAppParams
{
    public string AppId { get; set; } = string.Empty;
    public string PartnerId { get; set; } = string.Empty;
    public string PrepayId { get; set; } = string.Empty;
    public string Package { get; set; } = "Sign=WXPay";
    public string NonceStr { get; set; } = string.Empty;
    public int Timestamp { get; set; }
    public string Sign { get; set; } = string.Empty;
}

/// <summary>
///     微信支付查询结果
/// </summary>
public class WeChatPayQueryResult
{
    public bool Success { get; set; }

    /// <summary>
    ///     交易状态: SUCCESS, REFUND, NOTPAY, CLOSED, USERPAYING, PAYERROR
    /// </summary>
    public string TradeState { get; set; } = string.Empty;

    /// <summary>
    ///     微信支付订单号
    /// </summary>
    public string? TransactionId { get; set; }

    /// <summary>
    ///     商户订单号
    /// </summary>
    public string? OutTradeNo { get; set; }

    /// <summary>
    ///     支付完成时间
    /// </summary>
    public DateTimeOffset? SuccessTime { get; set; }

    /// <summary>
    ///     错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    ///     原始 JSON 响应
    /// </summary>
    public string? RawResponse { get; set; }
}

/// <summary>
///     微信支付回调解密结果
/// </summary>
public class WeChatPayNotificationResult
{
    public bool IsValid { get; set; }

    /// <summary>
    ///     交易状态: SUCCESS, REFUND, NOTPAY, CLOSED, USERPAYING, PAYERROR
    /// </summary>
    public string TradeState { get; set; } = string.Empty;

    /// <summary>
    ///     微信支付订单号
    /// </summary>
    public string? TransactionId { get; set; }

    /// <summary>
    ///     商户订单号
    /// </summary>
    public string? OutTradeNo { get; set; }

    /// <summary>
    ///     支付完成时间
    /// </summary>
    public DateTimeOffset? SuccessTime { get; set; }

    /// <summary>
    ///     错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
///     微信支付服务接口
/// </summary>
public interface IWeChatPayService
{
    /// <summary>
    ///     创建 APP 支付预订单
    /// </summary>
    /// <param name="outTradeNo">商户订单号</param>
    /// <param name="description">商品描述</param>
    /// <param name="totalAmountInCents">总金额（单位：分）</param>
    /// <param name="cancellationToken"></param>
    /// <returns>预支付交易会话标识 (prepay_id)</returns>
    Task<WeChatPayAppOrderResponse> CreateAppOrderAsync(
        string outTradeNo,
        string description,
        int totalAmountInCents,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     生成 APP 支付调起参数（包含签名）
    /// </summary>
    WeChatPayAppParams GenerateAppPayParams(string prepayId);

    /// <summary>
    ///     查询订单状态
    /// </summary>
    Task<WeChatPayQueryResult> QueryOrderAsync(
        string outTradeNo,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     验证并解密回调通知
    /// </summary>
    Task<WeChatPayNotificationResult> VerifyAndDecryptNotificationAsync(
        string timestamp,
        string nonce,
        string signature,
        string serialNumber,
        string body,
        CancellationToken cancellationToken = default);
}
