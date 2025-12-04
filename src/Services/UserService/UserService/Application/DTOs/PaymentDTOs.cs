namespace UserService.Application.DTOs;

/// <summary>
///     创建订单请求
/// </summary>
public class CreateOrderRequest
{
    /// <summary>
    ///     订单类型: membership_upgrade, membership_renew, moderator_deposit
    /// </summary>
    public string OrderType { get; set; } = "membership_upgrade";

    /// <summary>
    ///     会员等级 (1=Basic, 2=Pro, 3=Premium)
    /// </summary>
    public int? MembershipLevel { get; set; }

    /// <summary>
    ///     订阅时长 (天)
    /// </summary>
    public int? DurationDays { get; set; } = 365;

    /// <summary>
    ///     保证金金额 (仅用于 moderator_deposit)
    /// </summary>
    public decimal? DepositAmount { get; set; }
}

/// <summary>
///     订单 DTO
/// </summary>
public class OrderDto
{
    public string Id { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public int? MembershipLevel { get; set; }
    public int? DurationDays { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ExpiredAt { get; set; }

    /// <summary>
    ///     PayPal 支付链接 (仅在创建订单时返回)
    /// </summary>
    public string? ApprovalUrl { get; set; }
}

/// <summary>
///     确认支付请求
/// </summary>
public class CapturePaymentRequest
{
    /// <summary>
    ///     PayPal 订单 ID (从 URL 参数获取)
    /// </summary>
    public string PayPalOrderId { get; set; } = string.Empty;

    /// <summary>
    ///     PayPal Payer ID (可选，从 URL 参数获取)
    /// </summary>
    public string? PayerId { get; set; }
}

/// <summary>
///     支付结果 DTO
/// </summary>
public class PaymentResultDto
{
    public bool Success { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public MembershipResponse? Membership { get; set; }
}

/// <summary>
///     微信支付创建订单请求
/// </summary>
public class CreateWeChatPayOrderRequest
{
    /// <summary>
    ///     订单类型: membership_upgrade, membership_renew, moderator_deposit
    /// </summary>
    public string OrderType { get; set; } = "membership_upgrade";

    /// <summary>
    ///     会员等级 (1=Basic, 2=Pro, 3=Premium)
    /// </summary>
    public int? MembershipLevel { get; set; }

    /// <summary>
    ///     订阅时长 (天)
    /// </summary>
    public int? DurationDays { get; set; } = 365;

    /// <summary>
    ///     保证金金额 (仅用于 moderator_deposit)
    /// </summary>
    public decimal? DepositAmount { get; set; }
}

/// <summary>
///     微信支付订单响应
/// </summary>
public class WeChatPayOrderDto
{
    /// <summary>
    ///     内部订单 ID
    /// </summary>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>
    ///     微信 AppId
    /// </summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    ///     商户号
    /// </summary>
    public string PartnerId { get; set; } = string.Empty;

    /// <summary>
    ///     预支付交易会话标识
    /// </summary>
    public string PrepayId { get; set; } = string.Empty;

    /// <summary>
    ///     扩展字段
    /// </summary>
    public string Package { get; set; } = "Sign=WXPay";

    /// <summary>
    ///     随机字符串
    /// </summary>
    public string NonceStr { get; set; } = string.Empty;

    /// <summary>
    ///     时间戳
    /// </summary>
    public int Timestamp { get; set; }

    /// <summary>
    ///     签名
    /// </summary>
    public string Sign { get; set; } = string.Empty;
}

/// <summary>
///     支付宝创建订单请求
/// </summary>
public class CreateAlipayOrderRequest
{
    /// <summary>
    ///     订单类型: membership_upgrade, membership_renew, moderator_deposit
    /// </summary>
    public string OrderType { get; set; } = "membership_upgrade";

    /// <summary>
    ///     会员等级 (1=Basic, 2=Pro, 3=Premium)
    /// </summary>
    public int? MembershipLevel { get; set; }

    /// <summary>
    ///     订阅时长 (天)
    /// </summary>
    public int? DurationDays { get; set; } = 365;

    /// <summary>
    ///     保证金金额 (仅用于 moderator_deposit)
    /// </summary>
    public decimal? DepositAmount { get; set; }
}

/// <summary>
///     支付宝订单响应
/// </summary>
public class AlipayOrderDto
{
    /// <summary>
    ///     内部订单 ID
    /// </summary>
    public string OrderId { get; set; } = string.Empty;

    /// <summary>
    ///     签名后的订单信息字符串 (用于客户端 SDK 调起支付)
    /// </summary>
    public string OrderString { get; set; } = string.Empty;
}

