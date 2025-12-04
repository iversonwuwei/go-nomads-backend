using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using UserService.Infrastructure.Configuration;

namespace UserService.Application.Services;

/// <summary>
///     PayPal 服务实现
/// </summary>
public class PayPalService : IPayPalService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PayPalService> _logger;
    private readonly PayPalSettings _settings;
    private string? _accessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public PayPalService(
        HttpClient httpClient,
        IOptions<PayPalSettings> settings,
        ILogger<PayPalService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<PayPalOrderResponse> CreateOrderAsync(
        decimal amount,
        string currency,
        string description,
        string referenceId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📤 创建 PayPal 订单: Amount={Amount} {Currency}", amount, currency);

        try
        {
            await EnsureAccessTokenAsync(cancellationToken);

            var requestBody = new
            {
                intent = "CAPTURE",
                purchase_units = new[]
                {
                    new
                    {
                        reference_id = referenceId,
                        description = description,
                        amount = new
                        {
                            currency_code = currency,
                            value = amount.ToString("F2")
                        }
                    }
                },
                application_context = new
                {
                    brand_name = "Go Nomads",
                    landing_page = "LOGIN",
                    user_action = "PAY_NOW",
                    return_url = _settings.ReturnUrl,
                    cancel_url = _settings.CancelUrl
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl}/v2/checkout/orders")
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogDebug("PayPal 创建订单响应: {Response}", responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("❌ PayPal 创建订单失败: {StatusCode} - {Response}",
                    response.StatusCode, responseContent);
                throw new Exception($"PayPal 创建订单失败: {response.StatusCode}");
            }

            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;

            var orderId = root.GetProperty("id").GetString() ?? "";
            var status = root.GetProperty("status").GetString() ?? "";

            // 获取 approval URL
            string? approvalUrl = null;
            if (root.TryGetProperty("links", out var links))
            {
                foreach (var link in links.EnumerateArray())
                {
                    if (link.GetProperty("rel").GetString() == "approve")
                    {
                        approvalUrl = link.GetProperty("href").GetString();
                        break;
                    }
                }
            }

            _logger.LogInformation("✅ PayPal 订单创建成功: OrderId={OrderId}, ApprovalUrl={ApprovalUrl}",
                orderId, approvalUrl);

            return new PayPalOrderResponse
            {
                OrderId = orderId,
                Status = status,
                ApprovalUrl = approvalUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 创建 PayPal 订单异常");
            throw;
        }
    }

    public async Task<PayPalCaptureResponse> CapturePaymentAsync(
        string paypalOrderId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📤 确认 PayPal 支付: OrderId={OrderId}", paypalOrderId);

        try
        {
            await EnsureAccessTokenAsync(cancellationToken);

            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{_settings.BaseUrl}/v2/checkout/orders/{paypalOrderId}/capture")
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogDebug("PayPal Capture 响应: {Response}", responseContent);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("❌ PayPal 确认支付失败: {StatusCode} - {Response}",
                    response.StatusCode, responseContent);

                return new PayPalCaptureResponse
                {
                    Success = false,
                    ErrorCode = response.StatusCode.ToString(),
                    ErrorMessage = $"PayPal 确认支付失败: {response.StatusCode}",
                    RawResponse = responseContent
                };
            }

            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;

            var status = root.GetProperty("status").GetString() ?? "";

            // 获取 capture 信息
            string? captureId = null;
            string? transactionId = null;
            if (root.TryGetProperty("purchase_units", out var purchaseUnits))
            {
                var firstUnit = purchaseUnits[0];
                if (firstUnit.TryGetProperty("payments", out var payments) &&
                    payments.TryGetProperty("captures", out var captures) &&
                    captures.GetArrayLength() > 0)
                {
                    var capture = captures[0];
                    captureId = capture.GetProperty("id").GetString();
                    transactionId = captureId; // PayPal capture ID 就是交易 ID
                }
            }

            // 获取 payer 信息
            string? payerId = null;
            string? payerEmail = null;
            if (root.TryGetProperty("payer", out var payer))
            {
                payerId = payer.GetProperty("payer_id").GetString();
                if (payer.TryGetProperty("email_address", out var email))
                {
                    payerEmail = email.GetString();
                }
            }

            var success = status == "COMPLETED";

            _logger.LogInformation("✅ PayPal 支付确认完成: Status={Status}, CaptureId={CaptureId}",
                status, captureId);

            return new PayPalCaptureResponse
            {
                Success = success,
                CaptureId = captureId,
                TransactionId = transactionId,
                PayerId = payerId,
                PayerEmail = payerEmail,
                Status = status,
                RawResponse = responseContent
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 确认 PayPal 支付异常");
            return new PayPalCaptureResponse
            {
                Success = false,
                ErrorCode = "EXCEPTION",
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<PayPalOrderResponse?> GetOrderDetailsAsync(
        string paypalOrderId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureAccessTokenAsync(cancellationToken);

            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{_settings.BaseUrl}/v2/checkout/orders/{paypalOrderId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("⚠️ 获取 PayPal 订单详情失败: {StatusCode}", response.StatusCode);
                return null;
            }

            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;

            return new PayPalOrderResponse
            {
                OrderId = root.GetProperty("id").GetString() ?? "",
                Status = root.GetProperty("status").GetString() ?? ""
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取 PayPal 订单详情异常");
            return null;
        }
    }

    public async Task<bool> VerifyWebhookSignatureAsync(
        string webhookId,
        string transmissionId,
        string transmissionTime,
        string certUrl,
        string authAlgo,
        string transmissionSig,
        string webhookEventBody,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureAccessTokenAsync(cancellationToken);

            var requestBody = new
            {
                auth_algo = authAlgo,
                cert_url = certUrl,
                transmission_id = transmissionId,
                transmission_sig = transmissionSig,
                transmission_time = transmissionTime,
                webhook_id = webhookId,
                webhook_event = JsonSerializer.Deserialize<object>(webhookEventBody)
            };

            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{_settings.BaseUrl}/v1/notifications/verify-webhook-signature")
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("⚠️ Webhook 签名验证失败: {StatusCode}", response.StatusCode);
                return false;
            }

            using var doc = JsonDocument.Parse(responseContent);
            var verificationStatus = doc.RootElement.GetProperty("verification_status").GetString();

            return verificationStatus == "SUCCESS";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Webhook 签名验证异常");
            return false;
        }
    }

    #region 私有方法

    private async Task EnsureAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry)
        {
            return;
        }

        _logger.LogInformation("🔑 获取 PayPal Access Token");

        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_settings.ClientId}:{_settings.ClientSecret}"));

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl}/v1/oauth2/token")
        {
            Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("❌ 获取 PayPal Access Token 失败: {Response}", responseContent);
            throw new Exception("获取 PayPal Access Token 失败");
        }

        using var doc = JsonDocument.Parse(responseContent);
        _accessToken = doc.RootElement.GetProperty("access_token").GetString();
        var expiresIn = doc.RootElement.GetProperty("expires_in").GetInt32();
        _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 60); // 提前 60 秒过期

        _logger.LogInformation("✅ PayPal Access Token 获取成功");
    }

    #endregion
}
