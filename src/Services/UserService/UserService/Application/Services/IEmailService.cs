namespace UserService.Application.Services;

/// <summary>
///     邮件发送服务接口
/// </summary>
public interface IEmailService
{
    /// <summary>
    ///     发送验证码邮件
    /// </summary>
    /// <param name="toEmail">收件人邮箱</param>
    /// <param name="code">验证码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>发送结果</returns>
    Task<EmailResult> SendVerificationCodeAsync(
        string toEmail,
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
///     邮件发送结果
/// </summary>
public class EmailResult
{
    /// <summary>
    ///     是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    ///     消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    public static EmailResult Ok(string message = "发送成功")
    {
        return new EmailResult { Success = true, Message = message };
    }

    public static EmailResult Fail(string message)
    {
        return new EmailResult { Success = false, Message = message };
    }
}
