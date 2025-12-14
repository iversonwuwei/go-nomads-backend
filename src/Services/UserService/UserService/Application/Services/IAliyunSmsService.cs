namespace UserService.Application.Services;

/// <summary>
///     阿里云短信服务接口
/// </summary>
public interface IAliyunSmsService
{
    /// <summary>
    ///     发送验证码短信
    /// </summary>
    /// <param name="phoneNumber">手机号（包含国际区号，如 +86）</param>
    /// <param name="code">验证码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>发送结果</returns>
    Task<SmsResult> SendVerificationCodeAsync(
        string phoneNumber,
        string code,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     生成验证码
    /// </summary>
    /// <param name="length">验证码长度</param>
    /// <returns>验证码</returns>
    string GenerateVerificationCode(int length = 6);
}

/// <summary>
///     短信发送结果
/// </summary>
public class SmsResult
{
    /// <summary>
    ///     是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    ///     消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    ///     请求 ID（阿里云返回）
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    ///     业务 ID（阿里云返回）
    /// </summary>
    public string? BizId { get; set; }

    /// <summary>
    ///     错误码
    /// </summary>
    public string? Code { get; set; }

    public static SmsResult Ok(string message = "发送成功", string? requestId = null, string? bizId = null)
    {
        return new SmsResult
        {
            Success = true,
            Message = message,
            RequestId = requestId,
            BizId = bizId,
            Code = "OK"
        };
    }

    public static SmsResult Fail(string message, string? code = null, string? requestId = null)
    {
        return new SmsResult
        {
            Success = false,
            Message = message,
            Code = code,
            RequestId = requestId
        };
    }
}
