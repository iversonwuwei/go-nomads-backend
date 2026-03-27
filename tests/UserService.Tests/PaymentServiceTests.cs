using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UserService.Application.DTOs;
using UserService.Application.Services;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;
using Xunit;

namespace UserService.Tests;

public class PaymentServiceTests
{
    [Fact]
    public async Task CreateWeChatPayOrderAsync_PersistsOrderAndUsesOrderNumberAsOutTradeNo()
    {
        var orderRepository = new Mock<IOrderRepository>();
        var transactionRepository = new Mock<IPaymentTransactionRepository>();
        var payPalService = new Mock<IPayPalService>();
        var weChatPayService = new Mock<IWeChatPayService>();
        var membershipRepository = new Mock<IMembershipRepository>();
        var membershipPlanRepository = new Mock<IMembershipPlanRepository>();

        string? createdOrderNumber = null;

        orderRepository
            .Setup(repository => repository.CreateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order order, CancellationToken _) =>
            {
                order.Id = "order-1";
                createdOrderNumber = order.OrderNumber;
                return order;
            });

        transactionRepository
            .Setup(repository => repository.CreateAsync(It.IsAny<PaymentTransaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentTransaction transaction, CancellationToken _) => transaction);

        weChatPayService
            .Setup(service => service.CreateAppOrderAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WeChatPayAppOrderResponse { PrepayId = "prepay-1" });

        weChatPayService
            .Setup(service => service.GenerateAppPayParams("prepay-1"))
            .Returns(new WeChatPayAppParams
            {
                AppId = "app-id",
                PartnerId = "partner-id",
                PrepayId = "prepay-1",
                Package = "Sign=WXPay",
                NonceStr = "nonce",
                Timestamp = 123,
                Sign = "sign"
            });

        var service = CreatePaymentService(
            orderRepository,
            transactionRepository,
            payPalService,
            weChatPayService,
            membershipRepository,
            membershipPlanRepository);

        var result = await service.CreateWeChatPayOrderAsync(
            "user-1",
            new CreateWeChatPayOrderRequest { OrderType = "membership_upgrade", MembershipLevel = 1 });

        Assert.Equal("order-1", result.OrderId);
        Assert.Equal("prepay-1", result.PrepayId);

        weChatPayService.Verify(service => service.CreateAppOrderAsync(
            createdOrderNumber!,
            "Go Nomads 探索者会员",
            2900,
            It.IsAny<CancellationToken>()), Times.Once);

        transactionRepository.Verify(repository => repository.CreateAsync(
            It.Is<PaymentTransaction>(transaction =>
                transaction.OrderId == "order-1" &&
                transaction.PaymentMethod == "wechatpay" &&
                transaction.Currency == "CNY"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmWeChatPaymentAsync_WhenTradeStateClosed_CancelsOrder()
    {
        var order = new Order
        {
            Id = "order-closed",
            UserId = "user-1",
            Status = "pending"
        };

        var orderRepository = new Mock<IOrderRepository>();
        var transactionRepository = new Mock<IPaymentTransactionRepository>();
        var payPalService = new Mock<IPayPalService>();
        var weChatPayService = new Mock<IWeChatPayService>();
        var membershipRepository = new Mock<IMembershipRepository>();
        var membershipPlanRepository = new Mock<IMembershipPlanRepository>();

        orderRepository.Setup(repository => repository.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        orderRepository.Setup(repository => repository.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>())).ReturnsAsync((Order o, CancellationToken _) => o);
        weChatPayService.Setup(service => service.QueryOrderAsync(order.OrderNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WeChatPayQueryResult { TradeState = "CLOSED" });

        var service = CreatePaymentService(
            orderRepository,
            transactionRepository,
            payPalService,
            weChatPayService,
            membershipRepository,
            membershipPlanRepository);

        var result = await service.ConfirmWeChatPaymentAsync("user-1", order.Id);

        Assert.False(result.Success);
        Assert.Equal("cancelled", order.Status);

        orderRepository.Verify(repository => repository.UpdateAsync(
            It.Is<Order>(updated => updated.Id == order.Id && updated.Status == "cancelled"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmWeChatPaymentAsync_WhenTradeStatePayError_FailsOrderAndTransaction()
    {
        var order = new Order
        {
            Id = "order-failed",
            UserId = "user-1",
            Status = "pending"
        };

        var existingTransaction = PaymentTransaction.CreatePayment(order.Id, 29m, "CNY", "wechatpay");

        var orderRepository = new Mock<IOrderRepository>();
        var transactionRepository = new Mock<IPaymentTransactionRepository>();
        var payPalService = new Mock<IPayPalService>();
        var weChatPayService = new Mock<IWeChatPayService>();
        var membershipRepository = new Mock<IMembershipRepository>();
        var membershipPlanRepository = new Mock<IMembershipPlanRepository>();

        orderRepository.Setup(repository => repository.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        orderRepository.Setup(repository => repository.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>())).ReturnsAsync((Order o, CancellationToken _) => o);
        transactionRepository.Setup(repository => repository.GetByOrderIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync([existingTransaction]);
        transactionRepository.Setup(repository => repository.UpdateAsync(It.IsAny<PaymentTransaction>(), It.IsAny<CancellationToken>())).ReturnsAsync((PaymentTransaction t, CancellationToken _) => t);
        weChatPayService.Setup(service => service.QueryOrderAsync(order.OrderNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WeChatPayQueryResult
            {
                TradeState = "PAYERROR",
                ErrorMessage = "bank failed",
                RawResponse = "{}"
            });

        var service = CreatePaymentService(
            orderRepository,
            transactionRepository,
            payPalService,
            weChatPayService,
            membershipRepository,
            membershipPlanRepository);

        var result = await service.ConfirmWeChatPaymentAsync("user-1", order.Id);

        Assert.False(result.Success);
        Assert.Equal("failed", order.Status);
        Assert.Equal("failed", existingTransaction.Status);
        Assert.Equal("PAYMENT_FAILED", existingTransaction.ErrorCode);
    }

    [Fact]
    public async Task ConfirmWeChatPaymentAsync_WhenTradeStateSuccess_CompletesOrderAndCreatesMembership()
    {
        var order = new Order
        {
            Id = "order-success",
            UserId = "user-1",
            OrderType = "membership_upgrade",
            Status = "pending",
            MembershipLevel = (int)MembershipLevel.Basic,
            DurationDays = 365,
            Amount = 29m,
            Currency = "CNY"
        };

        var existingTransaction = PaymentTransaction.CreatePayment(order.Id, 29m, "CNY", "wechatpay");

        var orderRepository = new Mock<IOrderRepository>();
        var transactionRepository = new Mock<IPaymentTransactionRepository>();
        var payPalService = new Mock<IPayPalService>();
        var weChatPayService = new Mock<IWeChatPayService>();
        var membershipRepository = new Mock<IMembershipRepository>();
        var membershipPlanRepository = new Mock<IMembershipPlanRepository>();

        orderRepository.Setup(repository => repository.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        orderRepository.Setup(repository => repository.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>())).ReturnsAsync((Order o, CancellationToken _) => o);
        transactionRepository.Setup(repository => repository.GetByOrderIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync([existingTransaction]);
        transactionRepository.Setup(repository => repository.UpdateAsync(It.IsAny<PaymentTransaction>(), It.IsAny<CancellationToken>())).ReturnsAsync((PaymentTransaction t, CancellationToken _) => t);
        weChatPayService.Setup(service => service.QueryOrderAsync(order.OrderNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WeChatPayQueryResult
            {
                TradeState = "SUCCESS",
                TransactionId = "wx-transaction-1",
                RawResponse = "{\"trade_state\":\"SUCCESS\"}"
            });
        membershipRepository.Setup(repository => repository.GetByUserIdAsync(order.UserId, It.IsAny<CancellationToken>())).ReturnsAsync((Membership?)null);
        membershipRepository.Setup(repository => repository.CreateAsync(It.IsAny<Membership>(), It.IsAny<CancellationToken>())).ReturnsAsync((Membership membership, CancellationToken _) => membership);

        var service = CreatePaymentService(
            orderRepository,
            transactionRepository,
            payPalService,
            weChatPayService,
            membershipRepository,
            membershipPlanRepository);

        var result = await service.ConfirmWeChatPaymentAsync("user-1", order.Id);

        Assert.True(result.Success);
        Assert.Equal("completed", order.Status);
        Assert.Equal("wx-transaction-1", order.ExternalPaymentId);
        Assert.Equal("completed", existingTransaction.Status);
        Assert.Equal("wx-transaction-1", existingTransaction.ExternalTransactionId);
        membershipRepository.Verify(repository => repository.CreateAsync(It.IsAny<Membership>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleWeChatWebhookAsync_WhenOrderMissing_ReturnsFailure()
    {
        var orderRepository = new Mock<IOrderRepository>();
        var transactionRepository = new Mock<IPaymentTransactionRepository>();
        var payPalService = new Mock<IPayPalService>();
        var weChatPayService = new Mock<IWeChatPayService>();
        var membershipRepository = new Mock<IMembershipRepository>();
        var membershipPlanRepository = new Mock<IMembershipPlanRepository>();

        orderRepository
            .Setup(repository => repository.GetByOrderNumberAsync("missing-order", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var service = CreatePaymentService(
            orderRepository,
            transactionRepository,
            payPalService,
            weChatPayService,
            membershipRepository,
            membershipPlanRepository);

        var result = await service.HandleWeChatWebhookAsync("missing-order", "wx-1", "{}");

        Assert.False(result.Success);
        Assert.Equal("订单不存在", result.Message);
    }

    [Fact]
    public async Task HandleWeChatWebhookAsync_WhenOrderAlreadyCompleted_IsIdempotent()
    {
        var order = new Order
        {
            Id = "order-completed",
            UserId = "user-1",
            Status = "completed"
        };

        var orderRepository = new Mock<IOrderRepository>();
        var transactionRepository = new Mock<IPaymentTransactionRepository>();
        var payPalService = new Mock<IPayPalService>();
        var weChatPayService = new Mock<IWeChatPayService>();
        var membershipRepository = new Mock<IMembershipRepository>();
        var membershipPlanRepository = new Mock<IMembershipPlanRepository>();

        orderRepository
            .Setup(repository => repository.GetByOrderNumberAsync(order.OrderNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var service = CreatePaymentService(
            orderRepository,
            transactionRepository,
            payPalService,
            weChatPayService,
            membershipRepository,
            membershipPlanRepository);

        var result = await service.HandleWeChatWebhookAsync(order.OrderNumber, "wx-transaction", "{}");

        Assert.True(result.Success);
        Assert.Equal("订单已完成", result.Message);
        orderRepository.Verify(repository => repository.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        transactionRepository.Verify(repository => repository.UpdateAsync(It.IsAny<PaymentTransaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static PaymentService CreatePaymentService(
        Mock<IOrderRepository> orderRepository,
        Mock<IPaymentTransactionRepository> transactionRepository,
        Mock<IPayPalService> payPalService,
        Mock<IWeChatPayService> weChatPayService,
        Mock<IMembershipRepository> membershipRepository,
        Mock<IMembershipPlanRepository> membershipPlanRepository)
    {
        return new PaymentService(
            orderRepository.Object,
            transactionRepository.Object,
            payPalService.Object,
            weChatPayService.Object,
            membershipRepository.Object,
            membershipPlanRepository.Object,
            NullLogger<PaymentService>.Instance);
    }
}