using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UserService.Infrastructure.Configuration;

namespace UserService.Application.Services;

/// <summary>
/// 微信支付 V3 服务接口
/// </summary>
public interface IWeChatPayService
{
    /// <summary>
    /// 调用微信 V3 APP 下单接口，返回调起支付所需参数
    /// </summary>
    Task<WeChatAppPayResult> CreateAppPayOrderAsync(string outTradeNo, int totalAmountCents, string description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证并解密微信支付回调通知
    /// </summary>
    WeChatNotifyResult? VerifyAndDecryptNotify(string serialNumber, string timestamp, string nonce,
        string signature, string body);
}

/// <summary>
/// APP 支付下单结果
/// </summary>
public class WeChatAppPayResult
{
    public bool Success { get; set; }
    public string? PrepayId { get; set; }
    public string? AppId { get; set; }
    public string? PartnerId { get; set; }
    public string? Package { get; set; }
    public string? NonceStr { get; set; }
    public int Timestamp { get; set; }
    public string? Sign { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 回调通知解密结果
/// </summary>
public class WeChatNotifyResult
{
    public string? OutTradeNo { get; set; }
    public string? TransactionId { get; set; }
    public string? TradeState { get; set; }
}

/// <summary>
/// 微信支付 V3 服务实现
/// </summary>
public class WeChatPayService : IWeChatPayService
{
    private const string BaseUrl = "https://api.mch.weixin.qq.com";
    private readonly HttpClient _httpClient;
    private readonly ILogger<WeChatPayService> _logger;
    private readonly WeChatPaySettings _settings;
    private readonly string _privateKeyPem;

    public WeChatPayService(HttpClient httpClient, IOptions<WeChatPaySettings> settings,
        ILogger<WeChatPayService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        _privateKeyPem = _settings.GetPrivateKeyContent();
        if (string.IsNullOrEmpty(_privateKeyPem))
            throw new InvalidOperationException("WeChatPay:PrivateKey is not configured or could not be loaded");

        _logger.LogInformation("✅ 已加载微信支付商户私钥");
    }

    /// <inheritdoc />
    public async Task<WeChatAppPayResult> CreateAppPayOrderAsync(string outTradeNo, int totalAmountCents,
        string description, CancellationToken cancellationToken = default)
    {
        const string apiPath = "/v3/pay/transactions/app";

        var requestBody = new
        {
            appid = _settings.AppId,
            mchid = _settings.MchId,
            description,
            out_trade_no = outTradeNo,
            notify_url = _settings.NotifyUrl,
            amount = new { total = totalAmountCents, currency = "CNY" }
        };

        var jsonBody = JsonSerializer.Serialize(requestBody);

        // 构造签名并发送请求
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var nonceStr = Guid.NewGuid().ToString("N");

        var signMessage = $"POST\n{apiPath}\n{timestamp}\n{nonceStr}\n{jsonBody}\n";
        var signature = SignWithPrivateKey(signMessage);

        var authHeader =
            $"WECHATPAY2-SHA256-RSA2048 mchid=\"{_settings.MchId}\",nonce_str=\"{nonceStr}\"," +
            $"signature=\"{signature}\",timestamp=\"{timestamp}\"," +
            $"serial_no=\"{_settings.CertificateSerialNumber}\"";

        var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}{apiPath}")
        {
            Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("Authorization", authHeader);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");

        _logger.LogInformation("📤 微信支付 V3 下单: OutTradeNo={OutTradeNo}, Amount={Amount}分",
            outTradeNo, totalAmountCents);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("❌ 微信支付下单失败: Status={Status}, Body={Body}",
                response.StatusCode, responseBody);
            return new WeChatAppPayResult { Success = false, ErrorMessage = responseBody };
        }

        // 解析 prepay_id
        using var doc = JsonDocument.Parse(responseBody);
        var prepayId = doc.RootElement.GetProperty("prepay_id").GetString()!;

        // 构造 APP 调起支付所需的签名
        var appTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var appNonceStr = Guid.NewGuid().ToString("N");
        var appSignMessage = $"{_settings.AppId}\n{appTimestamp}\n{appNonceStr}\n{prepayId}\n";
        var appSign = SignWithPrivateKey(appSignMessage);

        _logger.LogInformation("✅ 微信支付下单成功: PrepayId={PrepayId}", prepayId);

        return new WeChatAppPayResult
        {
            Success = true,
            AppId = _settings.AppId,
            PartnerId = _settings.MchId,
            PrepayId = prepayId,
            Package = "Sign=WXPay",
            NonceStr = appNonceStr,
            Timestamp = (int)appTimestamp,
            Sign = appSign
        };
    }

    /// <inheritdoc />
    public WeChatNotifyResult? VerifyAndDecryptNotify(string serialNumber, string timestamp, string nonce,
        string signature, string body)
    {
        // TODO: 验证微信平台证书签名（需要先获取并缓存平台证书）
        // 生产环境应严格验证签名，此处先解密 resource 字段

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            var resource = root.GetProperty("resource");
            var ciphertext = resource.GetProperty("ciphertext").GetString()!;
            var associatedData = resource.GetProperty("associated_data").GetString() ?? "";
            var resourceNonce = resource.GetProperty("nonce").GetString()!;

            var plaintext = AesGcmDecrypt(ciphertext, resourceNonce, associatedData);
            using var plainDoc = JsonDocument.Parse(plaintext);
            var plain = plainDoc.RootElement;

            return new WeChatNotifyResult
            {
                OutTradeNo = plain.GetProperty("out_trade_no").GetString(),
                TransactionId = plain.GetProperty("transaction_id").GetString(),
                TradeState = plain.GetProperty("trade_state").GetString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 微信支付回调解密失败");
            return null;
        }
    }

    /// <summary>
    /// 使用商户私钥进行 RSA-SHA256 签名
    /// </summary>
    private string SignWithPrivateKey(string message)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(_privateKeyPem);
        var signatureBytes = rsa.SignData(Encoding.UTF8.GetBytes(message), HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        return Convert.ToBase64String(signatureBytes);
    }

    /// <summary>
    /// AES-256-GCM 解密微信回调通知中的 resource
    /// </summary>
    private string AesGcmDecrypt(string ciphertextBase64, string nonce, string associatedData)
    {
        var ciphertextWithTag = Convert.FromBase64String(ciphertextBase64);
        var keyBytes = Encoding.UTF8.GetBytes(_settings.ApiV3Key);
        var nonceBytes = Encoding.UTF8.GetBytes(nonce);
        var associatedDataBytes = Encoding.UTF8.GetBytes(associatedData);

        // 最后 16 字节是 GCM Tag
        const int tagSize = 16;
        var ciphertext = ciphertextWithTag.AsSpan(0, ciphertextWithTag.Length - tagSize);
        var tag = ciphertextWithTag.AsSpan(ciphertextWithTag.Length - tagSize, tagSize);

        var plaintext = new byte[ciphertext.Length];
        using var aesGcm = new AesGcm(keyBytes, tagSize);
        aesGcm.Decrypt(nonceBytes, ciphertext, tag, plaintext, associatedDataBytes);

        return Encoding.UTF8.GetString(plaintext);
    }
}
