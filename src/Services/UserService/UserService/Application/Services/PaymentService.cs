using UserService.Application.DTOs;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

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
        var (amount, currency, description) = await CalculateOrderAmountAsync(
            request.OrderType, request.MembershipLevel, request.DurationDays, request.DepositAmount, cancellationToken);

        // 创建订单
        var order = new Order
        {
            UserId = userId,
            OrderType = request.OrderType.ToLower(),
            Amount = amount,
            TotalAmount = amount,
            Currency = currency,
            PaymentMethod = "paypal",
            MembershipLevel = request.MembershipLevel,
            DurationDays = request.DurationDays ?? 365
        };

        order = await _orderRepository.CreateAsync(order, cancellationToken);

        // 创建 PayPal 订单
        var paypalOrder = await _payPalService.CreateOrderAsync(
            amount,
            currency,
            description,
            order.OrderNumber,
            cancellationToken);

        // 更新订单的 PayPal 订单 ID
        order.SetPayPalOrderId(paypalOrder.OrderId);
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
        var order = await _orderRepository.GetByPayPalOrderIdAsync(request.PayPalOrderId, cancellationToken);
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
                var order = await _orderRepository.GetByPayPalOrderIdAsync(resourceId, cancellationToken);
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

    #region 私有方法

    /// <summary>
    ///     根据订单请求计算金额（支持 PayPal 和微信支付）
    /// </summary>
    private async Task<(decimal amount, string currency, string description)> CalculateOrderAmountAsync(
        string orderType, int? membershipLevel, int? durationDays, decimal? depositAmount,
        CancellationToken cancellationToken)
    {
        switch (orderType.ToLower())
        {
            case "membership_upgrade":
            case "membership_renew":
                if (!membershipLevel.HasValue)
                    throw new ArgumentException("会员升级/续费必须指定会员等级");

                var plan = await _membershipPlanRepository.GetByLevelAsync(membershipLevel.Value, cancellationToken);
                if (plan == null)
                    throw new ArgumentException($"未找到会员等级 {membershipLevel} 的计划");

                // 根据时长选择月付或年付价格
                var isMonthly = durationDays.HasValue && durationDays.Value <= 31;
                var price = isMonthly ? plan.PriceMonthly : plan.PriceYearly;
                var periodLabel = isMonthly ? "月" : "年";
                return (price, plan.Currency, $"Go Nomads {plan.Name} 会员（{periodLabel}付）");

            case "moderator_deposit":
                return (depositAmount ?? 50m, "CNY", "Go Nomads 版主保证金");

            default:
                throw new ArgumentException($"不支持的订单类型: {orderType}");
        }
    }

    public async Task<WeChatPayOrderDto> CreateWeChatPayOrderAsync(
        string userId,
        CreateWeChatPayOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 创建微信支付订单: UserId={UserId}, Type={Type}, Level={Level}",
            userId, request.OrderType, request.MembershipLevel);

        // 计算订单金额
        var (amount, currency, description) = await CalculateOrderAmountAsync(
            request.OrderType, request.MembershipLevel, request.DurationDays, request.DepositAmount, cancellationToken);

        // 创建订单实体
        var order = new Order
        {
            UserId = userId,
            OrderType = request.OrderType.ToLower(),
            Amount = amount,
            TotalAmount = amount,
            Currency = currency,
            PaymentMethod = "wechat",
            MembershipLevel = request.MembershipLevel,
            DurationDays = request.DurationDays ?? 365
        };

        order = await _orderRepository.CreateAsync(order, cancellationToken);

        // 微信支付金额单位为分
        var totalInCents = (int)(amount * 100);

        // 调用微信支付统一下单 API
        var prepayResult = await _weChatPayService.CreateAppOrderAsync(
            order.OrderNumber, description, totalInCents, cancellationToken);

        // 更新订单的预支付 ID
        order.SetWeChatPrepayId(prepayResult.PrepayId);
        await _orderRepository.UpdateAsync(order, cancellationToken);

        // 生成 APP 调起支付的签名参数
        var appParams = _weChatPayService.GenerateAppPayParams(prepayResult.PrepayId);

        _logger.LogInformation("✅ 微信支付订单创建成功: OrderNumber={OrderNumber}, PrepayId={PrepayId}",
            order.OrderNumber, prepayResult.PrepayId);

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

    public async Task<PaymentResultDto> ConfirmWeChatPaymentAsync(
        string userId,
        string orderId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 确认微信支付结果: UserId={UserId}, OrderId={OrderId}", userId, orderId);

        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order == null || order.UserId != userId)
        {
            return new PaymentResultDto { Success = false, Message = "订单不存在" };
        }

        // 已完成的订单直接返回
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

        // 查询微信支付状态
        var queryResult = await _weChatPayService.QueryOrderAsync(order.OrderNumber, cancellationToken);

        if (queryResult.Success && queryResult.TradeState == "SUCCESS")
        {
            // 支付成功，更新订单状态
            order.MarkAsCompletedByWeChat(queryResult.TransactionId!);
            await _orderRepository.UpdateAsync(order, cancellationToken);

            // 创建交易记录
            var transaction = PaymentTransaction.CreatePayment(order.Id, order.Amount, order.Currency);
            transaction.PaymentMethod = "wechat";
            transaction.MarkAsCompletedByWeChat(queryResult.TransactionId!, queryResult.RawResponse);
            await _transactionRepository.CreateAsync(transaction, cancellationToken);

            // 处理业务逻辑
            var membership = await ProcessOrderCompletionAsync(order, cancellationToken);

            _logger.LogInformation("✅ 微信支付确认成功: OrderNumber={OrderNumber}", order.OrderNumber);

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
        else if (queryResult.TradeState is "NOTPAY" or "USERPAYING")
        {
            return new PaymentResultDto
            {
                Success = false,
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                Status = "pending",
                Message = "支付处理中，请稍后查询"
            };
        }
        else
        {
            _logger.LogWarning("⚠️ 微信支付未成功: TradeState={State}", queryResult.TradeState);
            return new PaymentResultDto
            {
                Success = false,
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                Status = queryResult.TradeState,
                Message = queryResult.ErrorMessage ?? $"支付状态: {queryResult.TradeState}"
            };
        }
    }

    public async Task HandleWeChatPayNotificationAsync(
        string outTradeNo,
        string transactionId,
        DateTimeOffset? successTime,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📨 处理微信支付通知: OutTradeNo={OutTradeNo}, TransactionId={TransactionId}",
            outTradeNo, transactionId);

        var order = await _orderRepository.GetByOrderNumberAsync(outTradeNo, cancellationToken);
        if (order == null)
        {
            _logger.LogWarning("⚠️ 微信支付通知: 未找到订单 {OutTradeNo}", outTradeNo);
            return;
        }

        if (order.IsCompleted)
        {
            _logger.LogInformation("ℹ️ 订单已完成，跳过: {OrderNumber}", order.OrderNumber);
            return;
        }

        // 更新订单状态
        order.MarkAsCompletedByWeChat(transactionId);
        await _orderRepository.UpdateAsync(order, cancellationToken);

        // 创建交易记录
        var transaction = PaymentTransaction.CreatePayment(order.Id, order.Amount, order.Currency);
        transaction.PaymentMethod = "wechat";
        transaction.MarkAsCompletedByWeChat(transactionId);
        await _transactionRepository.CreateAsync(transaction, cancellationToken);

        // 处理业务逻辑
        await ProcessOrderCompletionAsync(order, cancellationToken);

        _logger.LogInformation("✅ 微信支付通知处理完成: OrderNumber={OrderNumber}", order.OrderNumber);
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
