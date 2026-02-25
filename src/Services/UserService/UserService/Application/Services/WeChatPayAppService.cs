using System.Text.Json;
using Microsoft.Extensions.Options;
using SKIT.FlurlHttpClient.Wechat.TenpayV3;
using SKIT.FlurlHttpClient.Wechat.TenpayV3.Events;
using SKIT.FlurlHttpClient.Wechat.TenpayV3.Models;
using SKIT.FlurlHttpClient.Wechat.TenpayV3.Settings;
using UserService.Infrastructure.Configuration;

namespace UserService.Application.Services;

/// <summary>
///     微信支付服务实现 — 基于 SKIT.FlurlHttpClient.Wechat.TenpayV3 SDK
/// </summary>
public class WeChatPayAppService : IWeChatPayService
{
    private readonly ILogger<WeChatPayAppService> _logger;
    private readonly WeChatPaySettings _settings;
    private readonly WechatTenpayClient _client;

    public WeChatPayAppService(
        IOptions<WeChatPaySettings> settings,
        ILogger<WeChatPayAppService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        // 校验关键配置
        if (string.IsNullOrWhiteSpace(_settings.MchId))
            _logger.LogError("❌ 微信支付配置缺失: MchId 为空");
        if (string.IsNullOrWhiteSpace(_settings.ApiV3Key))
            _logger.LogError("❌ 微信支付配置缺失: ApiV3Key 为空（请确认 GitHub Secret WECHAT_PAY_API_V3_KEY 已设置）");
        if (string.IsNullOrWhiteSpace(_settings.CertificateSerialNumber))
            _logger.LogError("❌ 微信支付配置缺失: CertificateSerialNumber 为空（请确认 GitHub Secret WECHAT_PAY_CERTIFICATE_SERIAL_NUMBER 已设置）");

        var privateKeyPem = _settings.GetPrivateKeyContent();
        if (string.IsNullOrWhiteSpace(privateKeyPem))
            _logger.LogError("❌ 微信支付配置缺失: PrivateKey 为空（请确认 GitHub Secret WECHAT_PAY_PRIVATE_KEY 已设置）");
        else if (!privateKeyPem.Contains("-----BEGIN"))
            _logger.LogError("❌ 微信支付 PrivateKey 格式异常: 不是有效的 PEM 格式（前20字符: {Prefix}）",
                privateKeyPem[..Math.Min(20, privateKeyPem.Length)]);
        else
            _logger.LogInformation("✅ 微信支付 PrivateKey 加载成功 (长度={Length})", privateKeyPem.Length);

        var certManager = new InMemoryCertificateManager();
        var options = new WechatTenpayClientOptions
        {
            MerchantId = _settings.MchId,
            MerchantV3Secret = _settings.ApiV3Key,
            MerchantCertificateSerialNumber = _settings.CertificateSerialNumber,
            MerchantCertificatePrivateKey = privateKeyPem,
            PlatformCertificateManager = certManager
        };

        _client = new WechatTenpayClient(options);

        _logger.LogInformation("✅ 微信支付服务初始化: MchId={MchId}, AppId={AppId}, CertSerial={CertSerial}",
            _settings.MchId, _settings.AppId,
            string.IsNullOrEmpty(_settings.CertificateSerialNumber) ? "(空)" : _settings.CertificateSerialNumber[..8] + "...");
    }

    /// <inheritdoc />
    public async Task<WeChatPayAppOrderResponse> CreateAppOrderAsync(
        string outTradeNo,
        string description,
        int totalAmountInCents,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "📝 创建微信 APP 支付预订单: OutTradeNo={OutTradeNo}, Amount={Amount}分",
            outTradeNo, totalAmountInCents);

        var request = new CreatePayTransactionAppRequest
        {
            AppId = _settings.AppId,
            MerchantId = _settings.MchId,
            OutTradeNumber = outTradeNo,
            Description = description,
            NotifyUrl = _settings.NotifyUrl,
            Amount = new CreatePayTransactionAppRequest.Types.Amount
            {
                Total = totalAmountInCents,
                Currency = "CNY"
            }
        };

        var response = await _client.ExecuteCreatePayTransactionAppAsync(request, cancellationToken);

        if (!response.IsSuccessful())
        {
            _logger.LogError("❌ 微信支付统一下单失败: Code={Code}, Message={Message}",
                response.ErrorCode, response.ErrorMessage);
            throw new InvalidOperationException(
                $"微信支付统一下单失败: [{response.ErrorCode}] {response.ErrorMessage}");
        }

        _logger.LogInformation("✅ 微信支付预订单创建成功: PrepayId={PrepayId}", response.PrepayId);

        return new WeChatPayAppOrderResponse
        {
            PrepayId = response.PrepayId!
        };
    }

    /// <inheritdoc />
    public WeChatPayAppParams GenerateAppPayParams(string prepayId)
    {
        _logger.LogInformation("🔐 生成 APP 支付调起参数: PrepayId={PrepayId}", prepayId);

        var paramMap = _client.GenerateParametersForAppPayRequest(_settings.AppId, prepayId);

        return new WeChatPayAppParams
        {
            AppId = paramMap["appid"],
            PartnerId = paramMap["partnerid"],
            PrepayId = paramMap["prepayid"],
            Package = paramMap["package"],
            NonceStr = paramMap["noncestr"],
            Timestamp = int.Parse(paramMap["timestamp"]),
            Sign = paramMap["sign"]
        };
    }

    /// <inheritdoc />
    public async Task<WeChatPayQueryResult> QueryOrderAsync(
        string outTradeNo,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 查询微信支付订单: OutTradeNo={OutTradeNo}", outTradeNo);

        var request = new GetPayTransactionByOutTradeNumberRequest
        {
            MerchantId = _settings.MchId,
            OutTradeNumber = outTradeNo
        };

        var response = await _client.ExecuteGetPayTransactionByOutTradeNumberAsync(request, cancellationToken);

        if (!response.IsSuccessful())
        {
            _logger.LogWarning("⚠️ 查询微信支付订单失败: Code={Code}, Message={Message}",
                response.ErrorCode, response.ErrorMessage);
            return new WeChatPayQueryResult
            {
                Success = false,
                ErrorMessage = $"[{response.ErrorCode}] {response.ErrorMessage}"
            };
        }

        var isSuccess = response.TradeState == "SUCCESS";

        _logger.LogInformation("✅ 微信支付订单查询结果: TradeState={State}, TransactionId={TxnId}",
            response.TradeState, response.TransactionId);

        return new WeChatPayQueryResult
        {
            Success = isSuccess,
            TradeState = response.TradeState ?? "",
            TransactionId = response.TransactionId,
            OutTradeNo = response.OutTradeNumber,
            SuccessTime = response.SuccessTime,
            RawResponse = JsonSerializer.Serialize(new
            {
                response.TradeState,
                response.TradeStateDescription,
                response.TransactionId,
                response.OutTradeNumber,
                response.BankType,
                response.SuccessTime
            })
        };
    }

    /// <inheritdoc />
    public async Task<WeChatPayNotificationResult> VerifyAndDecryptNotificationAsync(
        string timestamp,
        string nonce,
        string signature,
        string serialNumber,
        string body,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔒 验证微信支付回调通知签名...");

        try
        {
            // 验证签名
            var isValid = _client.VerifyEventSignature(timestamp, nonce, body, signature, serialNumber);
            if (!isValid)
            {
                // 如果验签失败，尝试刷新平台证书后重试
                _logger.LogWarning("⚠️ 首次验签失败，尝试刷新平台证书后重试...");

                var certResponse = await _client.ExecuteQueryCertificatesAsync(
                    new QueryCertificatesRequest(), cancellationToken);

                if (certResponse.IsSuccessful())
                {
                    // 解密证书响应中的敏感数据
                    certResponse = _client.DecryptResponseSensitiveProperty(certResponse);

                    foreach (var cert in certResponse.CertificateList)
                    {
                        var entry = CertificateEntry.Parse(cert);
                        _client.PlatformCertificateManager.AddEntry(entry);
                    }
                }

                isValid = _client.VerifyEventSignature(timestamp, nonce, body, signature, serialNumber);
                if (!isValid)
                {
                    _logger.LogError("❌ 微信支付回调签名验证失败");
                    return new WeChatPayNotificationResult
                    {
                        IsValid = false,
                        ErrorMessage = "签名验证失败"
                    };
                }
            }

            // 解密通知内容
            var callbackModel = _client.DeserializeEvent(body);
            var resource = _client.DecryptEventResource<TransactionResource>(callbackModel);

            _logger.LogInformation(
                "✅ 微信支付回调解密成功: TradeState={State}, TransactionId={TxnId}, OutTradeNo={OrderNo}",
                resource.TradeState, resource.TransactionId, resource.OutTradeNumber);

            return new WeChatPayNotificationResult
            {
                IsValid = true,
                TradeState = resource.TradeState ?? "",
                TransactionId = resource.TransactionId,
                OutTradeNo = resource.OutTradeNumber,
                SuccessTime = resource.SuccessTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 处理微信支付回调通知失败");
            return new WeChatPayNotificationResult
            {
                IsValid = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
