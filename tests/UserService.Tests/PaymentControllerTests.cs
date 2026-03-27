using System.Text;
using GoNomads.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using UserService.API.Controllers;
using UserService.Application.DTOs;
using UserService.Application.Services;
using UserService.Infrastructure.Configuration;
using Xunit;

namespace UserService.Tests;

public class PaymentControllerTests
{
    [Fact]
    public async Task WeChatPayWebhook_WhenSignatureInvalid_ReturnsFailPayload()
    {
        var paymentService = new Mock<IPaymentService>();
        var payPalService = new Mock<IPayPalService>();
        var alipayService = new Mock<IAlipayService>();
        var weChatPayService = new Mock<IWeChatPayService>();

        weChatPayService
            .Setup(service => service.VerifyAndDecryptNotificationAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WeChatPayNotificationResult
            {
                IsValid = false,
                ErrorMessage = "签名验证失败"
            });

        var controller = CreateController(paymentService, payPalService, alipayService, weChatPayService);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = CreateHttpContext("{\"id\":1}")
        };

        var result = await controller.WeChatPayWebhook();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsAssignableFrom<object>(okResult.Value);
        Assert.Contains("FAIL", payload.ToString());
        paymentService.Verify(service => service.HandleWeChatWebhookAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WeChatPayWebhook_WhenPaymentServiceFails_ReturnsFailPayload()
    {
        var paymentService = new Mock<IPaymentService>();
        var payPalService = new Mock<IPayPalService>();
        var alipayService = new Mock<IAlipayService>();
        var weChatPayService = new Mock<IWeChatPayService>();

        weChatPayService
            .Setup(service => service.VerifyAndDecryptNotificationAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WeChatPayNotificationResult
            {
                IsValid = true,
                OutTradeNo = "ORD123",
                TradeState = "SUCCESS",
                TransactionId = "WX123"
            });

        paymentService
            .Setup(service => service.HandleWeChatWebhookAsync("ORD123", "WX123", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentResultDto
            {
                Success = false,
                Message = "订单处理失败"
            });

        var controller = CreateController(paymentService, payPalService, alipayService, weChatPayService);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = CreateHttpContext("{\"event\":\"paid\"}")
        };

        var result = await controller.WeChatPayWebhook();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("FAIL", okResult.Value?.ToString());
    }

    [Fact]
    public async Task WeChatPayWebhook_WhenPaymentSucceeds_ReturnsSuccessPayload()
    {
        var paymentService = new Mock<IPaymentService>();
        var payPalService = new Mock<IPayPalService>();
        var alipayService = new Mock<IAlipayService>();
        var weChatPayService = new Mock<IWeChatPayService>();

        weChatPayService
            .Setup(service => service.VerifyAndDecryptNotificationAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WeChatPayNotificationResult
            {
                IsValid = true,
                OutTradeNo = "ORD123",
                TradeState = "SUCCESS",
                TransactionId = "WX123"
            });

        paymentService
            .Setup(service => service.HandleWeChatWebhookAsync("ORD123", "WX123", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentResultDto
            {
                Success = true,
                OrderNumber = "ORD123",
                Message = "支付成功"
            });

        var controller = CreateController(paymentService, payPalService, alipayService, weChatPayService);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = CreateHttpContext("{\"event\":\"paid\"}")
        };

        var result = await controller.WeChatPayWebhook();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("SUCCESS", okResult.Value?.ToString());
    }

    private static PaymentController CreateController(
        Mock<IPaymentService> paymentService,
        Mock<IPayPalService> payPalService,
        Mock<IAlipayService> alipayService,
        Mock<IWeChatPayService> weChatPayService)
    {
        return new PaymentController(
            paymentService.Object,
            payPalService.Object,
            alipayService.Object,
            weChatPayService.Object,
            Options.Create(new PayPalSettings()),
            NullLogger<PaymentController>.Instance);
    }

    private static HttpContext CreateHttpContext(string body)
    {
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        context.Request.Headers["Wechatpay-Serial"] = "serial";
        context.Request.Headers["Wechatpay-Timestamp"] = "timestamp";
        context.Request.Headers["Wechatpay-Nonce"] = "nonce";
        context.Request.Headers["Wechatpay-Signature"] = "signature";
        return context;
    }
}