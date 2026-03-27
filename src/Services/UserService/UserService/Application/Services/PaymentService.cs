using UserService.Application.DTOs;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;
using System.Text.Json;

namespace UserService.Application.Services;

/// <summary>
///     支付服务实现
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly ILogger<PaymentService> _logger;
    private readonly IMembershipRepository _membershipRepository;
    private readonly IMembershipPlanRepository _membershipPlanRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentTransactionRepository _transactionRepository;
    private readonly IPayPalService _payPalService;
    private readonly IWeChatPayService _weChatPayService;

    public PaymentService(
        IOrderRepository orderRepository,
        IPaymentTransactionRepository transactionRepository,
        IPayPalService payPalService,
        IWeChatPayService weChatPayService,
        IMembershipRepository membershipRepository,
        IMembershipPlanRepository membershipPlanRepository,
        ILogger<PaymentService> logger)
    {
        _orderRepository = orderRepository;
        _transactionRepository = transactionRepository;
        _payPalService = payPalService;
        _weChatPayService = weChatPayService;
        _membershipRepository = membershipRepository;
        _membershipPlanRepository = membershipPlanRepository;
        _logger = logger;
    }

    public async Task<OrderDto> CreateOrderAsync(
        string userId,
        CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 创建订单: UserId={UserId}, Type={Type}, Level={Level}",
            userId, request.OrderType, request.MembershipLevel);

        // 计算订单金额
        decimal amount;
        string description;

        switch (request.OrderType.ToLower())
        {
            case "membership_upgrade":
            case "membership_renew":
                if (!request.MembershipLevel.HasValue)
                {
                    throw new ArgumentException("会员升级/续费必须指定会员等级");
                }

                // 从数据库获取会员计划价格
                var plan = await _membershipPlanRepository.GetByLevelAsync(request.MembershipLevel.Value, cancellationToken);
                if (plan == null)
                {
                    throw new ArgumentException($"未找到会员等级 {request.MembershipLevel} 的计划");
                }

                // 根据订阅时长计算价格
                var durationDays = request.DurationDays ?? 365;
                if (durationDays >= 365)
                {
                    amount = plan.PriceYearly;
                    description = $"Go Nomads {plan.Name} 年度会员";
                }
                else if (durationDays >= 30)
                {
                    amount = plan.PriceMonthly * (durationDays / 30);
                    description = $"Go Nomads {plan.Name} {durationDays / 30}个月会员";
                }
                else
                {
                    throw new ArgumentException("订阅时长不能少于 30 天");
                }

                break;

            case "moderator_deposit":
                amount = request.DepositAmount ?? 50m;
                description = "Go Nomads 版主保证金";
                break;

            default:
                throw new ArgumentException($"不支持的订单类型: {request.OrderType}");
        }

        // 创建订单
        var order = new Order
        {
            UserId = userId,
            OrderType = request.OrderType.ToLower(),
            Amount = amount,
            TotalAmount = amount, // 设置总金额（与 amount 相同，未来可扩展加入税费等）
            Currency = "USD",
            MembershipLevel = request.MembershipLevel,
            DurationDays = request.DurationDays ?? 365
        };

        order = await _orderRepository.CreateAsync(order, cancellationToken);

        // 创建 PayPal 订单
        var paypalOrder = await _payPalService.CreateOrderAsync(
            amount,
            "USD",
            description,
            order.OrderNumber,
            cancellationToken);

        // 记录外部支付订单 ID，供后续 capture / webhook 查询
        order.SetExternalPaymentOrderId(paypalOrder.OrderId);
        await _orderRepository.UpdateAsync(order, cancellationToken);

        _logger.LogInformation("✅ 订单创建成功: OrderNumber={OrderNumber}, PayPalOrderId={PayPalOrderId}",
            order.OrderNumber, paypalOrder.OrderId);

        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            UserId = order.UserId,
            OrderType = order.OrderType,
            Status = order.Status,
            Amount = order.Amount,
            Currency = order.Currency,
            MembershipLevel = order.MembershipLevel,
            DurationDays = order.DurationDays,
            CreatedAt = order.CreatedAt,
            ExpiredAt = order.ExpiredAt,
            ApprovalUrl = paypalOrder.ApprovalUrl
        };
    }

    public async Task<PaymentResultDto> CapturePaymentAsync(
        string userId,
        CapturePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("💳 确认支付: UserId={UserId}, PayPalOrderId={PayPalOrderId}",
            userId, request.PayPalOrderId);

        // 查找订单
        var order = await _orderRepository.GetByExternalPaymentOrderIdAsync(request.PayPalOrderId, cancellationToken);
        if (order == null)
        {
            return new PaymentResultDto
            {
                Success = false,
                Message = "订单不存在"
            };
        }

        // 验证用户
        if (order.UserId != userId)
        {
            return new PaymentResultDto
            {
                Success = false,
                Message = "无权操作此订单"
            };
        }

        // 检查订单状态
        if (order.IsCompleted)
        {
            return new PaymentResultDto
            {
                Success = true,
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                Status = order.Status,
                Message = "订单已完成"
            };
        }

        if (order.IsExpired)
        {
            order.MarkAsCancelled();
            await _orderRepository.UpdateAsync(order, cancellationToken);

            return new PaymentResultDto
            {
                Success = false,
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                Status = order.Status,
                Message = "订单已过期"
            };
        }

        // 标记为处理中
        order.MarkAsProcessing();
        await _orderRepository.UpdateAsync(order, cancellationToken);

        // 创建交易记录
        var transaction = PaymentTransaction.CreatePayment(order.Id, order.Amount, order.Currency);
        transaction = await _transactionRepository.CreateAsync(transaction, cancellationToken);

        // 调用 PayPal Capture API
        var captureResult = await _payPalService.CapturePaymentAsync(request.PayPalOrderId, cancellationToken);

        if (captureResult.Success)
        {
            // 更新订单状态
            order.MarkAsCompleted(
                captureResult.CaptureId!,
                captureResult.PayerId,
                captureResult.PayerEmail);
            await _orderRepository.UpdateAsync(order, cancellationToken);

            // 更新交易记录
            transaction.MarkAsCompleted(
                captureResult.TransactionId,
                captureResult.CaptureId,
                captureResult.RawResponse);
            await _transactionRepository.UpdateAsync(transaction, cancellationToken);

            // 处理业务逻辑 (升级会员等)
            var membership = await ProcessOrderCompletionAsync(order, cancellationToken);

            _logger.LogInformation("✅ 支付成功: OrderNumber={OrderNumber}", order.OrderNumber);

            return new PaymentResultDto
            {
                Success = true,
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                Status = order.Status,
                Message = "支付成功",
                Membership = membership != null ? MapToMembershipDto(membership) : null
            };
        }
        else
        {
            // 更新订单状态
            order.MarkAsFailed(captureResult.ErrorMessage ?? "支付失败");
            await _orderRepository.UpdateAsync(order, cancellationToken);

            // 更新交易记录
            transaction.MarkAsFailed(
                captureResult.ErrorCode ?? "UNKNOWN",
                captureResult.ErrorMessage ?? "支付失败",
                captureResult.RawResponse);
            await _transactionRepository.UpdateAsync(transaction, cancellationToken);

            _logger.LogWarning("⚠️ 支付失败: OrderNumber={OrderNumber}, Error={Error}",
                order.OrderNumber, captureResult.ErrorMessage);

            return new PaymentResultDto
            {
                Success = false,
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                Status = order.Status,
                Message = captureResult.ErrorMessage ?? "支付失败"
            };
        }
    }

    public async Task<OrderDto?> GetOrderAsync(
        string userId,
        string orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order == null || order.UserId != userId)
        {
            return null;
        }

        return MapToOrderDto(order);
    }

    public async Task<List<OrderDto>> GetUserOrdersAsync(
        string userId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.GetByUserIdAsync(userId, page, pageSize, cancellationToken);
        return orders.Select(MapToOrderDto).ToList();
    }

    public async Task<bool> CancelOrderAsync(
        string userId,
        string orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order == null || order.UserId != userId)
        {
            return false;
        }

        if (!order.IsPending)
        {
            return false;
        }

        order.MarkAsCancelled();
        await _orderRepository.UpdateAsync(order, cancellationToken);

        _logger.LogInformation("✅ 订单已取消: OrderNumber={OrderNumber}", order.OrderNumber);
        return true;
    }

    public async Task HandleWebhookAsync(
        string eventType,
        string resourceId,
        string rawBody,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📨 处理 PayPal Webhook: EventType={EventType}, ResourceId={ResourceId}",
            eventType, resourceId);

        switch (eventType)
        {
            case "CHECKOUT.ORDER.APPROVED":
                // 订单已批准，等待 capture
                _logger.LogInformation("PayPal 订单已批准: {ResourceId}", resourceId);
                break;

            case "PAYMENT.CAPTURE.COMPLETED":
                // 支付已完成
                var order = await _orderRepository.GetByExternalPaymentOrderIdAsync(resourceId, cancellationToken);
                if (order != null && !order.IsCompleted)
                {
                    // 如果订单还没完成，这里处理
                    _logger.LogInformation("Webhook: 支付已完成: {OrderNumber}", order.OrderNumber);
                }
                break;

            case "PAYMENT.CAPTURE.DENIED":
            case "PAYMENT.CAPTURE.REFUNDED":
                _logger.LogWarning("PayPal 支付被拒绝或退款: {ResourceId}", resourceId);
                break;

            default:
                _logger.LogDebug("忽略 Webhook 事件: {EventType}", eventType);
                break;
        }
    }

    public async Task<WeChatPayOrderDto> CreateWeChatPayOrderAsync(
        string userId,
        CreateWeChatPayOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var (amountCents, description) = ResolveWeChatOrderPricing(request);
        if (amountCents <= 0)
        {
            throw new ArgumentException("无效的订单类型或等级");
        }

        var order = new Order
        {
            UserId = userId,
            OrderType = request.OrderType.ToLowerInvariant(),
            Amount = amountCents / 100m,
            TotalAmount = amountCents / 100m,
            Currency = "CNY",
            MembershipLevel = request.MembershipLevel,
            DurationDays = request.DurationDays ?? 365
        };

        order = await _orderRepository.CreateAsync(order, cancellationToken);

        var transaction = PaymentTransaction.CreatePayment(order.Id, order.Amount, order.Currency, "wechatpay");
        transaction = await _transactionRepository.CreateAsync(transaction, cancellationToken);

        try
        {
            var orderResult = await _weChatPayService.CreateAppOrderAsync(
                order.OrderNumber,
                description,
                amountCents,
                cancellationToken);

            var appParams = _weChatPayService.GenerateAppPayParams(orderResult.PrepayId);

            _logger.LogInformation("✅ 微信支付订单创建成功: OrderId={OrderId}, OrderNumber={OrderNumber}",
                order.Id, order.OrderNumber);

            return new WeChatPayOrderDto
            {
                OrderId = order.Id,
                AppId = appParams.AppId,
                PartnerId = appParams.PartnerId,
                PrepayId = appParams.PrepayId,
                Package = appParams.Package,
                NonceStr = appParams.NonceStr,
                Timestamp = appParams.Timestamp,
                Sign = appParams.Sign
            };
        }
        catch (Exception ex)
        {
            order.MarkAsFailed(ex.Message);
            await _orderRepository.UpdateAsync(order, cancellationToken);

            transaction.MarkAsFailed("WECHAT_CREATE_FAILED", ex.Message);
            await _transactionRepository.UpdateAsync(transaction, cancellationToken);

            throw;
        }
    }

    public async Task<PaymentResultDto> ConfirmWeChatPaymentAsync(
        string userId,
        string orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order == null)
        {
            return new PaymentResultDto
            {
                Success = false,
                Message = "订单不存在"
            };
        }

        if (order.UserId != userId)
        {
            return new PaymentResultDto
            {
                Success = false,
                Message = "无权操作此订单"
            };
        }

        if (order.IsCompleted)
        {
            return new PaymentResultDto
            {
                Success = true,
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                Status = order.Status,
                Message = "订单已完成"
            };
        }

        if (order.IsExpired)
        {
            order.MarkAsCancelled();
            await _orderRepository.UpdateAsync(order, cancellationToken);

            return new PaymentResultDto
            {
                Success = false,
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                Status = order.Status,
                Message = "订单已过期"
            };
        }

        var queryResult = await _weChatPayService.QueryOrderAsync(order.OrderNumber, cancellationToken);

        if (queryResult.TradeState == "SUCCESS")
        {
            return await CompleteExternalPaymentAsync(
                order,
                "wechatpay",
                queryResult.TransactionId,
                queryResult.RawResponse,
                cancellationToken);
        }

        if (queryResult.TradeState == "CLOSED")
        {
            order.MarkAsCancelled();
            await _orderRepository.UpdateAsync(order, cancellationToken);

            return new PaymentResultDto
            {
                Success = false,
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                Status = order.Status,
                Message = "订单已关闭"
            };
        }

        if (queryResult.TradeState == "PAYERROR")
        {
            await FailExternalPaymentAsync(
                order,
                "wechatpay",
                queryResult.ErrorMessage ?? "微信支付失败",
                queryResult.RawResponse,
                cancellationToken);

            return new PaymentResultDto
            {
                Success = false,
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                Status = order.Status,
                Message = queryResult.ErrorMessage ?? "支付失败"
            };
        }

        if (!string.IsNullOrWhiteSpace(queryResult.ErrorMessage))
        {
            return new PaymentResultDto
            {
                Success = false,
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                Status = order.Status,
                Message = queryResult.ErrorMessage
            };
        }

        return new PaymentResultDto
        {
            Success = false,
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status,
            Message = queryResult.TradeState switch
            {
                "USERPAYING" => "支付处理中",
                "NOTPAY" => "订单待支付",
                "CLOSED" => "订单已关闭",
                "PAYERROR" => "支付失败",
                _ => $"当前支付状态: {queryResult.TradeState}"
            }
        };
    }

    public async Task<PaymentResultDto> HandleWeChatWebhookAsync(
        string outTradeNo,
        string? transactionId,
        string rawBody,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByOrderNumberAsync(outTradeNo, cancellationToken);
        if (order == null)
        {
            _logger.LogWarning("⚠️ 微信支付回调未找到订单: OutTradeNo={OutTradeNo}", outTradeNo);
            return new PaymentResultDto
            {
                Success = false,
                OrderNumber = outTradeNo,
                Message = "订单不存在"
            };
        }

        return await CompleteExternalPaymentAsync(order, "wechatpay", transactionId, rawBody, cancellationToken);
    }

    #region 私有方法

    private static (int AmountCents, string Description) ResolveWeChatOrderPricing(CreateWeChatPayOrderRequest request)
    {
        return request.OrderType.ToLowerInvariant() switch
        {
            "membership_upgrade" or "membership_renew" => request.MembershipLevel switch
            {
                1 => (2900, "Go Nomads 探索者会员"),
                2 => (9900, "Go Nomads 旅行家会员"),
                3 => (29900, "Go Nomads 数字游民会员"),
                _ => (0, "Go Nomads 会员")
            },
            "moderator_deposit" => ((int)Math.Round((request.DepositAmount ?? 50m) * 100m), "Go Nomads 版主保证金"),
            _ => (0, "Go Nomads 订单")
        };
    }

    private async Task<PaymentResultDto> CompleteExternalPaymentAsync(
        Order order,
        string paymentMethod,
        string? transactionId,
        string? rawResponse,
        CancellationToken cancellationToken)
    {
        if (order.IsCompleted)
        {
            return new PaymentResultDto
            {
                Success = true,
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                Status = order.Status,
                Message = "订单已完成"
            };
        }

        var transactions = await _transactionRepository.GetByOrderIdAsync(order.Id, cancellationToken);
        var transaction = transactions.FirstOrDefault(t =>
                              t.TransactionType == "payment" &&
                              t.PaymentMethod == paymentMethod) ??
                          PaymentTransaction.CreatePayment(order.Id, order.Amount, order.Currency, paymentMethod);

        if (string.IsNullOrEmpty(transaction.Id))
        {
            transaction = await _transactionRepository.CreateAsync(transaction, cancellationToken);
        }

        order.MarkAsCompleted(transactionId);
        await _orderRepository.UpdateAsync(order, cancellationToken);

        transaction.MarkAsCompleted(transactionId, null, rawResponse ?? JsonSerializer.Serialize(new
        {
            order.OrderNumber,
            paymentMethod,
            transactionId
        }));

        if (transactions.All(t => t.Id != transaction.Id))
        {
            transaction = await _transactionRepository.CreateAsync(transaction, cancellationToken);
        }
        else
        {
            transaction = await _transactionRepository.UpdateAsync(transaction, cancellationToken);
        }

        var membership = await ProcessOrderCompletionAsync(order, cancellationToken);

        _logger.LogInformation("✅ 外部支付完成: OrderNumber={OrderNumber}, Method={PaymentMethod}, TransactionId={TransactionId}",
            order.OrderNumber, paymentMethod, transactionId);

        return new PaymentResultDto
        {
            Success = true,
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status,
            Message = "支付成功",
            Membership = membership != null ? MapToMembershipDto(membership) : null
        };
    }

    private async Task FailExternalPaymentAsync(
        Order order,
        string paymentMethod,
        string errorMessage,
        string? rawResponse,
        CancellationToken cancellationToken)
    {
        order.MarkAsFailed(errorMessage);
        await _orderRepository.UpdateAsync(order, cancellationToken);

        var transactions = await _transactionRepository.GetByOrderIdAsync(order.Id, cancellationToken);
        var transaction = transactions.FirstOrDefault(t =>
                              t.TransactionType == "payment" &&
                              t.PaymentMethod == paymentMethod) ??
                          PaymentTransaction.CreatePayment(order.Id, order.Amount, order.Currency, paymentMethod);

        transaction.MarkAsFailed("PAYMENT_FAILED", errorMessage, rawResponse);

        if (transactions.All(t => t.Id != transaction.Id))
        {
            await _transactionRepository.CreateAsync(transaction, cancellationToken);
        }
        else
        {
            await _transactionRepository.UpdateAsync(transaction, cancellationToken);
        }
    }

    private async Task<Membership?> ProcessOrderCompletionAsync(Order order, CancellationToken cancellationToken)
    {
        switch (order.OrderType.ToLower())
        {
            case "membership_upgrade":
            case "membership_renew":
                return await UpgradeMembershipAsync(order, cancellationToken);

            case "moderator_deposit":
                await ProcessModeratorDepositAsync(order, cancellationToken);
                return null;

            default:
                return null;
        }
    }

    private async Task<Membership?> UpgradeMembershipAsync(Order order, CancellationToken cancellationToken)
    {
        if (!order.MembershipLevel.HasValue) return null;

        var membership = await _membershipRepository.GetByUserIdAsync(order.UserId, cancellationToken);

        if (membership == null)
        {
            // 创建新会员记录
            membership = Membership.Create(
                order.UserId,
                (MembershipLevel)order.MembershipLevel.Value,
                order.DurationDays ?? 365);
            membership = await _membershipRepository.CreateAsync(membership, cancellationToken);
        }
        else
        {
            // 升级或续费
            if (order.OrderType == "membership_renew" && membership.Level == order.MembershipLevel.Value)
            {
                membership.Renew(order.DurationDays ?? 365);
            }
            else
            {
                membership.Upgrade((MembershipLevel)order.MembershipLevel.Value, order.DurationDays ?? 365);
            }
            membership = await _membershipRepository.UpdateAsync(membership, cancellationToken);
        }

        _logger.LogInformation("✅ 会员已升级: UserId={UserId}, Level={Level}",
            order.UserId, order.MembershipLevel);

        return membership;
    }

    private async Task ProcessModeratorDepositAsync(Order order, CancellationToken cancellationToken)
    {
        var membership = await _membershipRepository.GetByUserIdAsync(order.UserId, cancellationToken);
        if (membership != null)
        {
            membership.PayDeposit(order.Amount);
            await _membershipRepository.UpdateAsync(membership, cancellationToken);

            _logger.LogInformation("✅ 版主保证金已支付: UserId={UserId}, Amount={Amount}",
                order.UserId, order.Amount);
        }
    }

    private static OrderDto MapToOrderDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            UserId = order.UserId,
            OrderType = order.OrderType,
            Status = order.Status,
            Amount = order.Amount,
            Currency = order.Currency,
            MembershipLevel = order.MembershipLevel,
            DurationDays = order.DurationDays,
            CreatedAt = order.CreatedAt,
            CompletedAt = order.CompletedAt,
            ExpiredAt = order.ExpiredAt
        };
    }

    private static MembershipResponse MapToMembershipDto(Membership membership)
    {
        return MembershipResponse.FromEntity(membership);
    }

    #endregion
}
