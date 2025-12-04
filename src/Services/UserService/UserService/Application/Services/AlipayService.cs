using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Options;
using UserService.Infrastructure.Configuration;

namespace UserService.Application.Services;

/// <summary>
/// 支付宝服务接口
/// </summary>
public interface IAlipayService
{
    /// <summary>
    /// 创建 App 支付订单字符串
    /// </summary>
    string CreateAppPayOrderString(string outTradeNo, decimal amount, string subject, string body = "");
    
    /// <summary>
    /// 验证异步通知签名
    /// </summary>
    bool VerifyNotify(IDictionary<string, string> parameters);
}

/// <summary>
/// 支付宝服务实现 - 使用 .NET 原生加密库
/// </summary>
public class AlipayService : IAlipayService
{
    private readonly AlipaySettings _settings;
    private readonly ILogger<AlipayService> _logger;
    private readonly RSA _privateKeyRsa;
    private readonly RSA? _publicKeyRsa;

    public AlipayService(IOptions<AlipaySettings> settings, ILogger<AlipayService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        
        // 解析私钥
        var privateKey = _settings.PrivateKey?.Trim().Replace("\n", "").Replace("\r", "").Replace(" ", "");
        var alipayPublicKey = _settings.AlipayPublicKey?.Trim().Replace("\n", "").Replace("\r", "").Replace(" ", "");
        
        _logger.LogInformation("🔑 支付宝配置 - AppId: {AppId}, Gateway: {Gateway}, PrivateKey长度: {PkLen}, PublicKey长度: {PubLen}",
            _settings.AppId, _settings.GatewayUrl, privateKey?.Length ?? 0, alipayPublicKey?.Length ?? 0);
        
        // 初始化私钥 RSA
        _privateKeyRsa = RSA.Create();
        try
        {
            var privateKeyBytes = Convert.FromBase64String(privateKey!);
            _privateKeyRsa.ImportPkcs8PrivateKey(privateKeyBytes, out _);
            _logger.LogInformation("✅ 私钥加载成功 (PKCS#8 格式)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 私钥加载失败，尝试 PKCS#1 格式");
            try
            {
                var privateKeyBytes = Convert.FromBase64String(privateKey!);
                _privateKeyRsa.ImportRSAPrivateKey(privateKeyBytes, out _);
                _logger.LogInformation("✅ 私钥加载成功 (PKCS#1 格式)");
            }
            catch (Exception ex2)
            {
                _logger.LogError(ex2, "❌ 私钥加载失败");
                throw new InvalidOperationException("无法加载支付宝私钥，请检查格式");
            }
        }
        
        // 初始化公钥 RSA（用于验签）
        if (!string.IsNullOrEmpty(alipayPublicKey))
        {
            try
            {
                _publicKeyRsa = RSA.Create();
                var publicKeyBytes = Convert.FromBase64String(alipayPublicKey);
                _publicKeyRsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);
                _logger.LogInformation("✅ 公钥加载成功");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ 公钥加载失败，验签功能将不可用");
            }
        }
    }

    /// <summary>
    /// 创建 App 支付订单字符串
    /// </summary>
    public string CreateAppPayOrderString(string outTradeNo, decimal amount, string subject, string body = "")
    {
        _logger.LogInformation("📝 创建支付宝 App 支付订单: OutTradeNo={OutTradeNo}, Amount={Amount}", 
            outTradeNo, amount);

        // 构建业务参数
        var bizContent = new Dictionary<string, object>
        {
            ["out_trade_no"] = outTradeNo,
            ["total_amount"] = amount.ToString("F2"),
            ["subject"] = subject,
            ["product_code"] = "QUICK_MSECURITY_PAY",
            ["timeout_express"] = "30m"
        };
        
        if (!string.IsNullOrEmpty(body))
        {
            bizContent["body"] = body;
        }

        // 构建请求参数
        var parameters = new SortedDictionary<string, string>
        {
            ["app_id"] = _settings.AppId,
            ["method"] = "alipay.trade.app.pay",
            ["format"] = "JSON",
            ["charset"] = "utf-8",
            ["sign_type"] = "RSA2",
            ["timestamp"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            ["version"] = "1.0",
            ["notify_url"] = _settings.NotifyUrl,
            ["biz_content"] = JsonSerializer.Serialize(bizContent)
        };

        // 生成签名
        var signContent = BuildSignContent(parameters);
        var sign = SignWithRsa2(signContent);
        parameters["sign"] = sign;

        // 构建最终的订单字符串
        var orderString = BuildOrderString(parameters);
        
        _logger.LogInformation("✅ 支付宝订单字符串生成成功");
        
        return orderString;
    }

    /// <summary>
    /// 验证异步通知签名
    /// </summary>
    public bool VerifyNotify(IDictionary<string, string> parameters)
    {
        if (_publicKeyRsa == null)
        {
            _logger.LogError("❌ 公钥未初始化，无法验签");
            return false;
        }

        try
        {
            if (!parameters.TryGetValue("sign", out var sign) || string.IsNullOrEmpty(sign))
            {
                _logger.LogError("❌ 通知中缺少签名");
                return false;
            }

            // 移除 sign 和 sign_type 后重新构建待签名字符串
            var sortedParams = new SortedDictionary<string, string>();
            foreach (var kvp in parameters)
            {
                if (kvp.Key != "sign" && kvp.Key != "sign_type" && !string.IsNullOrEmpty(kvp.Value))
                {
                    sortedParams[kvp.Key] = kvp.Value;
                }
            }

            var signContent = BuildSignContent(sortedParams);
            var signBytes = Convert.FromBase64String(sign);
            var dataBytes = Encoding.UTF8.GetBytes(signContent);

            var isValid = _publicKeyRsa.VerifyData(dataBytes, signBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            
            if (isValid)
            {
                _logger.LogInformation("✅ 支付宝通知签名验证成功");
            }
            else
            {
                _logger.LogWarning("⚠️ 支付宝通知签名验证失败");
            }
            
            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 支付宝签名验证异常");
            return false;
        }
    }

    /// <summary>
    /// 构建待签名字符串
    /// </summary>
    private static string BuildSignContent(SortedDictionary<string, string> parameters)
    {
        var sb = new StringBuilder();
        foreach (var kvp in parameters)
        {
            if (!string.IsNullOrEmpty(kvp.Value))
            {
                if (sb.Length > 0)
                {
                    sb.Append('&');
                }
                sb.Append(kvp.Key).Append('=').Append(kvp.Value);
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// RSA2 签名
    /// </summary>
    private string SignWithRsa2(string content)
    {
        var dataBytes = Encoding.UTF8.GetBytes(content);
        var signatureBytes = _privateKeyRsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return Convert.ToBase64String(signatureBytes);
    }

    /// <summary>
    /// 构建订单字符串（使用支付宝要求的编码方式）
    /// </summary>
    private static string BuildOrderString(SortedDictionary<string, string> parameters)
    {
        var sb = new StringBuilder();
        foreach (var kvp in parameters)
        {
            if (!string.IsNullOrEmpty(kvp.Value))
            {
                if (sb.Length > 0)
                {
                    sb.Append('&');
                }
                // 使用支付宝要求的编码方式：只对特殊字符进行编码
                sb.Append(kvp.Key).Append('=').Append(UrlEncodeForAlipay(kvp.Value));
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// 支付宝专用 URL 编码
    /// 根据支付宝文档，使用 RFC 3986 标准编码
    /// </summary>
    private static string UrlEncodeForAlipay(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        
        var sb = new StringBuilder();
        foreach (char c in value)
        {
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') 
                || c == '-' || c == '_' || c == '.' || c == '~')
            {
                sb.Append(c);
            }
            else
            {
                foreach (byte b in Encoding.UTF8.GetBytes(c.ToString()))
                {
                    sb.Append('%').Append(b.ToString("X2"));
                }
            }
        }
        return sb.ToString();
    }
}
