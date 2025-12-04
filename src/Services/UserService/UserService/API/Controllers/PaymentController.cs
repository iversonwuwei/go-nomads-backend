using GoNomads.Shared.Middleware;
using GoNomads.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using UserService.Application.DTOs;
using UserService.Application.Services;
using UserService.Infrastructure.Configuration;

namespace UserService.API.Controllers;

/// <summary>
///     支付 API - RESTful endpoints for payment processing
/// </summary>
[ApiController]
[Route("api/v1/payments")]
public class PaymentController : ControllerBase
{
    private readonly ILogger<PaymentController> _logger;
    private readonly IPaymentService _paymentService;
    private readonly IPayPalService _payPalService;
    private readonly IAlipayService _alipayService;
    private readonly PayPalSettings _payPalSettings;

    public PaymentController(
        IPaymentService paymentService,
        IPayPalService payPalService,
        IAlipayService alipayService,
        IOptions<PayPalSettings> payPalSettings,
        ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _payPalService = payPalService;
        _alipayService = alipayService;
        _payPalSettings = payPalSettings.Value;
        _logger = logger;
    }

    /// <summary>
    ///     创建订单
    /// </summary>
    /// <remarks>
    ///     创建一个新的支付订单，返回 PayPal 支付链接
    /// </remarks>
    [HttpPost("orders")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> CreateOrder(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
        {
            return Unauthorized(new ApiResponse<OrderDto>
            {
                Success = false,
                Message = "未认证用户"
            });
        }

        _logger.LogInformation("📝 创建订单请求: UserId={UserId}, Type={Type}",
            userContext.UserId, request.OrderType);

        try
        {
            var order = await _paymentService.CreateOrderAsync(userContext.UserId, request, cancellationToken);

            return Ok(new ApiResponse<OrderDto>
            {
                Success = true,
                Message = "订单创建成功",
                Data = order
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ApiResponse<OrderDto>
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建订单失败");
            return StatusCode(500, new ApiResponse<OrderDto>
            {
                Success = false,
                Message = "创建订单失败"
            });
        }
    }

    /// <summary>
    ///     确认支付
    /// </summary>
    /// <remarks>
    ///     用户在 PayPal 完成支付后，调用此接口确认支付
    /// </remarks>
    [HttpPost("orders/{orderId}/capture")]
    public async Task<ActionResult<ApiResponse<PaymentResultDto>>> CapturePayment(
        string orderId,
        [FromBody] CapturePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
        {
            return Unauthorized(new ApiResponse<PaymentResultDto>
            {
                Success = false,
                Message = "未认证用户"
            });
        }

        _logger.LogInformation("💳 确认支付请求: UserId={UserId}, PayPalOrderId={PayPalOrderId}",
            userContext.UserId, request.PayPalOrderId);

        try
        {
            var result = await _paymentService.CapturePaymentAsync(userContext.UserId, request, cancellationToken);

            return Ok(new ApiResponse<PaymentResultDto>
            {
                Success = result.Success,
                Message = result.Message ?? (result.Success ? "支付成功" : "支付失败"),
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 确认支付失败");
            return StatusCode(500, new ApiResponse<PaymentResultDto>
            {
                Success = false,
                Message = "确认支付失败"
            });
        }
    }

    /// <summary>
    ///     获取订单详情
    /// </summary>
    [HttpGet("orders/{orderId}")]
    public async Task<ActionResult<ApiResponse<OrderDto>>> GetOrder(
        string orderId,
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
        {
            return Unauthorized(new ApiResponse<OrderDto>
            {
                Success = false,
                Message = "未认证用户"
            });
        }

        var order = await _paymentService.GetOrderAsync(userContext.UserId, orderId, cancellationToken);
        if (order == null)
        {
            return NotFound(new ApiResponse<OrderDto>
            {
                Success = false,
                Message = "订单不存在"
            });
        }

        return Ok(new ApiResponse<OrderDto>
        {
            Success = true,
            Data = order
        });
    }

    /// <summary>
    ///     获取用户订单列表
    /// </summary>
    [HttpGet("orders")]
    public async Task<ActionResult<ApiResponse<List<OrderDto>>>> GetOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
        {
            return Unauthorized(new ApiResponse<List<OrderDto>>
            {
                Success = false,
                Message = "未认证用户"
            });
        }

        var orders = await _paymentService.GetUserOrdersAsync(userContext.UserId, page, pageSize, cancellationToken);

        return Ok(new ApiResponse<List<OrderDto>>
        {
            Success = true,
            Data = orders
        });
    }

    /// <summary>
    ///     取消订单
    /// </summary>
    [HttpPost("orders/{orderId}/cancel")]
    public async Task<ActionResult<ApiResponse<bool>>> CancelOrder(
        string orderId,
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
        {
            return Unauthorized(new ApiResponse<bool>
            {
                Success = false,
                Message = "未认证用户"
            });
        }

        var success = await _paymentService.CancelOrderAsync(userContext.UserId, orderId, cancellationToken);

        return Ok(new ApiResponse<bool>
        {
            Success = success,
            Message = success ? "订单已取消" : "取消订单失败",
            Data = success
        });
    }

    /// <summary>
    ///     PayPal Webhook 回调
    /// </summary>
    [HttpPost("webhooks/paypal")]
    public async Task<IActionResult> PayPalWebhook(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📨 收到 PayPal Webhook");

        try
        {
            // 读取请求体
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync(cancellationToken);

            // 验证 Webhook 签名 (生产环境必须)
            if (!string.IsNullOrEmpty(_payPalSettings.WebhookId))
            {
                var transmissionId = Request.Headers["PayPal-Transmission-Id"].FirstOrDefault() ?? "";
                var transmissionTime = Request.Headers["PayPal-Transmission-Time"].FirstOrDefault() ?? "";
                var certUrl = Request.Headers["PayPal-Cert-Url"].FirstOrDefault() ?? "";
                var authAlgo = Request.Headers["PayPal-Auth-Algo"].FirstOrDefault() ?? "";
                var transmissionSig = Request.Headers["PayPal-Transmission-Sig"].FirstOrDefault() ?? "";

                var isValid = await _payPalService.VerifyWebhookSignatureAsync(
                    _payPalSettings.WebhookId,
                    transmissionId,
                    transmissionTime,
                    certUrl,
                    authAlgo,
                    transmissionSig,
                    body,
                    cancellationToken);

                if (!isValid)
                {
                    _logger.LogWarning("⚠️ PayPal Webhook 签名验证失败");
                    return BadRequest("Invalid signature");
                }
            }

            // 解析事件
            using var doc = System.Text.Json.JsonDocument.Parse(body);
            var root = doc.RootElement;

            var eventType = root.GetProperty("event_type").GetString() ?? "";
            var resourceId = "";

            if (root.TryGetProperty("resource", out var resource))
            {
                if (resource.TryGetProperty("id", out var id))
                {
                    resourceId = id.GetString() ?? "";
                }
            }

            _logger.LogInformation("PayPal Webhook: EventType={EventType}, ResourceId={ResourceId}",
                eventType, resourceId);

            // 处理事件
            await _paymentService.HandleWebhookAsync(eventType, resourceId, body, cancellationToken);

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 处理 PayPal Webhook 失败");
            return StatusCode(500);
        }
    }

    /// <summary>
    ///     支付成功回调页面 (重定向)
    /// </summary>
    [HttpGet("return")]
    public IActionResult PaymentReturn([FromQuery] string token, [FromQuery] string? PayerID)
    {
        _logger.LogInformation("💳 支付返回: Token={Token}, PayerID={PayerID}", token, PayerID);

        // 重定向到 Flutter 应用的 deep link
        // 格式: gonomads://payment/success?token=xxx&PayerID=xxx
        var redirectUrl = $"gonomads://payment/success?token={token}";
        if (!string.IsNullOrEmpty(PayerID))
        {
            redirectUrl += $"&PayerID={PayerID}";
        }

        return Redirect(redirectUrl);
    }

    /// <summary>
    ///     支付取消回调页面 (重定向)
    /// </summary>
    [HttpGet("cancel")]
    public IActionResult PaymentCancel([FromQuery] string? token)
    {
        _logger.LogInformation("❌ 支付取消: Token={Token}", token);

        // 重定向到 Flutter 应用的 deep link
        var redirectUrl = "gonomads://payment/cancel";
        if (!string.IsNullOrEmpty(token))
        {
            redirectUrl += $"?token={token}";
        }

        return Redirect(redirectUrl);
    }

    /// <summary>
    ///     创建微信支付订单
    /// </summary>
    /// <remarks>
    ///     创建微信支付订单，返回调用微信 SDK 所需的参数
    /// </remarks>
    [HttpPost("orders/wechat")]
    public async Task<ActionResult<ApiResponse<WeChatPayOrderDto>>> CreateWeChatPayOrder(
        [FromBody] CreateWeChatPayOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
        {
            return Unauthorized(new ApiResponse<WeChatPayOrderDto>
            {
                Success = false,
                Message = "未认证用户"
            });
        }

        _logger.LogInformation("📝 创建微信支付订单: UserId={UserId}, Type={Type}",
            userContext.UserId, request.OrderType);

        try
        {
            // TODO: 实现真正的微信支付订单创建
            // 需要配置微信商户号、API密钥等
            // 调用微信统一下单接口获取 prepay_id

            // 模拟返回 (实际需要对接微信支付 API)
            var mockOrder = new WeChatPayOrderDto
            {
                OrderId = Guid.NewGuid().ToString(),
                AppId = "wx_your_app_id",  // 微信开放平台 AppId
                PartnerId = "your_mch_id", // 商户号
                PrepayId = $"wx_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}",
                Package = "Sign=WXPay",
                NonceStr = Guid.NewGuid().ToString("N"),
                Timestamp = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Sign = "mock_sign_value" // 实际需要使用商户私钥签名
            };

            return Ok(new ApiResponse<WeChatPayOrderDto>
            {
                Success = true,
                Message = "微信支付订单创建成功",
                Data = mockOrder
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建微信支付订单失败");
            return StatusCode(500, new ApiResponse<WeChatPayOrderDto>
            {
                Success = false,
                Message = "创建微信支付订单失败"
            });
        }
    }

    /// <summary>
    ///     创建支付宝订单
    /// </summary>
    /// <remarks>
    ///     创建支付宝订单，返回签名后的订单信息字符串
    /// </remarks>
    [HttpPost("orders/alipay")]
    public async Task<ActionResult<ApiResponse<AlipayOrderDto>>> CreateAlipayOrder(
        [FromBody] CreateAlipayOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var userContext = UserContextMiddleware.GetUserContext(HttpContext);
        if (userContext?.IsAuthenticated != true || string.IsNullOrEmpty(userContext.UserId))
        {
            return Unauthorized(new ApiResponse<AlipayOrderDto>
            {
                Success = false,
                Message = "未认证用户"
            });
        }

        _logger.LogInformation("📝 创建支付宝订单: UserId={UserId}, Type={Type}",
            userContext.UserId, request.OrderType);

        try
        {
            // 生成订单号
            var outTradeNo = $"GN{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

            // 根据订单类型确定金额和商品名
            var (amount, subject) = request.OrderType switch
            {
                "membership_upgrade" => request.MembershipLevel switch
                {
                    1 => (29.00m, "Go Nomads 探索者会员"),
                    2 => (99.00m, "Go Nomads 旅行家会员"),
                    3 => (299.00m, "Go Nomads 数字游民会员"),
                    _ => (29.00m, "Go Nomads 会员")
                },
                _ => (0m, "Go Nomads 订单")
            };

            if (amount <= 0)
            {
                return BadRequest(new ApiResponse<AlipayOrderDto>
                {
                    Success = false,
                    Message = "无效的订单类型或等级"
                });
            }

            // 使用支付宝服务生成签名后的订单字符串
            var orderString = _alipayService.CreateAppPayOrderString(
                outTradeNo,
                amount,
                subject,
                $"用户 {userContext.UserId} 购买 {subject}"
            );

            var order = new AlipayOrderDto
            {
                OrderId = outTradeNo,
                OrderString = orderString
            };

            return Ok(new ApiResponse<AlipayOrderDto>
            {
                Success = true,
                Message = "支付宝订单创建成功",
                Data = order
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建支付宝订单失败");
            return StatusCode(500, new ApiResponse<AlipayOrderDto>
            {
                Success = false,
                Message = "创建支付宝订单失败"
            });
        }
    }

    /// <summary>
    ///     微信支付回调
    /// </summary>
    [HttpPost("webhooks/wechat")]
    public async Task<IActionResult> WeChatPayWebhook(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📨 收到微信支付 Webhook");

        try
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync(cancellationToken);

            // TODO: 验证微信支付签名
            // TODO: 解析通知内容并更新订单状态

            _logger.LogInformation("微信支付通知: {Body}", body);

            // 返回微信要求的格式
            return Ok(new { code = "SUCCESS", message = "成功" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 处理微信支付 Webhook 失败");
            return Ok(new { code = "FAIL", message = ex.Message });
        }
    }

    /// <summary>
    ///     支付宝支付回调
    /// </summary>
    [HttpPost("webhooks/alipay")]
    public async Task<IActionResult> AlipayWebhook(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📨 收到支付宝 Webhook");

        try
        {
            var form = await Request.ReadFormAsync(cancellationToken);

            // TODO: 验证支付宝签名
            // TODO: 解析通知内容并更新订单状态

            _logger.LogInformation("支付宝通知: TradeNo={TradeNo}, TradeStatus={TradeStatus}",
                form["trade_no"], form["trade_status"]);

            // 返回支付宝要求的格式
            return Content("success");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 处理支付宝 Webhook 失败");
            return Content("fail");
        }
    }
}
